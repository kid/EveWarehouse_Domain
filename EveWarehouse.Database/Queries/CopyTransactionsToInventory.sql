insert into [Live].[InventoryEntries] ([ItemId], [Date], [Price], [WalletId], [TransactionId], [SourceStationId], [Movement])
select 
	[ItemId], 
	[Date], 
	[Price],
	[WalletId], 
	[TransactionId], 
	[StationId],
	case  when [TransactionType] = 0 then 0 - [Quantity] else [Quantity] end as [Movement]
from [Live].[Transaction] t
where [WalletId] in (32742687, 48056485, 50514459, 59409242)
  and [ItemId] in (select [Id] from [Live].[Item])
  and not exists (
	select 1 from [Live].[InventoryEntries] i 
	where t.[WalletId] = i.[WalletId]
	  and t.[TransactionId] = i.[TransactionId]
)