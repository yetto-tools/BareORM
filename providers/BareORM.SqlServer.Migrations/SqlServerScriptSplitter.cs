using System;
using System.Collections.Generic;

namespace BareORM.SqlServer.Migrations
{
    /// <summary>
    /// Utilidad para dividir scripts T-SQL en lotes (batches) usando el separador <c>GO</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// En SQL Server, <c>GO</c> no es un comando del motor, sino un separador reconocido por herramientas
    /// como SSMS / sqlcmd. Para ejecutar scripts con <c>GO</c> desde código, necesitas partir el script
    /// en lotes y ejecutar cada lote por separado.
    /// </para>
    /// <para>
    /// Reglas de este splitter:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Normaliza saltos de línea a <c>\n</c>.</description></item>
    /// <item><description>Considera separador solo si la línea (trim) es exactamente <c>GO</c> (case-insensitive).</description></item>
    /// <item><description>Ignora lotes vacíos (solo whitespace).</description></item>
    /// </list>
    /// <para>
    /// Limitación intencional: no soporta variantes como <c>GO 10</c> (repetición) ni detección dentro de strings/comentarios.
    /// Está pensado para scripts simples/estándar generados por migraciones.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var sql = @"
    /// CREATE TABLE dbo.Users(Id uniqueidentifier NOT NULL);
    /// GO
    /// CREATE INDEX IX_Users_Id ON dbo.Users(Id);
    /// GO
    /// ";
    ///
    /// var batches = SqlServerScriptSplitter.SplitBatches(sql);
    /// foreach (var b in batches)
    ///     Console.WriteLine("--- BATCH ---\n" + b);
    /// </code>
    /// </example>
    public static class SqlServerScriptSplitter
    {
        /// <summary>
        /// Divide un script T-SQL en lotes independientes usando líneas con <c>GO</c>.
        /// </summary>
        /// <param name="sql">Script completo.</param>
        /// <returns>Lista de lotes, listos para ejecutarse secuencialmente.</returns>
        public static IReadOnlyList<string> SplitBatches(string sql)
        {
            sql = sql.Replace("\r\n", "\n").Replace("\r", "\n");

            var batches = new List<string>();
            var lines = sql.Split('\n');

            var cur = new List<string>();
            foreach (var line in lines)
            {
                if (line.Trim().Equals("GO", StringComparison.OrdinalIgnoreCase))
                {
                    Flush();
                    continue;
                }
                cur.Add(line);
            }
            Flush();

            return batches;

            void Flush()
            {
                var text = string.Join('\n', cur).Trim();
                cur.Clear();
                if (!string.IsNullOrWhiteSpace(text))
                    batches.Add(text);
            }
        }
    }
}
