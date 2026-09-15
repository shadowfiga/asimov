using Graphite.Engine.Graphics;
using Microsoft.Xna.Framework.Graphics;

namespace Graphite.Game.Graphics;

public sealed class CrtFilter() : ShaderScreenFilter("Content/Shaders/Crt.mgfxo")
{
    public static ScreenFilterParameter<float> Scanlines { get; } = new("Scanlines", 0);
    public static ScreenFilterParameter<float> Noise { get; } = new("Noise", 0);
    public static ScreenFilterParameter<float> Vignette { get; } = new("Vignette", 0);
    public static ScreenFilterParameter<float> Bloom { get; } = new("Bloom", 0);

    public static void Configure(ScreenFilterParameters parameters, float intensity)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        if (!float.IsFinite(intensity) || intensity is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(intensity));
        }

        // This is the original Aged profile; the settings control scales the
        // complete recipe without changing its balance.
        parameters.Set(Scanlines, .1f * intensity);
        parameters.Set(Noise, .02f * intensity);
        parameters.Set(Vignette, .28f * intensity);
        parameters.Set(Bloom, .2f * intensity);
    }

    protected override bool IsActive(ScreenFilterParameters parameters)
        => parameters.Get(Scanlines) > 0 || parameters.Get(Noise) > 0 || parameters.Get(Vignette) > 0 ||
           parameters.Get(Bloom) > 0;

    protected override void Configure(Effect effect, ScreenFilterContext context)
    {
        effect.Parameters["Time"].SetValue(context.ElapsedTime);
        effect.Parameters["Dimensions"].SetValue(context.Dimensions);
        effect.Parameters["Scanlines"].SetValue(context.Parameters.Get(Scanlines));
        effect.Parameters["Noise"].SetValue(context.Parameters.Get(Noise));
        effect.Parameters["Vignette"].SetValue(context.Parameters.Get(Vignette));
        effect.Parameters["Bloom"].SetValue(context.Parameters.Get(Bloom));
    }
}

public static class CrtPresentation
{
    public static ScreenFilter Aged { get; } = new CrtFilter();
}
