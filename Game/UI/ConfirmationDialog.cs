using Graphite.Game.UI.Theming;
using Myra.Events;
using Myra.Graphics2D.UI;

namespace Graphite.Game.UI;

/// <summary>A reusable modal prompt; the caller owns the actions and any live message updates.</summary>
internal sealed class ConfirmationDialog : IDisposable
{
    private readonly Label _message;
    private readonly Action _onConfirm;
    private readonly Action _onCancel;
    private bool _disposed;

    internal Dialog Overlay
    {
        get;
    }
    internal MenuButton ConfirmButton
    {
        get;
    }
    internal MenuButton CancelButton
    {
        get;
    }
    internal string Message
    {
        get => _message.Text;
        set
        {
            _message.Text = value;
            _message.Visible = !string.IsNullOrWhiteSpace(value);
        }
    }

    internal ConfirmationDialog(
        MenuAssets assets,
        string title,
        Action onConfirm,
        Action onCancel,
        string message = "",
        string confirmText = "CONFIRM",
        string cancelText = "CANCEL")
    {
        _onConfirm = onConfirm;
        _onCancel = onCancel;

        var theme = GameThemes.DeepDrive;
        var content = DialogLayout.Content(title, theme.Layout.ConfirmationWidth);
        _message = DialogLayout.Title("", 18);
        _message.TextColor = theme.SecondaryText;
        _message.Wrap = true;
        Message = message;
        content.Widgets.Add(_message);
        var buttons = new HorizontalStackPanel { Spacing = theme.Spacing.Sm };
        ConfirmButton = new MenuButton(assets, confirmText, size: MenuButtonSize.Confirmation);
        CancelButton = new MenuButton(assets, cancelText, tone: MenuButtonTone.Danger, size: MenuButtonSize.Confirmation);
        ConfirmButton.Click += ConfirmClicked;
        CancelButton.Click += CancelClicked;
        buttons.Widgets.Add(ConfirmButton);
        buttons.Widgets.Add(CancelButton);
        content.Widgets.Add(buttons);
        Overlay = new Dialog(content) { Visible = false };
    }

    internal void Show()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        Overlay.Visible = true;
    }

    /// <summary>Dismisses the prompt without choosing an action.</summary>
    internal void Hide() => Overlay.Visible = false;

    private void ConfirmClicked(object sender, MyraEventArgs args) => Complete(_onConfirm);
    private void CancelClicked(object sender, MyraEventArgs args) => Complete(_onCancel);

    private void Complete(Action action)
    {
        if (!Overlay.Visible)
        {
            return;
        }
        Hide();
        action();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;
        Hide();
        ConfirmButton.Click -= ConfirmClicked;
        CancelButton.Click -= CancelClicked;
    }
}
