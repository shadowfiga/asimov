using System.Text;
using System.Xml.Linq;
using AssetManagementBase;
using Myra;
using Myra.Graphics2D.UI.Styles;
using FontStashSharp;
using System.Globalization;

namespace Graphite.Game.UI.Theming;

internal static class ThemeAssets
{
    private const string FontFile = "Abel-Regular.ttf";
    private const string StylesheetFile = "default_ui_skin.xmms";
    private static AssetManager? _assets;

    internal static Stylesheet LoadStylesheet()
    {
        if (_assets is null)
        {
            _assets = new AssetManager(new Accessor(), string.Empty);
            MyraEnvironment.Game.Disposed += Release;
        }

        return _assets.LoadStylesheet(StylesheetFile);
    }

    internal static SpriteFontBase Font(int size)
    {
        _ = LoadStylesheet();
        return _assets!.LoadFont($"{FontFile}:{size.ToString(CultureInfo.InvariantCulture)}");
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
    }

    private sealed class Accessor : IAssetAccessor
    {
        private readonly AssetManager _resources = AssetManager.CreateResourceAssetManager(typeof(DefaultAssets).Assembly, "Resources.");
        private readonly string _fontPath = Path.Combine(AppContext.BaseDirectory, "Content", "Fonts", "Abel", FontFile);

        public string Name => "AFTERGREEN theme";
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
