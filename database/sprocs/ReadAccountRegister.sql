CREATE OR ALTER PROCEDURE dbo.ReadAccountRegister
    @AccountId bigint,
    @Today date,
    @SearchText nvarchar(1000)
AS
BEGIN
    SET NOCOUNT ON;
    -- Widen before SUM: individual line precision must not limit an aggregate.
    SELECT e.JournalEntryId, e.AccountingDate, e.Description, e.Payee,
           l.JournalEntryLineId, l.LineOrder, CAST(e.Version AS binary(8)) AS EntryVersion, CAST(l.Version AS binary(8)) AS LineVersion,
           CAST(l.Debit AS decimal(28,2))-CAST(l.Credit AS decimal(28,2)) AS Movement,
           CASE l.ReconciliationStatus WHEN 2 THEN 'Cleared' WHEN 1 THEN 'Reconciling' ELSE 'Uncleared' END AS Status
    INTO #Movements
    FROM dbo.JournalEntry e JOIN dbo.JournalEntryLine l ON l.JournalEntryId=e.JournalEntryId
    WHERE e.BookId=1 AND l.AccountId=@AccountId AND e.AccountingDate<=@Today;

    SELECT CAST(COALESCE(SUM(Movement),0) AS decimal(28,2)) AS CurrentBalance FROM #Movements;

    DECLARE @Cutoff date = DATEADD(day,-89,@Today);

    ;WITH Paths AS (
        SELECT AccountId,CAST(Name AS nvarchar(max)) AS Name
        FROM dbo.Account WHERE ParentAccountId IS NULL AND BookId=1
        UNION ALL
        SELECT a.AccountId,CAST(p.Name+N':'+a.Name AS nvarchar(max))
        FROM dbo.Account a JOIN Paths p ON a.ParentAccountId=p.AccountId WHERE a.BookId=1
    )
    SELECT AccountId,Name INTO #Paths FROM Paths OPTION(MAXRECURSION 0);

    ;WITH Running AS (
        SELECT *,CAST(SUM(Movement) OVER(ORDER BY AccountingDate,JournalEntryId,LineOrder ROWS UNBOUNDED PRECEDING) AS decimal(28,2)) AS Balance
        FROM #Movements
    ), CounterAccounts AS (
        SELECT DISTINCT l.JournalEntryId,l.AccountId
        FROM #Movements m JOIN dbo.JournalEntryLine l ON l.JournalEntryId=m.JournalEntryId
        WHERE m.AccountingDate>=@Cutoff AND l.AccountId<>@AccountId
    ), CounterLabels AS (
        SELECT c.JournalEntryId,
               STRING_AGG(p.Name,N', ') WITHIN GROUP(ORDER BY p.Name) AS Names,
               MAX(CASE WHEN CHARINDEX(@SearchText,p.Name COLLATE Latin1_General_100_CI_AS)>0
                        THEN 1 ELSE 0 END) AS MatchesSearch
        FROM CounterAccounts c JOIN #Paths p ON p.AccountId=c.AccountId
        GROUP BY c.JournalEntryId
    )
    SELECT r.JournalEntryId,r.AccountingDate,r.Description,COALESCE(c.Names,N'') AS CounterAccounts,
           CAST(CASE WHEN r.Movement<0 THEN -r.Movement END AS decimal(28,2)) AS Payment,
           CAST(CASE WHEN r.Movement>0 THEN r.Movement END AS decimal(28,2)) AS Deposit,r.Balance,r.Status,r.Payee,r.JournalEntryLineId,r.EntryVersion,r.LineVersion
    FROM Running r
    LEFT JOIN CounterLabels c ON c.JournalEntryId=r.JournalEntryId
    WHERE r.AccountingDate>=@Cutoff
      AND (@SearchText=N'' OR CHARINDEX(@SearchText,r.Description COLLATE Latin1_General_100_CI_AS)>0
           OR c.MatchesSearch=1)
    ORDER BY r.AccountingDate,r.JournalEntryId,r.LineOrder;
END;
