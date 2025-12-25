//Created at 24/12/2025 18:43:30
using BareORM.Migrations.Abstractions;


namespace BareORM.samples.Migrations;

public sealed class _20251225_004330_AddPingProc : Migration
{
    public override string Id => "20251225_004330";
    public override string Name => "Add_PingProc";

    public override void Up(MigrationBuilder mb)
    {
        mb.CreateOrAlterScalarFunction("dbo", "fn_ActivoTexto", 
        @"-- BareORM.samples.DbAssets.FunctionsScalar
        -- Path: samples/DbAssets/FunctionsScalar/dbo.fn_ActivoTexto.sql
        -- Description: Función escalar para convertir el estado activo a texto

        CREATE OR ALTER FUNCTION dbo.fn_ActivoTexto(@IsActive BIT)
        RETURNS NVARCHAR(10)
        AS
        BEGIN
            RETURN CASE WHEN @IsActive = 1 THEN N'Activo' ELSE N'Inactivo' END;
        END
        GO
        ");

        mb.CreateOrAlterProcedure("dbo", "sp_ListarUsuarios", 
        @"
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
                JSON_QUERY('{""Theme"": ""Dark"", ""Notifications"": 1}') AS Settings
            FROM dbo.Users
            WHERE IsActive = @Activo;
        END
        GO
        ");
        mb.CreateOrAlterTrigger("dbo", "trg_Users_Audit", @"-- BareORM.samples.DbAssets.Triggers
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
        ");

        mb.CreateOrAlterView("dbo", "vw_UsuariosActivos", @"
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
        ");
    }

    public override void Down(MigrationBuilder mb)
    {
        // MVP: no drops automáticos (seguro).
    }
}
