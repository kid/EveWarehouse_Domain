module Data

open FSharp.Data
open Microsoft.FSharp.Data.TypeProviders
open EveWarehouse.DomainTypes

[<Literal>]
let connectionString = "Server=(local);Initial Catalog=EveWarehouse.Database;Integrated Security=SSPI"

type internal EveWarehouse = SqlDataConnection<ConnectionString=connectionString, ForceUpdate=true>

module InventoryRepository =
    
    type AvailableStockForItemQuery = SqlCommandProvider<"../EveWarehouse.Database/Queries/FifoInventory.sql", connectionString>

    let take (ItemId id) quantity =
        let rec takeNeeded acc (entries: AvailableStockForItemQuery.Record list) =
            match entries with
            | [] -> acc
            | [x] -> [Some(x.Movement, x)]
            | x::y::xs -> Some() :: acc
            

        AvailableStockForItemQuery().Execute(id)
        |> Seq.toList
        |> takeNeeded None