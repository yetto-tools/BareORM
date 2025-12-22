using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace BareORM.Mapping
{
    internal static class RecordSignature
    {
        public static string Build(IDataRecord r, Func<string, string> normalize)
        {
            // Firma estable: N + lista de columnas normalizadas + tipo (si querés, puedes incluir FieldType)
            var sb = new StringBuilder(r.FieldCount * 16);
            sb.Append(r.FieldCount).Append('|');

            for (int i = 0; i < r.FieldCount; i++)
            {
                sb.Append(normalize(r.GetName(i))).Append(':');
                sb.Append(r.GetFieldType(i).FullName).Append('|');
            }

            return sb.ToString();
        }
    }
}
