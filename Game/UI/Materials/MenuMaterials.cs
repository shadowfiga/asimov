using Graphite.Engine.UI;
using Graphite.Engine.UI.Animation;
using Graphite.Engine.UI.Materials;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Graphite.Game.UI.Materials;

public sealed class CrtMaterial() : UIShaderMaterial("Content/Shaders/Crt.mgfxo")
{
    public static UIParameter<float> Scanlines { get; } = new("Scanlines", 0);
    public static UIParameter<float> Noise { get; } = new("Noise", 0);
    public static UIParameter<float> Vignette { get; } = new("Vignette", 0);
    public static UIParameter<float> Bloom { get; } = new("Bloom", 0);
    public static void Configure(UIParameters parameters, float intensity)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        if (!float.IsFinite(intensity) || intensity is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(intensity));
        }

        // Fifty percent matches the former full Aged treatment; the upper half
        // deliberately pushes into a more pronounced, weathered terminal look.
        parameters.Set(Scanlines, .1f * intensity);
        parameters.Set(Noise, .02f * intensity);
        parameters.Set(Vignette, .28f * intensity);
        parameters.Set(Bloom, .2f * intensity);
    }

    protected override bool IsActive(UIParameters parameters)
        => parameters.Get(Scanlines) > 0 || parameters.Get(Noise) > 0 || parameters.Get(Vignette) > 0 ||
           parameters.Get(Bloom) > 0;

    protected override void Configure(Effect effect, UIMaterialContext context)
    {
        effect.Parameters["Time"].SetValue(context.ElapsedTime);
        effect.Parameters["Dimensions"].SetValue(context.Dimensions);
        effect.Parameters["Scanlines"].SetValue(context.Parameters.Get(Scanlines));
        effect.Parameters["Noise"].SetValue(context.Parameters.Get(Noise));
        effect.Parameters["Vignette"].SetValue(context.Parameters.Get(Vignette));
        effect.Parameters["Bloom"].SetValue(context.Parameters.Get(Bloom));
    }
}

public static class MenuPresentation
{
    public static UIMaterial Crt { get; } = new CrtMaterial();
    public static UIAnimation FadeOut { get; } = new()
    {
        Duration = .15f,
        Tracks = [new UITransformTrack((frame, t) => frame.Fade(1 - t)) { Easing = UIEasing.Smooth }]
    };
    public static UIInteractionStyle FadeStyle { get; } = new()
    {
        Bindings = new Dictionary<string, UIInteractionBinding> { [UITrigger.Hide] = new() { Animation = FadeOut } }
    };
}
