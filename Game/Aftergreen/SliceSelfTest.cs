using Microsoft.Xna.Framework;

namespace Graphite.Game.Aftergreen;

public static class SliceSelfTest
{
    public static void Run()
    {
        var config = SliceConfig.Load();
        var state = new SliceState(config);
        void Check(bool value, string label)
        {
            if (!value)
            {
                throw new InvalidOperationException("FAILED: " + label);
            }
            Console.WriteLine("PASS: " + label);
        }
        void Tick(int frames, SliceInput input)
        {
            for (var i = 0; i < frames; i++)
            {
                state.Update(1 / 60f, input);
            }
        }
        Check(Math.Abs(state.Data.Trash.Sum(t => t.Mass) - 1300) < .01f, "Site A contains 1,300 kg including hidden rewards");
        Check(!state.BeginDeparture() && !state.EnterBiodome(), "Biodome and departure are gated");
        var first = state.Data.Trash.First(t => t.Mass == 1);
        state.Player = first.Position - Vector2.UnitX * 45;
        Tick(120, new(Vector2.Zero, Vector2.UnitX, true, false));
        Check(first.Collected && state.Data.Hopper > 0 && state.Data.Recovered == 0, "Suction collects; tonnage is credited only when recycled");
        state.Data.Hopper = state.Capacity;
        var remaining = state.Data.Trash.Count(t => !t.Collected);
        Tick(60, new(Vector2.Zero, Vector2.UnitX, true, false));
        Check(state.Data.Trash.Count(t => !t.Collected) == remaining, "Full hopper cannot consume debris");
        state.Player = new(0, 110);
        state.Interact();
        Tick(100, new(Vector2.Zero, Vector2.UnitX, false, false));
        Check(state.Data.Hopper == 0 && state.Data.Recovered == 100 && state.Data.Trips == 1, "Recycler deposits in 1.5 seconds");
        state.Interact();
        state.Data.Scrap = 1000;
        Check(state.BuyUpgrade(0) && !state.BuyUpgrade(0), "Upgrade purchases are charged once");
        Check(state.BuyUpgrade(1) && state.Capacity == 160 && state.BuyUpgrade(2), "All three upgrades apply");
        Check(!state.BuildBot(), "Collector requires the permanent relic");
        state.Phase = SlicePhase.Exterior;
        foreach (var group in new[] { 0, 1, 2 })
        {
            var anchors = state.Data.Trash.Where(t => t.Group == group && t.Buried).ToArray();
            foreach (var anchor in anchors)
            {
                var pullDirection = group == 1 ? -Vector2.UnitY : Vector2.UnitY;
                state.Player = anchor.Position + pullDirection * 60;
                Tick(60, new(Vector2.Zero, -pullDirection, false, true));
                Check(anchor.Loosened == 0, "Latch requires movement, not waiting");
                Tick(210, new(pullDirection, -pullDirection, false, true));
                Check(!anchor.Buried, $"Group {group} anchor pulls free");
                state.Card = null;
                Tick(1, new(Vector2.Zero, Vector2.UnitX, false, false));
            }
            Check(!state.Data.Trash.Any(t => t.Group == group && t.Hidden), $"Group {group} cascade reveals all rewards");
        }
        Check(state.Data.RoutingChip && state.Data.NpcFreed, "Relic and PARKR-7 encounter complete");
        state.Phase = SlicePhase.Workshop;
        state.Data.Components = 1;
        Check(state.BuildBot() && !state.BuildBot(), "One collector builds with scrap and component");
        state.Phase = SlicePhase.Exterior;
        Tick(3600, new(Vector2.Zero, Vector2.UnitX, false, false));
        Check(state.Data.BotMass > 0 && state.Data.Milestones.ContainsKey("first_bot_deposit"), "Collector completes autonomous collection and deposit within a minute");
        state.Data.Recovered = 999;
        state.Data.Hopper = 1;
        state.Player = new(0, 110);
        state.Interact();
        Tick(100, new(Vector2.Zero, Vector2.UnitX, false, false));
        Check(state.Data.CoreProduced, "One tonne produces Bio-Core");
        state.Interact();
        Check(state.EnterBiodome() && !state.BeginDeparture(), "Ecology choice required before departure");
        Check(state.ChooseEcology(Ecology.PaperFinch) && !state.ChooseEcology(Ecology.ScrubGrass), "One core purchases one permanent ecology");
        var path = Path.Combine(Path.GetTempPath(), "aftergreen-test-" + Guid.NewGuid(), "save.json");
        try
        {
            state.Save(path);
            var loaded = SliceState.LoadSave(path)!;
            Check(loaded.RoutingChip && loaded.Ecology == Ecology.PaperFinch && loaded.Trash.Count == state.Data.Trash.Count
                && loaded.Trash[0].Position == state.Data.Trash[0].Position && loaded.Bots.Count == 1, "Save round-trip preserves world, expedition and permanent state");
            Check(state.BeginDeparture(), "Departure confirmation starts sequence");
            Tick(430, new(Vector2.Zero, Vector2.UnitX, false, false));
            Check(state.Phase == SlicePhase.Teaser && state.Data.SiteRecovering && state.Data.Departures == 1,
                "Departure arrives at Site B and marks Site A recovering");
            Check(state.Data.RoutingChip && state.Data.Ecology == Ecology.PaperFinch && state.Data.Upgrades.All(x => !x)
                && state.Data.Bots.Count == 0 && state.Data.Scrap == 0 && state.Data.Hopper == 0 && state.Data.Recovered == 0,
                "Departure keeps permanent progress and resets field equipment and currency");
            var paper = state.Data.Trash.First(t => t.Kind == 0);
            var before = Vector2.Distance(paper.Position, new(60, 250));
            Tick(120, new(Vector2.Zero, Vector2.UnitX, false, false));
            Check(Vector2.Distance(paper.Position, new(60, 250)) < before, "Finches physically gather paper in Site B");
            state.Save(path);
            Check(new SliceState(config, SliceState.LoadSave(path)).Phase == SlicePhase.Teaser, "Site B reload resumes with persistent habitat");
            var grass = new SliceState(config, new SliceSave { Departures = 1, Ecology = Ecology.ScrubGrass });
            Check(grass.Data.Trash.Count == 36 && grass.Data.Ecology == Ecology.ScrubGrass, "Alternate ecology generates valid Site B");
            var plain = new SliceState(config, new SliceSave { Departures = 1 });
            foreach (var sample in new[] { grass, plain })
            {
                sample.Player = new(0, 180);
                sample.Data.Trash = [new Debris { Position = new(100, 180), Mass = 1, Kind = 0 }];
                sample.Update(1 / 60f, new(Vector2.Zero, Vector2.UnitX, true, false));
            }
            Check(grass.Data.Trash[0].X < plain.Data.Trash[0].X, "Scrub Grass measurably accelerates suction");
            state.Player = new(40, 245);
            Tick(1200, new(Vector2.Zero, Vector2.UnitX, true, false));
            Check(state.Phase == SlicePhase.Complete, "Site B ecological sweep reaches completion screen");
            state.Phase = SlicePhase.Teaser;
            Tick(5, new(Vector2.Zero, Vector2.UnitX, false, false));
            Check(state.Phase == SlicePhase.Teaser, "Completion dismissal allows continued exploration");
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(path)!, true);
        }
        Console.WriteLine("AFTERGREEN gameplay checks passed.");
    }
}
