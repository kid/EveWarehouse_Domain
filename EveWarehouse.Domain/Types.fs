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
    
    and InventoryInput = {
        Id : InventoryLineId option
        Date : DateTime
        ItemId : ItemId
        Quantity : int64
        Price : decimal
        LocationId : LocationId
        Source : InventorySource
    }

    and InventoryOutput = {
        Id : InventoryLineId option
        Date : DateTime
        ItemId : ItemId 
        Quantity : int64
        Price : decimal
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