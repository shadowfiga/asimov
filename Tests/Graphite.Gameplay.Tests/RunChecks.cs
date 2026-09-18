using System.Text.Json;
using Chisel.Generated;
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
        ElapsedTime();
        OreChanges();
        KillRewards();
        InvalidEnemyIds();
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
        first.Kills = 3;
        first.Duration = TimeSpan.FromMilliseconds(1250);
        first.Modifiers = [7, 9];
        first.Ore = 480;

        session.StartRun();
        CheckEmpty(session.CurrentRun);
        Check(!ReferenceEquals(first, session.CurrentRun), "Starting another run explicitly replaces the previous run");
        Check(first.Ore == 480, "Starting a new run does not change the previous run's Ore");
        Check(first.XP == 15 && first.Kills == 3 && first.Duration == TimeSpan.FromMilliseconds(1250)
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
        run.Kills = 19;
        run.Duration = TimeSpan.FromMilliseconds(65432) + TimeSpan.FromTicks(7);
        run.Modifiers = [2, 5];
        run.Ore = 4820;
        var bytes = serializer.Serialize(run);
        using var document = JsonDocument.Parse(bytes);
        Check(document.RootElement.EnumerateObject().Select(property => property.Name).Order()
            .SequenceEqual(new[] { "duration", "kills", "modifiers", "ore", "xp" }),
            "Run persistence uses explicit stable member names");
        Check(document.RootElement.GetProperty("duration").GetString() == "00:01:05.4320007",
            "The built-in TimeSpan save format preserves every tick without a custom converter");
        Check(document.RootElement.GetProperty("ore").GetInt32() == 4820,
            "Ore is saved directly as an integer on the run");
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
        restored.CurrentRun.Ore = 1;
        Check(run.Ore == 4820, "Loaded Ore does not share state with its source");
        var oreOnly = serializer.Deserialize<GameRun>("{\"ore\":75}"u8);
        Check(oreOnly.Ore == 75, "Ore loads directly from the run's integer field");
        CheckEmpty(serializer.Deserialize<GameRun>("{}"u8));
        CheckEmpty(serializer.Deserialize<GameRun>("{\"modifiers\":null}"u8));
    }

    private static void CheckEmpty(GameRun run)
    {
        Check(run.XP == 0 && run.Kills == 0 && run.Duration == TimeSpan.Zero && run.Modifiers.Length == 0,
            "A fresh run has zero counters and an empty modifiers array");
        Check(run.Ore == 0, "New runs and saves without Ore start with zero Ore");
    }

    private static void CheckSaved(GameRun run)
    {
        Check(run.XP == 240 && run.Kills == 19 && run.Duration == TimeSpan.FromMilliseconds(65432) + TimeSpan.FromTicks(7)
            && run.Modifiers.SequenceEqual(new[] { 2, 5 }), "All run fields survive serialization");
        Check(run.Ore == 4820, "Run Ore survives serialization");
    }

    private static void ElapsedTime()
    {
        var run = new GameRun();
        run.AddTime(TimeSpan.FromSeconds(1));
        Check(run.Duration == TimeSpan.FromSeconds(1), "Elapsed TimeSpan accumulates directly on the run");
        run.AddTime(TimeSpan.FromTicks(1));
        run.AddTime(TimeSpan.Zero);
        var expected = TimeSpan.FromSeconds(1) + TimeSpan.FromTicks(1);
        Check(run.Duration == expected, "A single tick is retained without manual fractional bookkeeping");

        var frames = new GameRun();
        var frameTime = TimeSpan.FromTicks(166667);
        for (var frame = 0; frame < 120; frame++)
        {
            frames.AddTime(frameTime);
        }
        Check(frames.Duration.Ticks == frameTime.Ticks * 120, "Repeated frame durations retain their exact tick sum");

        foreach (var framesPerSecond in new[] { 30, 60, 144, 240 })
        {
            var timedRun = new GameRun();
            var dt = 1f / framesPerSecond;
            for (var frame = 0; frame < framesPerSecond * 600; frame++)
            {
                timedRun.AddTime(TimeSpan.FromSeconds(dt));
            }
            Check((timedRun.Duration - TimeSpan.FromMinutes(10)).Duration() < TimeSpan.FromMilliseconds(10),
                $"Float dt conversion stays within 10 ms over a ten-minute run at {framesPerSecond} FPS");
        }

        var serializer = new SaveSerializer();
        var restored = serializer.Deserialize<GameRun>(serializer.Serialize(run));
        Check(restored.Duration == expected, "Saving and loading preserves sub-millisecond elapsed time");
        restored.AddTime(TimeSpan.FromMilliseconds(250));
        Check(restored.Duration == expected + TimeSpan.FromMilliseconds(250) && run.Duration == expected,
            "Loaded runs continue from their full persisted duration without a scene-owned accumulator");
        restored.AddTime(TimeSpan.FromDays(30));
        Check(serializer.Deserialize<GameRun>(serializer.Serialize(restored)).Duration == restored.Duration,
            "Duration is no longer limited by a 32-bit millisecond counter");

        var session = new Session();
        session.StartRun();
        var previous = session.CurrentRun;
        previous.AddTime(TimeSpan.FromTicks(7));
        session.StartRun();
        Check(session.CurrentRun.Duration == TimeSpan.Zero, "New runs do not inherit elapsed time");
        session.CurrentRun.AddTime(TimeSpan.FromTicks(1));
        Check(previous.Duration == TimeSpan.FromTicks(7) && session.CurrentRun.Duration == TimeSpan.FromTicks(1),
            "Each run owns its own TimeSpan");
    }

    private static void OreChanges()
    {
        var run = new GameRun();
        var notifications = 0;
        void Changed() => notifications++;
        run.OreChanged += Changed;
        run.AddOre(10);
        run.AddOre(5);
        Check(run.Ore == 15 && notifications == 2, "Adding Ore accumulates on the run and updates the HUD");
        Check(run.TrySpendOre(3) && run.Ore == 12 && notifications == 3, "Spending Ore updates the run and the HUD");
        Check(!run.TrySpendOre(13) && run.Ore == 12 && notifications == 3,
            "Unaffordable purchases leave Ore unchanged");
        Check(run.TrySpendOre(12) && run.Ore == 0 && notifications == 4, "The exact remaining Ore can be spent");
        run.OreChanged -= Changed;
        run.AddOre(6);
        Check(run.Ore == 6 && notifications == 4, "Ore observers can detach when their screen closes");
    }

    private static void KillRewards()
    {
        var session = new Session();
        session.StartRun();
        var run = session.CurrentRun;
        run.Ore = 80;
        const int enemyId = ChiselEnemiesIds.SWARMER;
        var experience = ChiselEnemies.Experience[enemyId];
        Check(ChiselEnemies.TableId == "enemies" && ChiselEnemies.Slugs[enemyId] == "SWARMER",
            "Enemy identity comes from the authored Chisel table");
        run.RecordKill(enemyId);
        Check(run.Kills == 1 && run.XP == experience, "Recording an enemy ID awards its Chisel experience and increments kills");
        WithChiselValue(ChiselEnemies.Experience, enemyId, 45, () => run.RecordKill(enemyId));
        Check(run.Kills == 2 && run.XP == experience + 45 && run.Ore == 80,
            "Kill rewards read Chisel on each call without caching a definition or creating a currency bounty");
        WithChiselValue(ChiselEnemies.Experience, enemyId, 0, () => run.RecordKill(enemyId));
        Check(run.Kills == 3 && run.XP == experience + 45, "Zero-XP enemies still count as kills");
        var serializer = new SaveSerializer();
        var restored = serializer.Deserialize<GameRun>(serializer.Serialize(run));
        restored.RecordKill(enemyId);
        Check(restored.Kills == 4 && restored.XP == experience * 2 + 45 && restored.Ore == 80,
            "Kills and experience continue from their saved values");
        session.StartRun();
        CheckEmpty(session.CurrentRun);
        Check(run.XP == experience + 45 && run.Kills == 3, "A fresh expedition does not alter the completed run's earned experience");
    }

    private static void InvalidEnemyIds()
    {
        var run = new GameRun { XP = 42, Kills = 3 };
        run.Ore = 80;
        foreach (var invalid in new[] { -1, -2, ChiselEnemies.Count, int.MaxValue })
        {
            Throws<IndexOutOfRangeException>(() => run.RecordKill(invalid));
            Check(run.XP == 42 && run.Kills == 3 && run.Ore == 80,
                "Invalid enemy IDs fail at Chisel lookup before mutating run counters or resources");
        }
    }
}
