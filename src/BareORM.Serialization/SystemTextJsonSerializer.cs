using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using BareORM.Abstractions;

namespace BareORM.Serialization
{
    public sealed class SystemTextJsonSerializer : ISerializer
    {
        private readonly JsonSerializerOptions _options;

        public SystemTextJsonSerializer(JsonSerializerOptions? options = null)
        {
            _options = options ?? new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        public string Serialize<T>(T value)
            => JsonSerializer.Serialize(value, _options);
        public string Serialize<T>(T value, JsonSerializerOptions options)
            => JsonSerializer.Serialize(value, options ?? _options);

        public T? Deserialize<T>(string data)
            => JsonSerializer.Deserialize<T>(data, _options);

        public object? Deserialize(string data, Type type)
            => JsonSerializer.Deserialize(data, type, _options);
    }
}
