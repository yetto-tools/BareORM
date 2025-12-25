using System;
using System.Security.Cryptography;
using System.Text;

namespace BareORM.Migrations.DbAssets
{
    /// <summary>
    /// Calcula un hash estable para “assets” SQL (scripts embebidos, archivos .sql, etc.)
    /// usando normalización simple + SHA-256.
    /// </summary>
    /// <remarks>
    /// <para>
    /// La idea es detectar cambios relevantes en el contenido SQL evitando falsos positivos por:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Diferencias de saltos de línea (CRLF vs LF).</description></item>
    /// <item><description>Espacios al final de línea.</description></item>
    /// <item><description>Whitespace extra al inicio/fin del archivo.</description></item>
    /// </list>
    /// <para>
    /// Importante: esta normalización es “barata” a propósito. No intenta parsear SQL ni eliminar comentarios,
    /// por lo que cambios en comentarios o en espacios internos (no trailing) sí pueden cambiar el hash.
    /// </para>
    /// </remarks>
    public static class AssetHasher
    {
        /// <summary>
        /// Calcula el hash SHA-256 (hex) del SQL normalizado.
        /// </summary>
        /// <param name="sql">Contenido SQL.</param>
        /// <returns>Hash en hexadecimal (uppercase) del SQL normalizado.</returns>
        /// <remarks>
        /// Internamente llama a <see cref="Normalize(string)"/> antes de calcular el SHA-256.
        /// </remarks>
        /// <example>
        /// <code>
        /// var hash = AssetHasher.Hash(File.ReadAllText("001_init.sql"));
        /// Console.WriteLine(hash);
        /// </code>
        /// </example>
        public static string Hash(string sql)
        {
            // normalización barata (suficiente para detectar cambios)
            var normalized = Normalize(sql);
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
            return Convert.ToHexString(bytes);
        }

        /// <summary>
        /// Normaliza SQL para comparación/hashing.
        /// </summary>
        /// <param name="sql">Contenido SQL.</param>
        /// <returns>
        /// SQL normalizado:
        /// <list type="number">
        /// <item><description>Trim completo del string.</description></item>
        /// <item><description>Normalización de saltos a <c>\n</c>.</description></item>
        /// <item><description>Elimina espacios al final de cada línea (<c>TrimEnd</c>).</description></item>
        /// <item><description>Trim final del resultado.</description></item>
        /// </list>
        /// </returns>
        /// <remarks>
        /// No elimina comentarios ni colapsa espacios internos; su objetivo es estabilidad mínima sin costo alto.
        /// </remarks>
        public static string Normalize(string sql)
        {
            // 1) trim
            sql = sql.Trim();

            // 2) normaliza saltos
            sql = sql.Replace("\r\n", "\n").Replace("\r", "\n");

            // 3) quita trailing spaces por línea
            var lines = sql.Split('\n').Select(l => l.TrimEnd());
            return string.Join('\n', lines).Trim();
        }
    }
}
