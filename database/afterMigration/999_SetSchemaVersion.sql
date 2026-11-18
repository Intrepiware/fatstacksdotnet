IF NOT EXISTS (SELECT 1 FROM dbo.SchemaVersion WHERE SchemaVersionId = 1)
    INSERT dbo.SchemaVersion VALUES (1, 1);
ELSE IF (SELECT Version FROM dbo.SchemaVersion WHERE SchemaVersionId = 1) <> 1
    THROW 51000, 'Schema version does not match this maintenance package.', 1;
