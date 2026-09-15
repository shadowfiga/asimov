using FontStashSharp;
using Graphite.Engine.Configuration;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Graphite.Engine.Graphics;

/// <summary>A host-owned diagnostic print, independent of scenes, UI layout, and post-processing.</summary>
public static class EnvironmentOverlay
{
    private static SpriteBatch? _batch;
    private static SpriteFontBase? _font;
    private static Color _color;
    private static Color _shadow;
    private static int _margin;

    internal static bool IsInitialized => _batch is not null;
    internal static string Text { get; private set; } = string.Empty;

    internal static void Initialize(GraphicsDevice graphicsDevice, SettingsEnvironment environment)
    {
        Shutdown();
        _batch = new SpriteBatch(graphicsDevice);
        Text = environment.ToString().ToUpperInvariant();
    }

    /// <summary>Uses a shared font at its native pixel size; the caller retains font ownership.</summary>
    public static void Configure(SpriteFontBase font, Color color, Color shadow, int margin)
    {
        ArgumentNullException.ThrowIfNull(font);
        ArgumentOutOfRangeException.ThrowIfNegative(margin);
        _font = font;
        _color = color;
        _shadow = shadow;
        _margin = margin;
    }

    internal static void Draw()
    {
        if (_batch is null || _font is null)
        {
            return;
        }

        // Physical pixels, recomputed every frame rather than using the UI's logical transform.
        var position = new Vector2(_margin, Math.Max(0, _batch.GraphicsDevice.Viewport.Height - _margin - _font.LineHeight));
        _batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp,
            DepthStencilState.None, RasterizerState.CullNone);
        _font.DrawText(_batch, Text, position + Vector2.One, _shadow);
        _font.DrawText(_batch, Text, position, _color);
        _batch.End();
    }

    internal static void Shutdown()
    {
        _batch?.Dispose();
        _batch = null;
        _font = null;
        Text = string.Empty;
    }
}
