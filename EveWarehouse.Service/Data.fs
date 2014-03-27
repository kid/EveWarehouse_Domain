module Data

open FSharp.Data
open Microsoft.FSharp.Data.TypeProviders
//open EveWarehouse.DomainTypes

[<Literal>]
let connectionString = "Server=(local);Initial Catalog=EveWarehouse.Database;Integrated Security=SSPI"

type internal EveWarehouse = SqlDataConnection<ConnectionString=connectionString, ForceUpdate=true>

module InventoryRepository =
    
    type AvailableStockForItemQuery = SqlCommandProvider<"../EveWarehouse.Database/Queries/FifoInventory.sql", connectionString>

    let take id quantity =
        let rec map (l:AvailableStockForItemQuery.Record list) =
            match l with
            | x::y::xs -> (x.Remaining, x) :: (x.Remaining + y.Remaining, y) :: map xs
            | x::xs -> (x.Remaining, x) :: map xs
            | [] -> []

//        let rec takeNeeded acc (entries: AvailableStockForItemQuery.Record list) =
//            match entries with
//            | [] -> acc
//            | [x] -> [Some(x.Movement, x)]
//            | x::y::xs -> Some() :: acc
            

        AvailableStockForItemQuery().Execute(id)
        |> Seq.toList
        |> map
        |> Seq.map (fun (c, x) -> printfn "%A" c |> ignore ; (c, x))
        //|> Seq.takeWhile (fun (count, _) -> count <= quantity)
        |> Seq.map (fun (_, x) -> x)
//        |> takeNeeded None