using Graphite.Game.UI.Theming;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Myra;
using Myra.Graphics2D;
using Myra.Graphics2D.TextureAtlases;
using Myra.Graphics2D.UI;

namespace Graphite.Game.UI;

internal sealed class MenuAssets : IDisposable
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

    internal Image Icon(string name, int size, Color color, Color? highlightColor = null)
    {
        var region = new TextureRegion(Texture(Path.Combine("Icons", "Lucide", $"{name}.png")));
        var theme = GameThemes.DeepDrive;
        return new Image
        {
            Renderable = new TintedRegion(region, color),
            DisabledRenderable = new TintedRegion(region, theme.Disabled),
            OverRenderable = new TintedRegion(region, highlightColor ?? theme.SelectionHighlight),
            FocusedRenderable = new TintedRegion(region, highlightColor ?? theme.SelectionHighlight),
            PressedRenderable = new TintedRegion(region, theme.DeepBlack),
            Width = size,
            Height = size,
            IsAnisotropicFiltering = true,
            VerticalAlignment = VerticalAlignment.Center
        };
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
