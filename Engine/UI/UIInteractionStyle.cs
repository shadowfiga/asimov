using Graphite.Engine.UI.Animation;
using Graphite.Engine.UI.Audio;
using Myra.Graphics2D.UI;

namespace Graphite.Engine.UI;

public static class UITrigger
{
    public const string Show = "show", Hide = "hide", Hover = "hover", HoverExit = "hover-exit",
        Press = "press", Release = "release", Click = "click", Focus = "focus", Blur = "blur",
        Enabled = "enabled", Disabled = "disabled", Selected = "selected", Deselected = "deselected";
}

public sealed record UIInteractionBinding
{
    public static UIInteractionBinding Empty { get; } = new();
    public UIAnimation? Animation
    {
        get; init;
    }
    public IReadOnlyList<UISoundCue> Sounds { get; init; } = [];
    public float SettleSeconds { get; init; } = .1f;

    internal UIAnimation? CreateAnimation()
    {
        if (Animation is null && Sounds.Count == 0)
        {
            return null;
        }

        var animation = Animation ?? new UIAnimation { Duration = .001f };
        return animation with
        {
            Sounds = animation.Sounds.Concat(Sounds).ToArray()
        };
    }
}

public sealed record UIInteractionStyle
{
    public IReadOnlyDictionary<string, UIInteractionBinding> Bindings { get; init; } = new Dictionary<string, UIInteractionBinding>();
}

public sealed class UIInteractionDefaults
{
    private readonly Dictionary<Type, UIInteractionStyle> _styles = [];
    public void Set<T>(UIInteractionStyle style) where T : Widget => _styles[typeof(T)] = style;
    public UIInteractionStyle Resolve(Type widgetType, UIInteractionStyle? overrides = null)
    {
        var types = new Stack<Type>();
        for (var type = widgetType; type is not null && typeof(Widget).IsAssignableFrom(type); type = type.BaseType)
        {
            types.Push(type);
        }

        var bindings = new Dictionary<string, UIInteractionBinding>();
        foreach (var type in types)
        {
            if (_styles.TryGetValue(type, out var style))
            {
                foreach (var pair in style.Bindings)
                {
                    bindings[pair.Key] = pair.Value;
                }
            }
        }

        if (overrides is not null)
        {
            foreach (var pair in overrides.Bindings)
            {
                bindings[pair.Key] = pair.Value;
            }
        }

        return new UIInteractionStyle { Bindings = bindings };
    }
    internal void Clear() => _styles.Clear();
}
