using System.Text.Json;
using BareORM.Migrations.DbAssets;

namespace BareORM.Migrations.Snapshot
{
    /// <summary>
    /// Almacén de snapshots (JSON) para objetos programables (views, procedures, functions, triggers).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Un snapshot guarda “estado conocido” de los assets (por ejemplo, hash de cada definición SQL)
    /// para detectar cambios entre ejecuciones y decidir si se requiere aplicar CREATE/ALTER.
    /// </para>
    /// <para>
    /// Este store:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Carga y guarda el snapshot en un archivo JSON.</description></item>
    /// <item><description>Construye un snapshot desde uno o más <see cref="IDbAssetProvider"/>.</description></item>
    /// </list>
    /// <para>
    /// La comparación se basa en <see cref="AssetHasher"/> (normalización simple + SHA-256).
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var store = new JsonProgrammablesSnapshotStore();
    /// var snap = store.BuildFromProviders(new IDbAssetProvider[]
    /// {
    ///     new FileSystemAssetProvider("DbAssets")
    /// });
    ///
    /// store.Save("snapshots/programmables.json", snap);
    /// var loaded = store.Load("snapshots/programmables.json");
    /// </code>
    /// </example>
    public sealed class JsonProgrammablesSnapshotStore
    {
        /// <summary>
        /// Opciones JSON usadas para serialización/deserialización del snapshot.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Actualmente solo habilita <see cref="JsonSerializerOptions.WriteIndented"/> para facilitar diffs en Git.
        /// </para>
        /// </remarks>
        private static readonly JsonSerializerOptions _json = new()
        {
            WriteIndented = true
        };

        /// <summary>
        /// Carga un snapshot desde un archivo JSON.
        /// </summary>
        /// <param name="path">Ruta del archivo snapshot.</param>
        /// <returns>
        /// Snapshot cargado. Si el archivo no existe o no se puede deserializar, retorna un snapshot vacío.
        /// </returns>
        /// <remarks>
        /// Si el archivo no existe, se devuelve <c>new ProgrammablesSnapshot()</c>.
        /// </remarks>
        public ProgrammablesSnapshot Load(string path)
        {
            if (!File.Exists(path)) return new ProgrammablesSnapshot();
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<ProgrammablesSnapshot>(json, _json) ?? new ProgrammablesSnapshot();
        }

        /// <summary>
        /// Guarda un snapshot en un archivo JSON.
        /// </summary>
        /// <param name="path">Ruta del archivo snapshot.</param>
        /// <param name="snap">Snapshot a persistir.</param>
        /// <remarks>
        /// Crea el directorio destino si no existe.
        /// </remarks>
        public void Save(string path, ProgrammablesSnapshot snap)
        {
            var json = JsonSerializer.Serialize(snap, _json);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, json);
        }

        /// <summary>
        /// Construye un snapshot a partir de uno o más providers de assets.
        /// </summary>
        /// <param name="providers">Providers que entregan <see cref="DbAsset"/>.</param>
        /// <returns>Snapshot generado con hashes de las definiciones SQL.</returns>
        /// <remarks>
        /// <para>
        /// Cada asset se transforma en un <see cref="ProgrammableSnapshotItem"/> con:
        /// </para>
        /// <list type="bullet">
        /// <item><description>Schema + Name</description></item>
        /// <item><description>Kind (como int)</description></item>
        /// <item><description>Hash (SHA-256 hex) de la definición SQL</description></item>
        /// </list>
        /// </remarks>
        public ProgrammablesSnapshot BuildFromProviders(IEnumerable<IDbAssetProvider> providers)
        {
            var items = new List<ProgrammableSnapshotItem>();

            foreach (var a in providers.SelectMany(p => p.GetAssets()))
            {
                items.Add(new ProgrammableSnapshotItem(
                    a.Schema,
                    a.Name,
                    (int)a.Kind,
                    AssetHasher.Hash(a.Sql)
                ));
            }

            return new ProgrammablesSnapshot { Items = items };
        }
    }
}
