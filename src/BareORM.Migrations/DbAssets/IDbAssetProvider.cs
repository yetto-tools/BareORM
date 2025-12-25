namespace BareORM.Migrations.DbAssets
{
    /// <summary>
    /// Provee objetos SQL (“assets”) para ser administrados por el sistema de migraciones.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Un <see cref="IDbAssetProvider"/> expone una colección de <see cref="DbAsset"/> (views, procedures,
    /// funciones, triggers, etc.) que el motor de migraciones puede crear/actualizar según hash/versionado.
    /// </para>
    /// <para>
    /// Implementaciones típicas:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Filesystem: lee <c>.sql</c> desde un directorio (ver <see cref="FileSystemAssetProvider"/>).</description></item>
    /// <item><description>Embedded resources: scripts embebidos en un assembly.</description></item>
    /// <item><description>Custom: scripts generados dinámicamente por código.</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// IDbAssetProvider provider = new FileSystemAssetProvider("DbAssets");
    /// foreach (var asset in provider.GetAssets())
    ///     Console.WriteLine($"{asset.Kind}: {asset.Schema}.{asset.Name}");
    /// </code>
    /// </example>
    public interface IDbAssetProvider
    {
        /// <summary>
        /// Retorna los assets disponibles.
        /// </summary>
        /// <returns>Secuencia de <see cref="DbAsset"/>.</returns>
        IEnumerable<DbAsset> GetAssets();
    }
}
