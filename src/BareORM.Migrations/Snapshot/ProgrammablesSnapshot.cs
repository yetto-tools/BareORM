namespace BareORM.Migrations.Snapshot
{
    /// <summary>
    /// Elemento de snapshot para un objeto programable (view/procedure/function/trigger).
    /// </summary>
    /// <param name="Schema">Schema del objeto (p.ej. <c>dbo</c>).</param>
    /// <param name="Name">Nombre del objeto (p.ej. <c>sp_ListarUsuarios</c>).</param>
    /// <param name="Kind">
    /// Tipo del asset como entero (valor de <c>DbAssetKind</c>).
    /// Se guarda como <see cref="int"/> para simplificar serialización y mantener compatibilidad.
    /// </param>
    /// <param name="Hash">
    /// Hash (hex) de la definición SQL normalizada. Usualmente generado por <c>AssetHasher.Hash(sql)</c>.
    /// </param>
    /// <remarks>
    /// Este record representa el “estado conocido” de un objeto programable para detectar cambios.
    /// </remarks>
    public sealed record ProgrammableSnapshotItem(
        string Schema,
        string Name,
        int Kind,          // DbAssetKind int
        string Hash
    );

    /// <summary>
    /// Snapshot completo de objetos programables.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Contiene la lista de items (schema/name/kind/hash) que representan la versión conocida de los assets.
    /// </para>
    /// <para>
    /// Este snapshot normalmente se serializa a JSON (ver <c>JsonProgrammablesSnapshotStore</c>) y se usa
    /// para comparar contra el estado actual generado desde providers.
    /// </para>
    /// </remarks>
    public sealed class ProgrammablesSnapshot
    {
        /// <summary>
        /// Lista de items del snapshot.
        /// </summary>
        public List<ProgrammableSnapshotItem> Items { get; init; } = new();
    }
}
