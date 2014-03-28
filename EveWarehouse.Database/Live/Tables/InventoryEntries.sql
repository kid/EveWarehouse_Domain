CREATE TABLE [Live].[InventoryEntries]
(
    [Id] BIGINT NOT NULL IDENTITY PRIMARY KEY, 
    [ItemId] INT NOT NULL, 
    [Price] DECIMAL(27, 2) NOT NULL, 
    [Movement] BIGINT NOT NULL,
    [Date] DATETIME2 NOT NULL, 
    [ContractId] INT NULL, 
    [WalletId] BIGINT NULL,
    [TransactionId] BIGINT NULL,
    [SourceStationId] INT NOT NULL,
    FOREIGN KEY ([WalletId], [TransactionId]) REFERENCES [Live].[Transaction] ([WalletId], [TransactionId])
)
