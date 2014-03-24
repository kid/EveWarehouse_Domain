CREATE TABLE [Live].[Wallet]
(
    [Id] BIGINT NOT NULL PRIMARY KEY,
    [AccountKey] INT NOT NULL, 
    [Description] NVARCHAR(MAX) NOT NULL, 
    [Balance] DECIMAL(27,2) NOT NULL, 
    [CharacterId] BIGINT NULL REFERENCES [Live].[Character] ([Id]), 
    [CorporationId] BIGINT NULL REFERENCES [Live].[Corporation] ([Id])
)
