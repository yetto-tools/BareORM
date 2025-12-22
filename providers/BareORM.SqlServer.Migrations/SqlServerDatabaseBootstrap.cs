using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Data.SqlClient;

namespace BareORM.SqlServer.Migrations
{
    public static class SqlServerDatabaseBootstrap
    {
        public static void EnsureDatabaseExists(string targetConnectionString)
        {
            var csb = new SqlConnectionStringBuilder(targetConnectionString);

            var db = csb.InitialCatalog;
            if (string.IsNullOrWhiteSpace(db))
                throw new InvalidOperationException("Connection string must include Initial Catalog.");

            // Probar abrir directo
            try
            {
                using var test = new SqlConnection(targetConnectionString);
                test.Open();
                return;
            }
            catch (SqlException ex) when (ex.Number == 4060)
            {
                // DB no existe o no accesible; intentar crearla desde master
            }

            var targetDb = db;

            csb.InitialCatalog = "master";
            using (var master = new SqlConnection(csb.ConnectionString))
            {
                master.Open();
                using var cmd = master.CreateCommand();
                cmd.CommandText = $@"
                    IF DB_ID(N'{Lit(targetDb)}') IS NULL
                    BEGIN
                        CREATE DATABASE [{Ident(targetDb)}];
                    END";
                cmd.ExecuteNonQuery();
            }

            // Verificar
            using (var conn = new SqlConnection(targetConnectionString))
            {
                conn.Open();
            }
        }

        private static string Lit(string s) => s.Replace("'", "''");
        private static string Ident(string s) => s.Replace("]", "]]");
    }
}
