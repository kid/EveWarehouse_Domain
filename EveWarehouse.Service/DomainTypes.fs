namespace EveWarehouse

module DomainTypes =

    open System
    
    type ItemId = ItemId of int64
    type StationId = StationId of int64
    type WalletId = WalletId of int64
    type TransactionId = TransactionId of int64

    type TransactionType =
        | Sell = 0
        | Buy = 1

    type TransactionFor =
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
        DoneFor: TransactionFor
        Type: TransactionType
    }

    type InventoryEntrySource =
        | Transaction of WalletId * TransactionId
        //| Contract of int64
        | ManualEntry

    type InventoryEntry = {
        Date: DateTime
        ItemId: ItemId
        StationId: StationId
        Price: decimal
        Quantity: int64
        Source: InventoryEntrySource
    }

    type ItemQuantity = {
        ItemId: ItemId
        Quantity: int64
    }

    type BillOfMaterial = {
        Output: ItemQuantity
        Input: ItemQuantity list
    }

    type ReactionBatch = {
        ItemId: ItemId
        CycleDuration: TimeSpan
    }

    let terahertzBom = {
        Output = { ItemId = ItemId 33360L ; Quantity = 300L }
        Input = [ { ItemId = ItemId 16657L ; Quantity = 16800L } ; { ItemId = ItemId 16652L ; Quantity = 8400L } ; { ItemId = ItemId 16646L ; Quantity = 8400L } ]
    }