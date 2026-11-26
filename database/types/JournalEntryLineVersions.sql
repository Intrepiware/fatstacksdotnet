IF TYPE_ID(N'dbo.JournalEntryLineVersions') IS NULL
EXEC(N'CREATE TYPE dbo.JournalEntryLineVersions AS TABLE (LineId bigint NOT NULL PRIMARY KEY, Version binary(8) NOT NULL)');
