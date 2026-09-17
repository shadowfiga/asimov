using System.Text.Json;
using Graphite.Engine.Persistence;
using Graphite.Game.Sessions;
using GameRun = Graphite.Game.Domain.Run.Run;
using static Graphite.Gameplay.Tests.Program;

namespace Graphite.Gameplay.Tests;

internal static class RunChecks
{
    internal static void Run()
    {
        Lifecycle();
        Persistence();
    }

    private static void Lifecycle()
    {
        var session = new Session();
        Throws<InvalidOperationException>(() => _ = session.CurrentRun);
        var loadout = session.CurrentLoadout;
        session.ClearSector("fixture-sector");
        session.StartRun();
        var first = session.CurrentRun;
        CheckEmpty(first);
        Check(ReferenceEquals(first, session.CurrentRun), "Getting CurrentRun returns the same owned instance");
        first.XP = 15;
        first.Biomass = 6;
        first.Kills = 3;
        first.DurationMs = 1250;
        first.Modifiers = [7, 9];

        session.StartRun();
        CheckEmpty(session.CurrentRun);
        Check(!ReferenceEquals(first, session.CurrentRun), "Starting another run explicitly replaces the previous run");
        Check(first.XP == 15 && first.Biomass == 6 && first.Kills == 3 && first.DurationMs == 1250
            && first.Modifiers.SequenceEqual(new[] { 7, 9 }), "Starting a run does not mutate the previous instance");
        Check(ReferenceEquals(loadout, session.CurrentLoadout) && session.ClearedSectors.Contains("fixture-sector"),
            "Starting a run preserves session loadout and progress");

        var other = new Session();
        Throws<InvalidOperationException>(() => _ = other.CurrentRun);
        other.StartRun();
        other.CurrentRun.XP = 42;
        other.CurrentRun.Modifiers = [11];
        Check(!ReferenceEquals(other.CurrentRun, session.CurrentRun), "Sessions own independent runs");
        CheckEmpty(session.CurrentRun);
    }

    private static void Persistence()
    {
        var serializer = new SaveSerializer();
        var session = new Session();
        var noRun = serializer.Deserialize<Session>(serializer.Serialize(session));
        Throws<InvalidOperationException>(() => _ = noRun.CurrentRun);
        var legacy = serializer.Deserialize<Session>("{}"u8);
        Throws<InvalidOperationException>(() => _ = legacy.CurrentRun);
        legacy.StartRun();
        CheckEmpty(legacy.CurrentRun);

        session.StartRun();
        session.ClearSector("saved-sector");
        var run = session.CurrentRun;
        run.XP = 240;
        run.Biomass = 35;
        run.Kills = 19;
        run.DurationMs = 65432;
        run.Modifiers = [2, 5];
        var bytes = serializer.Serialize(run);
        using var document = JsonDocument.Parse(bytes);
        Check(document.RootElement.EnumerateObject().Select(property => property.Name).Order()
            .SequenceEqual(new[] { "biomass", "durationMs", "kills", "modifiers", "xp" }),
            "Run persistence uses explicit stable member names");
        var standalone = serializer.Deserialize<GameRun>(bytes);
        CheckSaved(standalone);
        var restored = serializer.Deserialize<Session>(serializer.Serialize(session));
        CheckSaved(restored.CurrentRun);
        Check(restored.ClearedSectors.Contains("saved-sector") && restored.CurrentLoadout.ChassisId == session.CurrentLoadout.ChassisId,
            "A saved run coexists with the session's other saved state");
        Check(!ReferenceEquals(restored.CurrentRun, run) && !ReferenceEquals(restored.CurrentRun.Modifiers, run.Modifiers),
            "Loading recreates run data without sharing mutable state with its source");
        restored.CurrentRun.Modifiers[0] = 99;
        Check(run.Modifiers[0] == 2, "Changing loaded modifiers cannot alter the source run");
        CheckEmpty(serializer.Deserialize<GameRun>("{}"u8));
        CheckEmpty(serializer.Deserialize<GameRun>("{\"modifiers\":null}"u8));
    }

    private static void CheckEmpty(GameRun run)
    {
        Check(run.XP == 0 && run.Biomass == 0 && run.Kills == 0 && run.DurationMs == 0 && run.Modifiers.Length == 0,
            "A fresh run has zero counters and an empty modifiers array");
    }

    private static void CheckSaved(GameRun run)
    {
        Check(run.XP == 240 && run.Biomass == 35 && run.Kills == 19 && run.DurationMs == 65432
            && run.Modifiers.SequenceEqual(new[] { 2, 5 }), "All run fields survive serialization");
    }
}
