CREATE OR ALTER PROCEDURE dbo.ReadAccountRegister
    @AccountId bigint,
    @Today date,
    @SearchText nvarchar(1000)
AS
BEGIN
    SET NOCOUNT ON;
    -- Widen before SUM: individual line precision must not limit an aggregate.
    SELECT e.JournalEntryId, e.AccountingDate, e.Description,
           CAST(SUM(CAST(l.Debit AS decimal(28,2))-CAST(l.Credit AS decimal(28,2))) AS decimal(28,2)) AS Movement,
           CASE WHEN MIN(l.ReconciliationStatus)<>MAX(l.ReconciliationStatus) THEN 'Mixed'
                WHEN MIN(l.ReconciliationStatus)=2 THEN 'Cleared'
                WHEN MIN(l.ReconciliationStatus)=1 THEN 'Reconciling' ELSE 'Uncleared' END AS Status
    INTO #Movements
    FROM dbo.JournalEntry e JOIN dbo.JournalEntryLine l ON l.JournalEntryId=e.JournalEntryId
    WHERE e.BookId=1 AND l.AccountId=@AccountId AND e.AccountingDate<=@Today
    GROUP BY e.JournalEntryId,e.AccountingDate,e.Description;

    SELECT CAST(COALESCE(SUM(Movement),0) AS decimal(28,2)) AS CurrentBalance FROM #Movements;

    ;WITH Running AS (
        SELECT *,CAST(SUM(Movement) OVER(ORDER BY AccountingDate,JournalEntryId ROWS UNBOUNDED PRECEDING) AS decimal(28,2)) AS Balance
        FROM #Movements
    ), Paths AS (
        SELECT AccountId,CAST(Name AS nvarchar(max)) AS Name FROM dbo.Account WHERE ParentAccountId IS NULL AND BookId=1
        UNION ALL
        SELECT a.AccountId,CAST(p.Name+N':'+a.Name AS nvarchar(max)) FROM dbo.Account a JOIN Paths p ON a.ParentAccountId=p.AccountId WHERE a.BookId=1
    )
    SELECT r.JournalEntryId,r.AccountingDate,r.Description,COALESCE(c.Names,N'') AS CounterAccounts,
           CAST(CASE WHEN r.Movement<0 THEN -r.Movement END AS decimal(28,2)) AS Payment,
           CAST(CASE WHEN r.Movement>0 THEN r.Movement END AS decimal(28,2)) AS Deposit,r.Balance,r.Status
    FROM Running r
    OUTER APPLY (SELECT STRING_AGG(CAST(p.Name AS nvarchar(max)),N', ') WITHIN GROUP(ORDER BY p.Name) AS Names
                 FROM Paths p WHERE p.AccountId<>@AccountId AND EXISTS
                 (SELECT 1 FROM dbo.JournalEntryLine l WHERE l.JournalEntryId=r.JournalEntryId AND l.AccountId=p.AccountId)) c
    WHERE r.AccountingDate>=DATEADD(day,-89,@Today)
      AND (@SearchText=N'' OR CHARINDEX(@SearchText,r.Description COLLATE Latin1_General_100_CI_AS)>0
           OR CHARINDEX(@SearchText,c.Names COLLATE Latin1_General_100_CI_AS)>0)
    ORDER BY r.AccountingDate,r.JournalEntryId OPTION(MAXRECURSION 0);
END;
