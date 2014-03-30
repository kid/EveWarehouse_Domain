CREATE PROCEDURE [Live].[FindItemsByIds]
	@Ids [dbo].[LiveItem] readonly
AS
BEGIN
	SELECT *
	FROM [Live].[Item]
	WHERE [Id] IN (SELECT [Id] FROM @Ids)
END