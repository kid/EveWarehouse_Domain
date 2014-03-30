module EveWarehouse.Data

open System
open FSharp.Data
open Microsoft.FSharp.Data.TypeProviders
open EveWarehouse.Common
open EveWarehouse.DomainTypes

type internal EveWarehouse = SqlDataConnection<ConnectionStringName="EveWarehouse", ForceUpdate=true>


module InventoryManager =
    
    [<Literal>]
    let takeInventoryItemStatement = """
    INSERT INTO [Live].[InventoryEntries] ([Date], [Price], [Movement], [BatchId], [StationId])
    VALUES (@Date, @Price, @Movement, @BatchId, @StationId)
    """

    type private AvailableStockForItemQuery = SqlCommandProvider<"../EveWarehouse.Database/Queries/FifoInventory.sql", "name=EveWarehouse">
    type private TakeInventoryItemComand = SqlCommandProvider<takeInventoryItemStatement, "name=EveWarehouse", AllParametersOptional=true>

    let _store (entry: InventoryEntry) =
        let stationId = 
            match entry.StationId with
            | StationId id -> id

        let batchId = 
            match entry.Destination with
            | Some (Batch (BatchId id)) -> Some id
            | None -> None

        TakeInventoryItemComand().AsyncExecute(
            Date = Some entry.Date,
            Price = Some entry.Price,
            Movement = Some entry.Quantity,
            StationId = Some stationId,
            BatchId = batchId
        )
        |> Async.RunSynchronously
        |> ignore

    /// <summary>
    /// Look at the inventory for the asked numbed of items, in the order the where bought.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="quantity"></param>
    let tryPeek (ItemId id) quantity =
        let rec takeNeeded needed (l:AvailableStockForItemQuery.Record list) =
            match l with
            | [] -> []
            | x :: xs when x.Remaining >= needed ->
                [{ ItemId = ItemId x.ItemId ; Quantity = needed ; Price = x.Price ; StationId = StationId x.StationId }]
            | x :: xs ->
                { ItemId = ItemId x.ItemId ; Quantity = x.Remaining ; Price = x.Price ; StationId = StationId x.StationId }
                :: takeNeeded (needed - x.Remaining) xs

        AvailableStockForItemQuery().AsyncExecute(id)
        |> Async.RunSynchronously
        |> Seq.toList
        |> takeNeeded quantity
        |> succeed

    let take id quantity date batchId =
        let toInventoryEntry item =
            { 
                InventoryEntry.Date = date ; 
                ItemId = item.ItemId ; 
                StationId = item.StationId ; 
                Price = item.Price ; 
                Quantity = item.Quantity * -1L ; 
                Source = None ; 
                Destination = Some (Batch batchId) 
            }

        match tryPeek id quantity with
        | Success items -> 
            items
            |> Seq.map toInventoryEntry
            |> Seq.iter _store

            Success items
        | Failure f -> Failure f


module BillOfMaterialsManager =
    
    [<Literal>]
    let private getBillOfMaterialsStatement = """
        SELECT [Id], [Description], [Duration], [OutputItemId], [OutputQuantity], [InputItemId], [InputQuantity]
        FROM [Live].[BillOfMaterials]
        LEFT JOIN [Live].[BillOfMaterialsInput] ON [Id] = [BillOfMaterialsId]
        WHERE [Id] = @Id"""

    type private GetBillOfMaterialsQuery = SqlCommandProvider<getBillOfMaterialsStatement, "name=EveWarehouse">

    let private map (rows: GetBillOfMaterialsQuery.Record list) =
        let rec mapInput (rows: GetBillOfMaterialsQuery.Record list) =
            match rows with
            | [] -> []
            | x :: xs ->
                match (x.InputItemId, x.InputQuantity) with
                | (Some id, Some quantity) -> 
                    { ItemQuantity.ItemId = (ItemId id) ; Quantity = quantity } :: mapInput xs
                | _ -> 
                    mapInput xs

        match rows with
        | [] -> 
            None
        | x :: xs ->
            { Id = Some (BillOfMaterialsId x.Id);
              Description = x.Description ;
              Duration = TimeSpan.FromSeconds(float x.Duration) ;
              Output = { ItemId = (ItemId x.OutputItemId) ; Quantity = x.OutputQuantity ; }
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

module ItemManager =
    
    type private FindByIdsCommand = SqlCommandProvider<"exec [Live].[FindItemsByIds] @Ids", "name=EveWarehouse">
        
    let findByIds ids =
        ids 
        |> Seq.map (fun (ItemId id) -> FindByIdsCommand.LiveItem(Id = id))
        |> FindByIdsCommand().AsyncExecute
        |> Async.RunSynchronously
        |> Seq.map (fun x -> ItemId x.Id, { Item.Id = ItemId x.Id ; Name = x.Name ; Volume = x.Volume })
        |> Map.ofSeq

module BatchManager =
    
    let private peekFromStocks cycles bom =
        bom.Input
        |> Seq.map (fun x -> InventoryManager.tryPeek x.ItemId (x.Quantity * cycles))
        |> Seq.fold (append (fun a b -> List.append a b) (fun a b -> List.append a [b])) (Success [])
        

    /// <summary>
    /// Calculates the batch cost given the materials already in stock
    /// </summary>
    /// <param name="id">The BillOfMateraisl Id.</param>
    /// <param name="cycles">The number of cycles to run.</param>
    let getQuote id cycles stationId = 
        
        let materialsCost input = 
            input |> Seq.sumBy (fun x -> x.Price * decimal x.Quantity)

        let materialsTransportCost input =
            let items =  
                input 
                |> Seq.map (fun x -> x.ItemId) 
                |> Seq.distinct 
                |> ItemManager.findByIds

            input |> Seq.sumBy (fun x -> if x.StationId = stationId then 0m else decimal x.Quantity * (Map.find x.ItemId items).Volume * 250m)

        let materiaslAndTransport =
            plus (+) (List.append) (switch materialsCost) (switch materialsTransportCost)

        let outputCost bom =
            decimal bom.Output.Quantity * 250m
        
        let inputCost bom = 
            peekFromStocks cycles bom
            >>= materiaslAndTransport

        BillOfMaterialsManager.getBillOfMaterials id
        |> someOrFail ["Bill of materials not found"]
        |> plus (+) (List.append) (bind inputCost) (map outputCost)
        
    /// <summary>
    /// Submit a batch.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="cycles"></param>
    /// <param name="systemId"></param>
    let submit id cycles stationId =
        
        let bom = 
            BillOfMaterialsManager.getBillOfMaterials id
            |> someOrFail ["Bill of materials not found"]

        match bom with
        | Success x -> 
//            x.Input
//            |> Seq.map (fun x -> InventoryManager.tryPeek x.ItemId (x.Quantity * cycles))
//            |> Seq.fold (append (fun a b -> List.append a b) (fun a b -> List.append a [b])) (Success [])

            match peekFromStocks cycles x with
            | Success input -> Success input
            | Failure f -> Failure f
        | Failure f -> Failure f