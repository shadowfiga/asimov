namespace Graphite.Engine.Persistence;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
public sealed class SaveContractAttribute(string id) : Attribute
{
    public string Id { get; } = id;
    public int Version { get; init; } = 1;
}

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class SaveMemberAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}

/// <summary>Validation must only inspect saved data, without accessing scenes or services.</summary>
public interface ISaveValidatable
{
    void Validate();
}
