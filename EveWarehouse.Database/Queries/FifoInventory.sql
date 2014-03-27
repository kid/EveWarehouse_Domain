with 
StockSum as (
	select [ItemId], sum([Movement]) as [TotalQuantity]
	from [Live].[InventoryEntries]
	group by [ItemId]
),
ReverseSum as (
	select
		s.[Id],
		s.[ItemId], 
		s.[Date],
		s.[Movement] as ThisStock,
		(
			select sum(i.[Movement])
			from [Live].[InventoryEntries] as i
			where i.[ItemId] = s.[ItemId]
			  and i.[Movement] > 0
			  and i.[Date] >= s.[Date]
		) as [RollingStock]
	from [Live].[InventoryEntries] s
	where s.[Movement] > 0
),
FirstOut as (
	select
		p.[Id],
		s.[ItemId],
		p.[Date],
		s.[TotalQuantity] - p.[RollingStock] + p.[StockToUse] as [Remaining],
		p.[PriceToUse]
	from [StockSum] s
	cross apply (
		select top 1
			r.[Id],
			r.[Date],
			r.[ThisStock] as [StockToUse],
			e.[Price] as [PriceToUse],
			r.[RollingStock] as [RollingStock]
		from ReverseSum r
		left join [Live].[InventoryEntries] e on e.[Id] = r.[Id]
		where r.[ItemId] = s.[ItemId]
		  and r.[RollingStock] >= s.[TotalQuantity]
		order by r.[Date] desc, r.[Id] desc
	) as p
)
select 
	f.[ItemId],
	s.[Id] as [StockId],
	case when s.Id = f.[Id] then isnull(f.[Remaining], cast(0 as bigint)) else s.[Movement] end as [Remaining],
	s.[Price]
from [FirstOut] f
join [Live].[InventoryEntries] s on s.[ItemId] = f.[ItemId] and s.[Date] >= f.[Date] and s.[Movement] > 0
where s.ItemId = @ItemId
order by f.[ItemId], s.[Date], s.[Id]