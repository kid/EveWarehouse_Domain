module EveWarehouse.Data

open System
open System.Transactions
open FSharp.Data
open Microsoft.FSharp.Data.TypeProviders
open EveWarehouse.Common
open EveWarehouse.DomainTypes

type internal EveWarehouse = SqlDataConnection<ConnectionStringName="EveWarehouse", ForceUpdate=true>
