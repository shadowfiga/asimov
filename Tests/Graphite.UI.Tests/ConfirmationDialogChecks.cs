using Graphite.Engine.Graphics;
using Graphite.Game.UI;
using Graphite.Game.UI.Theming;
using Microsoft.Xna.Framework;
using Myra.Graphics2D.UI;

namespace Graphite.UI.Tests;

internal static class ConfirmationDialogChecks
{
    internal static void Run()
    {
        using var assets = new MenuAssets();
        var display = DisplaySettings.Current;
        var needsConfirmation = DisplaySettings.NeedsConfirmation;
        var confirmed = 0;
        var cancelled = 0;
        ConfirmationDialog? active = null;
        using var dialog = new ConfirmationDialog(assets, "CONFIRM ACTION?",
            onConfirm: () =>
            {
                Program.Check(active?.Overlay.Visible == false, "Confirmation hides before invoking its confirm action");
                confirmed++;
            },
            onCancel: () =>
            {
                Program.Check(active?.Overlay.Visible == false, "Confirmation hides before invoking its cancel action");
                cancelled++;
            },
            message: "An arbitrary caller-provided message.", confirmText: "PROCEED", cancelText: "GO BACK");
        active = dialog;
        var content = (VerticalStackPanel)dialog.Overlay.Content;
        var title = content.Widgets.OfType<Label>().First();
        var message = content.Widgets.OfType<Label>().Last();
        var confirmLabel = dialog.ConfirmButton.GetChildren(true).OfType<Label>().Single();
        var cancelLabel = dialog.CancelButton.GetChildren(true).OfType<Label>().Single();
        Program.Check(!dialog.Overlay.Visible && title.Text == "CONFIRM ACTION?"
            && confirmLabel.Text == "PROCEED" && cancelLabel.Text == "GO BACK"
            && message.Text == dialog.Message && message.Visible && message.Wrap,
            "Generic confirmations start hidden and accept their own title, message, and action captions");
        Program.Check(!content.GetChildren(true).OfType<Image>().Any()
            && cancelLabel.TextColor == GameThemes.DeepDrive.Danger,
            "Shared confirmation buttons are text-only and Cancel uses the palette's red tone");
        Program.Check(content.Width == GameThemes.DeepDrive.Layout.ConfirmationWidth
            && dialog.ConfirmButton.Width == GameThemes.DeepDrive.MenuButton.Confirmation.Width
            && dialog.CancelButton.Height == GameThemes.DeepDrive.MenuButton.Confirmation.Height,
            "Shared confirmations use themed dialog and button dimensions");

        dialog.ConfirmButton.DoClick();
        dialog.CancelButton.DoClick();
        Program.Check(confirmed == 0 && cancelled == 0, "Hidden confirmations cannot trigger actions");
        dialog.Show();
        foreach (var bounds in new[] { new Point(800, 600), new Point(560, 360), new Point(1000, 700) })
        {
            dialog.Overlay.Measure(bounds);
            dialog.Overlay.Arrange(new Rectangle(Point.Zero, bounds));
            var center = content.ToGlobal(new Vector2(content.Bounds.Width / 2f, content.Bounds.Height / 2f));
            Program.Check(dialog.Overlay.Bounds.Size == bounds
                && Math.Abs(center.X - bounds.X / 2f) <= 1 && Math.Abs(center.Y - bounds.Y / 2f) <= 1
                && content.Bounds.Width <= bounds.X - 2 * GameThemes.DeepDrive.Spacing.Xl,
                "Shared confirmation layout follows the parent and remains centered without resize handlers");
        }
        dialog.Message = "Updated while open.";
        Program.Check(message.Text == "Updated while open.", "Callers can update live confirmation state without rebuilding");
        dialog.Message = "";
        Program.Check(!message.Visible, "An absent confirmation message reserves no layout space");
        dialog.Message = "Message restored.";
        Program.Check(message.Visible, "A previously omitted message can be shown again");

        dialog.ConfirmButton.DoClick();
        dialog.ConfirmButton.DoClick();
        dialog.CancelButton.DoClick();
        Program.Check(confirmed == 1 && cancelled == 0 && !dialog.Overlay.Visible,
            "Confirm completes once and prevents repeated or competing actions");
        dialog.Show();
        dialog.CancelButton.DoClick();
        dialog.CancelButton.DoClick();
        Program.Check(confirmed == 1 && cancelled == 1 && !dialog.Overlay.Visible,
            "The same component can reopen and complete through Cancel");
        dialog.Show();
        dialog.Hide();
        Program.Check(confirmed == 1 && cancelled == 1, "Dismissal without a decision invokes neither action");
        dialog.Show();
        dialog.Dispose();
        dialog.Dispose();
        dialog.ConfirmButton.DoClick();
        dialog.CancelButton.DoClick();
        Program.Check(confirmed == 1 && cancelled == 1 && !dialog.Overlay.Visible,
            "Disposal hides the confirmation and detaches its callbacks safely");
        try
        {
            dialog.Show();
            throw new InvalidOperationException("Expected a disposed confirmation to reject reopening.");
        }
        catch (ObjectDisposedException)
        {
        }

        using var defaults = new ConfirmationDialog(assets, "CONTINUE?", () => confirmed++, () => cancelled++);
        Program.Check(defaults.Message == ""
            && defaults.Overlay.Content.GetChildren(true).OfType<Label>().Count(label => !label.Visible) == 1
            && defaults.ConfirmButton.GetChildren(true).OfType<Label>().Single().Text == "CONFIRM"
            && defaults.CancelButton.GetChildren(true).OfType<Label>().Single().Text == "CANCEL",
            "A minimal confirmation needs only a title and actions; optional text has sensible defaults");
        Program.Check(DisplaySettings.Current == display && DisplaySettings.NeedsConfirmation == needsConfirmation,
            "Generic confirmation actions and lifecycle have no display-settings side effects");
    }
}
