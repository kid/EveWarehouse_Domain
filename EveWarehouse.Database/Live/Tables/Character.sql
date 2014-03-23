CREATE TABLE [Live].[Character]
(
    [Id] BIGINT NOT NULL PRIMARY KEY, 
    [Name] NVARCHAR(MAX) NOT NULL,
    [ApiKeyId] BIGINT NOT NULL REFERENCES [Live].[ApiKey] ([Id])
)
