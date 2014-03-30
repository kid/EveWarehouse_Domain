namespace EveWarehouse.Domain

open System

[<AutoOpen>]
module Types =

    type ItemId = ItemId of int
    type StationId = StationId of int
    type BatchId = BatchId of int
    type WalletId = WalletId of int64
    type TransactionId = TransactionId of int64
    
    type InventoryLine =
    | InventoryInput of InventoryInput
    | InventoryOutput of InventoryOutput
    
    and InventoryInput = {
        Id : InventoryLineId option
        Date : DateTime
        ItemId : ItemId
        Quantity : int64
        Price : decimal
        StationId : StationId
        Source : InventorySource
    }

    and InventoryOutput = {
        Id : InventoryLineId option
        Date : DateTime
        ItemId : ItemId
        Quantity : int64
        Price : decimal
        Destination : InventoryDestination
    }
    
    and InventoryLineId = InventoryLineId of int64
    
    and InventorySource =
    | Batch of BatchId
    | Transaction of WalletId * TransactionId
    | UserEntry
    
    and InventoryDestination =
    | Batch of BatchId
    