CREATE TABLE [Live].[InventoryEntries]
(
    [Id] BIGINT NOT NULL IDENTITY PRIMARY KEY, 
    [ItemId] INT NOT NULL, 
    [Price] DECIMAL(27, 2) NOT NULL, 
	[ShippingCost] DECIMAL (27, 2) NOT NULL,
    [Movement] BIGINT NOT NULL,
    [Date] DATETIME2 NOT NULL, 
	[SourceLineId] BIGINT NULL REFERENCES [Live].[InventoryEntries] ([Id]),
    [BatchId] INT NULL REFERENCES [Live].[Batch] ([Id]),
	[WalletId] BIGINT NULL REFERENCES [Live].[Wallet] ([Id]),
    [TransactionId] BIGINT NULL,
    [StationId] INT NOT NULL,
    FOREIGN KEY ([WalletId], [TransactionId]) REFERENCES [Live].[Transaction] ([WalletId], [TransactionId])
)
