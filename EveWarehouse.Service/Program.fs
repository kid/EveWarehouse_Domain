open FSharp.Data
open FSharp.Data.CsvExtensions
open EveWarehouse.DomainTypes
open Data
open System
open Account
open Transactions
open MarketHistory

[<Literal>]
let insertStatement = """
INSERT INTO [Live].[Transaction] ([WalletId], [TransactionId], [Date], [ItemId], [ItemName], [Price], [Quantity], [ClientId], [ClientName], [StationId], [StationName], [TransactionFor], [TransactionType])
VALUES (@WalletId, @TransactionId, @Date, @ItemId, @ItemName, @Price, @Quantity, @ClientId, @ClientName, @StationId, @StationName, @TransactionFor, @TransactionType)
"""
    

type InsertTransactionCommand = SqlCommandProvider<insertStatement, connectionString>

[<EntryPoint>]
let main argv = 
    let terahertzBom = {
        Output = { ItemId = ItemId 33360 ; Quantity = 300L * 168L * 2L }
        Input = 
        [
            { ItemId = ItemId 16657 ; Quantity = 16800L * 2L } ; 
            { ItemId = ItemId 16652 ; Quantity = 8400L * 2L } ; 
            { ItemId = ItemId 16646 ; Quantity = 8400L * 2L } ; 
            { ItemId = ItemId 4312 ; Quantity = 5040L * 2L } 
        ]
    }

    let materials = 
        terahertzBom.Input
        |> Seq.collect (fun x -> InventoryManager.tryPeek x.ItemId x.Quantity)
        |> Seq.sumBy (fun x -> x.Price * decimal x.Quantity)

    let transport = decimal (2 * (17640000 + 12600000))

    let total = materials + transport

    printfn "Materials:  %A" materials
    printfn "Transport:  %A" transport
    printfn "Total:      %A" total
    printfn "Unit Price: %A" (total / decimal terahertzBom.Output.Quantity)


//    addApiKey 3080241L "w2pF9LaPbdNpvLAcJx17tMzwwLoskKDpv2PmHR0VXPGicqVzrMncyASYSCMzrb8J"
//    addApiKey 3092040L "Zi0ySl6v2xjJfNJaj62piYA1RWCaUzE1NWgK0xPcCJG1hnQ3V8pobD9MnJkTzYhI"
//    addApiKey 3180276L "8zoa5OldjSDeONty2if8wSaCepH4XXg73U0wdw8WcwzKzXvp6J6nOU5mqA1oJz1D"
//    
//    updateCharactersWallet
//    updateCorporationsWallets
//    
//    updateAllPrices
//    updateTransactions

//    let command = InsertTransactionCommand()
//    for row in CsvFile.Load("..\..\..\Transactions.csv").Rows do
//        let transactionType = 
//            match row?TransactionType with
//            | "BUY" -> 1
//            | "SELL" -> 0
//
//        let transactionFor = 
//            match row?TransactionFor with
//            | "PERSONAL" -> 1
//            | "CORPORATION" -> 2
//
//        command.Execute(
//            row?WalletId.AsInteger64(),
//            row?TransactionId.AsInteger64(),
//            row?Date.AsDateTime(),
//            row?TypeId.AsInteger(),
//            row?TypeName,
//            row?Price.AsDecimal(),
//            row?Quantity.AsInteger64(),
//            row?ClientId.AsInteger64(),
//            row?ClientName,
//            row?StationId.AsInteger(),
//            row?StationName,
//            transactionFor,
//            transactionType
//        )
//        |> ignore
    
    printfn "Done"
    Console.ReadLine() |> ignore
    0