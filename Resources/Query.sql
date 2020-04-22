declare @dd int
declare @mm int
declare @yy int

declare @environment varchar(24) --mandatory
set @environment = 'demo'

declare @page varchar(255) --mandatory
set @page = 'basda-order-v3.ashx'

set @dd = datepart(day, getdate())
set @mm = datepart(MONTH, getdate())
set @yy = datepart(year, getdate())

declare @Row bigint
set @Row = 0

declare @tab as table(
	[row] bigint,
	[displayTime] [varchar](8), 
	[BubbleID] varchar(64),
	[severity] int	
)

INSERT INTO @tab
SELECT 
	rank() over (order by endpoint, logDate, logTime) as [row]
	,[displayTime] as [displayTime]
	,[BubbleID] as [BubbleID]
	,[severity] as [severity]				  
	FROM [system].[dbo].[apiLog]
	where 0=0
		and [logYear] = @yy
		and [logMonth] = @mm
		and [logDay]= @dd
		and lower([environment]) = lower(@environment)
		and lower([endpoint]) = lower(@page)

	order by logTime

select '', (
	SELECT 
		[row] as "@row"
		,[displayTime] as "@Time"
		,[BubbleID] as "@BubbleID"
		,[severity] as "@Severity"			
	FROM @tab
	where 0=0
		and row > @Row	  
	for XML PATH('item'), type)

for XML PATH('log'), type