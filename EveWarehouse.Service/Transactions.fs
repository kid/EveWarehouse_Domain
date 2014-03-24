module Transactions

open System
open EveAI
open EveAI.Live
open EveAI.Live.Account
open Data

let updateTransactions =
    use context = EveWarehouse.GetDataContext()
    context.DataContext.Log <- Console.Out

    let query = 
        query {
            for apiKey in context.Live_ApiKey do
            where (apiKey.KeyType = (int)APIKeyInfo.APIKeyType.Corporation)
            join char in context.Live_Character on (apiKey.Id = char.ApiKeyId)
            join corp in context.Live_Corporation on (apiKey.Id = corp.ApiKeyId)
            join wallet in context.Live_Wallet on (corp.Id = wallet.CorporationId.Value)
            select (apiKey.Id, apiKey.Code, char.Id, wallet.Id, wallet.AccountKey)
        }
    
    for (id, code, charId, walletId, accountKey) in query do
        let api = new EveApi(id, code, charId)
        let transactions = api.GetCorporationWalletTransactions(accountKey)

        let toEntity (entry:TransactionEntry) =
            new EveWarehouse.ServiceTypes.Live_Transaction(
                Id = entry.TransactionID,
                Date = entry.Date,
                WalletId = walletId,
                ItemId = entry.TypeID,
                ItemName = entry.TypeName,
                StationId = entry.StationID,
                StationName = entry.StationName,
                ClientId = entry.ClientID,
                ClientName = entry.ClientName,
                ClientTypeId = entry.ClientTypeID,
                ClientTypeName = entry.ClientType.Name,
                Price = (decimal)entry.Price,
                Quantity = entry.Quantity,
                TransactionFor = (int)entry.TransactionFor,
                TransactionType = (int)entry.TransactionType
            )
        
        use context = EveWarehouse.GetDataContext()
        transactions
        |> Seq.map toEntity
        |> Seq.iter context.Live_Transaction.InsertOnSubmit

        context.DataContext.SubmitChanges()