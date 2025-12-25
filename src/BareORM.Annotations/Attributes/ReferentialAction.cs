namespace BareORM.Annotations
{
    /// <summary>
    /// Acciones referenciales para llaves foráneas (FK): ON DELETE / ON UPDATE.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Estas opciones representan la intención a nivel de modelo (agnóstico del provider).
    /// El provider traduce cada valor a la sintaxis y comportamiento soportado por el motor.
    /// </para>
    /// <para>
    /// Nota práctica:
    /// </para>
    /// <list type="bullet">
    /// <item><description><see cref="NoAction"/> y <see cref="Restrict"/> pueden ser equivalentes o diferir según el motor.</description></item>
    /// <item><description><see cref="SetDefault"/> requiere que la columna tenga un DEFAULT válido; si no, fallará en runtime/DDL.</description></item>
    /// <item><description><see cref="SetNull"/> requiere que la columna permita NULL.</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Ejemplo conceptual:
    /// // FOREIGN KEY (...) REFERENCES (...) ON DELETE CASCADE ON UPDATE NO ACTION
    /// var onDelete = ReferentialAction.Cascade;
    /// var onUpdate = ReferentialAction.NoAction;
    /// </code>
    /// </example>
    public enum ReferentialAction
    {
        /// <summary>
        /// No realiza ninguna acción automática; si hay filas dependientes, la operación puede fallar por integridad referencial.
        /// </summary>
        /// <remarks>
        /// Usualmente se traduce a <c>NO ACTION</c> (o comportamiento equivalente) según el motor.
        /// </remarks>
        NoAction = 0,

        /// <summary>
        /// Restringe la operación si existen filas dependientes (bloquea DELETE/UPDATE).
        /// </summary>
        /// <remarks>
        /// Usualmente se traduce a <c>RESTRICT</c> o comportamiento equivalente.
        /// En algunos motores, <c>RESTRICT</c> y <c>NO ACTION</c> se comportan igual.
        /// </remarks>
        Restrict = 1,

        /// <summary>
        /// Propaga la operación a las filas dependientes (borrado/actualización en cascada).
        /// </summary>
        /// <remarks>
        /// Se traduce a <c>CASCADE</c>.
        /// </remarks>
        Cascade = 2,

        /// <summary>
        /// Establece NULL en las columnas dependientes cuando se borra/actualiza la fila referenciada.
        /// </summary>
        /// <remarks>
        /// Se traduce a <c>SET NULL</c>. Requiere que la columna sea nullable.
        /// </remarks>
        SetNull = 3,

        /// <summary>
        /// Establece el valor DEFAULT en las columnas dependientes cuando se borra/actualiza la fila referenciada.
        /// </summary>
        /// <remarks>
        /// Se traduce a <c>SET DEFAULT</c>. Requiere un DEFAULT definido y soportado por el motor.
        /// </remarks>
        SetDefault = 4
    }
}
