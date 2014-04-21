module EveWarehouse.Domain.Repositories

open System
open System.Transactions
open FSharp.Data
open EveWarehouse.Common
open EveWarehouse.Domain

type EveWarehouse = SqlProgrammabilityProvider<"name=EveWarehouse">
let EveWarehouseDb = EveWarehouse()

let createTransactionScope = 
    let mutable options = new TransactionOptions()

    options.IsolationLevel <- IsolationLevel.ReadCommitted
    options.Timeout <- TransactionManager.MaximumTimeout

    new TransactionScope(TransactionScopeOption.Required, options)

module ItemRepository =
    
    [<Literal>]
    let private getStatement = "SELECT * FROM [Live].[Item] WHERE [Id] = @Id"

    type private GetCommand = SqlCommandProvider<getStatement, "name=EveWarehouse", SingleRow = true>

    let get (ItemId id) =
        let item = GetCommand().AsyncExecute(id) |> Async.RunSynchronously |> Option.get
        { Item.Id = (ItemId item.Id) ; Name = item.Name ; Volume = item.Volume }

module BatchRepository =

    [<Literal>]
    let private insertStatement = 
        """
        INSERT INTO [Live].[Batch] ([BillOfMaterialsId], [Cycles], [StartDate])
        VALUES (@BillOfMaterialsId, @Cycles, @StartDate)
        SELECT SCOPE_IDENTITY()
        """

    type private InsertCommand = SqlCommandProvider<insertStatement, "name=EveWarehouse", AllParametersOptional = true, SingleRow = true>

    let save batch =
        match batch with
        | ReactionBatch b ->
            let getBomId = function
                | { BillOfMaterialsId = (BillOfMaterialsId id) } -> Some id

            let id = 
                InsertCommand().AsyncExecute(
                    getBomId b,
                    Some b.Cycles,
                    Some b.StartDate
                )
                |> Async.RunSynchronously
                |> Option.get |> Option.get |> int |> BatchId

            ReactionBatch { b with Id = Some id }


module BillOfMaterialsRepository =
    
    [<Literal>]
    let private getBillOfMaterialsStatement = 
        """
        SELECT 
        	b.[Id], 
        	b.[Description], 
        	b.[Duration], 
        	o.[Id] as [OutputItemId], 
        	o.[Name] as [OutputItemName], 
        	o.[Volume] as [OutputItemVolume], 
        	[OutputQuantity], 
        	i.[Id] as [InputItemId], 
        	i.[Name] as [InputItemName],
        	i.[Volume] as [InputItemVolume],
        	[InputQuantity]
        FROM [Live].[BillOfMaterials] b
        INNER JOIN [Live].[BillOfMaterialsInput] ON [Id] = [BillOfMaterialsId]
        INNER JOIN [Live].[Item] i ON i.[Id] = [InputItemId]
        INNER JOIN [Live].[Item] o ON o.[Id] = [OutputItemId]
        WHERE b.[Id] = @Id
        """

    type private GetBillOfMaterialsQuery = SqlCommandProvider<getBillOfMaterialsStatement, "name=EveWarehouse">

    let private map (rows: GetBillOfMaterialsQuery.Record list) =
        let rec mapInput (rows: GetBillOfMaterialsQuery.Record list) =
            match rows with
            | [] -> []
            | x :: xs ->
                ({ Id = ItemId x.InputItemId ; Name = x.InputItemName ; Volume = x.InputItemVolume }, x.InputQuantity) :: mapInput xs

        match rows with
        | [] -> None
        | x :: xs ->
            { Id = Some (BillOfMaterialsId x.Id);
              Description = x.Description ;
              Duration = TimeSpan.FromSeconds(float x.Duration) ;
              Output = { Id = ItemId x.OutputItemId ; Name = x.OutputItemName ; Volume = x.OutputItemVolume }, x.OutputQuantity
              Input = mapInput rows }
            |> Some
        
    /// <summary>
    /// Get the BOM's definition
    /// </summary>
    /// <param name="id"></param>
    let getBillOfMaterials (BillOfMaterialsId id) = 
        GetBillOfMaterialsQuery().AsyncExecute(id)
        |> Async.RunSynchronously
        |> Seq.toList
        |> map

module InventoryLineRepository = 

    [<Literal>]
    let private insertStatement = 
        """
        INSERT INTO [Live].[InventoryEntries] ([ItemId], [Date], [Price], [ShippingCost], [Movement], [SourceLineId], [BatchId], [WalletId], [TransactionId], [StationId])
        VALUES (@ItemId, @Date, @Price, @ShippingCost, @Movement, @SourceLineId, @BatchId, @WalletId, @TransactionId, @StationId)
        SELECT SCOPE_IDENTITY()
        """

    type private InsertCommand = SqlCommandProvider<insertStatement, "name=EveWarehouse", AllParametersOptional = true, SingleRow = true>

    /// <summary>
    /// Save an inventory line, returning a new line item with the id filled.
    /// </summary>
    /// <param name="line">The inventory line to save.</param>
    let save line =
        let getItemId = function
            | ItemId id -> Some id

        let getSourceLineId = function
            | Some (InventoryLineId id) -> Some id
            | None -> None

        let getStationId = function
            | LocationId.StationId (StationId id) -> Some id
            | LocationId.SolarSystemId _ -> None
        
        let itemId, stationId, date, quantity, price, shippingCost, sourceLineId =
            match line with
            | InventoryInput input -> getItemId input.ItemId, getStationId input.LocationId, input.Date, input.Quantity, input.Price, input.ShippingCost, getSourceLineId input.SourceLineId
            | InventoryOutput output -> getItemId output.ItemId, getStationId output.LocationId, output.Date, output.Quantity * -1L, output.Price, output.ShippingCost, getSourceLineId output.SourceLineId
            
        let batchId, walletId, transactionId = 
            match line with
            | InventoryInput input ->
                match input.Source with
                | InventorySource.Batch (BatchId id) -> Some id, None, None
                | InventorySource.Transaction (WalletId w, TransactionId t) -> None, Some w, Some t
                | InventorySource.UserEntry -> None, None, None
            | InventoryOutput output ->
                match output.Destination with
                | InventoryDestination.Batch (BatchId id) -> Some id, None, None
        
        let id = 
            InsertCommand().AsyncExecute(itemId, Some date, Some price, Some shippingCost, Some quantity, sourceLineId, batchId, walletId, transactionId, stationId)
            |> Async.RunSynchronously
            |> Option.get |> Option.get |> int64 |> InventoryLineId
            
        match line with
        | InventoryInput input -> InventoryInput { input with Id = Some id }
        | InventoryOutput output -> InventoryOutput { output with Id = Some id }

    let move (line: InventoryInput) date (destination: LocationId) shippingCost =
        match line.Id, destination with
        | Some (InventoryLineId id), LocationId.StationId (StationId stationId) -> 
            EveWarehouseDb.``Stored Procedures``.``Live.MoveInventoryLine``.AsyncExecute(
                lineId = id, 
                quantity = line.Quantity,
                date = date, 
                destinationId = stationId, 
                shippingCost = shippingCost)
            |> Async.RunSynchronously
            |> Seq.toList
            |> (function
                | [x] -> 
                    let source =
                        match x.WalletId, x.TransactionId, x.BatchId with
                        | Some w, Some t, None -> InventorySource.Transaction ((WalletId w), (TransactionId t))
                        | None, None, Some b -> InventorySource.Batch (BatchId b)
                        | _, _, _ -> InventorySource.UserEntry

                    Success { Id = Some (InventoryLineId x.Id); SourceLineId = Some (InventoryLineId x.SourceLineId.Value) ; ItemId = ItemId x.ItemId ; Date = x.Date ; Quantity = x.Movement ; Price = x.Price ; ShippingCost = x.ShippingCost ; LocationId = LocationId.StationId (StationId x.StationId) ; Source = source }
                | x :: xs -> Failure "StoredProc returned more than one row"
                | [] -> Failure "StoredProc returned 0 rows"
            )
        | Some _, LocationId.SolarSystemId _ -> Failure "Can't move line to a solar system"
        | None, _ -> Failure "Can't move unsaved line"
        