namespace BareORM.Migrations.DbAssets
{
    /// <summary>
    /// Tipo de asset de base de datos (objeto lógico) administrado por migraciones.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Un “asset” representa objetos como views, stored procedures, funciones y triggers cuyo SQL
    /// se gestiona como parte del pipeline de migraciones (por ejemplo, creando/actualizando por hash).
    /// </para>
    /// <para>
    /// El provider es quien traduce <see cref="DbAssetKind"/> a la sintaxis DDL correspondiente
    /// (CREATE/ALTER/DROP) y reglas de existencia (IF EXISTS, CREATE OR ALTER, etc.).
    /// </para>
    /// </remarks>
    public enum DbAssetKind
    {
        /// <summary>Vista (VIEW).</summary>
        View = 0,

        /// <summary>Procedimiento almacenado (PROCEDURE).</summary>
        Procedure = 1,

        /// <summary>Función escalar (scalar function).</summary>
        ScalarFunction = 2,

        /// <summary>Función de tabla (table-valued function).</summary>
        TableFunction = 3,

        /// <summary>Trigger.</summary>
        Trigger = 4
    }

    /// <summary>
    /// Representa un objeto SQL administrado como “asset” por el sistema de migraciones.
    /// </summary>
    /// <param name="Schema">Schema del objeto (p.ej. <c>dbo</c>).</param>
    /// <param name="Name">Nombre del objeto (p.ej. <c>vwUsers</c>).</param>
    /// <param name="Kind">Tipo del objeto (view/proc/function/trigger).</param>
    /// <param name="Sql">
    /// Definición SQL del asset (script). Se recomienda que sea el script completo del objeto.
    /// </param>
    /// <remarks>
    /// <para>
    /// El contenido de <paramref name="Sql"/> suele hashearse (ver <see cref="AssetHasher"/>) para detectar cambios
    /// y decidir si se debe aplicar un CREATE/ALTER.
    /// </para>
    /// <para>
    /// Recomendación: mantener el SQL en formato estable (normalizado) para reducir diffs por whitespace.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var asset = new DbAsset(
    ///     Schema: "dbo",
    ///     Name: "vw_ActiveUsers",
    ///     Kind: DbAssetKind.View,
    ///     Sql: "CREATE OR ALTER VIEW dbo.vw_ActiveUsers AS SELECT ...");
    /// </code>
    /// </example>
    public sealed record DbAsset(string Schema, string Name, DbAssetKind Kind, string Sql);
}
