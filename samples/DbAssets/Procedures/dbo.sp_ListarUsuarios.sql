
-- BareORM.samples.DbAssets.Procedures
-- Path: samples/DbAssets/Procedures/dbo.sp_ListarUsuarios.sql
-- Description: Procedimiento almacenado para listar usuarios activos o inactivos

CREATE OR ALTER PROCEDURE dbo.sp_ListarUsuarios
    @Activo INT = 1
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        UserId,
        Email,
        DisplayName,
        IsActive,
        CreatedAt,
        JSON_QUERY('{"Theme": "Dark", "Notifications": 1}') AS Settings
    FROM dbo.Users
    WHERE IsActive = @Activo;
END
GO
