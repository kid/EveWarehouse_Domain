CREATE TABLE [Live].[ApiKey] (
    [Id]   BIGINT           NOT NULL,
    [Code] NVARCHAR (64) NOT NULL,
    [AccessMask] BIGINT NOT NULL, 
    [Expires] DATETIME2 NULL, 
    PRIMARY KEY CLUSTERED ([Id] ASC)
);

