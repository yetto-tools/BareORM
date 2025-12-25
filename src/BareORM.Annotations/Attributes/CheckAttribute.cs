namespace BareORM.Annotations.Attributes
{
    /// <summary>
    /// Define una restricción CHECK de base de datos para validar que los valores cumplan una expresión específica.
    /// </summary>
    /// <remarks>
    /// Este atributo permite definir restricciones CHECK (validaciones a nivel de base de datos)
    /// en clases o propiedades de manera declarativa. Las restricciones CHECK garantizan que
    /// solo se permitan valores que cumplan con la expresión especificada.
    /// 
    /// <strong>¿Qué es una restricción CHECK?</strong>
    /// Una restricción CHECK es una regla a nivel de base de datos que valida los datos antes
    /// de insertarlos o actualizarlos. Si un valor no cumple la expresión, la BD rechaza
    /// la operación. Esto proporciona integridad de datos garantizada a nivel de BD.
    /// 
    /// <strong>Ventajas de usar restricciones CHECK:</strong>
    /// <list type="bullet">
    ///     <item>
    ///         <description>
    ///         <strong>Integridad garantizada:</strong> La BD valida datos independientemente
    ///         de qué aplicación intente insertarlos.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///         <strong>Evita datos inválidos:</strong> Impide que datos corruptos se almacenen.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///         <strong>Validación consistente:</strong> Todos acceden a la misma lógica de validación.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///         <strong>Documentación declarativa:</strong> Las reglas están definidas junto con el modelo.
    ///         </description>
    ///     </item>
    /// </list>
    /// 
    /// <strong>Diferencia entre aplicación y base de datos:</strong>
    /// <list type="table">
    ///     <listheader>
    ///         <term>Aspecto</term>
    ///         <description>Validación en Aplicación</description>
    ///         <description>Restricción CHECK en BD</description>
    ///     </listheader>
    ///     <item>
    ///         <term>Ejecución</term>
    ///         <description>En código C#</description>
    ///         <description>En la base de datos</description>
    ///     </item>
    ///     <item>
    ///         <term>Protección</term>
    ///         <description>Solo contra aplicaciones conocidas</description>
    ///         <description>Contra cualquier inserción (apps, queries directas, etc.)</description>
    ///     </item>
    ///     <item>
    ///         <term>Rendimiento</term>
    ///         <description>Rápida (en memoria)</description>
    ///         <description>Un poco más lenta (en BD)</description>
    ///     </item>
    ///     <item>
    ///         <term>Mantenimiento</term>
    ///         <description>Cambios en código</description>
    ///         <description>Cambios en esquema</description>
    ///     </item>
    /// </list>
    /// 
    /// <strong>Contextos de uso:</strong>
    /// <list type="bullet">
    ///     <item>
    ///         <description>
    ///         <strong>En clase:</strong> Restricción sobre múltiples columnas
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///         <strong>En propiedad:</strong> Validación de una columna específica
    ///         </description>
    ///     </item>
    /// </list>
    /// 
    /// <strong>Expresiones comunes en diferentes bases de datos:</strong>
    /// <list type="table">
    ///     <listheader>
    ///         <term>Escenario</term>
    ///         <description>SQL Server</description>
    ///         <description>PostgreSQL</description>
    ///     </listheader>
    ///     <item>
    ///         <term>Valor positivo</term>
    ///         <description>Precio > 0</description>
    ///         <description>Precio > 0</description>
    ///     </item>
    ///     <item>
    ///         <term>Rango</term>
    ///         <description>Edad  &gt;= 18 AND Edad  &lt;= 120</description>
    ///         <description>Edad BETWEEN 18 AND 120</description>
    ///     </item>
    ///     <item>
    ///         <term>Valores específicos</term>
    ///         <description>Estado IN ('Activo', 'Inactivo')</description>
    ///         <description>Estado IN ('Activo', 'Inactivo')</description>
    ///     </item>
    ///     <item>
    ///         <term>Comparación de columnas</term>
    ///         <description>FechaFin >= FechaInicio</description>
    ///         <description>FechaFin >= FechaInicio</description>
    ///     </item>
    /// </list>
    /// </remarks>
    /// <summary>Crea un constraint CHECK.</summary>
    /// <param>
    /// La expresión SQL que debe cumplirse. Por ejemplo: <c>Precio &gt; 0</c>, <c>Edad &gt;= 18 AND Edad &lt;= 120</c>.
    /// No incluya <c>CHECK (</c> ni la palabra clave <c>CHECK</c>; solo la condición.
    /// </param>
    /// <example>
    /// <code>
    /// // Ejemplo 1: Restricción en propiedad (validación de una columna)
    /// public class Producto
    /// {
    ///     public int ProductoId { get; set; }
    ///     
    ///     [Check("Precio > 0")]
    ///     public decimal Precio { get; set; }
    ///     
    ///     [Check("Stock >= 0")]
    ///     public int Stock { get; set; }
    /// }
    /// // Genera en BD: ALTER TABLE Productos ADD CHECK (Precio > 0), CHECK (Stock >= 0)
    /// 
    /// 
    /// // Ejemplo 2: Restricción en clase (validación de múltiples columnas)
    /// [Check("FechaFin >= FechaInicio", Name = "CK_FechasValidas")]
    /// [Check("Precio > 0")]
    /// public class Pedido
    /// {
    ///     public int PedidoId { get; set; }
    ///     public DateTime FechaInicio { get; set; }
    ///     public DateTime FechaFin { get; set; }
    ///     public decimal Precio { get; set; }
    /// }
    /// // Genera en BD:
    /// // ALTER TABLE Pedidos ADD CONSTRAINT CK_FechasValidas CHECK (FechaFin >= FechaInicio)
    /// // ALTER TABLE Pedidos ADD CHECK (Precio > 0)
    /// 
    /// 
    /// // Ejemplo 3: Validación con rangos y IN
    /// public class Usuario
    /// {
    ///     public int UsuarioId { get; set; }
    ///     
    ///     [Check("Edad &gt;= 18 AND Edad &lt;= 120")]
    ///     public int Edad { get; set; }
    ///     
    ///     [Check("Estado IN ('Activo', 'Inactivo', 'Suspendido')")]
    ///     public string Estado { get; set; } = "";
    /// }
    /// 
    /// 
    /// // Ejemplo 4: Restricción con LIKE (SQL Server)
    /// public class Contacto
    /// {
    ///     public int ContactoId { get; set; }
    ///     
    ///     [Check("Email LIKE '%@%.%'")]
    ///     public string Email { get; set; } = "";
    /// }
    /// 
    /// 
    /// // Ejemplo 5: Restricción con funciones (SQL Server)
    /// public class Factura
    /// {
    ///     public int FacturaId { get; set; }
    ///     
    ///     [Check("LEN(NumeroFactura) > 0")]
    ///     public string NumeroFactura { get; set; } = "";
    ///     
    ///     [Check("YEAR(Fecha) >= 2020")]
    ///     public DateTime Fecha { get; set; }
    /// }
    /// 
    /// 
    /// // Ejemplo 6: Restricción compleja con múltiples condiciones
    /// [Check("(Estado = 'Cancelado' AND Saldo = 0) OR (Estado != 'Cancelado' AND Saldo > 0)")]
    /// public class Cuenta
    /// {
    ///     public int CuentaId { get; set; }
    ///     public string Estado { get; set; } = "";
    ///     public decimal Saldo { get; set; }
    /// }
    /// // Asegura que: si está cancelada, debe tener saldo 0; si no está cancelada, debe tener saldo > 0
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = true)]
    public sealed class CheckAttribute : Attribute
    {
        /// <summary>
        /// Obtiene la expresión SQL de la restricción CHECK que se aplicará en la base de datos.
        /// </summary>
        /// <remarks>
        /// Esta expresión define la condición que deben cumplir los valores para ser aceptados.
        /// La expresión se ejecuta automáticamente por la BD en cada INSERT o UPDATE.
        /// 
        /// <strong>Sintaxis esperada:</strong>
        /// - Expresión SQL válida para la base de datos destino (SQL Server, PostgreSQL, MySQL, Oracle, etc.)
        /// - No incluya "CHECK (" ni paréntesis de envoltura; solo la condición
        /// - Use nombres de columnas exactos tal como aparecen en la tabla
        /// - Operadores soportados: &gt;, &lt;, =, &lt;&gt;, &gt;=, &lt;=, AND, OR, NOT, IN, LIKE, BETWEEN, etc.
        /// 
        /// <strong>Ejemplos válidos:</strong>
        /// - "Precio > 0"
        /// - "Edad &gt;= 18 AND Edad &lt;= 65"
        /// - "Estado IN ('Activo', 'Inactivo')"
        /// - "FechaFin >= FechaInicio"
        /// - "YEAR(Fecha) >= 2020" (SQL Server)
        /// - "EXTRACT(YEAR FROM Fecha) >= 2020" (PostgreSQL)
        /// </remarks>
        /// <value>La expresión CHECK de la restricción. Nunca es null.</value>
        public string Expression { get; }

        /// <summary>
        /// Obtiene o establece el nombre explícito de la restricción CHECK en la base de datos.
        /// </summary>
        /// <remarks>
        /// Si se proporciona un nombre, la restricción se crea con ese nombre específico.
        /// Esto es útil para:
        /// <list type="bullet">
        ///     <item>
        ///         <description>
        ///         <strong>Identificación:</strong> Facilita identificar la restricción en logs de error
        ///         (ej: "Violation of constraint CK_Precio_Positivo")
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <description>
        ///         <strong>Administración:</strong> Permite modificar o eliminar restricciones específicas
        ///         por nombre.
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <description>
        ///         <strong>Documentación:</strong> Nombres descriptivos documentan el propósito de la restricción.
        ///         </description>
        ///     </item>
        /// </list>
        /// 
        /// Si es <c>null</c> (predeterminado), la base de datos genera automáticamente un nombre
        /// (típicamente con formato como CK_TableName_Sequence).
        /// 
        /// <strong>Convención de nombres:</strong>
        /// Se recomienda seguir patrones como:
        /// - "CK_" + NombreTabla + "_" + Descripción
        /// - Ejemplo: "CK_Productos_PrecioPositivo", "CK_Usuarios_EdadValida"
        /// </remarks>
        /// <value>
        /// El nombre de la restricción, o <c>null</c> para dejar que la BD genere uno automáticamente.
        /// </value>
        public string? Name { get; set; }

        /// <summary>
        /// Inicializa una nueva instancia del atributo CheckAttribute.
        /// </summary>
        /// <remarks>
        /// Crea una restricción CHECK con la expresión especificada.
        /// El atributo se aplica a la clase o propiedad marcada, y el ORM lo traduce
        /// en una restricción CHECK en la base de datos durante la creación del esquema.
        /// </remarks>
        /// <param name="expression">
        /// La expresión SQL de la restricción. Por ejemplo: "Precio > 0".
        /// No puede ser nula ni vacía.
        /// </param>
        /// <example>
        /// <code>
        /// // Uso simple sin nombre
        /// [Check("Precio > 0")]
        /// public decimal Precio { get; set; }
        /// 
        /// // Uso con nombre explícito
        /// [Check("Precio > 0", Name = "CK_Precio_Positivo")]
        /// public decimal Precio { get; set; }
        /// </code>
        /// </example>
        public CheckAttribute(string expression) => Expression = expression;
    }
}