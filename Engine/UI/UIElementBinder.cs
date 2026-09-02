using System.Reflection;
using Graphite.Engine.UI.Controls;
using Graphite.Engine.UI.Gum;

namespace Graphite.Engine.UI;

internal static class UIElementBinder
{
    public static void Bind(UIScreen screen, GumScreenInstance visual)
    {
        for (var type = screen.GetType(); type is not null && type != typeof(UIScreen); type = type.BaseType)
        {
            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            foreach (var field in fields)
            {
                var attribute = field.GetCustomAttribute<UIElementAttribute>();
                if (attribute is null)
                {
                    continue;
                }

                if (field.IsInitOnly)
                {
                    throw new InvalidOperationException($"[UIElement] field {type.FullName}.{field.Name} cannot be readonly.");
                }

                var elementName = attribute.Name ?? InferElementName(field.Name);
                var control = CreateControl(field.FieldType, visual, elementName);
                field.SetValue(screen, control);
            }
        }
    }

    private static object CreateControl(Type fieldType, GumScreenInstance visual, string elementName)
    {
        if (fieldType == typeof(Text))
        {
            return new Text(visual.Get<global::Gum.Forms.Controls.Label>(elementName));
        }

        if (fieldType == typeof(Button))
        {
            return new Button(visual.Get<global::Gum.Forms.Controls.Button>(elementName));
        }

        throw new InvalidOperationException(
            $"[UIElement] does not support fields of type {fieldType.FullName}. Add a Graphite UI control adapter first.");
    }

    private static string InferElementName(string fieldName)
    {
        var name = fieldName.TrimStart('_');
        if (name.Length == 0)
        {
            throw new InvalidOperationException($"Cannot infer a Gum element name from field '{fieldName}'.");
        }

        return char.ToUpperInvariant(name[0]) + name[1..];
    }
}
