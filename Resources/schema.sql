select '', (
	select ENAME as "@Name", (
		select	NAME as "@Name", 
			case when len(FORMCLMNS.TITLE)=0 then COLUMNS.TITLE else  FORMCLMNS.TITLE end as "@title", 
			case when HIDE='H' then 1 else 0 end as "@hide", 
			case when READONLY='R' then 1 else 0 end as "@readonly",
			case when READONLY='M' then 1 else 0 end as "@mandatory"
		from system.dbo.FORMLIMITED 
		JOIN system.dbo.T$EXEC on T$EXEC.T$EXEC = FORMLIMITED.T$EXEC
		join system.dbo.FORMCLMNS on T$EXEC.T$EXEC = FORMCLMNS.FORM
		join system.[dbo].[COLUMNS] on COLUMNS.T$COLUMN = FORMCLMNS.T$COLUMN
		where T$EXEC.T$EXEC = EX.T$EXEC

		for XML PATH('Property'), type
		)
		from system.dbo.FORMLIMITED 
			JOIN system.dbo.T$EXEC EX on EX.T$EXEC = FORMLIMITED.T$EXEC
		where RESTFLAG = 'Y'
	for XML PATH('Entity'), type
) for XML PATH('Schema'), type