module Account

open System
open System.Linq
open EveAI.Live
open EveAI.Live.Account
open Common
open Data

let getKeyInfo id code =
    try
        let api = new EveApi(id, code)
        api.getApiKeyInfo() |> succeed
    with
    | ex -> fail ex.Message

let saveApiKey id code (details: APIKeyInfo) =
    use context = EveWarehouse.GetDataContext()
    
    match context.Live_ApiKey |> Seq.tryFind (fun x -> x.Id = id) with
    | Some key ->
        key.AccessMask <- details.AccessMask
        key.Expires <- new Nullable<DateTime>(details.Expires)
        key.KeyType <- (int)details.KeyType
    | None ->
        new EveWarehouse.ServiceTypes.Live_ApiKey(
            Id = id,
            Code = code,
            AccessMask = details.AccessMask,
            Expires = new Nullable<DateTime>(details.Expires),
            KeyType = (int)details.KeyType
        )
        |> context.Live_ApiKey.InsertOnSubmit
    
    try
        context.DataContext.SubmitChanges() |> succeed
    with
    | ex -> fail ex.Message

let upsert (table: Linq.Table<'A>) seq mapId insert update = 
    let ids = seq |> Seq.map fst |> Seq.toArray
    let entityMap =
        table
        |> Seq.filter (fun x -> Seq.exists ((=) (mapId x)) ids)
        |> Seq.map (fun x -> (mapId x, x))
        |> Map.ofSeq

    let updateOrInsert x =
        let id = fst x
        match entityMap.TryFind id with
        | Some ent -> update x ent
        | None -> insert x |> table.InsertOnSubmit

    seq |> Seq.iter updateOrInsert

let updateCharactersAndCorporations keyId code =
    use context = EveWarehouse.GetDataContext()
    context.DataContext.Log <- System.Console.Out

    let api = new EveApi(keyId, code)
    let entries = api.GetAccountEntries()

    let corpIds = entries |> Seq.map (fun e -> e.CorporationID) |> Seq.distinct |> Seq.toArray
    let charIds = entries |> Seq.map (fun e -> e.CharacterID) |> Seq.distinct |> Seq.toArray

    let existingCorps = 
        context.Live_Corporation
        |> Seq.filter (fun x -> Seq.exists ((=) x.Id) corpIds)
        |> Seq.map (fun x -> (x.Id, x))
        |> Map.ofSeq

    let existingChars = 
        context.Live_Character
        |> Seq.filter (fun x -> Seq.exists ((=) x.Id) charIds)
        |> Seq.map (fun x -> (x.Id, x))
        |> Map.ofSeq

    let updateOrInsertCorp (id, name) =
        match existingCorps.TryFind id with
        | Some entity ->
            entity.Name <- name
        | None ->
            new EveWarehouse.ServiceTypes.Live_Corporation(Id = id, Name = name, ApiKeyId = keyId)
            |> context.Live_Corporation.InsertOnSubmit
    
    let updateOrInsertChar (id, name, corpId) =
        match existingChars.TryFind id with
        | Some entity ->
            entity.Name <- name
            entity.CorporationId <- corpId
        | None ->
            new EveWarehouse.ServiceTypes.Live_Character(Id = id, Name = name, CorporationId = corpId, ApiKeyId = keyId)
            |> context.Live_Character.InsertOnSubmit

    entries 
    |> Seq.map (fun x -> (x.CorporationID, x.CorporationName))
    |> Seq.distinct
    |> Seq.iter updateOrInsertCorp

//    let corps = entries |> Seq.map (fun x -> (x.CorporationID, x.CorporationName))
//    let mapId (x:EveWarehouse.ServiceTypes.Live_Corporation) = x.Id
//    let insert (id, name) = new EveWarehouse.ServiceTypes.Live_Corporation(Id = id, Name = name, ApiKeyId = keyId)
//    let update (id, name) (x:EveWarehouse.ServiceTypes.Live_Corporation) = x.Name <- name
//    upsert context.Live_Corporation corps mapId insert update

    entries 
    |> Seq.map (fun x -> (x.CharacterID, x.Name, x.CorporationID))
    |> Seq.distinct
    |> Seq.iter updateOrInsertChar

    try
        context.DataContext.SubmitChanges() |> succeed
    with
    | ex -> fail ex.Message

let addApiKey id code =
    match getKeyInfo id code >>= saveApiKey id code with
    | Success _ -> printfn "Saved key"
    | Failure m -> printfn "Error: %s" m
    
    match updateCharactersAndCorporations id code with
    | Success _ -> printfn "Saved characters and corporations"
    | Failure m -> printfn "Error: %s" m