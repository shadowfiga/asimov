using Graphite.Engine.UI;
using Graphite.Engine.UI.Animation;
using Graphite.Engine.UI.Materials;
using Graphite.Game.UI.Theming;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Myra.Graphics2D.UI;

namespace Graphite.Game.UI.Materials;

public sealed class CrtMaterial() : UIShaderMaterial("Content/Shaders/Crt.mgfxo")
{
    public static UIParameter<float> Strength { get; } = new("Strength", 1);
    protected override void Configure(Effect effect, UIMaterialContext context)
    {
        effect.Parameters["Time"].SetValue(context.ElapsedTime);
        effect.Parameters["Dimensions"].SetValue(context.Dimensions);
        effect.Parameters["Strength"].SetValue(context.Parameters.Get(Strength));
    }
}

public sealed class FlowerMaterial(GameTheme theme) : UIShaderMaterial("Content/Shaders/Flowers.mgfxo")
{
    public const int Padding = 48;
    public static UIParameter<float> Progress { get; } = new("Progress", 1);
    protected override bool IsActive(UIParameters parameters) => parameters.Get(Progress) < 1;
    protected override void Configure(Effect effect, UIMaterialContext context)
    {
        effect.Parameters["Dimensions"].SetValue(context.Dimensions);
        effect.Parameters["Progress"].SetValue(context.Parameters.Get(Progress));
        effect.Parameters["Padding"].SetValue((float)Padding);
        effect.Parameters["PetalColor"].SetValue(theme.Color2.ToVector3());
        effect.Parameters["AlternateColor"].SetValue(theme.Color3.ToVector3());
        effect.Parameters["CenterColor"].SetValue(theme.Foreground.ToVector3());
    }
}

public static class MenuPresentation
{
    public static UIMaterial Crt { get; } = new CrtMaterial();
    public static UIMaterial Flowers { get; } = new FlowerMaterial(GameThemes.Aftergreen);
    public static UIAnimation Wiggle { get; } = new()
    {
        Duration = .3f,
        Tracks = [new UITransformTrack((frame, t) => frame.Rotate(MathF.Sin(t * MathF.PI * 6) * (1 - t) * 2.5f))]
    };
    public static UIAnimation Bloom { get; } = new()
    {
        Duration = .8f,
        Tracks = [new UIParameterTrack<float>(FlowerMaterial.Progress, 0, 1, MathHelper.Lerp)]
    };
    public static UIAnimation FadeOut { get; } = new()
    {
        Duration = .15f,
        Tracks = [new UITransformTrack((frame, t) => frame.Fade(1 - t)) { Easing = UIEasing.Smooth }]
    };
    public static UIInteractionStyle ContentStyle { get; } = new()
    {
        Bindings = new Dictionary<string, UIInteractionBinding>
        {
            [UITrigger.Show] = new() { Animation = Bloom },
            [UITrigger.Hide] = new() { Animation = FadeOut }
        }
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
                [UITrigger.Hover] = new() { Animation = Wiggle },
                [UITrigger.Focus] = new() { Animation = Wiggle }
            }
        });
    }
}
