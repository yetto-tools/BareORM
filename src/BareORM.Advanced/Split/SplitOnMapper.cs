using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using BareORM.Mapping;

namespace BareORM.Advanced.Split
{
    /// <summary>
    /// Split mapping para un SELECT con JOIN:
    /// SELECT u.UserId, u.Email, ..., o.OrderId, o.OrderNumber, ...
    /// splitOn = "OrderId" => todo lo anterior va a T1, desde OrderId en adelante va a T2.
    /// </summary>
    public static class SplitOnMapper
    {
        public static List<TResult> ReadSplit<T1, T2, TResult>(
            IDataReader reader,
            Func<T1, T2, TResult> project,
            MappingOptions? mappingOptions = null,
            SplitOptions? splitOptions = null)
            where T1 : new()
            where T2 : new()
        {
            splitOptions ??= new SplitOptions();
            var results = new List<TResult>();

            var mapper1 = new DefaultEntityMapper<T1>(mappingOptions);
            var mapper2 = new DefaultEntityMapper<T2>(mappingOptions);

            while (reader.Read())
            {
                var rec = (IDataRecord)reader;
                int split = FindSplitIndex(rec, splitOptions);

                var left = new RecordSlice(rec, 0, split);
                var right = new RecordSlice(rec, split, rec.FieldCount - split);

                var a = mapper1.Map(left);
                var b = mapper2.Map(right);

                results.Add(project(a, b));
            }

            return results;
        }

        private static int FindSplitIndex(IDataRecord r, SplitOptions opt)
        {
            var cmp = opt.IgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

            for (int i = 0; i < r.FieldCount; i++)
            {
                if (string.Equals(r.GetName(i), opt.SplitOn, cmp))
                    return i;
            }

            if (opt.Strict)
                throw new InvalidOperationException($"SplitOn column '{opt.SplitOn}' was not found in resultset.");

            return r.FieldCount / 2;
        }

        /// <summary>
        /// “Vista” de un IDataRecord pero con un rango de columnas.
        /// El mapper por nombre funciona porque GetName(i) devuelve el nombre del slice.
        /// </summary>
        private sealed class RecordSlice : IDataRecord
        {
            private readonly IDataRecord _inner;
            private readonly int _offset;
            private readonly int _count;

            public RecordSlice(IDataRecord inner, int offset, int count)
            {
                _inner = inner;
                _offset = offset;
                _count = count;
            }

            public int FieldCount => _count;

            public string GetName(int i) => _inner.GetName(_offset + i);
            public string GetDataTypeName(int i) => _inner.GetDataTypeName(_offset + i);
            public Type GetFieldType(int i) => _inner.GetFieldType(_offset + i);
            public object GetValue(int i) => _inner.GetValue(_offset + i);
            public int GetValues(object[] values)
            {
                var n = Math.Min(values.Length, _count);
                for (int i = 0; i < n; i++) values[i] = GetValue(i);
                return n;
            }

            public int GetOrdinal(string name)
            {
                for (int i = 0; i < _count; i++)
                    if (string.Equals(GetName(i), name, StringComparison.OrdinalIgnoreCase))
                        return i;
                return -1;
            }

            public bool GetBoolean(int i) => (bool)GetValue(i);
            public byte GetByte(int i) => (byte)GetValue(i);
            public long GetBytes(int i, long fieldOffset, byte[]? buffer, int bufferoffset, int length)
                => _inner.GetBytes(_offset + i, fieldOffset, buffer, bufferoffset, length);
            public char GetChar(int i) => (char)GetValue(i);
            public long GetChars(int i, long fieldoffset, char[]? buffer, int bufferoffset, int length)
                => _inner.GetChars(_offset + i, fieldoffset, buffer, bufferoffset, length);
            public Guid GetGuid(int i) => (Guid)GetValue(i);
            public short GetInt16(int i) => (short)GetValue(i);
            public int GetInt32(int i) => (int)GetValue(i);
            public long GetInt64(int i) => (long)GetValue(i);
            public float GetFloat(int i) => (float)GetValue(i);
            public double GetDouble(int i) => (double)GetValue(i);
            public string GetString(int i) => (string)GetValue(i);
            public decimal GetDecimal(int i) => (decimal)GetValue(i);
            public DateTime GetDateTime(int i) => (DateTime)GetValue(i);
            public IDataReader GetData(int i) => _inner.GetData(_offset + i);
            public bool IsDBNull(int i) => _inner.IsDBNull(_offset + i);

            public object this[int i] => GetValue(i);
            public object this[string name] => GetValue(GetOrdinal(name));
        }
    }
}
