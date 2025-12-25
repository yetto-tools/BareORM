using BareORM.Annotations;
using BareORM.Schema.Types;

namespace BareORM.Schema
{
    /// <summary>
    /// Modelo de esquema agnóstico del provider: agrupa schemas y tablas descubiertas desde entidades CLR.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="SchemaModel"/> es el resultado típico de un builder/reflection pipeline:
    /// contiene todos los <see cref="DbSchema"/> y sus <see cref="DbTable"/>.
    /// </para>
    /// <para>
    /// Las claves en <see cref="Schemas"/> son case-insensitive para facilitar compatibilidad entre motores.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var model = new SchemaModel();
    /// var dbo = model.GetOrAddSchema("dbo");
    /// var users = dbo.GetOrAddTable("Users", typeof(User));
    ///
    /// Console.WriteLine(model); // Schemas=1, Tables=1
    /// </code>
    /// </example>
    public sealed class SchemaModel
    {
        /// <summary>
        /// Diccionario de schemas por nombre (case-insensitive).
        /// </summary>
        public Dictionary<string, DbSchema> Schemas { get; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Obtiene un schema existente o lo crea si no existe.
        /// </summary>
        /// <param name="name">Nombre del schema.</param>
        /// <returns>Instancia existente o nueva de <see cref="DbSchema"/>.</returns>
        public DbSchema GetOrAddSchema(string name)
        {
            if (!Schemas.TryGetValue(name, out var s))
            {
                s = new DbSchema(name);
                Schemas[name] = s;
            }
            return s;
        }

        /// <summary>
        /// Devuelve todas las tablas de todos los schemas.
        /// </summary>
        public IEnumerable<DbTable> AllTables()
            => Schemas.Values.SelectMany(s => s.Tables.Values);

        /// <summary>
        /// Resumen rápido del modelo para debugging/logs.
        /// </summary>
        public override string ToString()
            => $"Schemas={Schemas.Count}, Tables={AllTables().Count()}";
    }

    /// <summary>
    /// Representa un schema lógico dentro del <see cref="SchemaModel"/>.
    /// </summary>
    public sealed class DbSchema
    {
        /// <summary>Nombre del schema.</summary>
        public string Name { get; }

        /// <summary>
        /// Tablas pertenecientes a este schema (case-insensitive).
        /// </summary>
        public Dictionary<string, DbTable> Tables { get; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Crea una instancia de <see cref="DbSchema"/>.
        /// </summary>
        /// <param name="name">Nombre del schema.</param>
        public DbSchema(string name) => Name = name;

        /// <summary>
        /// Obtiene una tabla existente o la crea si no existe.
        /// </summary>
        /// <param name="tableName">Nombre de la tabla.</param>
        /// <param name="clrType">Tipo CLR que originó el mapeo.</param>
        /// <returns>Instancia existente o nueva de <see cref="DbTable"/>.</returns>
        public DbTable GetOrAddTable(string tableName, Type clrType)
        {
            if (!Tables.TryGetValue(tableName, out var t))
            {
                t = new DbTable(Name, tableName, clrType);
                Tables[tableName] = t;
            }
            return t;
        }
    }

    /// <summary>
    /// Representa una tabla dentro de un schema: columnas y constraints.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Esta estructura es “neutral”: no contiene SQL específico, solo información suficiente
    /// para generar DDL en un provider o para comparar (diff) contra otro modelo.
    /// </para>
    /// </remarks>
    public sealed class DbTable
    {
        /// <summary>Schema que contiene la tabla.</summary>
        public string Schema { get; }

        /// <summary>Nombre de la tabla.</summary>
        public string Name { get; }

        /// <summary>Tipo CLR que originó esta tabla (entidad).</summary>
        public Type ClrType { get; }

        /// <summary>Columnas de la tabla.</summary>
        public List<DbColumn> Columns { get; } = new();

        /// <summary>Llave primaria (si existe).</summary>
        public DbPrimaryKey? PrimaryKey { get; set; }

        /// <summary>Restricciones UNIQUE.</summary>
        public List<DbUnique> Uniques { get; } = new();

        /// <summary>Restricciones FK.</summary>
        public List<DbForeignKey> ForeignKeys { get; } = new();

        /// <summary>Restricciones CHECK.</summary>
        public List<DbCheck> Checks { get; } = new();

        /// <summary>Índices (únicos o no).</summary>
        public List<DbIndex> Indexes { get; } = new();

        /// <summary>
        /// Crea una instancia de <see cref="DbTable"/>.
        /// </summary>
        /// <param name="schema">Nombre del schema.</param>
        /// <param name="name">Nombre de la tabla.</param>
        /// <param name="clrType">Tipo CLR (entidad).</param>
        public DbTable(string schema, string name, Type clrType)
        {
            Schema = schema;
            Name = name;
            ClrType = clrType;
        }

        /// <summary>
        /// Busca una columna por nombre (case-insensitive).
        /// </summary>
        /// <param name="columnName">Nombre de la columna.</param>
        /// <returns>La columna si existe, de lo contrario null.</returns>
        public DbColumn? FindColumn(string columnName)
            => Columns.FirstOrDefault(c => string.Equals(c.Name, columnName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Representa una columna dentro de una <see cref="DbTable"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="Type"/> es el tipo lógico (agnóstico) que el provider mapeará a un tipo físico.
    /// <see cref="ClrType"/> conserva el tipo CLR original (útil para diff y validaciones).
    /// </para>
    /// <para>
    /// Las propiedades “Anotaciones de schema” (MaxLength/FixedLength/Precision/Scale) suelen venir de atributos.
    /// </para>
    /// </remarks>
    public sealed class DbColumn
    {
        /// <summary>Nombre de la columna en BD.</summary>
        public string Name { get; }

        /// <summary>Tipo CLR original (p.ej. typeof(string), typeof(int?), etc.).</summary>
        public Type ClrType { get; }

        /// <summary>
        /// Tipo lógico/agnóstico de la columna.
        /// </summary>
        public ColumnType Type { get; }

        /// <summary>Indica si permite NULL.</summary>
        public bool IsNullable { get; set; }

        /// <summary>Valor por defecto (si aplica).</summary>
        public object? DefaultValue { get; set; }

        /// <summary>
        /// Indica si esta columna usa generación incremental (agnóstico del motor).
        /// </summary>
        /// <remarks>
        /// El provider decide si lo implementa como IDENTITY, SEQUENCE, SERIAL, etc.
        /// </remarks>
        public bool IsIncrementalKey { get; set; }

        /// <summary>
        /// Nombre explícito de secuencia si el provider la usa (opcional).
        /// </summary>
        public string? SequenceName { get; set; }

        /// <summary>Valor inicial si aplica generación incremental.</summary>
        public long? StartWith { get; set; }

        /// <summary>Incremento si aplica generación incremental.</summary>
        public long? IncrementBy { get; set; }

        /// <summary>Longitud máxima (strings de longitud variable).</summary>
        public int? MaxLength { get; set; }

        /// <summary>Longitud fija (strings de longitud fija, tipo CHAR/NCHAR).</summary>
        public int? FixedLength { get; set; }

        /// <summary>Precisión (DECIMAL/NUMERIC).</summary>
        public byte? Precision { get; set; }

        /// <summary>Escala (DECIMAL/NUMERIC).</summary>
        public byte? Scale { get; set; }

        /// <summary>
        /// Nombre de la propiedad CLR que originó la columna (útil para diff/tracking).
        /// </summary>
        public string ClrPropertyName { get; }

        /// <summary>
        /// Crea una instancia de <see cref="DbColumn"/>.
        /// </summary>
        /// <param name="name">Nombre de columna en BD.</param>
        /// <param name="clrType">Tipo CLR.</param>
        /// <param name="type">Tipo lógico.</param>
        /// <param name="clrPropertyName">Nombre de la propiedad CLR (tracking).</param>
        public DbColumn(string name, Type clrType, ColumnType type, string clrPropertyName)
        {
            Name = name;
            ClrType = clrType;
            Type = type;
            ClrPropertyName = clrPropertyName;
        }
    }

    /// <summary>
    /// Definición de llave primaria (PK) para una tabla.
    /// </summary>
    public sealed class DbPrimaryKey
    {
        /// <summary>Nombre de la PK.</summary>
        public string Name { get; }

        /// <summary>Columnas que componen la PK (en orden).</summary>
        public string[] Columns { get; }

        /// <summary>
        /// Crea una definición de llave primaria (PRIMARY KEY) para una tabla.
        /// </summary>
        /// <param name="name">
        /// Nombre del constraint PRIMARY KEY. Recomendación: usar un nombre estable y descriptivo
        /// (p.ej. <c>PK_Users</c>, <c>PK_OrderItems</c> o <c>PK_Orders_CompanyId_OrderId</c>).
        /// </param>
        /// <param name="columns">
        /// Columnas que componen la PK, en el orden deseado.
        /// Para PK compuesta el orden importa (afecta el clustering/índice según provider).
        /// </param>
        /// <remarks>
        /// <para>
        /// La PK define la identidad única de una fila y, en la mayoría de motores, crea un índice implícito.
        /// </para>
        /// <para>
        /// Algunos providers pueden tratar la PK como índice clustered/nonclustered según configuración.
        /// BareORM mantiene esto agnóstico: el provider decide el SQL final.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Recomendado si <paramref name="name"/> es null/vacío o si <paramref name="columns"/> está vacío.
        /// </exception>
        public DbPrimaryKey(string name, string[] columns)
        {
            Name = name;
            Columns = columns;
        }

    }

    /// <summary>
    /// Definición de restricción UNIQUE para una tabla.
    /// </summary>
    public sealed class DbUnique
    {
        /// <summary>Nombre del UNIQUE.</summary>
        public string Name { get; }

        /// <summary>Columnas del UNIQUE (en orden).</summary>
        public string[] Columns { get; }

        /// <summary>
        /// Crea una definición de restricción UNIQUE.
        /// </summary>
        /// <param name="name">
        /// Nombre del constraint UNIQUE. Recomendación: usar un nombre estable y descriptivo
        /// (p.ej. <c>UQ_Users_Email</c> o <c>UQ_Orders_OrderNumber_CompanyId</c>).
        /// </param>
        /// <param name="columns">
        /// Lista de columnas que componen el UNIQUE, en el orden deseado.
        /// Para UNIQUE compuesto el orden importa (especialmente si el provider lo implementa como índice único).
        /// </param>
        /// <remarks>
        /// <para>
        /// Un UNIQUE constraint garantiza que la combinación de valores en <paramref name="columns"/> sea única.
        /// </para>
        /// <para>
        /// Dependiendo del provider/estrategia, un UNIQUE puede materializarse como constraint o como índice único.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Recomendado si <paramref name="name"/> es null/vacío o si <paramref name="columns"/> está vacío.
        /// </exception>
        public DbUnique(string name, string[] columns)
        {
            Name = name;
            Columns = columns;
        }

    }

    /// <summary>
    /// Definición de llave foránea (FK) entre tablas.
    /// </summary>
    public sealed class DbForeignKey
    {
        /// <summary>Nombre de la FK.</summary>
        public string Name { get; }

        /// <summary>Columnas locales (lado dependiente).</summary>
        public string[] Columns { get; }

        /// <summary>Schema de la tabla referenciada.</summary>
        public string RefSchema { get; }

        /// <summary>Tabla referenciada.</summary>
        public string RefTable { get; }

        /// <summary>Columnas referenciadas (lado principal).</summary>
        public string[] RefColumns { get; }

        /// <summary>Acción referencial al borrar.</summary>
        public ReferentialAction OnDelete { get; }

        /// <summary>Acción referencial al actualizar.</summary>
        public ReferentialAction OnUpdate { get; }

        /// <summary>
        /// Crea una definición de llave foránea (FOREIGN KEY) entre una tabla y una tabla referenciada.
        /// </summary>
        /// <param name="name">
        /// Nombre del constraint FK. Recomendación: usar un nombre estable y descriptivo
        /// (p.ej. <c>FK_Orders_Customers_CustomerId</c>).
        /// </param>
        /// <param name="columns">
        /// Columnas locales (lado dependiente) que referencian a la tabla destino.
        /// El orden debe corresponder 1:1 con <paramref name="refColumns"/>.
        /// </param>
        /// <param name="refSchema">Schema de la tabla referenciada (lado principal).</param>
        /// <param name="refTable">Nombre de la tabla referenciada (lado principal).</param>
        /// <param name="refColumns">
        /// Columnas referenciadas en la tabla destino.
        /// El orden debe corresponder 1:1 con <paramref name="columns"/>.
        /// Normalmente apunta a una PK o a un UNIQUE.
        /// </param>
        /// <param name="onDelete">
        /// Acción referencial al eliminar una fila referenciada (p.ej. Cascade, Restrict/NoAction, SetNull).
        /// </param>
        /// <param name="onUpdate">
        /// Acción referencial al actualizar la clave referenciada (p.ej. Cascade, Restrict/NoAction, SetNull).
        /// </param>
        /// <remarks>
        /// <para>
        /// Una FK mantiene integridad referencial: los valores en <paramref name="columns"/> deben existir
        /// en <paramref name="refTable"/>.<paramref name="refColumns"/>.
        /// </para>
        /// <para>
        /// Para FKs compuestas, <paramref name="columns"/> y <paramref name="refColumns"/> deben tener la misma longitud
        /// y el mismo orden lógico.
        /// </para>
        /// <para>
        /// BareORM mantiene el modelo agnóstico: el provider decide el SQL final y cómo traducir
        /// <see cref="ReferentialAction"/> según el motor.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Recomendado si <paramref name="name"/> es null/vacío, si <paramref name="columns"/> o
        /// <paramref name="refColumns"/> están vacíos, o si sus longitudes no coinciden.
        /// </exception>
        public DbForeignKey(
            string name,
            string[] columns,
            string refSchema,
            string refTable,
            string[] refColumns,
            ReferentialAction onDelete,
            ReferentialAction onUpdate)
        {
            Name = name;
            Columns = columns;
            RefSchema = refSchema;
            RefTable = refTable;
            RefColumns = refColumns;
            OnDelete = onDelete;
            OnUpdate = onUpdate;
        }

    }

    /// <summary>
    /// Definición de restricción CHECK para una tabla.
    /// </summary>
    public sealed class DbCheck
    {
        /// <summary>Nombre del CHECK.</summary>
        public string Name { get; }

        /// <summary>Expresión SQL del CHECK (sin la palabra CHECK).</summary>
        public string Expression { get; }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="expression"></param>
        public DbCheck(string name, string expression)
        {
            Name = name;
            Expression = expression;
        }
    }

    /// <summary>
    /// Definición de índice para una tabla.
    /// </summary>
    /// <remarks>
    /// Nota: un índice único puede solaparse con un UNIQUE constraint según el provider/estrategia.
    /// </remarks>
    public sealed class DbIndex
    {
        /// <summary>Nombre del índice.</summary>
        public string Name { get; }

        /// <summary>Columnas del índice (en orden).</summary>
        public string[] Columns { get; }

        /// <summary>Indica si el índice es único.</summary>
        public bool IsUnique { get; }

        /// <summary>
        /// Crea una definición de índice para una tabla.
        /// </summary>
        /// <param name="name">
        /// Nombre del índice. Recomendación: usar un nombre estable y descriptivo
        /// (p.ej. <c>IX_Users_Email</c>, <c>IX_Orders_CreatedAt</c>, <c>IX_OrderItems_OrderId_ProductId</c>).
        /// </param>
        /// <param name="columns">
        /// Columnas del índice, en el orden deseado.
        /// Para índices compuestos, el orden importa (afecta el rendimiento de búsquedas y ordenamientos).
        /// </param>
        /// <param name="isUnique">
        /// Indica si el índice debe ser único.
        /// Si <c>true</c>, la combinación de valores en <paramref name="columns"/> no podrá repetirse.
        /// </param>
        /// <remarks>
        /// <para>
        /// Un índice mejora consultas y puede acelerar filtros (WHERE), joins y ORDER BY.
        /// </para>
        /// <para>
        /// Si <paramref name="isUnique"/> es <c>true</c>, el provider puede implementarlo como “índice único”.
        /// Esto es similar a un <see cref="DbUnique"/>, pero no siempre es lo mismo:
        /// algunos motores distinguen entre constraint UNIQUE y unique index, aunque ambos garanticen unicidad.
        /// </para>
        /// <para>
        /// BareORM mantiene el modelo agnóstico: el provider decide el SQL final (clustered/nonclustered, include columns, etc.).
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Recomendado si <paramref name="name"/> es null/vacío o si <paramref name="columns"/> está vacío.
        /// </exception>
        public DbIndex(string name, string[] columns, bool isUnique)
        {
            Name = name;
            Columns = columns;
            IsUnique = isUnique;
        }

    }
}
