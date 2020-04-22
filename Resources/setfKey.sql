USE [system]
declare @BubbleID varchar(64)
select top 1 
	@BubbleID = BubbleID 
from [dbo].[apiLog]
WHERE BubbleID = '{0}'
order by logDate Desc, logTime Desc;
update [dbo].[apiLog] 
set [fKey] = '{1}'
where BubbleID = @BubbleID