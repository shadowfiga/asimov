using Graphite.Engine.UI;
using Graphite.Game.Sessions;
using Graphite.Game.UI.Hud;
using Graphite.Game.UI.Theming;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;

namespace Graphite.Game.Scenes;

public sealed class SandboxUI : UIScreen
{
    internal ResourceHud Resources { get; private set; } = null!;

    protected override Widget Build()
    {
        var spacing = GameThemes.DeepDrive.Spacing;
        Resources = new ResourceHud(SessionManager.ActiveSession.CurrentRun)
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Bottom,
            // Leave the engine's fixed-pixel environment print visible below the HUD.
            Margin = new Thickness(spacing.Xl, 0, 0, spacing.Xl * 3)
        };
        return Resources;
    }

    protected override void OnDestroy() => Resources.Dispose();
}
