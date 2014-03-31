﻿namespace EveWarehouse.Domain.Services

open FSharp.Data

open EveWarehouse
open EveWarehouse.Domain
open EveWarehouse.Domain.Repositories

module ShippingService =

    /// <summary>
    /// Calculates the cost to ship the given volume betweeen two locations.
    /// </summary>
    /// <param name="sourceId">The source location id.</param>
    /// <param name="destinationId">The destination location id.</param>
    /// <param name="volume">The volume to move.</param>
    let quote (source: LocationId) (destination: LocationId) volume = 
        if source = destination then
            0m
        else
            volume * 250m

module InventoryService =

    type private AvailableStockForItemQuery = SqlCommandProvider<"../EveWarehouse.Database/Queries/FifoInventory.sql", "name=EveWarehouse">

    let private saveOutput (line: InventoryOutput) = 
        // TODO
        { line with Id = Some(InventoryLineId 1L) }

    /// <summary>
    /// Return the asked number of items from the stock, in the order they were added.
    /// </summary>
    /// <param name="id">The item id.</param>
    /// <param name="quantity">The asked quantity.</param>
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

    /// <summary>
    /// Take the asked number of items, in the order they were added, and remove them from the stock.
    /// </summary>
    /// <param name="id">The item id.</param>
    /// <param name="quantity">The asked quantity.</param>
    /// <param name="destination">The stock destination.</param>
    let take id quantity (destination : InventoryDestination) =
        match peek id quantity with
        | Success input ->
            input
            |> Seq.map (fun x -> { Id = None ; ItemId = x.ItemId ; Date = x.Date ; Quantity = x.Quantity ; Price = x.Price ; Destination = destination })
            |> Seq.map saveOutput
            |> succeed
        | Failure f -> Failure f

module BatchService = 

    let private peekFromStocks cycles bom =
        bom.Input
        |> Seq.map (fun (item, quantity) -> 
            match InventoryService.peek item.Id (quantity * cycles) with
            | Success lines -> Success (item, quantity * cycles, lines)
            | Failure m -> Failure m)
        |> Seq.fold (append (fun a b -> List.append a [b]) (fun a b -> List.append a [b])) (Success [])
        
    /// <summary>
    /// Calculates the batch cost given the materials already in stock
    /// </summary>
    /// <param name="id">The BillOfMateraisl Id.</param>
    /// <param name="cycles">The number of cycles to run.</param>
    let quote id cycles locationId destinationId = 
        
        let materialsCost (inputLines: (Item * int64 * InventoryInput list) seq) = 
            inputLines
            |> Seq.map (fun (item, quantity, lines) -> lines |> Seq.sumBy (fun l -> l.Price * decimal quantity))
            |> Seq.sum

        let materialsTransportCost (inputLines: (Item * int64 * InventoryInput list) seq) =
            inputLines
            |> Seq.map (fun (item, quantity, lines) -> 
                lines |> Seq.sumBy (fun l -> ShippingService.quote (Station l.StationId) locationId (decimal l.Quantity * item.Volume)))
            |> Seq.sum

        let materiaslAndTransport =
            plus (+) (List.append) (switch materialsCost) (switch materialsTransportCost)

        let inputCost bom = 
            peekFromStocks cycles bom
            >>= materiaslAndTransport

        let outputCost bom =
            let item, quantity = bom.Output
            ShippingService.quote locationId destinationId (decimal quantity * item.Volume)
        
        BillOfMaterialsRepository.getBillOfMaterials id
        |> someOrFail ["Bill of materials not found"]
        |> plus (+) (List.append) (bind inputCost) (map outputCost)
    