using System.IO;

namespace BareORM.Migrations.Tooling
{
    /// <summary>
    /// Utilidad para localizar la raíz del proyecto (project root) subiendo directorios desde una ruta base.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Busca hacia arriba desde <paramref name="baseDir"/> hasta <paramref name="maxUp"/> niveles,
    /// y considera “project root” el primer directorio que contenga al menos uno de estos archivos:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Un archivo <c>*.csproj</c> (proyecto .NET)</description></item>
    /// <item><description>Un archivo <c>*.sln</c> (solución de Visual Studio)</description></item>
    /// </list>
    /// <para>
    /// Esto es útil para tooling ejecutado desde:
    /// </para>
    /// <list type="bullet">
    /// <item><description><c>bin/Debug/.../</c></description></item>
    /// <item><description>carpetas internas de tests</description></item>
    /// <item><description>comandos CLI que se corren en subdirectorios</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// var root = ProjectRootFinder.FindFromBaseDirectory(AppContext.BaseDirectory);
    /// Console.WriteLine(root);
    /// </code>
    /// </example>
    public static class ProjectRootFinder
    {
        /// <summary>
        /// Encuentra el project root subiendo desde un directorio base.
        /// </summary>
        /// <param name="baseDir">Directorio desde donde iniciar la búsqueda (p.ej. <c>AppContext.BaseDirectory</c>).</param>
        /// <param name="maxUp">Máximo de niveles a subir antes de fallar.</param>
        /// <returns>Ruta absoluta del project root encontrado.</returns>
        /// <exception cref="InvalidOperationException">
        /// Si no se encuentra un directorio que contenga <c>*.csproj</c> o <c>*.sln</c> dentro del límite.
        /// </exception>
        public static string FindFromBaseDirectory(string baseDir, int maxUp = 12)
        {
            var dir = new DirectoryInfo(baseDir);

            for (int i = 0; i < maxUp && dir is not null; i++, dir = dir.Parent)
            {
                var hasCsproj = dir.EnumerateFiles("*.csproj").Any();
                var hasSln = dir.EnumerateFiles("*.sln").Any();

                if (hasCsproj || hasSln)
                    return dir.FullName;
            }

            throw new InvalidOperationException("No se encontró project root (no hay .csproj o .sln subiendo desde baseDir).");
        }
    }
}
