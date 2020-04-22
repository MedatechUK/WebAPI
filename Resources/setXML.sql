USE [system]
declare @BubbleID varchar(64)
select top 1 
	@BubbleID = BubbleID 
from [dbo].[apiLog]
WHERE BubbleID = '{0}'
order by logDate Desc, logTime Desc;
declare @request xml;
set @request = '{1}';
update [dbo].[apiLog] set 
[logXml] = @request
where BubbleID = @BubbleID