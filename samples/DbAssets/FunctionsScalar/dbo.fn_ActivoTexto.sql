-- BareORM.samples.DbAssets.FunctionsScalar
-- Path: samples/DbAssets/FunctionsScalar/dbo.fn_ActivoTexto.sql
-- Description: Función escalar para convertir el estado activo a texto

CREATE OR ALTER FUNCTION dbo.fn_ActivoTexto(@IsActive BIT)
RETURNS NVARCHAR(10)
AS
BEGIN
    RETURN CASE WHEN @IsActive = 1 THEN N'Activo' ELSE N'Inactivo' END;
END
GO
