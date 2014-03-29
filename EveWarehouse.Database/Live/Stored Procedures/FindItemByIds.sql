CREATE PROCEDURE [Live].[FindItemByIds]
	@Ids [Live].[Item] readonly
AS
BEGIN
	SELECT *
	FROM [Live].[Item]
	WHERE [Id] IN (SELECT [Id] FROM @Ids)
END