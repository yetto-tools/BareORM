using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using BareORM.Abstractions;

namespace BareORM.SqlServer;

public sealed class SqlServerCommandFactory : ICommandFactory
{
    public CommandDefinition Create(string commandText, CommandType commandType = CommandType.StoredProcedure, object? parameters = null, int timeoutSeconds = 30)
    {
        IReadOnlyList<DbParameter>? dbParams = null;

        if (parameters is not null)
        {
            if (parameters is IEnumerable<DbParam> p2) dbParams = CreateParameters(p2);
            else dbParams = CreateParameters(parameters);
        }

        return new CommandDefinition(commandText, commandType, dbParams, timeoutSeconds);
    }

    public CommandDefinition Create(string commandText, CommandType commandType, IEnumerable<DbParam> parameters, int timeoutSeconds = 30)
        => new(commandText, commandType, CreateParameters(parameters), timeoutSeconds);

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

    public IReadOnlyList<DbParameter> CreateParameters(IEnumerable<DbParam> parameters)
        => parameters.Select(CreateParameter).ToList();

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

    private static string EnsureAtPrefix(string name)
        => name.StartsWith("@", StringComparison.Ordinal) ? name : "@" + name;
}
