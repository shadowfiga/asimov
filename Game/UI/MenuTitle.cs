using FontStashSharp;
using Graphite.Game.UI.Theming;
using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;

namespace Graphite.Game.UI;

internal sealed class MenuTitle : Widget
{
    private readonly string[] _letters = "AFTERGREEN".Select(letter => letter.ToString()).ToArray();
    private SpriteFontBase _font = ThemeAssets.Font(40);
    private float _tracking;

    internal void Fit(int width, int fontSize)
    {
        _font = ThemeAssets.Font(fontSize);
        _tracking = Math.Max(0, (width - _letters.Sum(letter => _font.MeasureString(letter).X)) / (_letters.Length - 1));
        Width = width;
        Height = _font.LineHeight;
        InvalidateMeasure();
    }

    public override void InternalRender(RenderContext context)
    {
        var position = new Vector2(ActualBounds.X, ActualBounds.Y);
        foreach (var letter in _letters)
        {
            context.DrawString(_font, letter, position, GameThemes.Aftergreen.Gray5);
            position.X += _font.MeasureString(letter).X + _tracking;
        }
    }
}
