using Microsoft.Xna.Framework;

namespace Graphite.Game.UI.Theming;

/// <summary>Preferred logical sizes; the UI backend fits them to their parent automatically.</summary>
public sealed record ScreenLayoutTokens
{
    public int MenuWidth { get; init; } = 520;
    public int MenuTitleFontSize { get; init; } = 48;
    public int LoadingWidth { get; init; } = 360;
    public int LoadingBarHeight { get; init; } = 8;
    public int HudExperienceWidth { get; init; } = 360;
    public int HudMapSize { get; init; } = 176;
    public int HudPilotWidth { get; init; } = 280;
    public int HudPortraitSize { get; init; } = 64;
    public int HudTitleFontSize { get; init; } = 20;
    public int HudTimerFontSize { get; init; } = 28;
    public int HudBarHeight { get; init; } = 6;
    public Point SettingsSize { get; init; } = new(1080, 620);
    public Point CreditsSize { get; init; } = new(620, 420);
    public int ConfirmationWidth { get; init; } = 520;
    public int SidebarWidth { get; init; } = 200;
    public int CompactSidebarWidth { get; init; } = 180;
    public int SidebarBreakpoint { get; init; } = 836;
}
