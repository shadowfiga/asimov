using System.Text;
using System.Xml.Linq;
using AssetManagementBase;
using Myra;
using Myra.Graphics2D.UI.Styles;
using FontStashSharp;
using System.Globalization;
using Ui = Graphite.Engine.UI.UI;

namespace Graphite.Game.UI.Theming;

internal static class ThemeAssets
{
    private const string FontFile = "Abel-Regular.ttf";
    private const string StylesheetFile = "default_ui_skin.xmms";
    private static AssetManager? _assets;
    private static readonly HashSet<FontSystem> ThemeFontSystems = [];
    private static readonly Dictionary<int, FontSystem> ResolutionFonts = [];
    private static byte[] _fontData = [];
    internal static int ResolutionFontCount => ResolutionFonts.Count;

    internal static Stylesheet LoadStylesheet()
    {
        if (_assets is null)
        {
            _assets = new AssetManager(new Accessor(), string.Empty);
            MyraEnvironment.Game.Disposed += Release;
        }

        var stylesheet = _assets.LoadStylesheet(StylesheetFile);
        foreach (var font in _assets.Cache.Values.OfType<DynamicSpriteFont>())
        {
            ThemeFontSystems.Add(font.FontSystem);
        }
        foreach (var system in _assets.Cache.Values.OfType<FontSystem>())
        {
            ThemeFontSystems.Add(system);
        }
        Ui.SetFontResolver(ResolveFont);
        return stylesheet;
    }

    internal static SpriteFontBase Font(int size)
    {
        _ = LoadStylesheet();
        var font = _assets!.LoadFont($"{FontFile}:{size.ToString(CultureInfo.InvariantCulture)}");
        if (font is DynamicSpriteFont dynamicFont)
        {
            ThemeFontSystems.Add(dynamicFont.FontSystem);
        }
        return ResolveFont(font, Ui.Scale);
    }

    internal static SpriteFontBase ResolveFont(SpriteFontBase font, float scale)
    {
        if (font is not DynamicSpriteFont dynamicFont || !ThemeFontSystems.Contains(dynamicFont.FontSystem))
        {
            return font;
        }
        // Whole-density buckets prevent new atlases on every resize pixel. Small UI uses 1x;
        // 1440p at 175% uses 3x, and 4K at 175% uses 5x. Logical font sizes stay unchanged.
        var density = Math.Max(1, (int)MathF.Ceiling(scale));
        if (!ResolutionFonts.TryGetValue(density, out var system))
        {
            if (_fontData.Length == 0)
            {
                _fontData = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Content", "Fonts", "Abel", FontFile));
            }
            system = new FontSystem(new FontSystemSettings { FontResolutionFactor = density });
            try
            {
                system.AddFont(_fontData);
            }
            catch
            {
                system.Dispose();
                throw;
            }
            ResolutionFonts.Add(density, system);
            ThemeFontSystems.Add(system);
        }
        if (ReferenceEquals(dynamicFont.FontSystem, system))
        {
            return font;
        }
        return system.GetFont(font.FontSize);
    }

    private static void Release(object? sender, EventArgs args)
    {
        if (sender is Microsoft.Xna.Framework.Game game)
        {
            game.Disposed -= Release;
        }

        if (_assets is null)
        {
            return;
        }

        foreach (var resource in _assets.Cache.Values.OfType<IDisposable>().Distinct<IDisposable>(ReferenceEqualityComparer.Instance))
        {
            resource.Dispose();
        }

        _assets.Unload();
        _assets = null;
        foreach (var system in ResolutionFonts.Values)
        {
            system.Dispose();
        }
        ResolutionFonts.Clear();
        ThemeFontSystems.Clear();
        _fontData = [];
    }

    private sealed class Accessor : IAssetAccessor
    {
        private readonly AssetManager _resources = AssetManager.CreateResourceAssetManager(typeof(DefaultAssets).Assembly, "Resources.");
        private readonly string _fontPath = Path.Combine(AppContext.BaseDirectory, "Content", "Fonts", "Abel", FontFile);

        public string Name => "DEEP // DRIVE theme";
        public bool Exists(string path) => path == FontFile ? File.Exists(_fontPath) : _resources.Exists(path);

        public Stream Open(string path)
        {
            if (path == FontFile)
            {
                return File.OpenRead(_fontPath);
            }

            if (path != StylesheetFile)
            {
                return _resources.Open(path);
            }

            // Replace the font definitions before Myra resolves every nested control style.
            var document = XDocument.Parse(_resources.ReadAsString(path));
            foreach (var font in document.Root!.Element("Fonts")!.Elements("Font"))
            {
                font.SetAttributeValue("File", FontFile);
            }

            return new MemoryStream(Encoding.UTF8.GetBytes(document.ToString()));
        }
    }
}
