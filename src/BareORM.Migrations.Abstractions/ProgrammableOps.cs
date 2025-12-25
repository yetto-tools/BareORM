namespace BareORM.Migrations.Abstractions
{
    /// <summary>
    /// Identifica el tipo de “rutina” (procedimiento o función) para operaciones de migración.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Se usa principalmente por operaciones tipo <see cref="CreateOrAlterRoutineOp"/> y <see cref="DropRoutineOp"/>
    /// para que el provider traduzca a DDL correcto (PROCEDURE / FUNCTION...).
    /// </para>
    /// </remarks>
    public enum RoutineKind
    {
        /// <summary>Procedimiento almacenado (PROCEDURE).</summary>
        Procedure = 0,

        /// <summary>Función escalar (scalar function).</summary>
        ScalarFunction = 1,

        /// <summary>Función tabular (table-valued function).</summary>
        TableFunction = 2
    }

    /// <summary>
    /// Operación para crear o alterar una vista (VIEW).
    /// </summary>
    /// <param name="Schema">Schema del objeto.</param>
    /// <param name="Name">Nombre del objeto.</param>
    /// <param name="DefinitionSql">
    /// SQL de definición (idealmente <c>CREATE OR ALTER</c> o equivalente, según provider).
    /// </param>
    /// <remarks>
    /// El provider decide si usa <c>CREATE OR ALTER</c>, <c>CREATE</c> + <c>ALTER</c>, o scripts condicionales.
    /// </remarks>
    public sealed record CreateOrAlterViewOp(string Schema, string Name, string DefinitionSql) : MigrationOperation;

    /// <summary>
    /// Operación para eliminar una vista (VIEW).
    /// </summary>
    /// <param name="Schema">Schema del objeto.</param>
    /// <param name="Name">Nombre del objeto.</param>
    public sealed record DropViewOp(string Schema, string Name) : MigrationOperation;

    /// <summary>
    /// Operación para crear o alterar una rutina (procedimiento o función).
    /// </summary>
    /// <param name="Schema">Schema del objeto.</param>
    /// <param name="Name">Nombre del objeto.</param>
    /// <param name="Kind">Tipo de rutina.</param>
    /// <param name="DefinitionSql">SQL de definición de la rutina.</param>
    /// <remarks>
    /// <para>
    /// <paramref name="Kind"/> determina el DDL final:
    /// PROCEDURE / FUNCTION (scalar o table).
    /// </para>
    /// <para>
    /// El provider decide si usa <c>CREATE OR ALTER</c>, <c>CREATE</c> + <c>ALTER</c> o scripts condicionales.
    /// </para>
    /// </remarks>
    public sealed record CreateOrAlterRoutineOp(string Schema, string Name, RoutineKind Kind, string DefinitionSql) : MigrationOperation;

    /// <summary>
    /// Operación para eliminar una rutina (procedimiento o función).
    /// </summary>
    /// <param name="Schema">Schema del objeto.</param>
    /// <param name="Name">Nombre del objeto.</param>
    /// <param name="Kind">Tipo de rutina.</param>
    public sealed record DropRoutineOp(string Schema, string Name, RoutineKind Kind) : MigrationOperation;

    /// <summary>
    /// Operación para crear o alterar un trigger.
    /// </summary>
    /// <param name="Schema">Schema del objeto.</param>
    /// <param name="Name">Nombre del objeto.</param>
    /// <param name="DefinitionSql">SQL de definición del trigger.</param>
    /// <remarks>
    /// El provider decide la estrategia (CREATE/ALTER/condicional) según el motor.
    /// </remarks>
    public sealed record CreateOrAlterTriggerOp(string Schema, string Name, string DefinitionSql) : MigrationOperation;

    /// <summary>
    /// Operación para eliminar un trigger.
    /// </summary>
    /// <param name="Schema">Schema del objeto.</param>
    /// <param name="Name">Nombre del objeto.</param>
    public sealed record DropTriggerOp(string Schema, string Name) : MigrationOperation;
}
