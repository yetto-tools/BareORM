
using BareORM.Abstractions;

namespace BareORM.Core
{
    public sealed class DbSession : IDisposable
    {
        public DbContextLite Db { get; }

        public DbSession(DbContextLite db)
        {
            Db = db;
        }

        public void Dispose()
        {
            // Aquí puedes cerrar recursos si usas conexiones por sesión.
            // Con diseño por operación, normalmente no hace nada.
        }
    } 
   
}
