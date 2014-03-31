namespace EveWarehouse.Domain.Repositories

open System
open FSharp.Data

open EveWarehouse.Domain

module BillOfMaterialsRepository =
    
    [<Literal>]
    let private getBillOfMaterialsStatement = """
        SELECT 
        	b.[Id], 
        	b.[Description], 
        	b.[Duration], 
        	o.[Id] as [OutputItemId], 
        	o.[Name] as [OutputItemName], 
        	o.[Volume] as [OutputItemVolume], 
        	[OutputQuantity], 
        	i.[Id] as [InputItemId], 
        	i.[Name] as [InputItemName],
        	i.[Volume] as [InputItemVolume],
        	[InputQuantity]
        FROM [Live].[BillOfMaterials] b
        INNER JOIN [Live].[BillOfMaterialsInput] ON [Id] = [BillOfMaterialsId]
        INNER JOIN [Live].[Item] i ON i.[Id] = [InputItemId]
        INNER JOIN [Live].[Item] o ON o.[Id] = [OutputItemId]
        WHERE b.[Id] = @Id"""

    type private GetBillOfMaterialsQuery = SqlCommandProvider<getBillOfMaterialsStatement, "name=EveWarehouse">

    let private map (rows: GetBillOfMaterialsQuery.Record list) =
        let rec mapInput (rows: GetBillOfMaterialsQuery.Record list) =
            match rows with
            | [] -> []
            | x :: xs ->
                ({ Id = ItemId x.InputItemId ; Name = x.InputItemName ; Volume = x.InputItemVolume }, x.InputQuantity) :: mapInput xs

        match rows with
        | [] -> None
        | x :: xs ->
            { Id = Some (BillOfMaterialsId x.Id);
              Description = x.Description ;
              Duration = TimeSpan.FromSeconds(float x.Duration) ;
              Output = { Id = ItemId x.OutputItemId ; Name = x.OutputItemName ; Volume = x.OutputItemVolume }, x.OutputQuantity
              Input = mapInput rows }
            |> Some
        
    /// <summary>
    /// Get the BOM's definition
    /// </summary>
    /// <param name="id"></param>
    let getBillOfMaterials (BillOfMaterialsId id) = 
        GetBillOfMaterialsQuery().AsyncExecute(id)
        |> Async.RunSynchronously
        |> Seq.toList
        |> map