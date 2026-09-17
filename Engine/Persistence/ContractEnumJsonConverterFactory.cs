using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Graphite.Engine.Persistence;

/// <summary>Persists opt-in enum contracts by stable EnumMember identity, independent of numeric values.</summary>
internal sealed class ContractEnumJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
        => typeToConvert.IsEnum && typeToConvert.IsDefined(typeof(DataContractAttribute), false);

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        => (JsonConverter)Activator.CreateInstance(typeof(ContractEnumConverter<>).MakeGenericType(typeToConvert))!;

    private sealed class ContractEnumConverter<T> : JsonConverter<T> where T : struct, Enum
    {
        private readonly Dictionary<string, T> _values = new(StringComparer.Ordinal);
        private readonly Dictionary<T, string> _names = [];

        public ContractEnumConverter()
        {
            foreach (var field in typeof(T).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                var member = field.GetCustomAttribute<EnumMemberAttribute>();
                if (member is null)
                {
                    continue;
                }
                var name = member.Value ?? field.Name;
                var value = (T)field.GetValue(null)!;
                if (string.IsNullOrWhiteSpace(name) || !_values.TryAdd(name, value) || !_names.TryAdd(value, name))
                {
                    throw new NotSupportedException($"Enum contract {typeof(T)} requires nonempty, unique names and values.");
                }
            }
        }

        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String || !_values.TryGetValue(reader.GetString()!, out var value))
            {
                throw new JsonException($"Expected a declared {typeof(T).Name} identity string.");
            }
            return value;
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            if (!_names.TryGetValue(value, out var name))
            {
                throw new JsonException($"Value '{value}' has no saved identity in {typeof(T).Name}.");
            }
            writer.WriteStringValue(name);
        }
    }
}
