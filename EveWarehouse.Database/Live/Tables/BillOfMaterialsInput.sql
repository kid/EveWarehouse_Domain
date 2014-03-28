CREATE TABLE [Live].[BillOfMaterialsInput]
(
	[BillOfMaterialsId] INT NOT NULL REFERENCES [Live].[BillOfMaterials] ([Id]),
	[InputItemId] INT NOT NULL REFERENCES [Live].[Item] ([Id]), 
	[InputQuantity] BIGINT NOT NULL,
	PRIMARY KEY CLUSTERED ([BillOfMaterialsId] ASC, [InputItemId] ASC)
)
