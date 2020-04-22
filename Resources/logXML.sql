declare @dd int --mandatory
declare @mm int --mandatory
declare @yy int --mandatory

set @dd = datepart(day, getdate()) -1
set @mm = datepart(MONTH, getdate())
set @yy = datepart(year, getdate())

declare @environment varchar(24) --mandatory
set @environment = 'demo'

declare @BubbleID varchar(64) --mandatory
set @BubbleID = 'e4592754-7f54-421c-8437-f54c00780a04'

select
	logXml
	 
FROM [system].[dbo].[apiLog]
where 0=0
	and [logYear] = @yy
	and [logMonth] = @mm
	and [logDay]= @dd
	and [environment] = @environment
	and [BubbleID] = @BubbleID