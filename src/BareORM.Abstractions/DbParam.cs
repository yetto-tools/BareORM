using System.Data;

namespace BareORM.Abstractions
{
    /// <summary>
    /// Descriptor agnóstico de parámetro de base de datos.
    /// Los proveedores convierten este descriptor en sus parámetros nativos específicos
    /// (SqlParameter para SQL Server, NpgsqlParameter para PostgreSQL, etc.).
    /// </summary>
    /// <remarks>
    /// Este record proporciona una interfaz agnóstica para describir parámetros que serán
    /// pasados a comandos de base de datos. Permite especificar toda la información necesaria
    /// (nombre, tipo, dirección, tamaño, precisión, etc.) sin depender de un proveedor específico.
    /// 
    /// El ORM utiliza este descriptor para crear el parámetro nativo correspondiente en tiempo
    /// de ejecución. Esto facilita la portabilidad de código entre diferentes bases de datos.
    /// 
    /// Muchas propiedades son opcionales, por lo que solo es necesario especificar las que
    /// sean relevantes para el caso de uso específico.
    /// </remarks>
    /// <param name="Name">
    /// El nombre del parámetro (identificador único dentro del comando).
    /// La convención es usar el prefijo '@' en SQL Server y bases de datos similares,
    /// aunque algunos proveedores (como PostgreSQL) pueden usar convenciones diferentes.
    /// El nombre debe coincidir con el parámetro esperado por el procedimiento almacenado
    /// o consulta SQL. Ejemplo: "@usuarioId", "@nombre".
    /// </param>
    /// <param name="Value">
    /// El valor actual del parámetro (lo que será pasado a la base de datos).
    /// Puede ser: un valor .NET (int, string, DateTime, etc.), <c>null</c> para parámetros nulos,
    /// <c>DBNull.Value</c> para NULL explícito en BD, o un DataTable para parámetros TVP.
    /// Para parámetros OUTPUT, este valor se ignora en la entrada y se actualiza después de la ejecución.
    /// Valor por defecto: <c>null</c>.
    /// </param>
    /// <param name="DbType">
    /// El tipo de dato de la base de datos del parámetro (define el tipo SQL esperado).
    /// Ejemplos: DbType.String (VARCHAR, NVARCHAR), DbType.Int32 (INT), DbType.DateTime (DATETIME),
    /// DbType.Decimal (DECIMAL, NUMERIC), DbType.Boolean (BIT), DbType.Object (TVP, UDT).
    /// Si no se especifica, el proveedor intentará inferir el tipo a partir del valor.
    /// Valor por defecto: <c>null</c>.
    /// </param>
    /// <param name="Direction">
    /// La dirección del parámetro (entrada, salida, entrada/salida o valor de retorno).
    /// Define cómo se usa: ParameterDirection.Input (pasa datos a BD, más común),
    /// ParameterDirection.Output (BD devuelve valor), ParameterDirection.InputOutput (bidireccional),
    /// ParameterDirection.ReturnValue (valor de retorno de procedimiento).
    /// Valor por defecto: <c>ParameterDirection.Input</c>.
    /// </param>
    /// <param name="Size">
    /// El tamaño máximo de datos para parámetros de tamaño variable.
    /// Para String/VarChar: número máximo de caracteres. Para Binary/VarBinary: número máximo de bytes.
    /// Si se omite, el proveedor usa el tamaño máximo por defecto. Especificar un tamaño menor
    /// puede mejorar rendimiento y detectar errores de desbordamiento.
    /// Valor por defecto: <c>null</c>.
    /// </param>
    /// <param name="Precision">
    /// La precisión de un parámetro numérico decimal (número total de dígitos significativos).
    /// Ejemplo: en precio 199.99, Precision=5, Scale=2. Se usa con DbType.Decimal o DbType.Numeric.
    /// Valor por defecto: <c>null</c>.
    /// </param>
    /// <param name="Scale">
    /// La escala de un parámetro numérico decimal (número de dígitos después del separador decimal).
    /// Ejemplo: en precio 199.99, Precision=5, Scale=2. Se usa con DbType.Decimal o DbType.Numeric.
    /// Valor por defecto: <c>null</c>.
    /// </param>
    /// <param name="IsNullable">
    /// Un valor que indica si el parámetro puede ser nulo. Define si la BD acepta valores NULL.
    /// Configurar explícitamente mejora validación, genera SQL más eficiente y documenta el esquema.
    /// <c>true</c>: puede ser nulo, <c>false</c>: no puede, <c>null</c>: el proveedor infiere.
    /// Valor por defecto: <c>null</c>.
    /// </param>
    /// <param name="TypeName">
    /// El nombre del tipo definido por el usuario o tipo de valor de tabla (TVP).
    /// Se usa para tipos especializados fuera del conjunto estándar de DbType:
    /// - SQL Server TVP: nombre del tipo de tabla (ej: "dbo.TipoUsuarios")
    /// - Oracle UDT: nombre del tipo personalizado
    /// - PostgreSQL tipos personalizados: nombre del tipo registrado
    /// Solo necesario para tipos complejos, no para Int, String, etc.
    /// Valor por defecto: <c>null</c>.
    /// </param>
    /// <example>
    /// <code>
    /// // Ejemplo 1: Parámetro de entrada simple
    /// var param1 = new DbParam(
    ///     Name: "@usuarioId",
    ///     Value: 42,
    ///     DbType: System.Data.DbType.Int32
    /// );
    /// 
    /// // Ejemplo 2: Parámetro de entrada con tamaño (cadena)
    /// var param2 = new DbParam(
    ///     Name: "@nombre",
    ///     Value: "Juan Pérez",
    ///     DbType: System.Data.DbType.String,
    ///     Size: 100
    /// );
    /// 
    /// // Ejemplo 3: Parámetro de salida (OUTPUT)
    /// var param3 = new DbParam(
    ///     Name: "@nuevoId",
    ///     DbType: System.Data.DbType.Int32,
    ///     Direction: System.Data.ParameterDirection.Output
    /// );
    /// 
    /// // Ejemplo 4: Parámetro decimal con precisión y escala
    /// var param4 = new DbParam(
    ///     Name: "@precio",
    ///     Value: 199.99m,
    ///     DbType: System.Data.DbType.Decimal,
    ///     Precision: 10,
    ///     Scale: 2
    /// );
    /// 
    /// // Ejemplo 5: Parámetro de valor de tabla (TVP) en SQL Server
    /// var param5 = new DbParam(
    ///     Name: "@usuarios",
    ///     Value: dataTableUsuarios,
    ///     DbType: System.Data.DbType.Object,
    ///     TypeName: "dbo.TipoUsuarios"
    /// );
    /// 
    /// // Ejemplo 6: Parámetro nullable
    /// var param6 = new DbParam(
    ///     Name: "@correo",
    ///     Value: null,
    ///     DbType: System.Data.DbType.String,
    ///     IsNullable: true,
    ///     Size: 250
    /// );
    /// </code>
    /// </example>
    public sealed record DbParam(
        string Name,
        object? Value = null,
        DbType? DbType = null,
        ParameterDirection Direction = ParameterDirection.Input,
        int? Size = null,
        byte? Precision = null,
        byte? Scale = null,
        bool? IsNullable = null,
        string? TypeName = null
    );
}