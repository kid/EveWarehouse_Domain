CREATE TABLE [Live].[Character]
(
    [Id] BIGINT NOT NULL PRIMARY KEY, 
    [Name] NVARCHAR(MAX) NOT NULL,
	[CorporationId] BIGINT NOT NULL REFERENCES [Live].[Corporation] ([Id]),
    [ApiKeyId] BIGINT NOT NULL REFERENCES [Live].[ApiKey] ([Id])
)
