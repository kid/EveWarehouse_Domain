CREATE TABLE [Live].[Transaction]
(
    [Id] BIGINT NOT NULL PRIMARY KEY, 
    [WalletId] BIGINT NOT NULL, 
    [ItemId] INT NOT NULL,
    [ItemName] NVARCHAR(MAX) NOT NULL,
    [Date] DATETIME2 NOT NULL, 
    [Price] DECIMAL(27, 2) NOT NULL, 
    [Quantity] BIGINT NOT NULL, 
    [ClientId] BIGINT NOT NULL, 
    [ClientName] NVARCHAR(MAX) NOT NULL, 
    [ClientTypeId] INT NOT NULL, 
    [ClientTypeName] NVARCHAR(MAX) NOT NULL,
    [StationId] INT NOT NULL,
    [StationName] NVARCHAR(MAX) NOT NULL,
    [TransactionFor] INT NOT NULL, 
    [TransactionType] INT NOT NULL
)
