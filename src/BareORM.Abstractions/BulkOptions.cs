namespace BareORM.Abstractions
{
    /// <summary>
    /// Opciones de operaciones masivas independientes del proveedor de base de datos.
    /// Cada proveedor puede ignorar las opciones que no soporta de manera nativa.
    /// </summary>
    /// <remarks>
    /// Este registro proporciona una configuración agnóstica para operaciones de inserción,
    /// actualización o eliminación en lote. El comportamiento específico dependerá del
    /// proveedor de base de datos utilizado (SQL Server, PostgreSQL, MySQL, Oracle, etc.).
    /// 
    /// Por ejemplo, algunos proveedores como SQL Server soportan TABLOCK, mientras que
    /// otros pueden no implementar esta funcionalidad.
    /// </remarks>
    /// <param name="BatchSize">
    /// Tamaño del lote para procesar registros en grupos. Define cuántos registros se 
    /// procesarán en cada operación enviada a la base de datos. Un tamaño mayor reduce 
    /// el número de viajes a la BD (mejor rendimiento) pero consume más memoria. 
    /// Un tamaño menor consume menos memoria pero aumenta la latencia.
    /// Valor por defecto: 5000 registros por lote.
    /// </param>
    /// <param name="TimeoutSeconds">
    /// Tiempo máximo de espera en segundos para completar cada operación en lote.
    /// Si una operación de lote tarda más tiempo que este valor, se cancela y se
    /// lanza una excepción de timeout. Esto previene que operaciones largas bloqueen
    /// indefinidamente la aplicación. Valor por defecto: 60 segundos.
    /// </param>
    /// <param name="UseInternalTransaction">
    /// Indica si el ORM debe crear y gestionar automáticamente una transacción interna
    /// para encapsular toda la operación en lote. Cuando es <c>true</c>, toda la operación 
    /// se ejecuta dentro de una transacción. Si algún lote falla, la transacción se revierte 
    /// y todos los cambios previos se descartan. Cuando es <c>false</c>, cada lote se 
    /// confirma independientemente. Valor por defecto: <c>true</c>.
    /// </param>
    /// <param name="KeepIdentity">
    /// Indica si se deben preservar los valores de identidad/clave incremental proporcionados 
    /// en los registros a insertar. Cuando es <c>true</c>, el ORM respeta los valores de 
    /// clave incremental en los datos de entrada en lugar de dejar que la base de datos genere 
    /// nuevos valores. Esto es útil para migraciones de datos o restauración de copias de 
    /// seguridad. Requiere permisos especiales en bases de datos como SQL Server (IDENTITY_INSERT).
    /// Valor por defecto: <c>false</c>.
    /// </param>
    /// <param name="TableLock">
    /// Indica si se debe aplicar un bloqueo de tabla durante la operación en lote.
    /// Cuando es <c>true</c>, la tabla se bloquea exclusivamente durante toda la operación,
    /// evitando que otros usuarios accedan a ella. Esto mejora significativamente el
    /// rendimiento en operaciones masivas, pero puede afectar la concurrencia.
    /// Cuando es <c>false</c>, se usan bloqueos de fila/página. Nota: SQL Server soporta 
    /// esta opción (TABLOCK). Otros proveedores pueden ignorarla. Valor por defecto: <c>true</c>.
    /// </param>
    /// <example>
    /// <code>
    /// var opciones = new BulkOptions(
    ///     BatchSize: 10000,
    ///     TimeoutSeconds: 120,
    ///     UseInternalTransaction: true,
    ///     KeepIdentity: false,
    ///     TableLock: true
    /// );
    /// 
    /// await miORM.InsertarEnLoteAsync(usuarios, opciones);
    /// </code>
    /// </example>
    public sealed record BulkOptions(
        int BatchSize = 5000,
        int TimeoutSeconds = 60,
        bool UseInternalTransaction = true,
        bool KeepIdentity = false,
        bool TableLock = true
    );
}