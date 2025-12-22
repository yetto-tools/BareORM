using System;
using System.Collections.Generic;
using System.Text;
using BareORM.Abstractions;

namespace BareORM.Mapping
{
    public enum MappingMode
    {
        ByName,
        ByOrdinal
    }

    public sealed class MappingOptions
    {
        /// <summary>Mapeo por nombre de columna</summary>
        public MappingMode Mode { get; init; } = MappingMode.ByName;

        /// <summary>Si true, ignora mayúsculas/minúsculas en match de columnas.</summary>
        public bool IgnoreCase { get; init; } = true;

        /// <summary>Si true, permite mapear "user_id" ↔ "UserId".</summary>
        public bool NormalizeUnderscores { get; init; } = true;

        /// <summary>Si true, si una propiedad no tiene columna, lanza error. Default: false (tolerante).</summary>
        public bool StrictColumnMatch { get; init; } = false;

        /// <summary>Política de nombres (ej. snake_case). Default: normalize underscores.</summary>
        public Func<string, string>? NamePolicy { get; init; } = NamePolicies.Default;

        /// <summary>Para ByOrdinal: si true, exige que existan todas las columnas requeridas.</summary>
        public bool StrictOrdinalMatch { get; init; } = true;


        /// <summary>Trabajar con JSON (serialización/deserialización)</summary>
        public ISerializer? Serializer { get; init; }
    }
}
