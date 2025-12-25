using System.Text.Json;

namespace BareORM.Migrations.Tooling
{
    /// <summary>
    /// Opciones para definir/ajustar la estructura estándar de carpetas y archivos del proyecto de migraciones.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Este layout está pensado para que el tooling encuentre fácilmente:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Assets SQL (programmables) en <c>DbAssets/...</c></description></item>
    /// <item><description>Migraciones y archivos de soporte en <c>Migrations/</c></description></item>
    /// <item><description>Snapshot para detectar cambios en programmables.</description></item>
    /// <item><description>Manifest para indexar migraciones generadas.</description></item>
    /// </list>
    /// </remarks>
    public sealed class ProjectLayoutOptions
    {
        /// <summary>
        /// Nombre del directorio raíz donde viven los assets SQL.
        /// </summary>
        /// <remarks>Default: <c>DbAssets</c>.</remarks>
        public string AssetsDirName { get; init; } = "DbAssets";

        /// <summary>
        /// Nombre del directorio raíz donde viven las migraciones (y archivos de tooling).
        /// </summary>
        /// <remarks>Default: <c>Migrations</c>.</remarks>
        public string MigrationsDirName { get; init; } = "Migrations";

        /// <summary>
        /// Subcarpetas estándar dentro de <see cref="AssetsDirName"/> para objetos programables.
        /// </summary>
        /// <remarks>
        /// Default:
        /// <list type="bullet">
        /// <item><description><c>Procedures</c></description></item>
        /// <item><description><c>Views</c></description></item>
        /// <item><description><c>FunctionsScalar</c></description></item>
        /// <item><description><c>FunctionsTable</c></description></item>
        /// <item><description><c>Triggers</c></description></item>
        /// </list>
        /// </remarks>
        public string[] AssetSubdirs { get; init; } = new[]
        {
            "Procedures",
            "Views",
            "FunctionsScalar",
            "FunctionsTable",
            "Triggers"
        };

        /// <summary>
        /// Nombre del archivo snapshot de programmables (hashes).
        /// </summary>
        /// <remarks>Default: <c>programmables.snapshot.json</c>.</remarks>
        public string SnapshotFileName { get; init; } = "programmables.snapshot.json";

        /// <summary>
        /// Nombre del archivo manifest de migraciones (opcional, para tooling).
        /// </summary>
        /// <remarks>Default: <c>migrations.manifest.json</c>.</remarks>
        public string ManifestFileName { get; init; } = "migrations.manifest.json";
    }

    /// <summary>
    /// Utilidad para asegurar (crear si falta) la estructura de directorios y archivos de migraciones/assets del proyecto.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="Ensure(string, ProjectLayoutOptions?)"/> crea:
    /// </para>
    /// <list type="bullet">
    /// <item><description><c>{ProjectRoot}/{AssetsDirName}/</c> y sus subcarpetas.</description></item>
    /// <item><description><c>{ProjectRoot}/{MigrationsDirName}/</c>.</description></item>
    /// <item><description>Archivo snapshot vacío si no existe.</description></item>
    /// <item><description>Archivo manifest vacío si no existe.</description></item>
    /// </list>
    /// <para>
    /// Retorna un <see cref="ProjectLayoutInfo"/> con rutas absolutas calculadas.
    /// </para>
    /// </remarks>
    public static class ProjectLayout
    {
        /// <summary>
        /// Asegura que el layout del proyecto exista (carpetas/archivos mínimos) y devuelve las rutas resultantes.
        /// </summary>
        /// <param name="projectRoot">Raíz del proyecto.</param>
        /// <param name="options">Opciones de layout. Si es null se usan defaults.</param>
        /// <returns>Información de rutas del layout creado/encontrado.</returns>
        /// <remarks>
        /// <para>
        /// Snapshot inicial (si no existe) se crea con:
        /// <c>{ "items": [] }</c>
        /// </para>
        /// <para>
        /// Manifest inicial (si no existe) se crea con:
        /// <c>{ "scope": "...", "createdAtUtc": "...", "migrations": [] }</c>
        /// </para>
        /// </remarks>
        public static ProjectLayoutInfo Ensure(string projectRoot, ProjectLayoutOptions? options = null)
        {
            options ??= new ProjectLayoutOptions();

            var assetsRoot = Path.Combine(projectRoot, options.AssetsDirName);
            var migrationsRoot = Path.Combine(projectRoot, options.MigrationsDirName);

            Directory.CreateDirectory(assetsRoot);
            Directory.CreateDirectory(migrationsRoot);

            foreach (var s in options.AssetSubdirs)
                Directory.CreateDirectory(Path.Combine(assetsRoot, s));

            // Snapshot (si no existe)
            var snapshotPath = Path.Combine(migrationsRoot, options.SnapshotFileName);
            if (!File.Exists(snapshotPath))
            {
                var empty = new { items = Array.Empty<object>() };
                File.WriteAllText(
                    snapshotPath,
                    JsonSerializer.Serialize(empty, new JsonSerializerOptions { WriteIndented = true })
                );
            }

            // Manifest local (si no existe)
            var manifestPath = Path.Combine(migrationsRoot, options.ManifestFileName);
            if (!File.Exists(manifestPath))
            {
                var empty = new
                {
                    scope = "BareORM.Migrations",
                    createdAtUtc = DateTime.UtcNow,
                    migrations = Array.Empty<object>()
                };
                File.WriteAllText(
                    manifestPath,
                    JsonSerializer.Serialize(empty, new JsonSerializerOptions { WriteIndented = true })
                );
            }

            return new ProjectLayoutInfo(projectRoot, assetsRoot, migrationsRoot, snapshotPath, manifestPath);
        }
    }

    /// <summary>
    /// Resultado de <see cref="ProjectLayout.Ensure"/>: rutas del layout del proyecto.
    /// </summary>
    /// <param name="ProjectRoot">Raíz del proyecto.</param>
    /// <param name="AssetsRoot">Ruta absoluta a la carpeta raíz de assets.</param>
    /// <param name="MigrationsRoot">Ruta absoluta a la carpeta raíz de migraciones.</param>
    /// <param name="SnapshotPath">Ruta absoluta al archivo snapshot.</param>
    /// <param name="ManifestPath">Ruta absoluta al archivo manifest.</param>
    public sealed record ProjectLayoutInfo(
        string ProjectRoot,
        string AssetsRoot,
        string MigrationsRoot,
        string SnapshotPath,
        string ManifestPath
    );
}
