module MarketHistory

open System
open System.Linq
open FSharp.Data
open FSharp.Data.JsonExtensions

open EveWarehouse.Common
open EveWarehouse.Data

[<Literal>]
let marketHistorySample = """ 
{ 
    "pageCount": 1, 
    "pageCount_str": "1", 
    "totalCount": 1, 
    "totalCount_str": "1", 
    "items": [
        {
            "volume": 200000000000, 
            "volume_str": "200000000000", 
            "orderCount": 200000000000, 
            "lowPrice": 222220000000000.1, 
            "highPrice": 3000000000.01, 
            "avgPrice": 26111.1, 
            "orderCount_str": "2", 
            "date": "2013-06-04T00:00:00" 
        }
    ]
} """

type internal MarketHistoryLines = JsonProvider<marketHistorySample>

let fetchHistoryData itemId regionId =
    let url = sprintf "http://public-crest.eveonline.com/market/%i/types/%i/history/" regionId itemId
    try
        MarketHistoryLines.Load url |> succeed
    with
    | ex -> fail ex.Message

let internal mapHistoryData itemId regionId (data: MarketHistoryLines.Root) =
    let mapLine (line: MarketHistoryLines.Item) =
        new EveWarehouse.ServiceTypes.Live_MarketHistoryLine(
            ItemId = itemId,
            RegionId = regionId,
            Date = line.Date,
            Volume = line.Volume,
            OrderCount = line.OrderCount,
            Low = line.LowPrice,
            High = line.HighPrice,
            Average = line.AvgPrice
        )

    data.Items
    |> Seq.map mapLine
    |> succeed

let internal saveHistoryData itemId regionId (lines: seq<EveWarehouse.ServiceTypes.Live_MarketHistoryLine>) =
    use context = EveWarehouse.GetDataContext()
    
    let lastDate =
        let dates =
            query {
                for line in context.Live_MarketHistoryLine do
                where (line.RegionId = regionId)
                where (line.ItemId = itemId)
                select line.Date
            } |> Seq.toList
        match dates.Any() with
        | true -> dates |> Seq.max |> Some
        | false -> None

    let filtered =
        lines
        |> Seq.filter (fun l -> lastDate.IsNone || l.Date.Date > lastDate.Value)
        |> Seq.toList
    
    filtered
    |> context.Live_MarketHistoryLine.InsertAllOnSubmit

    try
        context.DataContext.SubmitChanges()
        succeed (Seq.length filtered)
    with
    | ex -> fail ex.Message

let updateHistory itemId regionId =
    fetchHistoryData itemId regionId
    >>= mapHistoryData itemId regionId
    >>= saveHistoryData itemId regionId

let updateAllPrices =
    use context = EveWarehouse.GetDataContext()
    
    let items = context.Live_Item |> Seq.toList
    let regions = context.Live_Region |> Seq.toList
    
    for item in items do
        for region in regions do
            match updateHistory item.Id region.Id with
            | Success r -> printfn "Saved %i new entries for item %i in region %i" r item.Id region.Id
            | Failure m -> printfn "%s" m