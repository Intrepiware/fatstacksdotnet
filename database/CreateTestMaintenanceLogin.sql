-- Run this script in SSMS as a SQL Server administrator.
-- Replace the password below before running it. Do not save the real password here.
-- This script runs once. It does not change an existing login or user.
-- Run this script manually. It is outside the grate migration folders.
USE [master];

DECLARE @Password AS NVARCHAR (128) = N'REPLACE_WITH_A_UNIQUE_PASSWORD';

IF @Password = N'REPLACE_WITH_A_UNIQUE_PASSWORD'
    THROW 50000, 'Set a unique maintenance password before running this script.', 1;

DECLARE @CreateLogin AS NVARCHAR (MAX) = N'CREATE LOGIN [FatStacksTestMaintenance] WITH PASSWORD = ' + QUOTENAME(@Password, '''') + N', CHECK_POLICY = ON, CHECK_EXPIRATION = OFF, DEFAULT_DATABASE = [master];';

EXECUTE sys.sp_executesql @CreateLogin;

CREATE USER [FatStacksTestMaintenance] FOR LOGIN [FatStacksTestMaintenance];

GRANT CREATE DATABASE TO [FatStacksTestMaintenance];


-- The login owns databases that it creates. This permits schema setup and cleanup.
-- No user is needed in each generated test database: its owner connects as dbo.
-- This permission permits creation of other database names too. Use this login
-- only for test maintenance on this development instance.
-- The script does not grant sysadmin, dbcreator, or access to existing books.