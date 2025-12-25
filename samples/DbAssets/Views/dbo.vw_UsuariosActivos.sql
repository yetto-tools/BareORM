
-- BareORM.samples.DbAssets.Views
-- Path: samples/DbAssets/Views/dbo.vw_UsuariosActivos
-- Description: Vista para listar usuarios activos

CREATE OR ALTER VIEW dbo.vw_UsuariosActivos
AS
SELECT 
    UserId,
    Email,
    DisplayName,
    CreatedAt
FROM dbo.Users
WHERE IsActive = 1;
GO
