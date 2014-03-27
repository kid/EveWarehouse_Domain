module Data

open FSharp.Data
open Microsoft.FSharp.Data.TypeProviders
open EveWarehouse.DomainTypes

[<Literal>]
let connectionString = "Server=(local);Initial Catalog=EveWarehouse.Database;Integrated Security=SSPI"

type internal EveWarehouse = SqlDataConnection<ConnectionString=connectionString, ForceUpdate=true>

module InventoryRepository =
    
    type AvailableStockForItemQuery = SqlCommandProvider<"../EveWarehouse.Database/Queries/FifoInventory.sql", connectionString>

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
