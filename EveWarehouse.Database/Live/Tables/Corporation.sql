CREATE TABLE [Live].[Corporation]
(
    [Id] BIGINT NOT NULL PRIMARY KEY, 
    [Name] NVARCHAR(MAX) NOT NULL,
    [ApiKeyId] BIGINT NULL REFERENCES [Live].[ApiKey] ([Id])
)
