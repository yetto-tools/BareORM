using System;
using System.Collections.Generic;
using System.Data;

namespace BareORM.SqlServer
{
    /// <summary>
    /// Utilidades de conveniencia para demos/tests con SQL Server.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Esta clase agrupa helpers comunes para:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Construir <see cref="DataTable"/> para TVP (Table-Valued Parameters).</description></item>
    /// <item><description>Imprimir outputs de stored procedures.</description></item>
    /// <item><description>Extraer valores tipados desde diccionarios de outputs.</description></item>
    /// <item><description>Imprimir contenido de un <see cref="DataTable"/> para debugging.</description></item>
    /// </list>
    /// </remarks>
    public static class Helpers
    {
        // =============================
        // Helpers
        // =============================

        /// <summary>
        /// Construye un <see cref="DataTable"/> compatible con un TVP de items (SKU, Qty, UnitPrice).
        /// </summary>
        /// <param name="items">
        /// Secuencia de tuplas <c>(sku, qty, unitPrice)</c> que serán convertidas a filas.
        /// </param>
        /// <returns>
        /// Un <see cref="DataTable"/> con columnas: <c>SKU</c> (string), <c>Qty</c> (int), <c>UnitPrice</c> (decimal).
        /// </returns>
        /// <remarks>
        /// <para>
        /// Uso típico: pasar este DataTable como parámetro estructurado (TVP) hacia SQL Server.
        /// Asegúrate que el tipo de tabla en SQL Server tenga el mismo orden y tipos de columnas.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var items = new[]
        /// {
        ///     (sku: "A-001", qty: 2, unitPrice: 19.99m),
        ///     (sku: "B-777", qty: 1, unitPrice: 5.50m),
        /// };
        ///
        /// var tvp = Helpers.BuildItemsTvp(items);
        /// Helpers.PrintDataTable(tvp);
        /// </code>
        /// </example>
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

        /// <summary>
        /// Imprime en consola un diccionario de valores de salida (outputs) de una ejecución.
        /// </summary>
        /// <param name="outputs">
        /// Diccionario de outputs. Si es null o está vacío, imprime <c>(no output values)</c>.
        /// </param>
        /// <remarks>
        /// Pensado para debugging rápido en samples/benchmarks.
        /// </remarks>
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

        /// <summary>
        /// Lee un output por nombre y lo convierte a <see cref="int"/>.
        /// </summary>
        /// <param name="outputs">Diccionario de outputs.</param>
        /// <param name="name">Nombre de la clave a buscar (case-sensitive según el diccionario).</param>
        /// <returns>
        /// El valor convertido a int, o 0 si <paramref name="outputs"/> es null, la clave no existe o el valor es null.
        /// </returns>
        /// <remarks>
        /// Usa <see cref="Convert.ToInt32(object)"/>; si el valor no es convertible, lanzará excepción.
        /// </remarks>
        public static int GetInt(IReadOnlyDictionary<string, object?>? outputs, string name)
        {
            if (outputs is null) return 0;
            if (!outputs.TryGetValue(name, out var v) || v is null) return 0;
            return Convert.ToInt32(v);
        }

        /// <summary>
        /// Lee un output por nombre y lo convierte a <see cref="decimal"/>.
        /// </summary>
        /// <param name="outputs">Diccionario de outputs.</param>
        /// <param name="name">Nombre de la clave a buscar (case-sensitive según el diccionario).</param>
        /// <returns>
        /// El valor convertido a decimal, o 0m si <paramref name="outputs"/> es null, la clave no existe o el valor es null.
        /// </returns>
        /// <remarks>
        /// Usa <see cref="Convert.ToDecimal(object)"/>; si el valor no es convertible, lanzará excepción.
        /// </remarks>
        public static decimal GetDecimal(IReadOnlyDictionary<string, object?>? outputs, string name)
        {
            if (outputs is null) return 0m;
            if (!outputs.TryGetValue(name, out var v) || v is null) return 0m;
            return Convert.ToDecimal(v);
        }

        /// <summary>
        /// Imprime un <see cref="DataTable"/> a consola como tabla simple (headers + filas).
        /// </summary>
        /// <param name="dt">DataTable a imprimir.</param>
        /// <param name="maxRows">
        /// Máximo de filas a imprimir. Si el DataTable tiene más, se imprime un resumen “... (n more rows)”.
        /// </param>
        /// <remarks>
        /// Útil para debugging de resultados sin depender de visualizadores.
        /// </remarks>
        /// <example>
        /// <code>
        /// Helpers.PrintDataTable(dt, maxRows: 20);
        /// </code>
        /// </example>
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
