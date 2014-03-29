CREATE TABLE [Live].[BillOfMaterials]
(
    [Id] INT NOT NULL PRIMARY KEY IDENTITY, 
    [Description] NVARCHAR(MAX) NOT NULL, 
    [Duration] INT NOT NULL, 
    [OutputItemId] INT NOT NULL REFERENCES [Live].[Item] ([Id]), 
    [OutputQuantity] BIGINT NOT NULL 
)
