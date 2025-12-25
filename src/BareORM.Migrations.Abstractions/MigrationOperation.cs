using BareORM.Abstractions;
using BareORM.Schema.Types;

namespace BareORM.Migrations.Abstractions
{
    /// <summary>
    /// Operación base de migración.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Una migración (<see cref="Migration"/>) no ejecuta SQL directamente: registra una secuencia de
    /// <see cref="MigrationOperation"/> a través de <see cref="MigrationBuilder"/>.
    /// </para>
    /// <para>
    /// Luego, un provider (SQL Server, PostgreSQL, etc.) traduce estas operaciones a DDL/DML y las ejecuta.
    /// </para>
    /// </remarks>
    public abstract record MigrationOperation;

    /// <summary>
    /// Operación de SQL “raw” (texto libre), ejecutada tal cual por el provider.
    /// </summary>
    /// <param name="Sql">SQL a ejecutar.</param>
    /// <remarks>
    /// <para>
    /// Útil para casos especiales que aún no están modelados como operaciones tipadas.
    /// </para>
    /// <para>
    /// Recomendación: mantener este SQL específico del motor dentro de providers/migraciones del mismo motor
    /// para no perder portabilidad.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// mb.Sql("CREATE INDEX IX_Users_Email ON dbo.Users(Email);");
    /// </code>
    /// </example>
    public record SqlOp(string Sql) : MigrationOperation;
}
