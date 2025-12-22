using System.Data;
using BareORM.Abstractions;
using BareORM.Mapping;

namespace BareORM.Advanced.MultiResult
{
    public static class MultiResultReaderExtensions
    {
        /// <summary>
        /// Wrap para leer múltiples resultsets en forma tipada.
        /// </summary>
        public static ResultSetReader AsResultSetReader(this IDataReader reader, MappingOptions? options = null)
            => new(reader, options);

        /// <summary>
        /// Helper: lee el resultset actual como List&lt;T&gt; y NO hace NextResult automáticamente.
        /// </summary>
        public static List<T> ReadResultSet<T>(this IDataReader reader, MappingOptions? options = null, IEntityMapper<T>? mapper = null)
            where T : new()
        {
            mapper ??= new DefaultEntityMapper<T>(options);
            return ((IDataReader)reader).ReadAll(mapper);
        }

        /// <summary>
        /// Helper: lee solo la primera fila del resultset actual y NO hace NextResult automáticamente.
        /// </summary>
        public static T? ReadSingleResult<T>(this IDataReader reader, MappingOptions? options = null, IEntityMapper<T>? mapper = null)
            where T : new()
        {
            mapper ??= new DefaultEntityMapper<T>(options);
            return ((IDataReader)reader).ReadFirstOrDefault(mapper);
        }
    }
}
