using Graphite.Engine.UI;
using Graphite.Engine.UI.Controls;

namespace Graphite.Game.UI;

public sealed class CounterScreen : UIScreen
{
    private int _count;

    [UIElement] private Text _counterText = null!;
    [UIElement] private Button _incrementButton = null!;
    [UIElement] private Button _decrementButton = null!;

    protected override void Awake()
    {
        _incrementButton.Clicked += Increment;
        _decrementButton.Clicked += Decrement;
    }

    protected override void OnDestroy()
    {
        _incrementButton.Clicked -= Increment;
        _decrementButton.Clicked -= Decrement;
    }

    private void Increment() => SetCount(_count + 1);
    private void Decrement() => SetCount(_count - 1);

    private void SetCount(int value)
    {
        _count = value;
        _counterText.Value = $"Count: {_count}";
    }
}
