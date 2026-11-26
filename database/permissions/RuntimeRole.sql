IF DATABASE_PRINCIPAL_ID('FatStacksRuntime') IS NULL CREATE ROLE FatStacksRuntime;
GRANT SELECT ON dbo.SchemaVersion TO FatStacksRuntime;
GRANT SELECT ON dbo.AccountClass TO FatStacksRuntime;
GRANT SELECT ON dbo.AccountType TO FatStacksRuntime;
GRANT SELECT ON dbo.DisplayGroup TO FatStacksRuntime;
GRANT SELECT ON dbo.Currency TO FatStacksRuntime;
GRANT SELECT,INSERT,UPDATE,DELETE ON dbo.Book TO FatStacksRuntime;
GRANT SELECT,INSERT,UPDATE,DELETE ON dbo.Account TO FatStacksRuntime;
GRANT SELECT,INSERT,UPDATE,DELETE ON dbo.Security TO FatStacksRuntime;
GRANT SELECT,INSERT,UPDATE,DELETE ON dbo.JournalEntry TO FatStacksRuntime;
GRANT SELECT,INSERT,UPDATE,DELETE ON dbo.JournalEntryLine TO FatStacksRuntime;
-- Only these application procedures receive execution permission when delivered.
IF OBJECT_ID('dbo.ReadAccountRegister', 'P') IS NOT NULL GRANT EXECUTE ON dbo.ReadAccountRegister TO FatStacksRuntime;
IF OBJECT_ID('dbo.SaveJournalEntry', 'P') IS NOT NULL GRANT EXECUTE ON dbo.SaveJournalEntry TO FatStacksRuntime;
IF OBJECT_ID('dbo.DeleteJournalEntry', 'P') IS NOT NULL GRANT EXECUTE ON dbo.DeleteJournalEntry TO FatStacksRuntime;
-- Maintenance creates/maps an existing login separately and adds its user to this role.

IF TYPE_ID(N'dbo.JournalEntryLines') IS NOT NULL GRANT EXECUTE, REFERENCES ON TYPE::dbo.JournalEntryLines TO FatStacksRuntime;
IF TYPE_ID(N'dbo.JournalEntryLineVersions') IS NOT NULL GRANT EXECUTE, REFERENCES ON TYPE::dbo.JournalEntryLineVersions TO FatStacksRuntime;
