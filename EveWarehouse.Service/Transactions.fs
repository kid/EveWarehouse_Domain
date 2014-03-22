module Transactions

open System
open EveAI
open EveAI.Live
open Data

let updateTransactions =
    use context = EveWarehouse.GetDataContext()
    context.DataContext.Log <- Console.Out

    let query = 
        query {
            for apiKey in context.Live_ApiKey do
            join char in context.Live_Character on (apiKey.Id = char.ApiKeyId)
            select (apiKey.Id, apiKey.Code, char.Id)
        }
    
    for (id, code, charId) in query do
        let api = new EveApi(id, code, charId)
        
        api.GetCharacterWalletTransactions()
        |> Seq.map (fun t -> t.TypeID)
        |> Seq.iter (printf "%i")

    ()
    