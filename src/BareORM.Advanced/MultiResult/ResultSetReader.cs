using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using BareORM.Mapping;

namespace BareORM.Advanced.MultiResult
{
    /// <summary>
    /// Reader tipado para SPs con múltiples SELECT.
    /// Uso:
    /// using var rs = ((IDataReader)meta.Data).AsResultSetReader(options);
    /// var a = rs.Read&lt;A&gt;();
    /// rs.NextResult();
    /// var b = rs.Read&lt;B&gt;();
    /// </summary>
    public sealed class ResultSetReader : IDisposable
    {
        private readonly IDataReader _reader;
        private readonly MappingOptions? _options;

        public ResultSetReader(IDataReader reader, MappingOptions? options = null)
        {
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));
            _options = options;
        }

        public List<T> Read<T>() where T : new()
        {
            var mapper = new DefaultEntityMapper<T>(_options);
            return _reader.ReadAll(mapper);
        }

        public T? ReadSingle<T>() where T : new()
        {
            var mapper = new DefaultEntityMapper<T>(_options);
            return _reader.ReadFirstOrDefault(mapper);
        }

        public bool NextResult() => _reader.NextResult();

        public void Dispose() => _reader.Dispose();
    }

}
