using System.Text.Json;

namespace BareORM.Migrations.Tooling
{
    /// <summary>
    /// Escribe/actualiza el manifest JSON de migraciones agregando un nuevo registro al final.
    /// </summary>
    /// <remarks>
    /// <para>
    /// El manifest es un archivo JSON usado por tooling para llevar un índice de migraciones generadas/aplicadas,
    /// normalmente con estructura similar a:
    /// </para>
    /// <code language="json">
    /// {
    ///   "scope": "BareORM.Migrations",
    ///   "updatedAtUtc": "2025-12-25T15:00:00Z",
    ///   "migrations": [
    ///     { "id": "...", "name": "...", "ops": 3, "file": "Migrations/....cs", "createdAtUtc": "..." }
    ///   ]
    /// }
    /// </code>
    /// <para>
    /// <see cref="Append"/> lee el manifest existente, conserva <c>scope</c> y el arreglo <c>migrations</c>,
    /// agrega un nuevo item y re-escribe el archivo con <c>WriteIndented</c> para facilitar diffs.
    /// </para>
    /// <para>
    /// Nota: este método asume que <paramref name="manifestPath"/> existe y contiene JSON válido.
    /// Si necesitas “create if missing”, lo ideal es agregar una capa arriba o extender esta clase.
    /// </para>
    /// </remarks>
    public static class ManifestWriter
    {
        /// <summary>
        /// Agrega una entrada nueva al archivo manifest de migraciones.
        /// </summary>
        /// <param name="manifestPath">Ruta al archivo JSON manifest (debe existir).</param>
        /// <param name="migrationId">Id único/ordenable de la migración (p.ej. <c>20251225_090000</c>).</param>
        /// <param name="migrationName">Nombre humano de la migración (p.ej. <c>CreateUsers</c>).</param>
        /// <param name="opsCount">Cantidad de operaciones registradas en la migración.</param>
        /// <param name="filePath">Ruta del archivo generado (se normaliza a <c>/</c>).</param>
        /// <remarks>
        /// Se agregan timestamps:
        /// <list type="bullet">
        /// <item><description><c>createdAtUtc</c>: fecha UTC de creación del item.</description></item>
        /// <item><description><c>updatedAtUtc</c>: fecha UTC de actualización del manifest.</description></item>
        /// </list>
        /// </remarks>
        /// <example>
        /// <code>
        /// ManifestWriter.Append(
        ///     manifestPath: "Migrations/manifest.json",
        ///     migrationId: "20251225_090000",
        ///     migrationName: "CreateUsers",
        ///     opsCount: 5,
        ///     filePath: "Migrations/20251225_090000_CreateUsers.cs");
        /// </code>
        /// </example>
        public static void Append(string manifestPath, string migrationId, string migrationName, int opsCount, string filePath)
        {
            var json = File.ReadAllText(manifestPath);
            var doc = JsonDocument.Parse(json);

            var root = doc.RootElement;

            var scope = root.TryGetProperty("scope", out var s) ? s.GetString() : "BareORM.Migrations";
            var migrations = root.TryGetProperty("migrations", out var m) ? m.EnumerateArray().ToList() : new List<JsonElement>();

            // construir nuevo array con el item extra
            var list = new List<object>();
            foreach (var e in migrations)
                list.Add(JsonSerializer.Deserialize<object>(e.GetRawText())!);

            list.Add(new
            {
                id = migrationId,
                name = migrationName,
                ops = opsCount,
                file = filePath.Replace("\\", "/"),
                createdAtUtc = DateTime.UtcNow
            });

            var output = new
            {
                scope,
                updatedAtUtc = DateTime.UtcNow,
                migrations = list
            };

            File.WriteAllText(
                manifestPath,
                JsonSerializer.Serialize(output, new JsonSerializerOptions { WriteIndented = true })
            );
        }
    }
}
