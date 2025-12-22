using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using BareORM.Abstractions;

namespace BareORM.SqlServer
{
    public static class Helpers
    {
        // =============================
        // Helpers
        // =============================
        public static DataTable BuildItemsTvp(IEnumerable<(string sku, int qty, decimal unitPrice)> items)
        {
            var dt = new DataTable();
            dt.Columns.Add("SKU", typeof(string));
            dt.Columns.Add("Qty", typeof(int));
            dt.Columns.Add("UnitPrice", typeof(decimal));

            foreach (var (sku, qty, unitPrice) in items)
                dt.Rows.Add(sku, qty, unitPrice);

            return dt;
        }

        public static void DumpOutputs(IReadOnlyDictionary<string, object?>? outputs)
        {
            if (outputs is null || outputs.Count == 0)
            {
                Console.WriteLine("(no output values)");
                return;
            }

            Console.WriteLine("OutputValues:");
            foreach (var kv in outputs)
                Console.WriteLine($"  {kv.Key} = {kv.Value}");
        }

        public static int GetInt(IReadOnlyDictionary<string, object?>? outputs, string name)
        {
            if (outputs is null) return 0;
            if (!outputs.TryGetValue(name, out var v) || v is null) return 0;
            return Convert.ToInt32(v);
        }

        public static decimal GetDecimal(IReadOnlyDictionary<string, object?>? outputs, string name)
        {
            if (outputs is null) return 0m;
            if (!outputs.TryGetValue(name, out var v) || v is null) return 0m;
            return Convert.ToDecimal(v);
        }

        public static void PrintDataTable(DataTable dt, int maxRows = 10)
        {
            Console.WriteLine($"DataTable: rows={dt.Rows.Count}, cols={dt.Columns.Count}");

            // headers
            for (int c = 0; c < dt.Columns.Count; c++)
                Console.Write(dt.Columns[c].ColumnName + (c == dt.Columns.Count - 1 ? "" : " | "));
            Console.WriteLine();

            // rows
            int take = Math.Min(maxRows, dt.Rows.Count);
            for (int r = 0; r < take; r++)
            {
                for (int c = 0; c < dt.Columns.Count; c++)
                {
                    var val = dt.Rows[r][c];
                    Console.Write(val + (c == dt.Columns.Count - 1 ? "" : " | "));
                }
                Console.WriteLine();
            }

            if (dt.Rows.Count > take)
                Console.WriteLine($"... ({dt.Rows.Count - take} more rows)");
        }


    }
}
