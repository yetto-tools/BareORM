using System.Reflection;

namespace BareORM.Annotations.Metadata
{
    /// <summary>
    /// Metadata estructural de una entidad (tabla) obtenida vía reflexión + anotaciones.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Este objeto representa el “contrato” de esquema para una entidad:
    /// schema/tabla, columnas, PKs, uniques, FKs y checks.
    /// </para>
    /// <para>
    /// Está pensado para ser consumido por el builder de esquema y/o por el provider de migraciones
    /// para generar operaciones DDL.
    /// </para>
    /// </remarks>
    public sealed class EntityMetadata
    {
        /// <summary>Tipo CLR de la entidad (p.ej. <c>typeof(User)</c>).</summary>
        public required Type EntityType { get; init; }

        /// <summary>Schema destino (p.ej. <c>dbo</c>).</summary>
        public required string Schema { get; init; }

        /// <summary>Nombre de tabla destino.</summary>
        public required string Table { get; init; }

        /// <summary>Listado completo de columnas mapeadas por propiedades.</summary>
        public required IReadOnlyList<ColumnMetadata> Columns { get; init; }

        /// <summary>
        /// Columnas que forman la llave primaria (en orden).
        /// </summary>
        /// <remarks>
        /// Para PK compuesta, el orden viene dado por <see cref="ColumnMetadata.PrimaryKeyOrder"/>.
        /// </remarks>
        public required IReadOnlyList<ColumnMetadata> PrimaryKeys { get; init; }

        /// <summary>Restricciones UNIQUE declaradas para la entidad.</summary>
        public required IReadOnlyList<UniqueMetadata> Uniques { get; init; }

        /// <summary>Restricciones de llave foránea declaradas para la entidad.</summary>
        public required IReadOnlyList<ForeignKeyMetadata> ForeignKeys { get; init; }

        /// <summary>Restricciones CHECK (por entidad o por propiedad).</summary>
        public required IReadOnlyList<CheckMetadata> Checks { get; init; }
    }

    /// <summary>
    /// Metadata de una columna mapeada desde una <see cref="PropertyInfo"/>.
    /// </summary>
    /// <remarks>
    /// Incluye flags comunes de DDL como PK e identidad.
    /// El nombre final de columna puede provenir de atributos (o convenciones).
    /// </remarks>
    public sealed class ColumnMetadata
    {
        /// <summary>Propiedad CLR que origina la columna.</summary>
        public required PropertyInfo Property { get; init; }

        /// <summary>Nombre de la columna en base de datos.</summary>
        public required string ColumnName { get; init; }

        /// <summary>Indica si esta columna pertenece a la llave primaria.</summary>
        public bool IsPrimaryKey { get; init; }

        /// <summary>
        /// Orden de la columna dentro de una PK compuesta.
        /// </summary>
        /// <remarks>
        /// Útil cuando la PK tiene múltiples columnas: 1,2,3...
        /// Si no es PK, típicamente se deja en 0.
        /// </remarks>
        public long PrimaryKeyOrder { get; init; }

        /// <summary>
        /// Indica si la columna es identidad/autoincremental (provider decide implementación).
        /// </summary>
        public bool IsIdentity { get; init; }

        /// <summary>Valor inicial de la identidad (seed).</summary>
        public long IdentitySeed { get; init; }

        /// <summary>Incremento de la identidad.</summary>
        public long IdentityIncrement { get; init; }
    }

    /// <summary>
    /// Metadata de una restricción UNIQUE (simple o compuesta).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="Columns"/> almacena: propiedad origen, nombre de columna y orden dentro del UNIQUE.
    /// </para>
    /// <para>
    /// El provider normalmente mapeará esto a un constraint UNIQUE o a un índice único,
    /// según estrategia del motor.
    /// </para>
    /// </remarks>
    public sealed class UniqueMetadata
    {
        /// <summary>Nombre de la restricción UNIQUE (recomendado).</summary>
        public required string Name { get; init; }

        /// <summary>
        /// Columnas que componen el UNIQUE, con orden explícito.
        /// </summary>
        /// <remarks>
        /// El orden importa para índices únicos compuestos.
        /// </remarks>
        public required IReadOnlyList<(PropertyInfo Prop, string Column, int Order)> Columns { get; init; }
    }

    /// <summary>
    /// Metadata de una llave foránea (FK) definida desde una propiedad/columna local.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Representa la columna local que referencia otra entidad/columna.
    /// El nombre de la FK es opcional: si es null, el provider puede generar uno por convención.
    /// </para>
    /// <para>
    /// <see cref="OnDelete"/> y <see cref="OnUpdate"/> definen la acción referencial.
    /// </para>
    /// </remarks>
    public sealed class ForeignKeyMetadata
    {
        /// <summary>Propiedad CLR que origina la FK (lado dependiente).</summary>
        public required PropertyInfo Property { get; init; }

        /// <summary>Nombre de la columna local que referencia.</summary>
        public required string ColumnName { get; init; }

        /// <summary>Tipo CLR de la entidad referenciada (principal).</summary>
        public required Type RefEntityType { get; init; }

        /// <summary>
        /// Nombre de la propiedad (o columna) referenciada en la entidad principal.
        /// </summary>
        /// <remarks>
        /// Usualmente corresponde a una PK o columna con UNIQUE.
        /// </remarks>
        public required string RefProperty { get; init; }

        /// <summary>Nombre explícito de la FK. Si es null, se genera por convención.</summary>
        public string? Name { get; init; }

        /// <summary>Acción referencial en DELETE.</summary>
        public ReferentialAction OnDelete { get; init; }

        /// <summary>Acción referencial en UPDATE.</summary>
        public ReferentialAction OnUpdate { get; init; }
    }

    /// <summary>
    /// Metadata de una restricción CHECK (por entidad o por propiedad).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Si <see cref="Property"/> es null, el CHECK aplica a nivel de entidad (tabla).
    /// Si no es null, el CHECK está asociado a una propiedad/columna.
    /// </para>
    /// <para>
    /// El <see cref="Expression"/> debe ser sintaxis SQL válida para el provider.
    /// </para>
    /// </remarks>
    public sealed class CheckMetadata
    {
        /// <summary>
        /// Expresión SQL del CHECK (sin la palabra CHECK).
        /// </summary>
        /// <example>
        /// <code>
        /// "[Age] &gt;= 18"
        /// </code>
        /// </example>
        public required string Expression { get; init; }

        /// <summary>Nombre explícito del CHECK. Si es null, se genera por convención.</summary>
        public string? Name { get; init; }

        /// <summary>
        /// Propiedad asociada. Null significa CHECK a nivel de entidad (tabla completa).
        /// </summary>
        public PropertyInfo? Property { get; init; } // null = entity-level
    }
}
