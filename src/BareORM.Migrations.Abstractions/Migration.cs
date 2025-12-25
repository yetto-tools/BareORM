namespace BareORM.Migrations.Abstractions
{
    /// <summary>
    /// Define una migración de esquema (DDL) con operaciones reversibles.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Una migración debe tener un <see cref="Id"/> único y ordenable (normalmente por fecha/hora),<br/>
    /// y un <see cref="Name"/> legible por humanos.
    /// </para>
    /// <para>
    /// El método <see cref="Up(MigrationBuilder)"/> aplica cambios (crear tablas, columnas, índices, etc.),<br/>
    /// y <see cref="Down(MigrationBuilder)"/> revierte esos cambios en el orden correcto.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// public sealed class 20251221_153501_CreateUsers : Migration
    /// {
    ///     public override string Id =&gt; "20251221_153501";
    ///     public override string Name =&gt; "CreateUsers";
    ///
    ///     public override void Up(MigrationBuilder mb)
    ///     {
    ///         mb.CreateTable("dbo", "Users", t =&gt;
    ///         {
    ///             t.Column("Id", ColumnType.Guid, nullable: false);
    ///             t.Column("Email", ColumnType.NVarChar(320), nullable: false);
    ///             t.PrimaryKey("PK_Users", "Id");
    ///             t.Unique("UQ_Users_Email", "Email");
    ///         });
    ///     }
    ///
    ///     public override void Down(MigrationBuilder mb)
    ///     {
    ///         mb.DropTable("dbo", "Users");
    ///     }
    /// }
    /// </code>
    /// </example>
    public abstract class Migration
    {
        /// <summary>
        /// Identificador único y ordenable de la migración.
        /// </summary>
        /// <remarks>
        /// Formato recomendado: <c>yyyyMMdd_HHmmss</c>.
        /// Ejemplo: <c>"20251221_153501"</c>.
        /// </remarks>
        public abstract string Id { get; }

        /// <summary>
        /// Nombre humano y descriptivo de la migración.
        /// </summary>
        /// <remarks>
        /// Ejemplo: <c>"CreateUsers"</c>.
        /// </remarks>
        public abstract string Name { get; }

        /// <summary>
        /// Aplica los cambios de esquema (operación forward).
        /// </summary>
        /// <param name="mb">Constructor de operaciones de migración.</param>
        public abstract void Up(MigrationBuilder mb);

        /// <summary>
        /// Revierte los cambios aplicados en <see cref="Up(MigrationBuilder)"/> (operación rollback).
        /// </summary>
        /// <param name="mb">Constructor de operaciones de migración.</param>
        public abstract void Down(MigrationBuilder mb);
    }
}
