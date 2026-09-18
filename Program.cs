using Graphite.Engine.Core;
using Graphite.Game;
using Graphite.Game.Audio;
using Graphite.Game.Configuration;

GameAudio.ValidateAssets();
using var game = new GameHost(GameSettings.Instance, Startup.Initialize);
game.Run();
