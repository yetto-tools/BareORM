-- BareORM.samples.DbAssets.Triggers
-- Path: samples/DbAssets/Triggers/dbo.trg_Users_Audit.sql
-- Description: Trigger de auditoría para la tabla Users

CREATE OR ALTER TRIGGER dbo.trg_Users_Audit
ON dbo.Users
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    -- Ejemplo “dummy” de auditoría: solo imprime algo
    -- En real: INSERT a tabla de auditoría, etc.
    DECLARE @cnt INT = (SELECT COUNT(1) FROM inserted);
    PRINT CONCAT('Users changed rows: ', @cnt);
END
GO
