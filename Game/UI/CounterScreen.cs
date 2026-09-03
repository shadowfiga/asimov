using Graphite.Engine.UI;
using Myra.Events;
using Myra.Graphics2D.UI;

namespace Graphite.Game.UI;

public sealed class CounterScreen : UIScreen
{
    private int _count;
    private Label _counterText = null!;
    private Button _incrementButton = null!;
    private Button _decrementButton = null!;

    protected override Widget Build()
    {
        _counterText = new Label
        {
            Text = "Count: 0"
        };
        _incrementButton = Button.CreateTextButton("+ Increment");
        _incrementButton.Width = 350;
        _decrementButton = Button.CreateTextButton("- Decrement");
        _decrementButton.Width = 350;

        var content = new VerticalStackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Spacing = 8
        };
        content.Widgets.Add(new Label { Text = "Graphite Counter" });
        content.Widgets.Add(_counterText);
        content.Widgets.Add(_incrementButton);
        content.Widgets.Add(_decrementButton);
        return content;
    }

    protected override void Awake()
    {
        _incrementButton.Click += Increment;
        _decrementButton.Click += Decrement;
    }

    protected override void OnDestroy()
    {
        _incrementButton.Click -= Increment;
        _decrementButton.Click -= Decrement;
    }

    private void Increment(object sender, MyraEventArgs args) => SetCount(_count + 1);
    private void Decrement(object sender, MyraEventArgs args) => SetCount(_count - 1);

    private void SetCount(int value)
    {
        _count = value;
        _counterText.Text = $"Count: {_count}";
    }
}
