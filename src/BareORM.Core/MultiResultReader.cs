using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using BareORM.Abstractions;

namespace BareORM.Core
{
    public sealed class MultiResultReader : IDisposable
    {
        private readonly DbResult<IDataReader> _wrapped;
        private readonly IDataReader _reader;

        public IReadOnlyDictionary<string, object?>? OutputValues => _wrapped.OutputValues;
        public int RecordsAffected => _wrapped.RecordsAffected;

        public MultiResultReader(DbResult<IDataReader> wrapped)
        {
            _wrapped = wrapped;
            _reader = wrapped.Data;
        }

        public List<T> Read<T>(Func<IDataRecord, T> map)
        {
            var list = new List<T>();
            while (_reader.Read())
                list.Add(map(_reader));

            _reader.NextResult();
            return list;
        }

        public void Dispose()
            => _reader.Dispose();
    }
}
