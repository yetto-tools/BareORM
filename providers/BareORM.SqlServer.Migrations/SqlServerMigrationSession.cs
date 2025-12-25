using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Data.SqlClient;

namespace BareORM.SqlServer.Migrations
{
    /// <summary>
    /// Proporciona una sesión de migración de base de datos SQL Server con soporte para transacciones.
    /// </summary>
    /// <remarks>
    /// Esta clase encapsula una conexión a SQL Server y permite ejecutar comandos SQL
    /// con o sin transacciones. La conexión se abre automáticamente al crear la instancia
    /// y se cierra al llamar a <see cref="Dispose"/>.
    /// </remarks>
    /// <example>
    /// <code>
    /// using (var session = new SqlServerMigrationSession("Server=localhost;Database=MyDb;"))
    /// {
    ///     session.BeginTransaction();
    ///     try
    ///     {
    ///         session.ExecuteNonQuery("CREATE TABLE Users (Id INT PRIMARY KEY)");
    ///         session.Commit();
    ///     }
    ///     catch
    ///     {
    ///         session.Rollback();
    ///         throw;
    ///     }
    /// }
    /// </code>
    /// </example>
    public sealed class SqlServerMigrationSession : IDisposable
    {
        /// <summary>
        /// Obtiene la conexión SQL Server subyacente.
        /// </summary>
        /// <value>
        /// Una instancia de <see cref="SqlConnection"/> que está abierta y lista para usar.
        /// </value>
        public SqlConnection Connection { get; }

        /// <summary>
        /// Obtiene la transacción activa actual, si existe.
        /// </summary>
        /// <value>
        /// Una instancia de <see cref="SqlTransaction"/> si hay una transacción activa; de lo contrario, <c>null</c>.
        /// </value>
        public SqlTransaction? Transaction { get; private set; }

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="SqlServerMigrationSession"/> 
        /// y abre la conexión a la base de datos.
        /// </summary>
        /// <remarks>
        /// <paramref name="connectionString"/> debe ser una cadena de conexión válida para SQL Server.
        /// </remarks>
        /// <param name="connectionString">La cadena de conexión para conectarse a SQL Server.</param>
        /// <exception cref="ArgumentNullException">Se produce cuando <paramref name="connectionString"/> es <c>null</c>.</exception>
        /// <exception cref="SqlException">Se produce cuando no se puede abrir la conexión a la base de datos.</exception>
        public SqlServerMigrationSession(string connectionString)
        {
            Connection = new SqlConnection(connectionString);
            Connection.Open();
        }

        /// <summary>
        /// Inicia una nueva transacción en la conexión actual.
        /// </summary>
        /// <remarks>
        /// Si ya existe una transacción activa, este método no hace nada.
        /// Las transacciones permiten agrupar múltiples operaciones y confirmarlas o revertirlas como una unidad.
        /// </remarks>
        public void BeginTransaction()
        {
            if (Transaction is not null) return;
            Transaction = Connection.BeginTransaction();
        }

        /// <summary>
        /// Confirma la transacción actual y libera los recursos asociados.
        /// </summary>
        /// <remarks>
        /// Después de llamar a este método, la propiedad <see cref="Transaction"/> se establece en <c>null</c>.
        /// Si no hay una transacción activa, este método no hace nada.
        /// </remarks>
        public void Commit()
        {
            Transaction?.Commit();
            Transaction?.Dispose();
            Transaction = null;
        }

        /// <summary>
        /// Revierte la transacción actual y libera los recursos asociados.
        /// </summary>
        /// <remarks>
        /// Después de llamar a este método, la propiedad <see cref="Transaction"/> se establece en <c>null</c>.
        /// Si no hay una transacción activa, este método no hace nada.
        /// Las excepciones durante el rollback se ignoran para evitar enmascarar errores anteriores.
        /// </remarks>
        public void Rollback()
        {
            try { Transaction?.Rollback(); } catch { /* swallow */ }
            Transaction?.Dispose();
            Transaction = null;
        }

        /// <summary>
        /// Ejecuta un comando SQL que no devuelve resultados.
        /// </summary>
        /// <param name="sql">La instrucción SQL a ejecutar (INSERT, UPDATE, DELETE, DDL, etc.).</param>
        /// <param name="timeoutSeconds">El tiempo de espera en segundos antes de que el comando expire. El valor predeterminado es 120 segundos.</param>
        /// <returns>El número de filas afectadas por el comando.</returns>
        /// <exception cref="SqlException">Se produce cuando ocurre un error al ejecutar el comando SQL.</exception>
        /// <remarks>
        /// Si hay una transacción activa, el comando se ejecuta dentro de esa transacción.
        /// </remarks>
        /// <example>
        /// <code>
        /// int rowsAffected = session.ExecuteNonQuery("DELETE FROM Users WHERE IsActive = 0");
        /// </code>
        /// </example>
        public int ExecuteNonQuery(string sql, int timeoutSeconds = 120)
        {
            using var cmd = Connection.CreateCommand();
            cmd.Transaction = Transaction;
            cmd.CommandTimeout = timeoutSeconds;
            cmd.CommandText = sql;
            return cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Ejecuta un comando SQL y devuelve la primera columna de la primera fila del resultado.
        /// </summary>
        /// <param name="sql">La instrucción SQL a ejecutar (SELECT, función escalar, etc.).</param>
        /// <param name="timeoutSeconds">El tiempo de espera en segundos antes de que el comando expire. El valor predeterminado es 120 segundos.</param>
        /// <returns>
        /// La primera columna de la primera fila del conjunto de resultados, o <c>null</c> si el conjunto de resultados está vacío.
        /// </returns>
        /// <exception cref="SqlException">Se produce cuando ocurre un error al ejecutar el comando SQL.</exception>
        /// <remarks>
        /// Si hay una transacción activa, el comando se ejecuta dentro de esa transacción.
        /// Este método es útil para consultas de agregación o cuando solo se necesita un valor único.
        /// </remarks>
        /// <example>
        /// <code>
        /// var count = session.ExecuteScalar("SELECT COUNT(*) FROM Users");
        /// int userCount = Convert.ToInt32(count);
        /// </code>
        /// </example>
        public object? ExecuteScalar(string sql, int timeoutSeconds = 120)
        {
            using var cmd = Connection.CreateCommand();
            cmd.Transaction = Transaction;
            cmd.CommandTimeout = timeoutSeconds;
            cmd.CommandText = sql;
            return cmd.ExecuteScalar();
        }

        /// <summary>
        /// Ejecuta una consulta SQL y devuelve una lista de cadenas de la primera columna de cada fila.
        /// </summary>
        /// <param name="sql">La instrucción SQL SELECT a ejecutar.</param>
        /// <param name="timeoutSeconds">El tiempo de espera en segundos antes de que el comando expire. El valor predeterminado es 120 segundos.</param>
        /// <returns>Una lista de cadenas que contiene los valores de la primera columna de cada fila.</returns>
        /// <exception cref="SqlException">Se produce cuando ocurre un error al ejecutar el comando SQL.</exception>
        /// <exception cref="InvalidCastException">Se produce cuando la primera columna no se puede convertir a cadena.</exception>
        /// <remarks>
        /// Si hay una transacción activa, el comando se ejecuta dentro de esa transacción.
        /// Este método es útil para obtener listas de nombres, identificadores u otros valores de texto.
        /// </remarks>
        /// <example>
        /// <code>
        /// List&lt;string&gt; tableNames = session.QueryStrings(
        ///     "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'"
        /// );
        /// </code>
        /// </example>
        public List<string> QueryStrings(string sql, int timeoutSeconds = 120)
        {
            using var cmd = Connection.CreateCommand();
            cmd.Transaction = Transaction;
            cmd.CommandTimeout = timeoutSeconds;
            cmd.CommandText = sql;
            using var r = cmd.ExecuteReader();
            var list = new List<string>();
            while (r.Read())
                list.Add(r.GetString(0));
            return list;
        }

        /// <summary>
        /// Libera todos los recursos utilizados por la sesión de migración.
        /// </summary>
        /// <remarks>
        /// Este método revierte cualquier transacción pendiente y cierra la conexión a la base de datos.
        /// Se recomienda usar esta clase dentro de una instrucción <c>using</c> para garantizar la liberación adecuada de recursos.
        /// </remarks>
        public void Dispose()
        {
            Rollback();
            Connection.Dispose();
        }
    }
}