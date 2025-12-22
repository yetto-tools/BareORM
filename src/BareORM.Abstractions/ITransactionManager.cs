using System.Data;
using System.Data.Common;

namespace BareORM.Abstractions
{
    /// <summary>
    /// Abstracción agnóstica de gestión de transacciones.
    /// Las implementaciones vinculan una transacción a la conexión/ejecutor subyacente del proveedor.
    /// </summary>
    /// <remarks>
    /// Esta interfaz define un contrato para administrar transacciones de base de datos de manera agnóstica,
    /// sin depender de detalles específicos del proveedor (SQL Server, PostgreSQL, MySQL, Oracle, etc.).
    /// 
    /// <strong>¿Qué es una transacción?</strong>
    /// Una transacción es un conjunto de una o más operaciones de base de datos que se ejecutan como
    /// una unidad atómica: todas se completan (COMMIT) o todas se revierten (ROLLBACK).
    /// Garantiza ACID (Atomicity, Consistency, Isolation, Durability).
    /// 
    /// <strong>Ventajas de usar una abstracción:</strong>
    /// <list type="bullet">
    ///     <item>
    ///         <description>
    ///         <strong>Agnóstico del proveedor:</strong> El mismo código funciona con cualquier BD.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///         <strong>Control de aislamiento:</strong> Especificar el nivel de aislamiento
    ///         sin conocer detalles del proveedor.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///         <strong>Inyección de dependencias:</strong> Diferentes implementaciones
    ///         pueden ser inyectadas según el contexto.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///         <strong>Seguridad:</strong> IDisposable garantiza limpieza de recursos.
    ///         </description>
    ///     </item>
    /// </list>
    /// 
    /// <strong>Niveles de aislamiento (IsolationLevel):</strong>
    /// <list type="table">
    ///     <listheader>
    ///         <term>Nivel</term>
    ///         <description>Permiso</description>
    ///         <description>Casos de uso</description>
    ///     </listheader>
    ///     <item>
    ///         <term><c>Unspecified</c></term>
    ///         <description>Usa el predeterminado del proveedor (típicamente ReadCommitted)</description>
    ///         <description>Cuando no importa el nivel exacto</description>
    ///     </item>
    ///     <item>
    ///         <term><c>Chaos</c></term>
    ///         <description>Máximo aislamiento mínimo (sin aislamiento)</description>
    ///         <description>Raramente usado, lecturas sucias permitidas</description>
    ///     </item>
    ///     <item>
    ///         <term><c>ReadUncommitted</c></term>
    ///         <description>Permite lecturas sucias (dirty reads)</description>
    ///         <description>Máximo rendimiento, datos potencialmente inconsistentes</description>
    ///     </item>
    ///     <item>
    ///         <term><c>ReadCommitted</c> ⭐</term>
    ///         <description>Solo datos committed, evita dirty reads. PREDETERMINADO en SQL Server</description>
    ///         <description>Balance entre rendimiento y consistencia. Recomendado</description>
    ///     </item>
    ///     <item>
    ///         <term><c>RepeatableRead</c></term>
    ///         <description>Evita dirty reads y non-repeatable reads</description>
    ///         <description>Cuando necesita lecturas consistentes pero no máximo aislamiento</description>
    ///     </item>
    ///     <item>
    ///         <term><c>Serializable</c></term>
    ///         <description>Máximo aislamiento, solo serialización equivalente</description>
    ///         <description>Operaciones críticas, máxima consistencia, puede ser lento</description>
    ///     </item>
    ///     <item>
    ///         <term><c>Snapshot</c></term>
    ///         <description>Aislamiento mediante snapshots (SQL Server)</description>
    ///         <description>Alta concurrencia sin bloqueos, lectura consistente</description>
    ///     </item>
    /// </list>
    /// 
    /// <strong>Patrones comunes de uso:</strong>
    /// <code>
    /// // Patrón 1: Using statement (recomendado)
    /// using (var txn = miORM.BeginTransaction())
    /// {
    ///     // Operaciones de BD
    ///     txn.Commit();
    /// } // Rollback automático si excepción no capturada
    /// 
    /// // Patrón 2: Manejo manual de transacciones
    /// var txn = miORM.BeginTransaction();
    /// try
    /// {
    ///     // Operaciones
    ///     txn.Commit();
    /// }
    /// catch
    /// {
    ///     txn.Rollback();
    ///     throw;
    /// }
    /// finally
    /// {
    ///     txn.Dispose();
    /// }
    /// </code>
    /// 
    /// <strong>Casos de uso típicos:</strong>
    /// <list type="bullet">
    ///     <item>
    ///         <description>
    ///         <strong>Operaciones multi-tabla:</strong> INSERT padre + INSERTs hijos, todo o nada.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///         <strong>Transferencias de dinero:</strong> Restar de una cuenta, sumar a otra, atómicamente.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///         <strong>Procesamiento batch:</strong> Actualizar muchos registros, revertir si error.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///         <strong>Integridad referencial:</strong> Mantener relaciones padre-hijo consistentes.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///         <strong>Auditoría atómica:</strong> Cambiar datos + registrar cambio, juntos.
    ///         </description>
    ///     </item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Definición de entidades
    /// public class Cuenta
    /// {
    ///     public int CuentaId { get; set; }
    ///     public string Propietario { get; set; } = "";
    ///     public decimal Saldo { get; set; }
    /// }
    /// 
    /// public class Transaccion
    /// {
    ///     public int TransaccionId { get; set; }
    ///     public int CuentaId { get; set; }
    ///     public decimal Monto { get; set; }
    ///     public string Tipo { get; set; } = ""; // "Ingreso" o "Egreso"
    ///     public DateTime Fecha { get; set; }
    /// }
    /// 
    /// 
    /// // Ejemplo 1: Transferencia de dinero (caso de uso crítico)
    /// public class ServicioTransferencias
    /// {
    ///     private readonly IDbExecutor _executor;
    ///     private readonly ITransactionManager _txnManager;
    ///     
    ///     public async Task TransferirDineroAsync(
    ///         int cuentaOrigenId,
    ///         int cuentaDestinoId,
    ///         decimal monto)
    ///     {
    ///         // Validaciones previas
    ///         if (monto &lt;= 0)
    ///             throw new ArgumentException("El monto debe ser positivo");
    ///         
    ///         // Comenzar transacción en nivel Serializable para máxima consistencia
    ///         await _txnManager.BeginAsync(IsolationLevel.Serializable);
    ///         
    ///         try
    ///         {
    ///             // 1. Verificar que la cuenta origen tiene fondos
    ///             var cmdOrigen = new CommandDefinition(
    ///                 "SELECT Saldo FROM Cuentas WHERE CuentaId = @id",
    ///                 parameters: new { id = cuentaOrigenId }
    ///             );
    ///             var saldoOrigen = (decimal?)await _executor.ExecuteScalarAsync(cmdOrigen);
    ///             
    ///             if (saldoOrigen == null)
    ///                 throw new InvalidOperationException($"Cuenta origen {cuentaOrigenId} no existe");
    ///             
    ///             if (saldoOrigen  monto)
    ///                 throw new InvalidOperationException("Fondos insuficientes");
    ///             
    ///             // 2. Restar de la cuenta origen
    ///             var cmdRestar = new CommandDefinition(
    ///                 "UPDATE Cuentas SET Saldo = Saldo - @monto WHERE CuentaId = @id",
    ///                 parameters: new { monto, id = cuentaOrigenId }
    ///             );
    ///             await _executor.ExecuteAsync(cmdRestar);
    ///             
    ///             // 3. Sumar a la cuenta destino
    ///             var cmdSumar = new CommandDefinition(
    ///                 "UPDATE Cuentas SET Saldo = Saldo + @monto WHERE CuentaId = @id",
    ///                 parameters: new { monto, id = cuentaDestinoId }
    ///             );
    ///             await _executor.ExecuteAsync(cmdSumar);
    ///             
    ///             // 4. Registrar la transacción
    ///             var cmdRegistro = new CommandDefinition(
    ///                 "INSERT INTO Transacciones (CuentaId, Monto, Tipo, Fecha) VALUES (@cuenta, @monto, @tipo, @fecha)",
    ///                 parameters: new 
    ///                 { 
    ///                     cuenta = cuentaOrigenId, 
    ///                     monto = -monto, 
    ///                     tipo = "Transferencia", 
    ///                     fecha = DateTime.UtcNow 
    ///                 }
    ///             );
    ///             await _executor.ExecuteAsync(cmdRegistro);
    ///             
    ///             // Todo OK, confirmar la transacción
    ///             await _txnManager.CommitAsync();
    ///             Console.WriteLine($"Transferencia exitosa: ${monto} de {cuentaOrigenId} a {cuentaDestinoId}");
    ///         }
    ///         catch (Exception ex)
    ///         {
    ///             // Error en cualquier paso, revertir todo
    ///             await _txnManager.RollbackAsync();
    ///             Console.WriteLine($"Transferencia fallida, revirtiendo: {ex.Message}");
    ///             throw;
    ///         }
    ///     }
    /// }
    /// 
    /// 
    /// // Ejemplo 2: Inserción multi-tabla (padre + detalles)
    /// public class ServicioVentas
    /// {
    ///     private readonly IDbExecutor _executor;
    ///     private readonly ITransactionManager _txnManager;
    ///     
    ///     public async Task CrearPedidoConDetallesAsync(
    ///         int clienteId,
    ///         List&lt;(int productoId, int cantidad, decimal precio)&gt; detalles)
    ///     {
    ///         // Comienza transacción con nivel ReadCommitted (predeterminado)
    ///         await _txnManager.BeginAsync();
    ///         
    ///         try
    ///         {
    ///             // 1. Insertar el encabezado del pedido
    ///             var cmdPedido = new CommandDefinition(
    ///                 @"INSERT INTO Pedidos (ClienteId, Fecha, Estado) 
    ///                   OUTPUT INSERTED.PedidoId
    ///                   VALUES (@cliente, @fecha, 'Pendiente')",
    ///                 parameters: new { cliente = clienteId, fecha = DateTime.UtcNow }
    ///             );
    ///             int pedidoId = (int)(await _executor.ExecuteScalarAsync(cmdPedido))!;
    ///             Console.WriteLine($"Pedido creado: {pedidoId}");
    ///             
    ///             // 2. Insertar cada detalle
    ///             foreach (var (productoId, cantidad, precio) in detalles)
    ///             {
    ///                 var cmdDetalle = new CommandDefinition(
    ///                     @"INSERT INTO DetallesPedido (PedidoId, ProductoId, Cantidad, PrecioUnitario)
    ///                       VALUES (@pedido, @producto, @cantidad, @precio)",
    ///                     parameters: new 
    ///                     { 
    ///                         pedido = pedidoId, 
    ///                         producto = productoId, 
    ///                         cantidad, 
    ///                         precio 
    ///                     }
    ///                 );
    ///                 await _executor.ExecuteAsync(cmdDetalle);
    ///                 Console.WriteLine($"  Detalle agregado: Producto {productoId} x{cantidad}");
    ///             }
    ///             
    ///             // 3. Actualizar inventario
    ///             foreach (var (productoId, cantidad, _) in detalles)
    ///             {
    ///                 var cmdInventario = new CommandDefinition(
    ///                     "UPDATE Productos SET Stock = Stock - @cantidad WHERE ProductoId = @id",
    ///                     parameters: new { cantidad, id = productoId }
    ///                 );
    ///                 await _executor.ExecuteAsync(cmdInventario);
    ///             }
    ///             
    ///             // Todo OK
    ///             await _txnManager.CommitAsync();
    ///             Console.WriteLine("Pedido completamente creado y confirmado");
    ///         }
    ///         catch
    ///         {
    ///             await _txnManager.RollbackAsync();
    ///             Console.WriteLine("Error creando pedido, revirtiendo cambios");
    ///             throw;
    ///         }
    ///     }
    /// }
    /// 
    /// 
    /// // Ejemplo 3: Control granular con HasActiveTransaction
    /// public class ProcesadorBatch
    /// {
    ///     private readonly IDbExecutor _executor;
    ///     private readonly ITransactionManager _txnManager;
    ///     
    ///     public async Task ProcesarRegistrosAsync(List&lt;int&gt; ids)
    ///     {
    ///         // Verificar si ya hay una transacción activa
    ///         if (_txnManager.HasActiveTransaction)
    ///         {
    ///             // Reutilizar la transacción existente
    ///             Console.WriteLine("Usando transacción existente");
    ///         }
    ///         else
    ///         {
    ///             // Crear nueva transacción
    ///             Console.WriteLine("Comenzando nueva transacción");
    ///             await _txnManager.BeginAsync(IsolationLevel.ReadCommitted);
    ///         }
    ///         
    ///         try
    ///         {
    ///             int procesados = 0;
    ///             foreach (var id in ids)
    ///             {
    ///                 var cmd = new CommandDefinition(
    ///                     "UPDATE Registros SET Procesado = 1 WHERE Id = @id",
    ///                     parameters: new { id }
    ///                 );
    ///                 await _executor.ExecuteAsync(cmd);
    ///                 procesados++;
    ///             }
    ///             
    ///             Console.WriteLine($"Procesados {procesados} registros");
    ///             // NO hacer commit si fue transacción externa
    ///         }
    ///         catch
    ///         {
    ///             Console.WriteLine("Error durante procesamiento batch");
    ///             throw;
    ///         }
    ///     }
    /// }
    /// 
    /// 
    /// // Ejemplo 4: Transacciones con diferentes niveles de aislamiento
    /// public class ComparativoAislamiento
    /// {
    ///     private readonly ITransactionManager _txnManager;
    ///     
    ///     public async Task DemostracionNivelesAsync()
    ///     {
    ///         // ReadCommitted - Recomendado para la mayoría de casos
    ///         await _txnManager.BeginAsync(IsolationLevel.ReadCommitted);
    ///         Console.WriteLine("Transacción en ReadCommitted: lecturas consistentes, buen rendimiento");
    ///         await _txnManager.CommitAsync();
    ///         
    ///         // RepeatableRead - Cuando necesita que los datos no cambien durante la txn
    ///         await _txnManager.BeginAsync(IsolationLevel.RepeatableRead);
    ///         Console.WriteLine("Transacción en RepeatableRead: datos no cambian, más bloqueos");
    ///         await _txnManager.CommitAsync();
    ///         
    ///         // Serializable - Máxima seguridad, posible rendimiento bajo
    ///         await _txnManager.BeginAsync(IsolationLevel.Serializable);
    ///         Console.WriteLine("Transacción en Serializable: máxima consistencia, riesgo de deadlocks");
    ///         await _txnManager.CommitAsync();
    ///     }
    /// }
    /// </code>
    /// </example>
    public interface ITransactionManager : IDisposable
    {
        /// <summary>
        /// Obtiene un valor que indica si hay una transacción activa en este momento.
        /// </summary>
        /// <remarks>
        /// Esta propiedad permite verificar el estado actual de la transacción sin modificarla.
        /// 
        /// <strong>Casos de uso:</strong>
        /// <list type="bullet">
        ///     <item>
        ///         <description>
        ///         Verificar antes de iniciar una nueva transacción (evitar transacciones anidadas).
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <description>
        ///         Decidir si hacer commit/rollback basado en si la transacción es interna o externa.
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <description>
        ///         Logging y debugging para rastrear estado de transacciones.
        ///         </description>
        ///     </item>
        /// </list>
        /// 
        /// <strong>Comportamiento:</strong>
        /// - <c>true</c>: Una transacción está activa y puede hacer Commit/Rollback
        /// - <c>false</c>: No hay transacción activa
        /// 
        /// Cambios de valor:
        /// - Después de <see cref="Begin(IsolationLevel)"/> o <see cref="BeginAsync(IsolationLevel, CancellationToken)"/>: pasa a <c>true</c>
        /// - Después de <see cref="Commit"/> o <see cref="CommitAsync"/>: pasa a <c>false</c>
        /// - Después de <see cref="Rollback"/> o <see cref="RollbackAsync"/>: pasa a <c>false</c>
        /// - Después de <see cref="IDisposable.Dispose"/>: típicamente pasa a <c>false</c> (y revierte la transacción)
        /// 
        /// <strong>Nota importante:</strong>
        /// En algunas bases de datos (SQL Server) siempre hay una transacción implícita activa, incluso cuando
        /// no la ha iniciado explícitamente. En estos casos, <c>HasActiveTransaction</c> puede ser <c>true</c>
        /// incluso sin llamar a <see cref="Begin(IsolationLevel)"/>. Depende de la implementación.
        /// </remarks>
        /// <value>
        /// <c>true</c> si hay una transacción activa iniciada explícitamente; <c>false</c> en caso contrario.
        /// </value>
        bool HasActiveTransaction { get; }

        /// <summary>
        /// Comienza una nueva transacción de forma síncrona con el nivel de aislamiento especificado.
        /// </summary>
        /// <remarks>
        /// Inicia una nueva transacción a nivel de base de datos. Todas las operaciones ejecutadas
        /// después de este llamado formarán parte de la transacción hasta que se haga Commit o Rollback.
        /// 
        /// <strong>Comportamiento:</strong>
        /// 1. Crea una nueva transacción en la conexión subyacente
        /// 2. Establece el nivel de aislamiento especificado
        /// 3. Marca <see cref="HasActiveTransaction"/> como <c>true</c>
        /// 4. Todas las operaciones posteriores se ejecutan bajo esta transacción
        /// 
        /// <strong>Consideraciones:</strong>
        /// <list type="bullet">
        ///     <item>
        ///         <description>
        ///         <strong>Transacciones anidadas:</strong> La mayoría de BD no soportan transacciones anidadas.
        ///         Si ya hay una transacción activa, lanza una excepción.
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <description>
        ///         <strong>Bloqueos:</strong> Comenzar una transacción puede adquirir bloqueos en la BD.
        ///         Cuanto más tiempo esté la transacción abierta, más riesgo de deadlocks.
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <description>
        ///         <strong>Aislamiento predeterminado:</strong> Si no especifica isolationLevel,
        ///         se usa <c>ReadCommitted</c>.
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <description>
        ///         <strong>Debe finalizarse:</strong> Toda transacción comenzada debe terminarse con
        ///         <see cref="Commit"/> o <see cref="Rollback"/>, o será revirtida al disponer.
        ///         </description>
        ///     </item>
        /// </list>
        /// 
        /// <strong>Niveles de aislamiento recomendados:</strong>
        /// <list type="bullet">
        ///     <item>
        ///         <description>
        ///         <strong>ReadCommitted:</strong> Para la mayoría de casos (predeterminado, buen balance)
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <description>
        ///         <strong>RepeatableRead:</strong> Cuando los datos no deben cambiar durante la transacción
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <description>
        ///         <strong>Serializable:</strong> Para operaciones críticas (transferencias, auditoría)
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <description>
        ///         <strong>Snapshot (SQL Server):</strong> Alta concurrencia sin bloqueos
        ///         </description>
        ///     </item>
        /// </list>
        /// </remarks>
        /// <param name="isolationLevel">
        /// El nivel de aislamiento para esta transacción.
        /// El valor predeterminado es <c>ReadCommitted</c>.
        /// Ver tabla de niveles en la documentación de clase.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// Se lanza si ya hay una transacción activa (no soporta anidamiento).
        /// </exception>
        /// <exception cref="DbException">
        /// Se lanza si hay un error al crear la transacción en la BD.
        /// </exception>
        void Begin(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted);

        /// <summary>
        /// Comienza una nueva transacción de forma asíncrona con el nivel de aislamiento especificado.
        /// </summary>
        /// <remarks>
        /// Versión asíncrona de <see cref="Begin(IsolationLevel)"/>.
        /// No bloquea el hilo mientras se crea la transacción en la BD.
        /// </remarks>
        /// <param name="isolationLevel">
        /// El nivel de aislamiento para esta transacción.
        /// El valor predeterminado es <c>ReadCommitted</c>.
        /// </param>
        /// <param name="ct">
        /// Token de cancelación para permitir cancelar la operación.
        /// </param>
        /// <returns>
        /// Una tarea que representa la operación asíncrona.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Se lanza si ya hay una transacción activa.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// Se lanza si la operación es cancelada.
        /// </exception>
        /// <exception cref="DbException">
        /// Se lanza si hay un error al crear la transacción en la BD.
        /// </exception>
        Task BeginAsync(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted, CancellationToken ct = default);

        /// <summary>
        /// Confirma (COMMIT) la transacción actual, haciendo que todos los cambios sean permanentes.
        /// </summary>
        /// <remarks>
        /// Finaliza la transacción actual de forma exitosa, confirmando todos los cambios
        /// realizados bajo su control. Después del commit, los cambios son permanentes en la BD
        /// y visibles para otras conexiones.
        /// 
        /// <strong>Comportamiento:</strong>
        /// 1. Valida todos los cambios pendientes
        /// 2. Escribe los cambios en la BD de forma durable
        /// 3. Libera todos los bloqueos adquiridos por la transacción
        /// 4. Marca <see cref="HasActiveTransaction"/> como <c>false</c>
        /// 5. Los cambios quedan permanentes
        /// 
        /// <strong>Consideraciones:</strong>
        /// <list type="bullet">
        ///     <item>
        ///         <description>
        ///         <strong>Una sola vez:</strong> No puede hacer commit dos veces de la misma transacción.
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <description>
        ///         <strong>Punto de no retorno:</strong> Después de commit, no puede hacer rollback.
        ///         Los cambios son permanentes.
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <description>
        ///         <strong>Duración:</strong> El commit puede tardar si hay muchos cambios.
        ///         En SQL Server, el tiempo depende de log flush a disco.
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <description>
        ///         <strong>Liberación de bloqueos:</strong> Todos los bloqueos se liberan inmediatamente.
        ///         Otros usuarios ahora pueden acceder a los datos modificados.
        ///         </description>
        ///     </item>
        /// </list>
        /// 
        /// <strong>Patrón recomendado:</strong>
        /// <code>
        /// try
        /// {
        ///     // Operaciones
        ///     txnManager.Commit();
        /// }
        /// catch
        /// {
        ///     txnManager.Rollback();
        ///     throw;
        /// }
        /// </code>
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// Se lanza si no hay transacción activa (<see cref="HasActiveTransaction"/> es <c>false</c>).
        /// </exception>
        /// <exception cref="DbException">
        /// Se lanza si hay un error al confirmar la transacción (ej: violación de restricción).
        /// </exception>
        void Commit();

        /// <summary>
        /// Confirma (COMMIT) la transacción actual de forma asíncrona.
        /// </summary>
        /// <remarks>
        /// Versión asíncrona de <see cref="Commit"/>.
        /// No bloquea el hilo durante el proceso de confirmación.
        /// </remarks>
        /// <param name="ct">Token de cancelación.</param>
        /// <returns>Una tarea que representa la operación asíncrona.</returns>
        /// <exception cref="InvalidOperationException">
        /// Se lanza si no hay transacción activa.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// Se lanza si la operación es cancelada.
        /// </exception>
        /// <exception cref="DbException">
        /// Se lanza si hay un error al confirmar la transacción.
        /// </exception>
        Task CommitAsync(CancellationToken ct = default);

        /// <summary>
        /// Revierte (ROLLBACK) la transacción actual, descartando todos los cambios.
        /// </summary>
        /// <remarks>
        /// Finaliza la transacción actual sin confirmar cambios, revirtiendo todos los cambios
        /// realizados bajo su control. La BD vuelve al estado anterior a la transacción.
        /// 
        /// <strong>Comportamiento:</strong>
        /// 1. Descarta todos los cambios pendientes (no se escriben en la BD)
        /// 2. Libera todos los bloqueos adquiridos por la transacción
        /// 3. Marca <see cref="HasActiveTransaction"/> como <c>false</c>
        /// 4. La BD queda sin cambios de esta transacción
        /// 
        /// <strong>Consideraciones:</strong>
        /// <list type="bullet">
        ///     <item>
        ///         <description>
        ///         <strong>Una sola vez:</strong> No puede hacer rollback dos veces de la misma transacción.
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <description>
        ///         <strong>No hay recuperación:</strong> Los cambios se descartan permanentemente.
        ///         No puede recuperarlos después de rollback.
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <description>
        ///         <strong>Generalmente rápido:</strong> El rollback es típicamente más rápido que el commit
        ///         porque no necesita escribir a disco.
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <description>
        ///         <strong>Uso en excepciones:</strong> Se llama automáticamente si la transacción se dispone
        ///         sin commit explícito (en muchas implementaciones).
        ///         </description>
        ///     </item>
        /// </list>
        /// 
        /// <strong>Casos de uso:</strong>
        /// <list type="bullet">
        ///     <item><description>Ocurre una excepción durante operaciones</description></item>
        ///     <item><description>Validación falla después de iniciar transacción</description></item>
        ///     <item><description>Usuario cancela la operación</description></item>
        ///     <item><description>Reintentar desde un punto anterior</description></item>
        /// </list>
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// Se lanza si no hay transacción activa.
        /// </exception>
        /// <exception cref="DbException">
        /// Se lanza si hay un error durante el rollback (raro, pero posible).
        /// </exception>
        void Rollback();

        /// <summary>
        /// Revierte (ROLLBACK) la transacción actual de forma asíncrona.
        /// </summary>
        /// <remarks>
        /// Versión asíncrona de <see cref="Rollback"/>.
        /// No bloquea el hilo durante la revert.
        /// </remarks>
        /// <param name="ct">Token de cancelación.</param>
        /// <returns>Una tarea que representa la operación asíncrona.</returns>
        /// <exception cref="InvalidOperationException">
        /// Se lanza si no hay transacción activa.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// Se lanza si la operación es cancelada.
        /// </exception>
        /// <exception cref="DbException">
        /// Se lanza si hay un error durante el rollback.
        /// </exception>
        Task RollbackAsync(CancellationToken ct = default);
    }
}