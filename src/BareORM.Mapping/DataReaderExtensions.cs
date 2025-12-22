using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using BareORM.Abstractions;

namespace BareORM.Mapping
{
    public static class DataReaderExtensions
    {
        public static List<T> ReadAll<T>(this IDataReader reader, IEntityMapper<T> mapper)
        {
            var list = new List<T>();
            while (reader.Read())
                list.Add(mapper.Map((IDataRecord)reader));
            return list;
        }

        public static T? ReadFirstOrDefault<T>(this IDataReader reader, IEntityMapper<T> mapper)
        {
            if (!reader.Read()) return default;
            return mapper.Map((IDataRecord)reader);
        }
    }
}
