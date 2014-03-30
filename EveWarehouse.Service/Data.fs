module EveWarehouse.Data

open System
open FSharp.Data
open Microsoft.FSharp.Data.TypeProviders
open EveWarehouse.Common
open EveWarehouse.DomainTypes

type internal EveWarehouse = SqlDataConnection<ConnectionStringName="EveWarehouse", ForceUpdate=true>

type internal EveWarehouse2 = SqlProgrammabilityProvider<"name=EveWarehouse">

module InventoryManager =
    
    type private AvailableStockForItemQuery = SqlCommandProvider<"../EveWarehouse.Database/Queries/FifoInventory.sql", "name=EveWarehouse">

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
                [{ ItemId = ItemId x.ItemId ; Quantity = needed ; Price = x.Price ; StationId = StationId x.SourceStationId }]
            | x :: xs ->
                { ItemId = ItemId x.ItemId ; Quantity = x.Remaining ; Price = x.Price ; StationId = StationId x.SourceStationId }
                :: takeNeeded (needed - x.Remaining) xs

        AvailableStockForItemQuery().AsyncExecute(id)
        |> Async.RunSynchronously
        |> Seq.toList
        |> takeNeeded quantity
        |> succeed

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
    
    type private FindByIdsCommand = SqlCommandProvider<"exec [Live].[FindItemByIds] @Ids", "name=EveWarehouse">
        
    let findByIds ids =
        ids 
        |> Seq.map (fun (ItemId id) -> FindByIdsCommand.Item(Id = id))
        |> FindByIdsCommand().AsyncExecute
        |> Async.RunSynchronously
        |> Seq.map (fun x -> ItemId x.Id, { Item.Id = ItemId x.Id ; Name = x.Name ; Volume = x.Volume })
        |> Map.ofSeq

module BatchManager =
    
    /// <summary>
    /// Calculates the batch cost given the materials already in stock
    /// </summary>
    /// <param name="id">The BillOfMateraisl Id.</param>
    /// <param name="cycles">The number of cycles to run.</param>
    let getQuote id cycles = 
        
        let peekFromStocks bom =
            bom.Input
            |> Seq.map (fun x -> InventoryManager.tryPeek x.ItemId (x.Quantity * cycles))
            |> Seq.fold (append (fun a b -> List.append a b) (fun a b -> List.append a [b])) (Success [])
            
        let materialsCost input = 
            input |> Seq.sumBy (fun x -> x.Price * decimal x.Quantity)

        let transportCost input =
            let items =  
                input 
                |> Seq.map (fun x -> x.ItemId) 
                |> Seq.distinct 
                |> ItemManager.findByIds

            input |> Seq.sumBy (fun x -> decimal x.Quantity * (Map.find x.ItemId items).Volume)

        let materiaslAndTransport =
            plus (+) (List.append) (switch materialsCost) (switch transportCost)
            
        BillOfMaterialsManager.getBillOfMaterials id
        |> someOrFail ["Bill of material not found"]
        >>= peekFromStocks
        >>= materiaslAndTransport
        