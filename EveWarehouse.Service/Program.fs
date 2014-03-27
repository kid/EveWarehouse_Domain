open EveWarehouse.DomainTypes
open Data
open System
open Account
open Transactions
open MarketHistory

[<EntryPoint>]
let main argv = 
    InventoryRepository.take 16652L 20000L
    |> Seq.iter ignore
    
//    addApiKey 3080241L "w2pF9LaPbdNpvLAcJx17tMzwwLoskKDpv2PmHR0VXPGicqVzrMncyASYSCMzrb8J"
//    addApiKey 3092040L "Zi0ySl6v2xjJfNJaj62piYA1RWCaUzE1NWgK0xPcCJG1hnQ3V8pobD9MnJkTzYhI"
//    addApiKey 3180276L "8zoa5OldjSDeONty2if8wSaCepH4XXg73U0wdw8WcwzKzXvp6J6nOU5mqA1oJz1D"
//    
//    updateCharactersWallet
//    updateCorporationsWallets
//    
//    updateAllPrices
//    updateTransactions

    printfn "Done"
    Console.ReadLine() |> ignore
    0