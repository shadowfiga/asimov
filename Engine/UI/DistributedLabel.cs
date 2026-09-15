using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;

namespace Graphite.Engine.UI;

/// <summary>A single-line label that distributes spare width between characters during layout/rendering.</summary>
public class DistributedLabel : Label
{
    private string _lastText = string.Empty;
    private string[] _letters = [];

    public DistributedLabel()
    {
        ClipToBounds = true;
    }

    public override void InternalRender(RenderContext context)
    {
        if (Font is null || string.IsNullOrEmpty(Text))
        {
            return;
        }
        if (_lastText != Text)
        {
            _lastText = Text;
            var elements = System.Globalization.StringInfo.GetTextElementEnumerator(Text);
            List<string> letters = [];
            while (elements.MoveNext())
            {
                letters.Add(elements.GetTextElement());
            }
            _letters = letters.ToArray();
        }

        var textWidth = _letters.Sum(letter => Font.MeasureString(letter).X);
        var tracking = _letters.Length < 2 ? 0 : Math.Max(0, (ActualBounds.Width - textWidth) / (_letters.Length - 1));
        var position = new Vector2(ActualBounds.X, ActualBounds.Y);
        foreach (var letter in _letters)
        {
            context.DrawString(Font, letter, position, Enabled ? TextColor : DisabledTextColor ?? TextColor);
            position.X += Font.MeasureString(letter).X + tracking;
        }
    }
}
