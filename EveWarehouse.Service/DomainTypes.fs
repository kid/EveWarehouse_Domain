namespace EveWarehouse

module DomainTypes =

    open System
    
    type BatchId = BatchId of int
    type ItemId = ItemId of int
    type SolarSystemId = SolarSystemId of int
    type StationId = StationId of int
    type WalletId = WalletId of int64
    type TransactionId = TransactionId of int64

    type Item = {
        Id: ItemId
        Name: string
        Volume: decimal
    }

    type TransactionType =
        | Sell = 0
        | Buy = 1

    type DoneFor =
        | Unknown = 0
        | Personal = 1
        | Corporation = 2

    type Transaction = {
        TransactionId: TransactionId
        Date: DateTime
        ItemId: ItemId
        StationId: StationId
        Quantity: int64
        Price: decimal
        DoneFor: DoneFor
        Type: TransactionType
    }

    type InventoryEntrySource =
        | Transaction of WalletId * TransactionId
        | Batch of BatchId
        | ManualEntry

    type InventoryEntryDestination = 
        | Batch of BatchId

    type InventoryEntry = {
        Date: DateTime
        ItemId: ItemId
        StationId: StationId
        Price: decimal
        Quantity: int64
        Source: InventoryEntrySource option
        Destination: InventoryEntryDestination option
    }

    type ItemQuantity = {
        ItemId: ItemId
        Quantity: int64
    }

    type ItemQuantityPriceStation = {
        ItemId: ItemId
        Quantity: int64
        Price: decimal
        StationId: StationId
    }

    type BillOfMaterialsId = BillOfMaterialsId of int

    type BillOfMaterials = {
        Id: BillOfMaterialsId option
        Description: string
        Duration: TimeSpan
        Output: ItemQuantity
        Input: ItemQuantity list
    }

    type Batch = 
    | ReactionBatch of ReactionBatch
    
    and ReactionBatch = {
        Id: BatchId option
        BillOfMaterials: BillOfMaterialsId
        PosSystem: SolarSystemId
    }
