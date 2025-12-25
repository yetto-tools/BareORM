using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using BareORM.Abstractions;

namespace BareORM.SqlServer;

/// <summary>
/// Factory de comandos para SQL Server: construye <see cref="CommandDefinition"/> y parámetros <see cref="DbParameter"/>.
/// </summary>
/// <remarks>
/// <para>
/// Esta clase implementa <see cref="ICommandFactory"/> y se encarga de:
/// </para>
/// <list type="bullet">
/// <item><description>Crear <see cref="CommandDefinition"/> (texto, tipo, parámetros, timeout).</description></item>
/// <item><description>Convertir parámetros desde distintos formatos hacia <see cref="DbParameter"/>.</description></item>
/// <item><description>Soportar <see cref="DbParam"/> (wrapper agnóstico), POCO/anonymous objects, y parámetros ya construidos.</description></item>
/// <item><description>Aplicar convención: todos los nombres llevan prefijo <c>@</c>.</description></item>
/// <item><description>Soportar TVP (Table-Valued Parameters) via <see cref="SqlParameter.TypeName"/> y <see cref="SqlDbType.Structured"/>.</description></item>
/// </list>
/// <para>
/// Entradas aceptadas para parámetros:
/// </para>
/// <list type="bullet">
/// <item><description><see cref="IEnumerable{DbParam}"/>: se mapean campo a campo.</description></item>
/// <item><description><see cref="DbParameter"/> o <see cref="IEnumerable{DbParameter}"/>: se respetan tal cual.</description></item>
/// <item><description>POCO/anonymous object: se hace reflection simple de propiedades → <see cref="SqlParameter"/>.</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// var f = new SqlServerCommandFactory();
///
/// // 1) POCO / anonymous
/// var cmd1 = f.Create("dbo.sp_UserById", CommandType.StoredProcedure, new { Id = userId });
///
/// // 2) DbParam (incluye output y tipos)
/// var cmd2 = f.Create("dbo.sp_CreateUser", CommandType.StoredProcedure, new[]
/// {
///     new DbParam("Email", "a@b.com"),
///     new DbParam("NewId", null) { Direction = ParameterDirection.Output, DbType = DbType.Guid }
/// });
///
/// // 3) TVP
/// var dt = Helpers.BuildItemsTvp(items);
/// var cmd3 = f.Create("dbo.sp_SaveItems", CommandType.StoredProcedure, new[]
/// {
///     new DbParam("Items", dt) { TypeName = "dbo.ItemTvp" } // => Structured
/// });
/// </code>
/// </example>
public sealed class SqlServerCommandFactory : ICommandFactory
{
    /// <summary>
    /// Crea un <see cref="CommandDefinition"/> con parámetros opcionales.
    /// </summary>
    /// <param name="commandText">Nombre del SP o SQL, según <paramref name="commandType"/>.</param>
    /// <param name="commandType">Tipo de comando (default: StoredProcedure).</param>
    /// <param name="parameters">
    /// Parámetros en alguno de estos formatos:
    /// <list type="bullet">
    /// <item><description><see cref="IEnumerable{DbParam}"/></description></item>
    /// <item><description><see cref="DbParameter"/> o <see cref="IEnumerable{DbParameter}"/></description></item>
    /// <item><description>POCO/anonymous object (propiedades → parámetros)</description></item>
    /// </list>
    /// </param>
    /// <param name="timeoutSeconds">Timeout en segundos (default: 30).</param>
    /// <returns>Definición del comando lista para ejecutar por el provider.</returns>
    public CommandDefinition Create(
        string commandText,
        CommandType commandType = CommandType.StoredProcedure,
        object? parameters = null,
        int timeoutSeconds = 30)
    {
        IReadOnlyList<DbParameter>? dbParams = null;

        if (parameters is not null)
        {
            if (parameters is IEnumerable<DbParam> p2) dbParams = CreateParameters(p2);
            else dbParams = CreateParameters(parameters);
        }

        return new CommandDefinition(commandText, commandType, dbParams, timeoutSeconds);
    }

    /// <summary>
    /// Crea un <see cref="CommandDefinition"/> recibiendo parámetros como <see cref="DbParam"/>.
    /// </summary>
    public CommandDefinition Create(
        string commandText,
        CommandType commandType,
        IEnumerable<DbParam> parameters,
        int timeoutSeconds = 30)
        => new(commandText, commandType, CreateParameters(parameters), timeoutSeconds);

    /// <summary>
    /// Construye parámetros desde un objeto (POCO/anonymous) o desde <see cref="DbParameter"/> ya existente.
    /// </summary>
    /// <param name="parameters">Objeto con propiedades o parámetros ya construidos.</param>
    /// <returns>Lista de <see cref="DbParameter"/> lista para SQL Server.</returns>
    /// <remarks>
    /// Si se provee un POCO/anonymous, cada propiedad se convierte a <see cref="SqlParameter"/>:
    /// <c>@{PropName}</c> = valor (o <see cref="DBNull.Value"/> si null).
    /// </remarks>
    public IReadOnlyList<DbParameter> CreateParameters(object parameters)
    {
        // Si ya te pasan DbParameter(s), los respetamos
        if (parameters is DbParameter single)
            return new[] { single };

        if (parameters is IEnumerable<DbParameter> many)
            return many.ToList();

        // Anonymous/POCO => reflection simple
        var props = parameters.GetType().GetProperties();
        var list = new List<DbParameter>(props.Length);

        foreach (var prop in props)
        {
            var name = EnsureAtPrefix(prop.Name);
            var value = prop.GetValue(parameters) ?? DBNull.Value;
            list.Add(new SqlParameter(name, value));
        }

        return list;
    }

    /// <summary>
    /// Construye parámetros desde una secuencia de <see cref="DbParam"/> (wrapper agnóstico).
    /// </summary>
    public IReadOnlyList<DbParameter> CreateParameters(IEnumerable<DbParam> parameters)
        => parameters.Select(CreateParameter).ToList();

    /// <summary>
    /// Convierte un <see cref="DbParam"/> a un <see cref="SqlParameter"/> de SQL Server.
    /// </summary>
    /// <param name="param">Definición agnóstica del parámetro.</param>
    /// <returns>Parámetro de SQL Server.</returns>
    /// <remarks>
    /// <para>
    /// Soporta: <see cref="DbParam.DbType"/>, <see cref="DbParam.Size"/>, <see cref="DbParam.Precision"/>,
    /// <see cref="DbParam.Scale"/>, <see cref="DbParam.IsNullable"/>, y <see cref="DbParam.TypeName"/>.
    /// </para>
    /// <para>
    /// TVP: si <see cref="DbParam.TypeName"/> está definido y el <see cref="DbParam.Value"/> es <see cref="DataTable"/>,
    /// se asigna <see cref="SqlDbType.Structured"/> automáticamente.
    /// </para>
    /// </remarks>
    public DbParameter CreateParameter(DbParam param)
    {
        var p = new SqlParameter
        {
            ParameterName = EnsureAtPrefix(param.Name),
            Direction = param.Direction,
            Value = param.Value ?? DBNull.Value
        };

        if (param.DbType.HasValue)
            p.DbType = param.DbType.Value;

        if (param.Size.HasValue)
            p.Size = param.Size.Value;

        if (param.Precision.HasValue)
            p.Precision = param.Precision.Value;

        if (param.Scale.HasValue)
            p.Scale = param.Scale.Value;

        if (param.IsNullable.HasValue)
            p.IsNullable = param.IsNullable.Value;

        // TVP/UDT TypeName (SQL Server)
        if (!string.IsNullOrWhiteSpace(param.TypeName))
        {
            p.TypeName = param.TypeName;

            // Si parece TVP, ayuda a setear SqlDbType
            if (param.Value is DataTable)
                p.SqlDbType = SqlDbType.Structured;
        }

        return p;
    }

    /// <summary>
    /// Asegura que el nombre del parámetro tenga el prefijo <c>@</c>.
    /// </summary>
    private static string EnsureAtPrefix(string name)
        => name.StartsWith("@", StringComparison.Ordinal) ? name : "@" + name;
}
