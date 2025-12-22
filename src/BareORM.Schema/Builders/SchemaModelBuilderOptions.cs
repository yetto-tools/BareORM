using System;
using System.Collections.Generic;
using System.Text;
using BareORM.Schema.Types;

namespace BareORM.Schema.Builders
{
    public sealed class SchemaModelBuilderOptions
    {
        public string DefaultSchema { get; init; } = "dbo";
        public IClrTypeMapper TypeMapper { get; init; } = new DefaultClrTypeMapper();

        /// <summary>Si true, solo modela entidades con [Table].</summary>
        public bool RequireTableAttribute { get; init; } = false;

        /// <summary>Convención: PK_..., UQ_..., FK_..., CK_...</summary>
        public bool UseConventionalConstraintNames { get; init; } = true;
    }
}
