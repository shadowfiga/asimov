using GumButton = Gum.Forms.Controls.Button;

namespace Graphite.Engine.UI.Controls;

public sealed class Button : UIControl
{
    private readonly GumButton _control;

    internal Button(GumButton control) : base(control.Name)
    {
        _control = control;
        _control.Click += HandleClick;
    }

    public event Action? Clicked;

    public string Text
    {
        get => _control.Text ?? string.Empty;
        set => _control.Text = value;
    }

    private void HandleClick(object? sender, EventArgs args)
    {
        Clicked?.Invoke();
    }
}
