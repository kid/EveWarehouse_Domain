namespace EveWarehouse.Domain.Services

open System
open System.Transactions
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

    let ship (destination: LocationId) date (line: InventoryInput) =
        if line.LocationId = destination then
            Success line
        else
            let item = ItemRepository.get line.ItemId
            let shippingCost = quote line.LocationId destination (decimal line.Quantity) * item.Volume

            InventoryLineRepository.move line date destination shippingCost
        

module InventoryService =

    type private AvailableStockForItemQuery = SqlCommandProvider<"../EveWarehouse.Database/Queries/FifoInventory.sql", "name=EveWarehouse">

    let private saveOutput (line: InventoryOutput) = 
        InventoryLineRepository.save (InventoryOutput line)
        

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
                let sourceLineId = 
                    match x.SourceLineId with
                    | Some id -> Some (InventoryLineId id)
                    | None -> None

                let source =
                    match x.WalletId, x.TransactionId, x.BatchId with
                    | Some w, Some t, None -> InventorySource.Transaction ((WalletId w), (TransactionId t))
                    | None, None, Some b -> InventorySource.Batch (BatchId b)
                    | _, _, _ -> InventorySource.UserEntry

                [{ Id = Some (InventoryLineId x.Id) ; ItemId = ItemId x.ItemId ; Date = x.Date ; Quantity = needed ; Price = x.Price ; ShippingCost = x.ShippingCost ; SourceLineId = sourceLineId ; LocationId = LocationId.StationId (StationId x.StationId) ; Source = source }]
            | x :: xs ->
                let sourceLineId = 
                    match x.SourceLineId with
                    | Some id -> Some (InventoryLineId id)
                    | None -> None

                let source =
                    match x.WalletId, x.TransactionId, x.BatchId with
                    | Some w, Some t, None -> InventorySource.Transaction ((WalletId w), (TransactionId t))
                    | None, None, Some b -> InventorySource.Batch (BatchId b)
                    | _, _, _ -> InventorySource.UserEntry

                { Id = Some (InventoryLineId x.Id) ; ItemId = ItemId x.ItemId ; Date = x.Date ; Quantity = x.Remaining ; Price = x.Price ; ShippingCost = x.ShippingCost ; SourceLineId = sourceLineId ; LocationId = LocationId.StationId (StationId x.StationId); Source = source }
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
    let take (destination : InventoryDestination) date (line : InventoryInput) =
        { Id = None ; ItemId = line.ItemId ; Date = date ; Quantity = line.Quantity ; Price = line.Price ; ShippingCost = line.ShippingCost ; SourceLineId = None ; Destination = destination ; LocationId = line.LocationId }
        |> saveOutput
        |> succeed

module BatchService = 

    let private plus addSuccess v1 v2 =
        let addFailure f1 f2 = List.append f1 f2
        Common.plus addSuccess addFailure v1 v2

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
    /// <param name="bomId">The BillOfMateraisl Id.</param>
    /// <param name="cycles">The number of cycles to run.</param>
    /// <param name="posLocation">The location where the reaction is run.</param>
    /// <param name="shipTo">The destination of the ouput.</param>
    let quote bomId cycles posLocation shipTo = 
        
        let materialsCost (inputLines: (Item * int64 * InventoryInput list) seq) = 
            inputLines
            |> Seq.map (fun (item, quantity, lines) -> lines |> Seq.sumBy (fun l -> l.Price * decimal l.Quantity))
            |> Seq.sum
        
        let materialsTransportCost (inputLines: (Item * int64 * InventoryInput list) seq) =
            inputLines
            |> Seq.map (fun (item, quantity, lines) -> 
                lines |> Seq.sumBy (fun l -> ShippingService.quote l.LocationId posLocation (decimal l.Quantity * item.Volume)))
            |> Seq.sum

        let materiaslAndTransport =
            plus (+) (switch materialsTransportCost) (switch materialsCost)

        let inputCost bom = 
            peekFromStocks cycles bom
            >>= materiaslAndTransport

        let outputCost bom =
            let item, quantity = bom.Output
            ShippingService.quote posLocation shipTo (decimal quantity * item.Volume)
        
        BillOfMaterialsRepository.getBillOfMaterials bomId
        |> someOrFail ["Bill of materials not found"]
        |> plus (+) (bind inputCost) (map outputCost)
    

    let submit bomId cycles (posLocation: LocationId) startDate (destination: LocationId) = 
        
        use t = createTransactionScope

        let createBatch _ =
            ReactionBatch { 
                Id = None
                BillOfMaterialsId = bomId
                Cycles = cycles
                StartDate = startDate 
            }
            |> BatchRepository.save

        let takeInput (bom, (batch : Batch)) =
            bom.Input
            |> Seq.map (fun (item, quantity) -> 
                match InventoryService.peek item.Id (quantity * cycles) with
                | Success lines -> 
                    let dest = InventoryDestination.Batch batch.Id.Value

                    lines
                    |> Seq.map (ShippingService.ship posLocation startDate)
                    |> Seq.map (function
                        | Success line -> InventoryService.take dest startDate line
                        | Failure f -> Failure f
                    )
                    |> Seq.fold (append (fun a b -> b :: a) (fun a b -> b :: a)) (Success [])
                | Failure f -> Failure f
            )
            |> Seq.fold (append (fun a b -> List.collect id (a :: [b])) (fun a b -> b :: a)) (Success [])
            |> (function 
                | Success s -> Success s
                | Failure f -> List.collect id f |> Failure
            )

        BillOfMaterialsRepository.getBillOfMaterials bomId
        |> someOrFail ["Bill of materials not found"]
        |> map (function bom -> bom, createBatch bom)
        |> (function
            | Success (bom, batch) ->
                takeInput (bom, batch)
                |> map (Seq.sumBy (fun x -> x.TotalPrice))
                |> map (fun totalPrice -> 
                    let outputItem, outputQuantity = bom.Output
                    let endDate = startDate + TimeSpan.FromTicks(bom.Duration.Ticks * cycles)
                    let unitPrice = totalPrice / (decimal (outputQuantity * cycles))
                    {
                        InventoryInput.Id = None
                        ItemId = outputItem.Id
                        Quantity = outputQuantity * cycles
                        Price = unitPrice 
                        ShippingCost = 0m 
                        SourceLineId = None
                        Date = endDate
                        LocationId = posLocation
                        Source = InventorySource.Batch batch.Id.Value
                    }
                    |> InventoryInput
                    |> InventoryLineRepository.save
                    |> (function 
                        | InventoryInput line -> ShippingService.ship destination endDate line
                        | _ -> Failure "Unexpected output line on save"
                    )
                )
            | Failure f -> Failure f
        )
        