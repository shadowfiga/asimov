using Gum.Forms.Controls;
using Graphite.Engine.UI.Gum;

namespace Graphite.Game.UI;

public sealed class CounterScreen : GumScreen
{
    private int _count;
    private Label _label = null!;

    protected override void Build(StackPanel root)
    {
        root.X = 305;
        root.Y = 135;
        root.Width = 350;

        var title = new Label { Text = "Graphite Counter" };
        _label = new Label { Text = "Count: 0" };

        var increment = new Button
        {
            Text = "+ Increment",
            Width = 350
        };

        var decrement = new Button
        {
            Text = "- Decrement",
            Width = 350
        };

        increment.Click += (_, _) => SetCount(_count + 1);
        decrement.Click += (_, _) => SetCount(_count - 1);

        root.AddChild(title);
        root.AddChild(_label);
        root.AddChild(increment);
        root.AddChild(decrement);
    }

    private void SetCount(int value)
    {
        _count = value;
        _label.Text = $"Count: {_count}";
    }
}
