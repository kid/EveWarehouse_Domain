open EveWarehouse.DomainTypes
open Data
open System
open Account
open Transactions
open MarketHistory

[<EntryPoint>]
let main argv = 
    for entry in InventoryRepository.take (ItemId 16650L) 10L do
        printfn "%A" entry.ItemId
    
    

//    addApiKey 3080241L "w2pF9LaPbdNpvLAcJx17tMzwwLoskKDpv2PmHR0VXPGicqVzrMncyASYSCMzrb8J"
//    addApiKey 3092040L "Zi0ySl6v2xjJfNJaj62piYA1RWCaUzE1NWgK0xPcCJG1hnQ3V8pobD9MnJkTzYhI"
//    
//    updateCharactersWallet
//    updateCorporationsWallets
//    
//    updateAllPrices
//    updateTransactions

    printfn "Done"
    Console.ReadLine() |> ignore
    0