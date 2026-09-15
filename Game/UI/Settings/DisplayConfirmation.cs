using Graphite.Engine.Graphics;

namespace Graphite.Game.UI.Settings;

/// <summary>Binds display-preview state to the shared confirmation component.</summary>
internal sealed class DisplayConfirmation : IDisposable
{
    private bool _attached;
    private bool _disposed;
    internal ConfirmationDialog Dialog
    {
        get;
    }

    internal DisplayConfirmation(MenuAssets assets)
    {
        Dialog = new ConfirmationDialog(assets, "KEEP DISPLAY SETTINGS?",
            DisplaySettings.KeepChanges, DisplaySettings.Revert, confirmText: "KEEP");
    }

    internal void Attach()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_attached)
        {
            return;
        }
        _attached = true;
        DisplaySettings.Changed += Synchronize;
        Synchronize();
    }

    private void Synchronize()
    {
        Dialog.Message = $"REVERTING IN {DisplaySettings.SecondsRemaining}s";
        if (DisplaySettings.NeedsConfirmation)
        {
            Dialog.Show();
        }
        else
        {
            Dialog.Hide();
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;
        DisplaySettings.Changed -= Synchronize;
        Dialog.Dispose();
    }
}
