using System;
using System.Collections.Generic;
using System.Text;

namespace BareORM.Advanced.Split
{
    public sealed class SplitOptions
    {
        /// <summary>
        /// Columna donde “parte” el objeto. Similar a Dapper splitOn.
        /// Default: "Id".
        /// </summary>
        public string SplitOn { get; init; } = "Id";

        /// <summary>
        /// Si true, busca SplitOn ignorando case.
        /// </summary>
        public bool IgnoreCase { get; init; } = true;

        /// <summary>
        /// Si true, si no encuentra SplitOn lanza excepción. Si false, split = mitad.
        /// </summary>
        public bool Strict { get; init; } = true;
    }
}
