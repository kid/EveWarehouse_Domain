module Data

open Microsoft.FSharp.Data.TypeProviders

[<Literal>]
let connectionString = "Server=(local);Initial Catalog=EveWarehouse.Database;Integrated Security=SSPI"

type internal EveWarehouse = SqlDataConnection<ConnectionString=connectionString, ForceUpdate=true>