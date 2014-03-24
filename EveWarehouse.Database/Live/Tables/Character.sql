CREATE TABLE [Live].[Character]
(
    [Id] BIGINT NOT NULL, 
    [Name] NVARCHAR(MAX) NOT NULL,
	[CorporationId] BIGINT NOT NULL REFERENCES [Live].[Corporation] ([Id]),
    [ApiKeyId] BIGINT NULL REFERENCES [Live].[ApiKey] ([Id]),
    [CorpApiKeyId] BIGINT NULL REFERENCES [Live].[ApiKey] ([Id])
	PRIMARY KEY CLUSTERED ([Id] ASC), 
)
