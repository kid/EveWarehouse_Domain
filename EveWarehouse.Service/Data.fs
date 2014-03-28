module Data

open System
open FSharp.Data
open Microsoft.FSharp.Data.TypeProviders
open EveWarehouse.DomainTypes

type internal EveWarehouse = SqlDataConnection<ConnectionStringName="EveWarehouse", ForceUpdate=true>

module InventoryManager =
    
    type private AvailableStockForItemQuery = SqlCommandProvider<"../EveWarehouse.Database/Queries/FifoInventory.sql", "name=EveWarehouse">

    let tryPeek (ItemId id) quantity =
        let rec takeNeeded needed (l:AvailableStockForItemQuery.Record list) =
            match l with
            | [] -> []
            | x::xs when x.Remaining >= needed ->
                [{ ItemQuantityPrice.ItemId = ItemId x.ItemId ; Quantity = needed ; Price = x.Price }]
            | x::xs ->
                { ItemQuantityPrice.ItemId = ItemId x.ItemId ; Quantity = x.Remaining ; Price = x.Price } :: takeNeeded (needed - x.Remaining) xs

        AvailableStockForItemQuery().Execute(id)
        |> Seq.toList
        |> takeNeeded quantity

module BillOfMaterialsManager =
    
    [<Literal>]
    let private getBillOfMaterialsStatement = """
        SELECT [Id], [Description], [Duration], [OutputItemId], [OutputQuantity], [InputItemId], [InputQuantity]
        FROM [Live].[BillOfMaterials]
        LEFT JOIN [Live].[BillOfMaterialsInput] ON [Id] = [BillOfMaterialsId]
        WHERE [Id] = @Id
    """

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
        

    let getBillOfMaterials (BillOfMaterialsId id) = 
        GetBillOfMaterialsQuery().AsyncExecute(id)
        |> Async.RunSynchronously
        |> Seq.toList
        |> map