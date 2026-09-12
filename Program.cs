using Graphite.Engine.Core;
using Graphite.Game;
using Graphite.Game.Configuration;

using var game = new GameHost(GameSettings.Instance, Startup.Initialize);
game.Run();
