SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    IF OBJECT_ID(N'dbo.Driver', N'U') IS NULL
        THROW 50001, 'Expected dbo.Driver table is missing.', 1;
    IF COL_LENGTH(N'dbo.Driver', N'DateOfBirth') IS NULL
        ALTER TABLE [dbo].[Driver] ADD [DateOfBirth] date NULL;
    IF COL_LENGTH(N'dbo.Driver', N'Gender') IS NULL
        ALTER TABLE [dbo].[Driver] ADD [Gender] nvarchar(10) NULL;
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
