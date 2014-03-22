open System
open MarketHistory
open Transactions

[<EntryPoint>]
let main argv = 
    updateAllPrices
    updateTransactions
    printfn "Done"
    Console.ReadLine() |> ignore
    0