open EveWarehouse.DomainTypes
open Data
open System
open Account
open Transactions
open MarketHistory

[<EntryPoint>]
let main argv = 
    InventoryRepository.tryPeek (ItemId 16652L) 20000L
    |> Seq.iter (printfn "%A")
    
    let terahertzBom = {
        Output = { ItemId = ItemId 33360L ; Quantity = 300L * 168L * 2L }
        Input = 
        [
            { ItemId = ItemId 16657L ; Quantity = 16800L * 2L } ; 
            { ItemId = ItemId 16652L ; Quantity = 8400L * 2L } ; 
            { ItemId = ItemId 16646L ; Quantity = 8400L * 2L } ; 
            { ItemId = ItemId 4312L ; Quantity = 5040L * 2L } 
        ]
    }

    let materials = 
        terahertzBom.Input
        |> Seq.collect (fun x -> InventoryRepository.tryPeek x.ItemId x.Quantity)
        |> Seq.sumBy (fun x -> x.Price * decimal x.Quantity)

    let transport = decimal (17640000 + 12600000)

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

    printfn "Done"
    Console.ReadLine() |> ignore
    0