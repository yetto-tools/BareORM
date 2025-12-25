using System.Reflection;
using BareORM.Annotations.Attributes;
using BareORM.Schema.Utilities;

namespace BareORM.Schema.Builders
{
    /// <summary>
    /// Construye un <see cref="SchemaModel"/> a partir de tipos CLR (entidades) usando atributos de <c>BareORM.Annotations</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Este builder convierte “metadata” (atributos en clases/propiedades) en un modelo de esquema agnóstico:
    /// <see cref="SchemaModel"/> → <see cref="DbSchema"/> → <see cref="DbTable"/> → <see cref="DbColumn"/> y constraints.
    /// </para>
    /// <para>
    /// El modelo resultante se usa para:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Generación de migraciones / diff de esquema.</description></item>
    /// <item><description>DDL por provider (SQL Server, PostgreSQL, etc.).</description></item>
    /// <item><description>Validar convenciones/atributos antes de ejecutar.</description></item>
    /// </list>
    /// <para>
    /// Reglas principales implementadas:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Incluye tipos con <c>[Table]</c> solo si <see cref="SchemaModelBuilderOptions.RequireTableAttribute"/> es true.</description></item>
    /// <item><description>Schema: <c>[Table.Schema]</c> o <see cref="SchemaModelBuilderOptions.DefaultSchema"/>.</description></item>
    /// <item><description>Tabla: <c>[Table.Name]</c> o nombre del tipo CLR.</description></item>
    /// <item><description>Nullability: NULL por defecto; <c>[ColumnNotNull]</c>, <c>[PrimaryKey]</c> e <c>[IncrementalKey]</c> fuerzan NOT NULL.</description></item>
    /// <item><description>Strings: <c>[ColumnMaxLength]</c> o <c>[ColumnFixedLength]/[ColumnLength]</c> (mutuamente excluyentes).</description></item>
    /// <item><description>Decimal: <c>[ColumnPrecision]</c> aplica solo a <see cref="decimal"/> / <see cref="decimal"/>?.</description></item>
    /// <item><description>PK: se arma con propiedades marcadas con <c>[PrimaryKey]</c> y se ordena por <c>Order</c>.</description></item>
    /// <item><description>Unique: se agrupa por <c>[Unique.Name]</c> y ordena por <c>Unique.Order</c>.</description></item>
    /// <item><description>Checks: soporta class-level y property-level <c>[Check]</c>.</description></item>
    /// <item><description>FK: soporta <c>[ForeignKey]</c> por propiedad (FK simple de 1 columna).</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// var builder = new SchemaModelBuilder(new SchemaModelBuilderOptions
    /// {
    ///     DefaultSchema = "dbo",
    ///     RequireTableAttribute = false
    /// });
    ///
    /// var model = builder.Build(typeof(User), typeof(Order), typeof(OrderItem));
    /// foreach (var t in model.AllTables())
    ///     Console.WriteLine($"{t.Schema}.{t.Name} cols={t.Columns.Count} pk={(t.PrimaryKey?.Name ?? "none")}");
    /// </code>
    /// </example>
    public sealed class SchemaModelBuilder
    {
        private readonly SchemaModelBuilderOptions _opt;

        /// <summary>
        /// Inicializa el builder con opciones (o defaults si no se proporcionan).
        /// </summary>
        /// <param name="options">Opciones de construcción del modelo.</param>
        public SchemaModelBuilder(SchemaModelBuilderOptions? options = null)
            => _opt = options ?? new SchemaModelBuilderOptions();

        /// <summary>
        /// Construye un <see cref="SchemaModel"/> a partir de uno o más tipos de entidad.
        /// </summary>
        /// <param name="entityTypes">Tipos CLR que representan entidades.</param>
        /// <returns>Modelo de esquema construido.</returns>
        /// <remarks>
        /// Si <see cref="SchemaModelBuilderOptions.RequireTableAttribute"/> es true, se omiten tipos sin <c>[Table]</c>.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// Se lanza si se detecta uso inválido de atributos (p.ej. precision en no-decimal, length en no-string,
        /// mezcla de max length y fixed length, o FK a propiedad inexistente).
        /// </exception>
        public SchemaModel Build(params Type[] entityTypes)
        {
            var model = new SchemaModel();

            foreach (var entityType in entityTypes)
            {
                var tableAttr = entityType.GetCustomAttribute<TableAttribute>();

                if (_opt.RequireTableAttribute && tableAttr is null)
                    continue;

                var schemaName = tableAttr?.Schema ?? _opt.DefaultSchema;
                var tableName = tableAttr?.Name ?? entityType.Name;

                var schema = model.GetOrAddSchema(schemaName);
                var table = schema.GetOrAddTable(tableName, entityType);

                BuildColumns(table, entityType);
                BuildConstraints(table, entityType, model);
            }

            return model;
        }

        /// <summary>
        /// Construye y registra columnas del <see cref="DbTable"/> a partir de propiedades mapeables del tipo.
        /// </summary>
        /// <param name="table">Tabla destino dentro del modelo.</param>
        /// <param name="entityType">Tipo CLR de la entidad.</param>
        /// <remarks>
        /// <para>
        /// Usa <see cref="ReflectionHelpers.GetMappableProperties(Type)"/> para determinar qué propiedades se incluyen.
        /// </para>
        /// <para>
        /// Aplica reglas de nulabilidad, incremental key, y anotaciones de tamaño/precisión.
        /// </para>
        /// </remarks>
        private void BuildColumns(DbTable table, Type entityType)
        {
            foreach (var p in ReflectionHelpers.GetMappableProperties(entityType))
            {
                var colAttr = p.GetCustomAttribute<ColumnNameAttribute>();
                var colName = colAttr?.Name ?? p.Name;

                var clrType = p.PropertyType;

                // 1) Leer anotaciones de schema
                var maxLen = p.GetCustomAttribute<ColumnMaxLengthAttribute>()?.Value;

                // Fixed: soporta ColumnFixedLength o ColumnLength (alias)
                var fixedLen =
                    p.GetCustomAttribute<ColumnFixedLengthAttribute>()?.Value
                    ?? p.GetCustomAttribute<ColumnLengthAttribute>()?.Value;

                var prec = p.GetCustomAttribute<ColumnPrecisionAttribute>(); // schema

                // Validaciones de uso correcto
                var isString = clrType == typeof(string);
                var isDecimal = clrType == typeof(decimal) || clrType == typeof(decimal?);

                if ((maxLen.HasValue || fixedLen.HasValue) && !isString)
                    throw new InvalidOperationException(
                        $"[ColumnMaxLength]/[ColumnFixedLength]/[ColumnLength] only apply to string. {entityType.Name}.{p.Name} is {clrType.Name}.");

                if (maxLen.HasValue && fixedLen.HasValue)
                    throw new InvalidOperationException(
                        $"Use either [ColumnMaxLength] OR [ColumnFixedLength]/[ColumnLength], not both: {entityType.Name}.{p.Name}.");

                if (prec is not null && !isDecimal)
                    throw new InvalidOperationException(
                        $"[ColumnPrecision] only applies to decimal/decimal?. {entityType.Name}.{p.Name} is {clrType.Name}.");

                // 2) Nullability (regla: NULL por defecto)
                var isNullable = true;

                if (p.GetCustomAttribute<ColumnNotNullAttribute>() is not null)
                    isNullable = false;

                if (p.GetCustomAttribute<PrimaryKeyAttribute>() is not null)
                    isNullable = false;

                var inc = p.GetCustomAttribute<IncrementalKeyAttribute>();
                if (inc is not null)
                    isNullable = false;

                var colType = _opt.TypeMapper.Map(clrType);

                // 3) Crear columna con metadata extra
                var col = new DbColumn(colName, clrType, colType, p.Name)
                {
                    IsNullable = isNullable,

                    // Incremental key
                    IsIncrementalKey = inc is not null,
                    SequenceName = inc?.SequenceName,
                    StartWith = inc?.StartWith,
                    IncrementBy = inc?.IncrementBy,

                    // String size
                    MaxLength = maxLen,
                    FixedLength = fixedLen,

                    // Decimal precision/scale
                    Precision = prec?.Precision,
                    Scale = prec?.Scale
                };

                table.Columns.Add(col);
            }
        }

        /// <summary>
        /// Construye constraints (PK, Unique, Checks, FK) para una tabla desde los atributos del tipo.
        /// </summary>
        /// <param name="table">Tabla a la cual se agregan constraints.</param>
        /// <param name="entityType">Tipo CLR de la entidad.</param>
        /// <param name="model">Modelo completo (disponible para reglas avanzadas o lookups).</param>
        /// <remarks>
        /// Actualmente las FKs soportadas son de una columna (atributo <see cref="ForeignKeyAttribute"/> por propiedad).
        /// </remarks>
        private void BuildConstraints(DbTable table, Type entityType, SchemaModel model)
        {
            // PK
            var pkProps = entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(p => new { Prop = p, Pk = p.GetCustomAttribute<PrimaryKeyAttribute>() })
                .Where(x => x.Pk is not null)
                .OrderBy(x => x.Pk!.Order)
                .ToList();

            if (pkProps.Count > 0)
            {
                var cols = pkProps.Select(x => GetColumnName(x.Prop)).ToArray();
                var pkName = pkProps.First().Pk!.Name
                             ?? (_opt.UseConventionalConstraintNames ? $"PK_{table.Name}" : "PK");

                table.PrimaryKey = new DbPrimaryKey(pkName, cols);
            }

            // Unique groups (por Name)
            var uniqueGroups = new Dictionary<string, List<(PropertyInfo Prop, int Order)>>(StringComparer.OrdinalIgnoreCase);
            foreach (var p in entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                foreach (var uq in p.GetCustomAttributes<UniqueAttribute>())
                {
                    if (!uniqueGroups.TryGetValue(uq.Name, out var list))
                        uniqueGroups[uq.Name] = list = new();
                    list.Add((p, uq.Order));
                }
            }

            foreach (var g in uniqueGroups)
            {
                var cols = g.Value.OrderBy(x => x.Order).Select(x => GetColumnName(x.Prop)).ToArray();
                table.Uniques.Add(new DbUnique(g.Key, cols));
            }

            // Checks (class-level)
            foreach (var chk in entityType.GetCustomAttributes<CheckAttribute>())
            {
                var name = chk.Name ?? (_opt.UseConventionalConstraintNames ? $"CK_{table.Name}_{table.Checks.Count + 1}" : "CK");
                table.Checks.Add(new DbCheck(name, chk.Expression));
            }

            // Checks (property-level)
            foreach (var p in entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                foreach (var chk in p.GetCustomAttributes<CheckAttribute>())
                {
                    var name = chk.Name ?? (_opt.UseConventionalConstraintNames ? $"CK_{table.Name}_{GetColumnName(p)}" : "CK");
                    table.Checks.Add(new DbCheck(name, chk.Expression));
                }
            }

            // FKs
            foreach (var p in entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var fk = p.GetCustomAttribute<ForeignKeyAttribute>();
                if (fk is null) continue;

                var refTableAttr = fk.RefEntityType.GetCustomAttribute<TableAttribute>();
                var refSchema = refTableAttr?.Schema ?? _opt.DefaultSchema;
                var refTable = refTableAttr?.Name ?? fk.RefEntityType.Name;

                // ref column name (si el prop en ref entity tiene [Column], la usamos)
                var refProp = fk.RefEntityType.GetProperty(fk.RefProperty, BindingFlags.Public | BindingFlags.Instance);
                if (refProp is null)
                    throw new InvalidOperationException($"ForeignKey reference property not found: {fk.RefEntityType.Name}.{fk.RefProperty}");

                var fkName = fk.Name
                             ?? (_opt.UseConventionalConstraintNames ? $"FK_{table.Name}_{refTable}_{GetColumnName(p)}" : "FK");

                table.ForeignKeys.Add(new DbForeignKey(
                    fkName,
                    new[] { GetColumnName(p) },
                    refSchema,
                    refTable,
                    new[] { GetColumnName(refProp) },
                    fk.OnDelete,
                    fk.OnUpdate
                ));
            }
        }

        /// <summary>
        /// Resuelve el nombre de columna para una propiedad, respetando <see cref="ColumnNameAttribute"/> si existe.
        /// </summary>
        /// <param name="p">Propiedad CLR.</param>
        /// <returns>Nombre final de columna.</returns>
        private static string GetColumnName(PropertyInfo p)
            => p.GetCustomAttribute<ColumnNameAttribute>()?.Name ?? p.Name;
    }
}
