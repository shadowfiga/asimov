namespace Graphite.Engine.UI;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public sealed class UIElementAttribute : Attribute
{
    public UIElementAttribute(string? name = null)
    {
        Name = name;
    }

    public string? Name { get; }
}
