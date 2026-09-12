using Graphite.Game.Configuration;
using Graphite.Engine.Core;
using Graphite.Game.Aftergreen;

var settings = GameSettings.Instance;

if (args.Contains("--self-test"))
{
    GameSettingsSelfTest.Run();
    SliceSelfTest.Run();
    return;
}

var renderIndex = Array.IndexOf(args, "--render-check");
if (renderIndex >= 0 && renderIndex + 1 < args.Length)
{
    SliceDiagnostics.RenderDirectory = Path.GetFullPath(args[renderIndex + 1]);
    Directory.CreateDirectory(SliceDiagnostics.RenderDirectory);
}

using var game = new GameHost(settings);
game.Run();
