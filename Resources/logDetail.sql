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
	1 as Tag,
	Null as Parent,		
	@BubbleID as "Log!1!BubbleID",  
	[severity] as "Log!1!Severity",
	environment as "Log!1!Environment",
	endpoint as "Log!1!EndPoint",
	DATEADD(day, DATEDIFF(day,'19000101',[logDate]), CAST([logTime] AS DATETIME2(7))) as "Log!1!timestamp",
	[logText] as 'Log!1!msg!cdata',
	logXml as 'Log!1!data'

FROM [system].[dbo].[apiLog]
where 0=0
	and [logYear] = @yy
	and [logMonth] = @mm
	and [logDay]= @dd
	and lower([environment]) = lower(@environment)
	and [BubbleID] = @BubbleID

for xml explicit