using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BareORM.Migrations.DbAssets
{
    /// <summary>
    /// Proveedor de assets SQL basado en archivos del sistema (carpeta).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Lee archivos <c>.sql</c> desde un directorio raíz y los expone como <see cref="DbAsset"/> para que
    /// el motor de migraciones pueda crear/actualizar objetos (views, procedures, functions, triggers).
    /// </para>
    /// <para>
    /// Convención de carpetas (case-insensitive):
    /// </para>
    /// <list type="bullet">
    /// <item><description><c>DbAssets/Procedures/dbo.sp_ListarUsuarios.sql</c> → <see cref="DbAssetKind.Procedure"/></description></item>
    /// <item><description><c>DbAssets/Views/dbo.vw_UsuariosActivos.sql</c> → <see cref="DbAssetKind.View"/></description></item>
    /// <item><description><c>DbAssets/FunctionsScalar/dbo.fn_X.sql</c> → <see cref="DbAssetKind.ScalarFunction"/></description></item>
    /// <item><description><c>DbAssets/FunctionsTable/dbo.tvf_Y.sql</c> → <see cref="DbAssetKind.TableFunction"/></description></item>
    /// <item><description><c>DbAssets/Triggers/dbo.trg_X.sql</c> → <see cref="DbAssetKind.Trigger"/></description></item>
    /// </list>
    /// <para>
    /// Convención de nombres de archivo:
    /// </para>
    /// <list type="bullet">
    /// <item><description><c>dbo.sp_X.sql</c> → schema=<c>dbo</c>, name=<c>sp_X</c></description></item>
    /// <item><description><c>sp_X.sql</c> → schema default=<c>dbo</c>, name=<c>sp_X</c></description></item>
    /// </list>
    /// <para>
    /// Nota: si el tipo no puede inferirse por la ruta, se usa <see cref="DbAssetKind.Procedure"/> como default “seguro”.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var provider = new FileSystemAssetProvider(Path.Combine(AppContext.BaseDirectory, "DbAssets"));
    /// foreach (var asset in provider.GetAssets())
    ///     Console.WriteLine($"{asset.Kind}: {asset.Schema}.{asset.Name}");
    /// </code>
    /// </example>
    public sealed class FileSystemAssetProvider : IDbAssetProvider
    {
        private readonly string _rootDir;

        /// <summary>
        /// Inicializa el provider apuntando a un directorio raíz.
        /// </summary>
        /// <param name="rootDir">Directorio donde viven los assets (.sql).</param>
        public FileSystemAssetProvider(string rootDir) => _rootDir = rootDir;

        /// <summary>
        /// Enumeración de assets encontrados en el filesystem.
        /// </summary>
        /// <returns>
        /// Secuencia de <see cref="DbAsset"/>. Si el directorio no existe, devuelve vacío.
        /// </returns>
        /// <remarks>
        /// Lee contenido en UTF-8 (<see cref="Encoding.UTF8"/>).
        /// </remarks>
        public IEnumerable<DbAsset> GetAssets()
        {
            if (!Directory.Exists(_rootDir)) yield break;

            foreach (var file in Directory.EnumerateFiles(_rootDir, "*.sql", SearchOption.AllDirectories))
            {
                var kind = InferKind(file);
                var (schema, name) = InferSchemaAndName(Path.GetFileNameWithoutExtension(file));
                var sql = File.ReadAllText(file, Encoding.UTF8);

                yield return new DbAsset(schema, name, kind, sql);
            }
        }

        /// <summary>
        /// Infiera el <see cref="DbAssetKind"/> desde la ruta del archivo usando convención de carpetas.
        /// </summary>
        /// <param name="path">Ruta del archivo.</param>
        /// <returns>Tipo inferido.</returns>
        /// <remarks>
        /// La comparación es case-insensitive y normaliza separadores a <c>/</c>.
        /// </remarks>
        private static DbAssetKind InferKind(string path)
        {
            var p = path.Replace('\\', '/').ToLowerInvariant();
            if (p.Contains("/views/")) return DbAssetKind.View;
            if (p.Contains("/procedures/")) return DbAssetKind.Procedure;
            if (p.Contains("/functionsscalar/")) return DbAssetKind.ScalarFunction;
            if (p.Contains("/functionstable/")) return DbAssetKind.TableFunction;
            if (p.Contains("/triggers/")) return DbAssetKind.Trigger;

            // default seguro
            return DbAssetKind.Procedure;
        }

        /// <summary>
        /// Infiera schema y nombre del objeto desde el nombre del archivo (sin extensión).
        /// </summary>
        /// <param name="fileNameNoExt">Nombre del archivo sin extensión, p.ej. <c>dbo.sp_X</c> o <c>sp_X</c>.</param>
        /// <returns>Tupla (schema, name).</returns>
        /// <remarks>
        /// Soporta:
        /// <list type="bullet">
        /// <item><description><c>dbo.sp_X</c> → (<c>dbo</c>, <c>sp_X</c>)</description></item>
        /// <item><description><c>sp_X</c> → (<c>dbo</c>, <c>sp_X</c>)</description></item>
        /// </list>
        /// </remarks>
        private static (string schema, string name) InferSchemaAndName(string fileNameNoExt)
        {
            // soporta "dbo.sp_X" o solo "sp_X"
            var parts = fileNameNoExt.Split('.', 2);
            if (parts.Length == 2) return (parts[0], parts[1]);
            return ("dbo", fileNameNoExt);
        }
    }
}
