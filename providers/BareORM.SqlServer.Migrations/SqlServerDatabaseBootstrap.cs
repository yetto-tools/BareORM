using System;
using Microsoft.Data.SqlClient;

namespace BareORM.SqlServer.Migrations
{
    /// <summary>
    /// Representa el estado de una operación de verificación de base de datos.
    /// </summary>
    /// <remarks>
    /// Este enum se utiliza para indicar el resultado de intentar asegurar que una base de datos 
    /// existe y está correctamente configurada. Los diferentes valores de estado proporcionan información 
    /// sobre qué acción se realizó o por qué se omitió la operación.
    /// </remarks>
    public enum DatabaseEnsureStatus
    {
        /// <summary>
        /// La operación de verificación de la base de datos falló.
        /// </summary>
        /// <remarks>
        /// Este estado indica que ocurrió un error al intentar verificar la base de datos.
        /// Revise los registros para más detalles sobre el error específico.
        /// </remarks>
        Failed = -1,

        /// <summary>
        /// La base de datos ya existe.
        /// </summary>
        /// <remarks>
        /// Se encontró que la base de datos ya existe, por lo que no fue necesaria la creación.
        /// La base de datos existente puede estar o no actualizada con el esquema más reciente.
        /// </remarks>
        AlreadyExists = 0,

        /// <summary>
        /// La base de datos se creó exitosamente.
        /// </summary>
        /// <remarks>
        /// Una nueva base de datos se creó como parte de la operación de verificación.
        /// </remarks>
        Created = 1,

        /// <summary>
        /// La operación de verificación de la base de datos se omitió debido a falta de acceso maestro.
        /// </summary>
        /// <remarks>
        /// La operación no se pudo completar porque la conexión carece de los permisos necesarios 
        /// para crear una base de datos o acceder a la base de datos maestra.
        /// Esto típicamente ocurre al conectarse con credenciales de usuario limitadas.
        /// </remarks>
        SkippedNoMasterAccess = 2,

        /// <summary>
        /// La operación de verificación de la base de datos se omitió debido a falta de permiso de creación.
        /// </summary>
        /// <remarks>
        /// El usuario actual no tiene el permiso CREATE DATABASE requerido para 
        /// crear una nueva base de datos. Un administrador puede necesitar otorgar este permiso.
        /// </remarks>
        SkippedNoCreatePermission = 3,
    }

    /// <summary>
    /// Representa el resultado de un intento de asegurar la existencia de una base de datos SQL Server.
    /// </summary>
    /// <remarks>
    /// Este record encapsula la información resultante de la operación <see cref="SqlServerDatabaseBootstrap.TryEnsureDatabaseExists"/>,
    /// incluyendo el estado final, el nombre de la base de datos y cualquier error que haya ocurrido durante el proceso.
    /// </remarks>
    public sealed record DatabaseEnsureResult(
        DatabaseEnsureStatus Status,
        string Database,
        Exception? Error = null
    );

    /// <summary>
    /// Proporciona utilidades para el proceso de arranque (bootstrapping) de bases de datos SQL Server.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Esta clase ofrece funcionalidad para verificar y crear automáticamente bases de datos SQL Server <br/>
    /// cuando es necesario. Realiza intentos inteligentes de conexión y manejo de permisos para garantizar<br/>
    /// que la base de datos se pueda utilizar con las credenciales proporcionadas.
    /// </para>
    /// <strong>Proceso general:</strong>
    /// <list type="number">
    /// <item> Intenta conectar a la base de datos de destino</item>
    /// <item> Si la conexión falla con error 4060 (base de datos no existe), intenta conectar a master</item>
    /// <item> Si accede a master, verifica si la base de datos existe</item>
    /// <item> Si no existe, intenta crearla</item>
    /// <item> Reintenta conectar a la base de datos de destino varias veces antes de fallar</item>
    /// </list>
    /// </remarks>
    public static class SqlServerDatabaseBootstrap
    {
        /// <summary>
        /// Intenta garantizar que la base de datos especificada existe y es accesible.
        /// </summary>
        /// <remarks>
        /// 
        /// <strong> Este método ejecuta un proceso de verificación y creación de base de datos que:</strong>
        /// <list type="number">
        ///     <item> Intenta conectar a la base de datos de destino indicada en la cadena de conexión</item>
        ///     <item> Si la conexión se rechaza con error 4060 (DB no existe), intenta acceder a la base de datos master</item>
        ///     <item> Verifica si la base de datos existe en el servidor</item>
        ///     <item> Si no existe, intenta crearla con permisos suficientes</item>
        ///     <item> Reintenta conectar a la base de datos creada hasta <paramref name="openRetries"/> veces</item>
        /// </list>
        /// <list type="table">
        ///     <item><strong>Casos de éxito:</strong></item>
        ///     <item> - La base de datos ya existía (estado: <see cref="DatabaseEnsureStatus.AlreadyExists"/>)</item>
        ///     <item> - La base de datos se creó exitosamente (estado: <see cref="DatabaseEnsureStatus.Created"/>)</item>
        /// </list>
        /// <list type="table">
        /// <item><strong>Casos de fallo:</strong></item>
        /// <item> - Cadena de conexión sin Initial Catalog</item>
        /// <item> - Sin acceso a la base de datos master</item>
        /// <item> - Sin permisos para crear base de datos</item>
        /// <item> - La base de datos se creó pero no es accesible tras los reintentos</item>
        /// </list>
        /// </remarks>
        /// <param name="targetConnectionString">
        /// Cadena de conexión a SQL Server que incluye el catálogo inicial (Initial Catalog) 
        /// con el nombre de la base de datos a verificar. Ejemplo: 
        /// "Server=localhost;User Id=sa;Password=pass;Initial Catalog=MiBaseDatos"
        /// </param>
        /// <param name="openRetries">
        /// Número máximo de reintentos para abrir la base de datos de destino después de crearla.
        /// Valor por defecto: 2. El método espera 150ms entre reintentos.
        /// </param>
        /// <returns>
        /// Un objeto <see cref="DatabaseEnsureResult"/> que contiene:
        /// - <strong>Status:</strong> El estado de la operación
        /// - <strong>Database:</strong> El nombre de la base de datos
        /// - <strong>Error:</strong> La excepción ocurrida, si aplica
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Se lanza indirectamente si la cadena de conexión es inválida (retorna Failed).
        /// </exception>
        /// <example>
        /// <code>
        /// var result = SqlServerDatabaseBootstrap.TryEnsureDatabaseExists(
        ///     "Server=localhost;User Id=sa;Password=MyPassword;Initial Catalog=MiDB");
        /// 
        /// if (result.Status == DatabaseEnsureStatus.Created)
        ///     Console.WriteLine("Base de datos creada exitosamente");
        /// else if (result.Status == DatabaseEnsureStatus.Failed)
        ///     Console.WriteLine($"Error: {result.Error?.Message}");
        /// </code>
        /// </example>
        public static DatabaseEnsureResult TryEnsureDatabaseExists(string targetConnectionString, int openRetries = 2)
        {
            var targetCsb = new SqlConnectionStringBuilder(targetConnectionString);
            var dbName = targetCsb.InitialCatalog;

            if (string.IsNullOrWhiteSpace(dbName))
                return new DatabaseEnsureResult(DatabaseEnsureStatus.Failed, "<empty>",
                    new InvalidOperationException("Connection string must include Initial Catalog."));

            // 1) Intentar abrir DB destino primero
            if (TryOpen(targetConnectionString, out var openErr))
                return new DatabaseEnsureResult(DatabaseEnsureStatus.AlreadyExists, dbName);

            // Si falló, vemos si es 4060 (db no existe/no accesible)
            if (openErr is not SqlException sqlEx || sqlEx.Number != 4060)
                return new DatabaseEnsureResult(DatabaseEnsureStatus.Failed, dbName, openErr);

            // 2) Intentar master (sin asumir permisos)
            var masterCsb = new SqlConnectionStringBuilder(targetConnectionString)
            {
                InitialCatalog = "master"
            };

            if (masterCsb.Encrypt && !masterCsb.TrustServerCertificate)
                masterCsb.TrustServerCertificate = true;

            if (!TryOpen(masterCsb.ConnectionString, out var masterOpenErr))
                return new DatabaseEnsureResult(DatabaseEnsureStatus.SkippedNoMasterAccess, dbName, masterOpenErr);

            // 3) Crear DB si no existe
            try
            {
                using var master = new SqlConnection(masterCsb.ConnectionString);
                master.Open();

                if (!DatabaseExists(master, dbName))
                {
                    try
                    {
                        CreateDatabase(master, dbName);
                    }
                    catch (SqlException createEx)
                    {
                        return new DatabaseEnsureResult(DatabaseEnsureStatus.SkippedNoCreatePermission, dbName, createEx);
                    }
                }
            }
            catch (Exception ex)
            {
                return new DatabaseEnsureResult(DatabaseEnsureStatus.Failed, dbName, ex);
            }

            // 4) Reintentar abrir DB destino (máximo N, sin loop infinito)
            for (int i = 0; i < Math.Max(1, openRetries); i++)
            {
                if (TryOpen(targetConnectionString, out _))
                    return new DatabaseEnsureResult(DatabaseEnsureStatus.Created, dbName);

                Thread.Sleep(150);
            }

            return new DatabaseEnsureResult(DatabaseEnsureStatus.Failed, dbName,
                new InvalidOperationException($"Database '{dbName}' exists/was created but cannot be opened after retries."));
        }

        /// <summary>
        /// Intenta abrir una conexión SQL Server usando la cadena de conexión proporcionada.
        /// </summary>
        /// <param name="cs">Cadena de conexión a SQL Server.</param>
        /// <param name="error">
        /// Cuando el método retorna, contiene la excepción que ocurrió, o null si la conexión fue exitosa.
        /// </param>
        /// <returns>
        /// true si la conexión se abrió exitosamente; de lo contrario, false.
        /// </returns>
        private static bool TryOpen(string cs, out Exception? error)
        {
            try
            {
                using var c = new SqlConnection(cs);
                c.Open();
                error = null;
                return true;
            }
            catch (Exception ex)
            {
                error = ex;
                return false;
            }
        }

        /// <summary>
        /// Verifica si una base de datos existe en el servidor SQL Server.
        /// </summary>
        /// <param name="masterConn">Conexión abierta a la base de datos master de SQL Server.</param>
        /// <param name="dbName">Nombre de la base de datos a verificar.</param>
        /// <returns>
        /// true si la base de datos existe; de lo contrario, false.
        /// </returns>
        private static bool DatabaseExists(SqlConnection masterConn, string dbName)
        {
            using var cmd = masterConn.CreateCommand();
            cmd.CommandText = "SELECT CASE WHEN DB_ID(@db) IS NULL THEN 0 ELSE 1 END;";
            cmd.Parameters.AddWithValue("@db", dbName);
            return Convert.ToInt32(cmd.ExecuteScalar()) == 1;
        }

        /// <summary>
        /// Crea una nueva base de datos en SQL Server.
        /// </summary>
        /// <remarks>
        /// Este método require permisos CREATE DATABASE en el servidor SQL Server.
        /// </remarks>
        /// <param name="masterConn">Conexión abierta a la base de datos master de SQL Server.</param>
        /// <param name="dbName">Nombre de la base de datos a crear.</param>
        /// <exception cref="SqlException">
        /// Se lanza si falta el permiso CREATE DATABASE o si ocurre otro error de SQL Server.
        /// </exception>
        private static void CreateDatabase(SqlConnection masterConn, string dbName)
        {
            using var cmd = masterConn.CreateCommand();
            cmd.CommandText = $"CREATE DATABASE [{Ident(dbName)}];";
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Escapa identificadores SQL Server duplicando los corchetes de cierre.
        /// </summary>
        /// <param name="s">Cadena a escapar.</param>
        /// <returns>
        /// La cadena con corchetes de cierre escapados para uso en identificadores SQL Server.
        /// </returns>
        private static string Ident(string s) => s.Replace("]", "]]");
    }
}