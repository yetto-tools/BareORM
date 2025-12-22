using System;
using System.Collections.Generic;
using System.Text;
using BareORM.Abstractions;

namespace BareORM.Mapping
{

    public sealed class EntityMapperFactory
    {
        private readonly MappingOptions _options;

        public EntityMapperFactory(MappingOptions? options = null)
            => _options = options ?? new MappingOptions();

        public IEntityMapper<T> Create<T>() where T : new()
            => new DefaultEntityMapper<T>(_options);
    }
}
