using BareORM.Schema.Types;

namespace BareORM.Migrations.Abstractions
{
    /// <summary>
    /// Builder de migraciones: acumula operaciones (<see cref="MigrationOperation"/>) que luego
    /// un provider traduce a SQL/DDL.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Una migración (<see cref="Migration"/>) recibe una instancia de <see cref="MigrationBuilder"/> en <c>Up</c>/<c>Down</c>
    /// para declarar cambios (create/drop/alter) de forma agnóstica.
    /// </para>
    /// <para>
    /// El builder NO ejecuta nada: únicamente registra operaciones en <see cref="Operations"/>.
    /// La ejecución/compilación a SQL depende del provider (SQL Server, Postgres, etc.).
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// public sealed class CreateUsers : Migration
    /// {
    ///     public override string Id =&gt; "20251225_090000";
    ///     public override string Name =&gt; "CreateUsers";
    ///
    ///     public override void Up(MigrationBuilder mb)
    ///     {
    ///         var t = mb.CreateTable("dbo", "Users");
    ///         t.Columns.Add(new AddColumnOp("dbo", "Users", "Id", new GuidType()) { IsNullable = false });
    ///         t.PrimaryKey = new AddPrimaryKeyOp("dbo", "Users", "PK_Users", new[] { "Id" });
    ///
    ///         mb.CreateOrAlterView("dbo", "vw_Users", "CREATE OR ALTER VIEW dbo.vw_Users AS SELECT Id FROM dbo.Users;");
    ///     }
    ///
    ///     public override void Down(MigrationBuilder mb)
    ///     {
    ///         mb.DropView("dbo", "vw_Users");
    ///         mb.DropTable("dbo", "Users");
    ///     }
    /// }
    /// </code>
    /// </example>
    public sealed class MigrationBuilder
    {
        private readonly List<MigrationOperation> _ops = new();

        /// <summary>
        /// Lista de operaciones registradas por la migración, en el orden en que se agregaron.
        /// </summary>
        /// <remarks>
        /// Visible para lectura desde afuera, pero no editable directamente.
        /// </remarks>
        public IReadOnlyList<MigrationOperation> Operations => _ops;

        /// <summary>
        /// Registra una operación de creación de tabla y devuelve la operación para configurar columnas/constraints.
        /// </summary>
        /// <param name="schema">Schema de la tabla.</param>
        /// <param name="name">Nombre de la tabla.</param>
        /// <returns>Operación <see cref="CreateTableOp"/> para configuración adicional.</returns>
        public CreateTableOp CreateTable(string schema, string name)
        {
            var op = new CreateTableOp(schema, name);
            _ops.Add(op);
            return op;
        }

        /// <summary>
        /// Registra una operación para eliminar una tabla.
        /// </summary>
        /// <param name="schema">Schema de la tabla.</param>
        /// <param name="name">Nombre de la tabla.</param>
        public void DropTable(string schema, string name)
            => _ops.Add(new DropTableOp(schema, name));

        /// <summary>
        /// Registra una operación para agregar una columna.
        /// </summary>
        /// <param name="schema">Schema de la tabla.</param>
        /// <param name="table">Nombre de la tabla.</param>
        /// <param name="name">Nombre de la columna.</param>
        /// <param name="type">Tipo lógico de columna (agnóstico del provider).</param>
        /// <param name="nullable">Indica si la columna permite NULL.</param>
        /// <param name="defaultValue">Valor por defecto (opcional).</param>
        public void AddColumn(string schema, string table, string name, ColumnType type, bool nullable = true, object? defaultValue = null)
            => _ops.Add(new AddColumnOp(schema, table, name, type) { IsNullable = nullable, DefaultValue = defaultValue });

        /// <summary>
        /// Registra una operación para eliminar una columna.
        /// </summary>
        /// <param name="schema">Schema de la tabla.</param>
        /// <param name="table">Nombre de la tabla.</param>
        /// <param name="name">Nombre de la columna.</param>
        public void DropColumn(string schema, string table, string name)
            => _ops.Add(new DropColumnOp(schema, table, name));

        /// <summary>
        /// Registra una operación de SQL “raw” (sin abstracción).
        /// </summary>
        /// <param name="sql">SQL a ejecutar tal cual.</param>
        /// <remarks>
        /// Útil para casos especiales que aún no están modelados como operaciones tipadas.
        /// </remarks>
        public void Sql(string sql)
            => _ops.Add(new SqlOp(sql));

        #region operaciones para vistas y rutinas (procedimientos y funciones)

        /// <summary>
        /// Registra una operación para crear o alterar una vista.
        /// </summary>
        /// <param name="schema">Schema de la vista.</param>
        /// <param name="name">Nombre de la vista.</param>
        /// <param name="sql">SQL de definición (CREATE/ALTER o equivalente según provider).</param>
        public void CreateOrAlterView(string schema, string name, string sql)
            => _ops.Add(new CreateOrAlterViewOp(schema, name, sql));

        /// <summary>
        /// Registra una operación para eliminar una vista.
        /// </summary>
        /// <param name="schema">Schema de la vista.</param>
        /// <param name="name">Nombre de la vista.</param>
        public void DropView(string schema, string name)
            => _ops.Add(new DropViewOp(schema, name));

        /// <summary>
        /// Registra una operación para crear o alterar un procedimiento almacenado.
        /// </summary>
        /// <param name="schema">Schema del procedimiento.</param>
        /// <param name="name">Nombre del procedimiento.</param>
        /// <param name="sql">SQL de definición.</param>
        public void CreateOrAlterProcedure(string schema, string name, string sql)
            => _ops.Add(new CreateOrAlterRoutineOp(schema, name, RoutineKind.Procedure, sql));

        /// <summary>
        /// Registra una operación para crear o alterar una función escalar.
        /// </summary>
        /// <param name="schema">Schema de la función.</param>
        /// <param name="name">Nombre de la función.</param>
        /// <param name="sql">SQL de definición.</param>
        public void CreateOrAlterScalarFunction(string schema, string name, string sql)
            => _ops.Add(new CreateOrAlterRoutineOp(schema, name, RoutineKind.ScalarFunction, sql));

        /// <summary>
        /// Registra una operación para crear o alterar una función tabular (TVF).
        /// </summary>
        /// <param name="schema">Schema de la función.</param>
        /// <param name="name">Nombre de la función.</param>
        /// <param name="sql">SQL de definición.</param>
        public void CreateOrAlterTableFunction(string schema, string name, string sql)
            => _ops.Add(new CreateOrAlterRoutineOp(schema, name, RoutineKind.TableFunction, sql));

        /// <summary>
        /// Registra una operación para eliminar una rutina (procedimiento o función).
        /// </summary>
        /// <param name="schema">Schema del objeto.</param>
        /// <param name="name">Nombre del objeto.</param>
        /// <param name="kind">Tipo de rutina (procedure/scalar function/table function).</param>
        public void DropRoutine(string schema, string name, RoutineKind kind)
            => _ops.Add(new DropRoutineOp(schema, name, kind));

        /// <summary>
        /// Registra una operación para crear o alterar un trigger.
        /// </summary>
        /// <param name="schema">Schema del trigger.</param>
        /// <param name="name">Nombre del trigger.</param>
        /// <param name="sql">SQL de definición.</param>
        public void CreateOrAlterTrigger(string schema, string name, string sql)
            => _ops.Add(new CreateOrAlterTriggerOp(schema, name, sql));

        /// <summary>
        /// Registra una operación para eliminar un trigger.
        /// </summary>
        /// <param name="schema">Schema del trigger.</param>
        /// <param name="name">Nombre del trigger.</param>
        public void DropTrigger(string schema, string name)
            => _ops.Add(new DropTriggerOp(schema, name));

        #endregion
    }
}
