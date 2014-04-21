namespace EveWarehouse.Domain

open System

[<AutoOpen>]
module Types =
    
    type BatchId = BatchId of int
    type WalletId = WalletId of int64
    type TransactionId = TransactionId of int64
    
    
    type LocationId =
    | StationId of StationId
    | SolarSystemId of SolarSystemId

    and StationId = StationId of int

    and SolarSystemId = SolarSystemId of int


    type Item = {
        Id : ItemId
        Name : string
        Volume : decimal
    }

    and ItemId = ItemId of int


    type InventoryLine =
    | InventoryInput of InventoryInput
    | InventoryOutput of InventoryOutput
    
        member this.Quantity =
            match this with
            | InventoryInput input -> input.Quantity
            | InventoryOutput output -> output.Quantity

        member this.TotalPrice =
            match this with
            | InventoryInput input -> decimal input.Quantity * input.Price + input.ShippingCost
            | InventoryOutput output -> decimal output.Quantity * output.Price

        member this.LocationId =
            match this with
            | InventoryInput input -> input.LocationId
            | InventoryOutput output -> output.LocationId

    and InventoryInput = {
        Id : InventoryLineId option
        SourceLineId : InventoryLineId option
        Date : DateTime
        ItemId : ItemId
        Quantity : int64
        Price : decimal
        ShippingCost : decimal
        LocationId : LocationId
        Source : InventorySource
    }

    and InventoryOutput = {
        Id : InventoryLineId option
        SourceLineId : InventoryLineId option
        Date : DateTime
        ItemId : ItemId 
        Quantity : int64
        Price : decimal
        ShippingCost : decimal
        LocationId : LocationId
        Destination : InventoryDestination
    }
    
    and InventoryLineId = InventoryLineId of int64
    
    and InventorySource =
    | Batch of BatchId
    | Transaction of WalletId * TransactionId
    | UserEntry
    
    and InventoryDestination =
    | Batch of BatchId

    type BillOfMaterials = {
        Id : BillOfMaterialsId option
        Description : string
        Duration: TimeSpan
        Output : Item * int64
        Input : (Item * int64) list
    }

    and BillOfMaterialsId = BillOfMaterialsId of int


    type Batch =
    | ReactionBatch of ReactionBatch

        member this.Id =
            match this with
            | ReactionBatch b -> b.Id

    and ReactionBatch = {
        Id : BatchId option
        Cycles : int64
        StartDate : DateTime
        BillOfMaterialsId : BillOfMaterialsId
    }