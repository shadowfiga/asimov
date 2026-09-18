using System.Collections;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Graphite.Engine.Persistence;

/// <summary>Opt-in, value-based JSON serialization. Cycles and polymorphic objects are unsupported.</summary>
public sealed class SaveSerializer
{
    private readonly JsonSerializerOptions _options = new()
    {
        TypeInfoResolver = new DefaultJsonTypeInfoResolver { Modifiers = { ConfigureContract } },
        MaxDepth = 64
    };

    public SaveSerializer(params JsonConverter[] converters)
    {
        foreach (var converter in converters)
        {
            _options.Converters.Add(converter);
        }
        _options.Converters.Add(new ContractEnumJsonConverterFactory());
    }

    public SaveContractAttribute Contract<T>() => Contract(typeof(T));

    public byte[] Serialize<T>(T value)
    {
        ArgumentNullException.ThrowIfNull(value);
        CheckType(typeof(T), []);
        CheckRuntimeType(typeof(T), value);
        try
        {
            return JsonSerializer.SerializeToUtf8Bytes(value, _options);
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }
    }

    public T Deserialize<T>(ReadOnlySpan<byte> data)
    {
        CheckType(typeof(T), []);
        try
        {
            var value = JsonSerializer.Deserialize<T>(data, _options);
            if (CollectionImplementation(typeof(T)) is not null)
            {
                return (T)NormalizeCollection(typeof(T), value);
            }

            if (value is null && Nullable.GetUnderlyingType(typeof(T)) is null)
            {
                throw new InvalidDataException("The saved root cannot be null.");
            }

            return value!;
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }
    }

    private static SaveContractAttribute Contract(Type type)
    {
        var contract = type.GetCustomAttribute<SaveContractAttribute>(false)
            ?? throw new NotSupportedException($"{type} needs a SaveContract attribute.");
        if (string.IsNullOrWhiteSpace(contract.Id) || contract.Version < 1)
        {
            throw new NotSupportedException($"{type} has an invalid save contract ID or version.");
        }

        return contract;
    }

    private static bool IsScalar(Type type) => type.IsEnum || type == typeof(string) || type == typeof(bool)
        || type == typeof(byte) || type == typeof(sbyte) || type == typeof(short) || type == typeof(ushort)
        || type == typeof(int) || type == typeof(uint) || type == typeof(long) || type == typeof(ulong)
        || type == typeof(float) || type == typeof(double) || type == typeof(decimal) || type == typeof(char)
        || type == typeof(Guid) || type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(TimeSpan);

    private static Type? CollectionImplementation(Type type)
    {
        if (type.IsArray)
        {
            return type.GetArrayRank() == 1 ? type : null;
        }

        if (!type.IsGenericType)
        {
            return null;
        }

        var definition = type.GetGenericTypeDefinition();
        var arguments = type.GetGenericArguments();
        if (definition == typeof(List<>) || definition == typeof(HashSet<>) || definition == typeof(Dictionary<,>))
        {
            return type;
        }

        if (definition == typeof(IReadOnlyList<>) || definition == typeof(IReadOnlyCollection<>) || definition == typeof(IList<>) || definition == typeof(ICollection<>))
        {
            return typeof(List<>).MakeGenericType(arguments);
        }

        if (definition == typeof(IReadOnlyDictionary<,>) || definition == typeof(IDictionary<,>))
        {
            return typeof(Dictionary<,>).MakeGenericType(arguments);
        }

        return null;
    }

    private static object EmptyCollection(Type implementation) => implementation.IsArray
        ? Array.CreateInstance(implementation.GetElementType()!, 0)
        : Activator.CreateInstance(implementation)!;

    private static object NormalizeCollection(Type type, object? value)
    {
        var implementation = CollectionImplementation(type)!;
        if (value is null)
        {
            return EmptyCollection(implementation);
        }

        var arguments = type.IsArray ? [type.GetElementType()!] : type.GetGenericArguments();
        var elementType = arguments[^1];
        if (CollectionImplementation(elementType) is null)
        {
            return value;
        }

        if (value is IDictionary dictionary)
        {
            foreach (var key in dictionary.Keys.Cast<object>().ToArray())
            {
                dictionary[key] = NormalizeCollection(elementType, dictionary[key]);
            }
        }
        else if (value is IList list)
        {
            for (var i = 0; i < list.Count; i++)
            {
                list[i] = NormalizeCollection(elementType, list[i]);
            }
        }
        else
        {
            var normalized = EmptyCollection(implementation);
            var add = implementation.GetMethod("Add")!;
            foreach (var item in (IEnumerable)value)
            {
                add.Invoke(normalized, [NormalizeCollection(elementType, item)]);
            }

            return normalized;
        }

        return value;
    }

    private void CheckType(Type type, HashSet<Type> visited)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        if (!visited.Add(type) || IsScalar(type) || _options.Converters.Any(converter => converter.CanConvert(type)))
        {
            return;
        }

        var collection = CollectionImplementation(type);
        if (collection is not null)
        {
            var arguments = type.IsArray ? [type.GetElementType()!] : type.GetGenericArguments();
            if (arguments.Length == 2 && arguments[0] != typeof(string))
            {
                throw new NotSupportedException("Save dictionaries require string keys.");
            }

            foreach (var argument in arguments)
            {
                CheckType(argument, visited);
            }

            return;
        }

        _ = Contract(type);
        if (type.IsAbstract || type.IsInterface)
        {
            throw new NotSupportedException($"Save member type {type} must be concrete.");
        }

        foreach (var member in Members(type))
        {
            CheckType(MemberType(member), visited);
        }
    }

    private static IEnumerable<MemberInfo> Members(Type type)
    {
        for (var current = type; current is not null && current != typeof(object); current = current.BaseType)
        {
            foreach (var member in current.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                if (member.IsDefined(typeof(SaveMemberAttribute)))
                {
                    yield return member;
                }
            }
        }
    }

    private static Type MemberType(MemberInfo member) => member switch
    {
        FieldInfo field when !field.IsStatic && !field.IsInitOnly => field.FieldType,
        PropertyInfo property when property.GetMethod is { IsStatic: false } && property.SetMethod is { IsStatic: false }
            && property.GetIndexParameters().Length == 0 => property.PropertyType,
        _ => throw new NotSupportedException($"Saved member {member.DeclaringType}.{member.Name} must be a writable instance field or property.")
    };

    private static void CheckRuntimeType(Type declared, object value)
    {
        declared = Nullable.GetUnderlyingType(declared) ?? declared;
        if (value.GetType() != declared && CollectionImplementation(declared) is null)
        {
            throw new NotSupportedException($"Polymorphic save member {declared} contains {value.GetType()}. Store a stable ID or a concrete data type.");
        }
    }

    private static void ConfigureContract(JsonTypeInfo info)
    {
        if (info.Kind != JsonTypeInfoKind.Object)
        {
            return;
        }

        _ = Contract(info.Type);
        var constructor = info.Type.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null);
        if (!info.Type.IsValueType && constructor is null)
        {
            throw new NotSupportedException($"{info.Type} needs a parameterless constructor for loading.");
        }

        info.CreateObject = () => info.Type.IsValueType ? Activator.CreateInstance(info.Type)! : constructor!.Invoke(null);
        info.Properties.Clear();
        var names = new HashSet<string>(StringComparer.Ordinal);
        var normalizeCollections = new List<Action<object>>();
        foreach (var member in Members(info.Type))
        {
            var type = MemberType(member);
            var name = member.GetCustomAttribute<SaveMemberAttribute>()!.Name;
            if (string.IsNullOrWhiteSpace(name) || !names.Add(name))
            {
                throw new NotSupportedException($"{info.Type} has an empty or duplicate saved member name: {name}.");
            }

            Func<object, object?> get = member is FieldInfo field ? field.GetValue : ((PropertyInfo)member).GetValue;
            Action<object, object?> set = member is FieldInfo writable ? writable.SetValue : ((PropertyInfo)member).SetValue;
            var collection = CollectionImplementation(type);
            if (collection is not null)
            {
                normalizeCollections.Add(instance =>
                {
                    if (get(instance) is null)
                    {
                        set(instance, EmptyCollection(collection));
                    }
                });
            }

            var property = info.CreateJsonPropertyInfo(type, name);
            property.Get = instance =>
            {
                var value = get(instance);
                if (value is not null)
                {
                    CheckRuntimeType(type, value);
                }

                return value ?? (collection is null ? null : EmptyCollection(collection));
            };
            property.Set = (instance, value) => set(instance, collection is null ? value : NormalizeCollection(type, value));
            info.Properties.Add(property);
        }

        info.OnSerializing = instance =>
        {
            CheckRuntimeType(info.Type, instance);
            (instance as ISaveValidatable)?.Validate();
        };
        info.OnDeserialized = instance =>
        {
            foreach (var normalize in normalizeCollections)
            {
                normalize(instance);
            }

            (instance as ISaveValidatable)?.Validate();
        };
    }
}
