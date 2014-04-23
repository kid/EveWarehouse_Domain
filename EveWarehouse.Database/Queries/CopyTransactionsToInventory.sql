insert into [Live].[InventoryEntries] ([ItemId], [Date], [Price], [ShippingCost], [WalletId], [TransactionId], [StationId], [Movement])
select 
	[ItemId], 
	[Date], 
	[Price],
	0 as [ShippingCost],
	[WalletId], 
	[TransactionId], 
	[StationId],
	case  when [TransactionType] = 0 then 0 - [Quantity] else [Quantity] end as [Movement]
from [Live].[Transaction] t
where [WalletId] in (32742687, 48056485, 50514459, 59409242)
  and [ItemId] in (select distinct [ItemId] from [Live].[BillOfMaterialsInput])
  and not exists (
	select 1 from [Live].[InventoryEntries] i 
	where t.[WalletId] = i.[WalletId]
	  and t.[TransactionId] = i.[TransactionId]
)