CREATE PROCEDURE [Live].[MoveInventoryLine]
	@lineId bigint,
	@quantity bigint,
	@destinationId int,
	@date date,
	@shippingCost decimal (27, 2)
AS
BEGIN
	IF NOT EXISTS (SELECT 1 FROM [Live].[InventoryEntries] WHERE [Id] = @lineId)
	RETURN

	INSERT INTO [Live].[InventoryEntries] ([ItemId], [Price], [ShippingCost], [Date], [BatchId], [WalletId], [TransactionId], [StationId], [Movement], [SourceLineId])
	SELECT [ItemId], [Price], [ShippingCost], @date as [Date], [BatchId], [WalletId], [TransactionId], [StationId], @quantity * - 1 as [Movement], [Id] as [SourceLineId]
	FROM [Live].[InventoryEntries]
	WHERE [Id] = @lineId;

	DECLARE @outputLineId bigint = SCOPE_IDENTITY();

	INSERT INTO [Live].[InventoryEntries] ([ItemId], [Price], [ShippingCost], [Date], [BatchId], [WalletId], [TransactionId], [StationId], [Movement], [SourceLineId])
	SELECT [ItemId], [Price], [ShippingCost] + @shippingCost as [ShippingCost], @date as [Date], [BatchId], [WalletId], [TransactionId], @destinationId as [StationId], @quantity as [Movement], [Id] as [SourceLineId]
	FROM [Live].[InventoryEntries]
	WHERE [Id] = @outputLineId;

	SET @outputLineId = SCOPE_IDENTITY()

	SELECT *
	FROM [Live].[InventoryEntries]
	WHERE [Id] = @outputLineId;
END
