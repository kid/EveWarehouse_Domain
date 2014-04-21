CREATE TABLE [Live].[Batch]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY, 
    [Cycles] BIGINT NOT NULL,
	[StartDate] DATETIME2 NOT NULL,
	[BillOfMaterialsId] INT NOT NULL REFERENCES [Live].[BillOfMaterials] ([Id]) 
)
