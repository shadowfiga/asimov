using Graphite.Engine.UI;
using Graphite.Engine.UI.Animation;
using Graphite.Engine.UI.Materials;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Myra.Graphics2D.UI;

namespace Graphite.Game.UI.Materials;

public sealed class CrtMaterial() : UIShaderMaterial("Content/Shaders/Crt.mgfxo")
{
    public static UIParameter<float> Strength { get; } = new("Strength", 0);
    protected override bool IsActive(UIParameters parameters) => parameters.Get(Strength) > 0;
    protected override void Configure(Effect effect, UIMaterialContext context)
    {
        effect.Parameters["Time"].SetValue(context.ElapsedTime);
        effect.Parameters["Dimensions"].SetValue(context.Dimensions);
        effect.Parameters["Strength"].SetValue(context.Parameters.Get(Strength));
    }
}

public static class MenuPresentation
{
    public static UIMaterial Crt { get; } = new CrtMaterial();
    public static UIAnimation CrtHover { get; } = new()
    {
        Duration = 1,
        Repeat = 0,
        Tracks = [new UIParameterTrack<float>(CrtMaterial.Strength, 1, 1, MathHelper.Lerp)]
    };
    public static UIAnimation FadeOut { get; } = new()
    {
        Duration = .15f,
        Tracks = [new UITransformTrack((frame, t) => frame.Fade(1 - t)) { Easing = UIEasing.Smooth }]
    };
    public static UIInteractionStyle FadeStyle { get; } = new()
    {
        Bindings = new Dictionary<string, UIInteractionBinding> { [UITrigger.Hide] = new() { Animation = FadeOut } }
    };
    public static void Initialize()
    {
        Engine.UI.UI.Interactions.Set<ButtonBase>(new UIInteractionStyle
        {
            Bindings = new Dictionary<string, UIInteractionBinding>
            {
                [UITrigger.Hover] = new() { Animation = CrtHover, SettleSeconds = 0 }
            }
        });
    }
}
