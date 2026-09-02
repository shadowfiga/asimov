using GumLabel = Gum.Forms.Controls.Label;

namespace Graphite.Engine.UI.Controls;

public sealed class Text : UIControl
{
    private readonly GumLabel _control;

    internal Text(GumLabel control) : base(control.Name)
    {
        _control = control;
    }

    public string Value
    {
        get => _control.Text ?? string.Empty;
        set => _control.Text = value;
    }
}
