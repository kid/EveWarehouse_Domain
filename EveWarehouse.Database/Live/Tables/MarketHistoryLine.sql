CREATE TABLE [Live].[MarketHistoryLine]
(
	[ItemId] BIGINT NOT NULL REFERENCES [Live].[Item] ([Id]),
	[RegionId] BIGINT NOT NULL REFERENCES [Live].[Region] ([Id]),
    [Date] DATE NOT NULL, 
	[Volume] BIGINT NOT NULL, 
    [OrderCount] BIGINT NOT NULL, 
    [Low] DECIMAL(27, 2) NOT NULL, 
    [High] DECIMAL(27, 2) NOT NULL, 
    [Average] DECIMAL(27, 4) NOT NULL,
	PRIMARY KEY ([ItemId], [RegionId], [Date])
)
