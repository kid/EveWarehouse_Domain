namespace EveWarehouse.Domain.Services

open FSharp.Data

open EveWarehouse
open EveWarehouse.Domain

module InventoryService =

    type private AvailableStockForItemQuery = SqlCommandProvider<"../EveWarehouse.Database/Queries/FifoInventory.sql", "name=EveWarehouse">

    let private saveOutput (line: InventoryOutput) = 
        { line with Id = Some(InventoryLineId 1L) }

    let peek (ItemId id) quantity =
        let rec takeNeeded needed (l:AvailableStockForItemQuery.Record list) =
            match l with
            | [] -> []
            | x :: xs when x.Remaining >= needed ->
                [{ Id = Some (InventoryLineId x.Id) ; ItemId = ItemId x.ItemId ; Date = x.Date ; Quantity = needed ; Price = x.Price ; StationId = StationId x.StationId ; Source = UserEntry }]
            | x :: xs ->
                { Id = Some (InventoryLineId x.Id) ; ItemId = ItemId x.ItemId ; Date = x.Date ; Quantity = x.Remaining ; Price = x.Price ; StationId = StationId x.StationId; Source = UserEntry }
                :: takeNeeded (needed - x.Remaining) xs

        AvailableStockForItemQuery().AsyncExecute(id)
        |> Async.RunSynchronously
        |> Seq.toList
        |> takeNeeded quantity
        |> succeed

    let take id quantity (destination : InventoryDestination) =
        match peek id quantity with
        | Success input ->
            input
            |> Seq.map (fun x -> { Id = None ; ItemId = x.ItemId ; Date = x.Date ; Quantity = x.Quantity ; Price = x.Price ; Destination = destination })
            |> Seq.map saveOutput
            |> succeed
        | Failure f -> Failure f
