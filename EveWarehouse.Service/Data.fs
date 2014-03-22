module Data

open Microsoft.FSharp.Data.TypeProviders

type internal EveWarehouse = SqlDataConnection<"Server=kid-pc\SQLEXPRESS;Initial Catalog=EveWarehouse.dev;Integrated Security=SSPI">