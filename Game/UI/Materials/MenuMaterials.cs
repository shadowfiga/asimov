using Graphite.Engine.UI;
using Graphite.Engine.UI.Animation;

namespace Graphite.Game.UI.Materials;

public static class MenuPresentation
{
    public static UIAnimation FadeOut
    {
        get;
    } = new()
    {
        Duration = .15f,
        Tracks = [new UITransformTrack((frame, t) => frame.Fade(1 - t)) { Easing = UIEasing.Smooth }]
    };
    public static UIInteractionStyle FadeStyle
    {
        get;
    } = new()
    {
        Bindings = new Dictionary<string, UIInteractionBinding> { [UITrigger.Hide] = new() { Animation = FadeOut } }
    };
}
