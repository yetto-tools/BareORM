using System;
using System.Collections.Generic;
using System.Text;

namespace BareORM.Annotations.Attributes
{

    /// <summary>
    /// 
    /// </summary>
    public enum ValueGenerationStrategy
    {
        None,           // lo pone la app
        Database,       // lo genera la DB (identity/auto_increment/sequence)
        Sequence        // lo genera una secuencia explícita (Oracle/Postgres/otros)
    }
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class ValueGeneratedAttribute : Attribute
    {
        public ValueGenerationStrategy Strategy { get; }
        public string? SequenceName { get; set; }        // para Strategy.Sequence
        public bool RetrieveAfterInsert { get; set; } = true; // si quieres devolver Id

        public ValueGeneratedAttribute(ValueGenerationStrategy strategy = ValueGenerationStrategy.Database)
            => Strategy = strategy;
    }
}
