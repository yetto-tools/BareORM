using System;
using System.Collections.Generic;
using System.Text;

namespace BareORM.Advanced.Graph
{
    public sealed class GraphOptions
    {
        /// <summary>
        /// Si true, cuando un padre no tiene hijos asigna lista vacía (default: true).
        /// </summary>
        public bool AssignEmptyCollections { get; init; } = true;
    }
}
