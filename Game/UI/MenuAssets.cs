using Graphite.Game.UI.Theming;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Myra;
using Myra.Graphics2D;
using Myra.Graphics2D.TextureAtlases;
using Myra.Graphics2D.UI;

namespace Graphite.Game.UI;

internal sealed class MenuAssets : IBrush, IDisposable
{
    private readonly Dictionary<string, Texture2D> _textures = [];

    private Texture2D Texture(string asset)
    {
        if (!_textures.TryGetValue(asset, out var texture))
        {
            using var stream = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "Content", asset));
            texture = Texture2D.FromStream(MyraEnvironment.GraphicsDevice, stream, DefaultColorProcessors.PremultiplyAlpha);
            _textures.Add(asset, texture);
        }

        return texture;
    }

    internal Image Icon(string name, int size, Color color)
    {
        var region = new TextureRegion(Texture(Path.Combine("Icons", "Lucide", $"{name}.png")));
        var theme = GameThemes.Aftergreen;
        return new Image
        {
            Renderable = new TintedRegion(region, color),
            DisabledRenderable = new TintedRegion(region, theme.Gray4),
            OverRenderable = new TintedRegion(region, theme.Foreground),
            FocusedRenderable = new TintedRegion(region, theme.Foreground),
            Width = size,
            Height = size,
            IsAnisotropicFiltering = true,
            VerticalAlignment = VerticalAlignment.Center
        };
    }

    public void Draw(RenderContext context, Rectangle dest, Color color)
    {
        if (dest.Width <= 0 || dest.Height <= 0)
        {
            return;
        }

        var texture = Texture(Path.Combine("Textures", "MainMenuBackground.jpg"));
        var scale = Math.Max((float)dest.Width / texture.Width, (float)dest.Height / texture.Height);
        var width = Math.Min(texture.Width, (int)MathF.Round(dest.Width / scale));
        var height = Math.Min(texture.Height, (int)MathF.Round(dest.Height / scale));
        var source = new Rectangle((int)((texture.Width - width) * .2f), (texture.Height - height) / 2, width, height);
        context.Draw(texture, dest, source, color);

        // Keep the menu readable while retaining the scenery beneath the glass surfaces.
        var shade = GameThemes.Aftergreen.BackgroundDark;
        var fadeWidth = Math.Max(1, (int)(dest.Width * .55f));
        for (var x = 0; x < fadeWidth; x += 4)
        {
            var opacity = .48f * MathF.Pow(1 - (float)x / fadeWidth, 1.5f);
            context.FillRectangle(new Rectangle(dest.X + x, dest.Y, Math.Min(4, fadeWidth - x), dest.Height), shade * opacity);
        }
    }

    public void Dispose()
    {
        foreach (var texture in _textures.Values)
        {
            texture.Dispose();
        }

        _textures.Clear();
    }
}
