using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Xna.Framework;

namespace Graphite.Game.Aftergreen;

public enum SlicePhase { Exterior, Workshop, Biodome, Departure, Teaser, Complete, Paused }
public enum Ecology { None, PaperFinch, ScrubGrass }

public sealed class Debris
{
    public int Id { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public float Mass { get; set; }
    public int Kind { get; set; }
    public int Group { get; set; } = -1;
    public bool Hidden { get; set; }
    public bool Buried { get; set; }
    public bool Collected { get; set; }
    public float Loosened { get; set; }
    public float Rotation { get; set; }
    [JsonIgnore] public Vector2 Position { get => new(X, Y); set { X = value.X; Y = value.Y; } }
}

public sealed class Collector
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Load { get; set; }
    public float Scrap { get; set; }
    public int Components { get; set; }
    public bool Returning { get; set; }
    public float Pause { get; set; }
    [JsonIgnore] public Vector2 Position { get => new(X, Y); set { X = value.X; Y = value.Y; } }
}

public sealed class SliceSave
{
    public int Version { get; set; } = 1;
    public bool RoutingChip { get; set; }
    public Ecology Ecology { get; set; }
    public int Departures { get; set; }
    public bool SiteRecovering { get; set; }
    public bool CoreProduced { get; set; }
    public bool NpcFreed { get; set; }
    public bool[] Upgrades { get; set; } = new bool[3];
    public float Scrap { get; set; }
    public int Components { get; set; }
    public float Recovered { get; set; }
    public float Hopper { get; set; }
    public float HopperScrap { get; set; }
    public int HopperComponents { get; set; }
    public float PlayerX { get; set; } = 0;
    public float PlayerY { get; set; } = 140;
    public List<Debris> Trash { get; set; } = [];
    public List<Collector> Bots { get; set; } = [];
    public float Seconds { get; set; }
    public int Trips { get; set; }
    public float BotMass { get; set; }
    public float IdleSeconds { get; set; }
    public Dictionary<string, float> Milestones { get; set; } = [];
}

public readonly record struct SliceInput(Vector2 Move, Vector2 Aim, bool Vacuum, bool Latch);
public readonly record struct Feedback(Vector2 Position, string Kind, int Count);

public sealed class SliceState
{
    public static readonly Vector2[] Heaps = [new(-430, -280), new(350, 370), new(620, -360), new(-640, 380), new(50, -620), new(680, 130)];
    public SliceConfig Config { get; }
    public SliceSave Data { get; private set; }
    public SlicePhase Phase { get; set; }
    public string Message { get; private set; } = "Wake up, little machine. There is a world to mend.";
    public float MessageTime { get; private set; } = 8;
    public string? Card { get; set; }
    public float AnimationTime { get; private set; }
    public float DepositTime { get; private set; }
    public bool VacuumActive { get; private set; }
    public Debris? LatchTarget { get; private set; }
    public Vector2 Aim { get; private set; } = Vector2.UnitY;
    public event Action<Feedback>? Feedback;
    public float Capacity => Data.Upgrades[1] ? Config.UpgradedCapacity : Config.HopperCapacity;
    public float Range => Config.VacuumRange * (Data.Upgrades[2] ? 1.18f : 1);
    public float Cone => Data.Upgrades[0] ? .92f : .67f;
    public Vector2 Player { get => new(Data.PlayerX, Data.PlayerY); set { Data.PlayerX = value.X; Data.PlayerY = value.Y; } }
    public bool NearArk => Player.Length() < 140;
    public float SiteBCollected { get; private set; }
    private float _phaseTime;
    private readonly Random _random = new(73);

    public SliceState(SliceConfig config, SliceSave? save = null)
    {
        Config = config;
        Data = save ?? new SliceSave();
        if (Data.Trash.Count == 0)
        {
            Generate(Data.Departures > 0);
        }
        Phase = Data.Departures > 0 ? SlicePhase.Teaser : SlicePhase.Exterior;
    }

    private void Add(Vector2 position, float mass, int kind, bool buried = false, int group = -1, bool hidden = false)
    {
        Data.Trash.Add(new Debris
        {
            Id = Data.Trash.Count,
            Position = position,
            Mass = mass,
            Kind = kind,
            Buried = buried,
            Group = group,
            Hidden = hidden,
            Rotation = (float)_random.NextDouble() * 6.28f
        });
    }

    private Vector2 Scatter(float radius)
    {
        var angle = (float)_random.NextDouble() * MathF.Tau;
        return new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * (MathF.Sqrt((float)_random.NextDouble()) * radius);
    }

    private void Generate(bool teaser)
    {
        Data.Trash.Clear();
        if (teaser)
        {
            for (var i = 0; i < 36; i++)
            {
                Add(new Vector2(90, 220) + Scatter(170), 1, i < 24 ? 0 : 1);
            }
            return;
        }
        for (var i = 0; i < Config.LightCount + Config.MediumCount; i++)
        {
            var p = Scatter(i < 100 ? 370 : 850);
            if (p.Length() < 160)
            {
                p = Vector2.Normalize(p == Vector2.Zero ? Vector2.One : p) * (165 + _random.Next(80));
            }
            Add(p, i < Config.LightCount ? Config.LightMass : Config.MediumMass, i < Config.LightCount ? i % 3 : 3 + i % 2);
        }
        for (var i = 0; i < Config.HeavyCount; i++)
        {
            Add(Heaps[i % 6] + Scatter(110), Config.HeavyMass, 5 + i % 3, true);
        }
        for (var h = 0; h < 6; h++)
        {
            var anchors = h == 1 ? 2 : 1;
            for (var a = 0; a < anchors; a++)
            {
                Add(Heaps[h] + new Vector2(a * 52 - 22, 0), Config.AnchorMass, h == 0 ? 5 : 7, true, h);
            }
            // Each structure contains exactly 95 kg, including its anchors.
            var rewardMass = Config.StructureMass - anchors * Config.AnchorMass;
            var count = (int)Math.Ceiling(rewardMass / 5);
            for (var i = 0; i < count; i++)
            {
                Add(Heaps[h] + Scatter(60), Math.Min(5, rewardMass - i * 5), i % 3 == 0 ? 4 : 3, false, h, true);
            }
        }
    }

    public void Notify(string text, string sound = "notice")
    {
        Message = text;
        MessageTime = 7;
        Feedback?.Invoke(new(Player, sound, 12));
    }

    private void Mark(string key)
    {
        Data.Milestones.TryAdd(key, Data.Seconds);
    }

    public void Update(float dt, SliceInput input)
    {
        dt = Math.Clamp(dt, 0, .05f);
        AnimationTime += dt;
        MessageTime = Math.Max(0, MessageTime - dt);
        VacuumActive = false;
        if (Phase == SlicePhase.Departure)
        {
            _phaseTime += dt;
            if (_phaseTime >= 7)
            {
                CompleteDeparture();
            }
            return;
        }
        if (Phase is not (SlicePhase.Exterior or SlicePhase.Teaser) || Card != null)
        {
            return;
        }
        Data.Seconds += dt;
        if (input.Move.LengthSquared() < .01f && !input.Vacuum && !input.Latch)
        {
            Data.IdleSeconds += dt;
        }
        var move = input.Move;
        if (move.LengthSquared() > 1)
        {
            move.Normalize();
        }
        var previousPosition = Player;
        var pulling = input.Latch && LatchTarget != null;
        Player += move * Config.PlayerSpeed * (pulling ? .4f : 1) * dt;
        if (Player.Length() > 960)
        {
            Player = Vector2.Normalize(Player) * 960;
        }
        // Keep the robot outside the Ark hull, with a clear recycler apron below it.
        if (Player.X > -105 && Player.X < 105 && Player.Y > -92 && Player.Y < 66)
        {
            var distances = new[] { Player.X + 105, 105 - Player.X, Player.Y + 92, 66 - Player.Y };
            var side = Array.IndexOf(distances, distances.Min());
            Player = side switch { 0 => new(-105, Player.Y), 1 => new(105, Player.Y), 2 => new(Player.X, -92), _ => new(Player.X, 66) };
        }
        if (input.Aim.LengthSquared() > .01f)
        {
            Aim = Vector2.Normalize(input.Aim);
        }
        if (DepositTime > 0)
        {
            DepositTime -= dt;
            if (DepositTime <= 0)
            {
                FinishDeposit();
            }
        }
        else
        {
            UpdateVacuum(dt, input.Vacuum);
            var actualMove = dt > 0 ? (Player - previousPosition) / (Config.PlayerSpeed * dt) : Vector2.Zero;
            UpdateLatch(dt, input.Latch, actualMove);
        }
        UpdateBots(dt);
        if (Phase == SlicePhase.Teaser)
        {
            UpdateEcology(dt);
            SiteBCollected = Data.Trash.Where(t => t.Collected).Sum(t => t.Mass);
            if (SiteBCollected >= 18 && !Data.Milestones.ContainsKey("demo_complete"))
            {
                Phase = SlicePhase.Complete;
                Mark("demo_complete");
            }
        }
    }

    private void UpdateVacuum(float dt, bool active)
    {
        VacuumActive = active && Data.Hopper < Capacity;
        if (!VacuumActive)
        {
            return;
        }
        foreach (var t in Data.Trash)
        {
            if (t.Collected || t.Hidden || t.Buried || t.Mass + Data.Hopper > Capacity + .01f)
            {
                continue;
            }
            var delta = Player - t.Position;
            var distance = delta.Length();
            if (distance > Range || (distance > 26 && Vector2.Dot(-delta / distance, Aim) < MathF.Cos(Cone)))
            {
                continue;
            }
            var ecologyBoost = Data.Departures > 0 && Data.Ecology == Ecology.ScrubGrass ? 1.6f : 1;
            var speed = Config.PullSpeed * (Data.Upgrades[2] ? 2.3f : 1) * ecologyBoost;
            speed *= (1 + 3 * (1 - distance / Range)) / MathF.Sqrt(t.Mass);
            t.Position += distance > 0 ? delta / distance * Math.Min(distance, speed * dt) : Vector2.Zero;
            t.Rotation += dt * 5;
            if (distance < 23)
            {
                t.Collected = true;
                Data.Hopper += t.Mass;
                Data.HopperScrap += ScrapFor(t);
                Data.HopperComponents += t.Kind == 4 ? 1 : 0;
                Mark("first_pickup");
                Feedback?.Invoke(new(t.Position, t.Kind == 0 ? "paper" : "intake", 5));
                if (Data.Hopper >= Capacity - .01f)
                {
                    Mark("first_full_hopper");
                    Notify("Hopper full. Follow the ARK marker and press E to recycle.");
                }
            }
        }
    }

    public Debris? FindLatchTarget()
    {
        return Data.Trash.Where(t => !t.Collected && !t.Hidden && t.Buried && Vector2.Distance(t.Position, Player) < 165
            && Vector2.Dot(Vector2.Normalize(t.Position - Player), Aim) > .3f)
            .MinBy(t => Vector2.Distance(t.Position, Player) * .1f
                + MathF.Abs((t.X - Player.X) * Aim.Y - (t.Y - Player.Y) * Aim.X));
    }

    private void UpdateLatch(float dt, bool active, Vector2 move)
    {
        if (!active)
        {
            LatchTarget = null;
            return;
        }
        LatchTarget ??= FindLatchTarget();
        var target = LatchTarget;
        if (target == null)
        {
            return;
        }
        var delta = Player - target.Position;
        var distance = delta.Length();
        if (distance > 240)
        {
            LatchTarget = null;
            return;
        }
        if (distance > 25 && Vector2.Dot(move, delta / distance) > .15f)
        {
            target.Loosened += dt / Config.ExcavationSeconds;
            target.Position += delta / distance * dt * Config.PlayerSpeed * .8f * Math.Min(1, move.Length());
            target.Rotation += dt * .4f;
            if (_random.NextDouble() < dt * 16)
            {
                Feedback?.Invoke(new(target.Position, "dust", 2));
            }
        }
        if (target.Loosened >= 1)
        {
            target.Buried = false;
            Mark("first_excavation");
            Feedback?.Invoke(new(target.Position, "release", 32));
            LatchTarget = null;
            if (target.Group >= 0 && !Data.Trash.Any(t => t.Group == target.Group && t.Buried))
            {
                Collapse(target.Group);
            }
            else
            {
                Notify("Unstuck. Even the desert has to let go eventually.", "release");
            }
        }
    }

    private void Collapse(int group)
    {
        foreach (var t in Data.Trash.Where(t => t.Group == group && t.Hidden))
        {
            t.Hidden = false;
            t.Position += Scatter(65);
        }
        Feedback?.Invoke(new(Heaps[group], "collapse", 60));
        Notify("Heap collapsed! Sweep up the exposed electronics.", "release");
        if (group == 1 && !Data.RoutingChip)
        {
            Data.RoutingChip = true;
            Mark("routing_chip");
            Card = "relic";
            Feedback?.Invoke(new(Heaps[group], "relic", 70));
        }
        if (group == 2 && !Data.NpcFreed)
        {
            Data.NpcFreed = true;
            Mark("parkr_freed");
            Card = "npc";
            Notify("PARKR-7: PUBLIC SERVICE RESUMED. Valuable metal marked to the west.");
        }
    }

    private static float ScrapFor(Debris t) => t.Mass <= 1 ? 1.2f : t.Mass <= 6 ? 3 : 8;

    public void Interact()
    {
        if (!NearArk || Phase is not (SlicePhase.Exterior or SlicePhase.Teaser))
        {
            return;
        }
        if (Data.Hopper > 0 && DepositTime <= 0)
        {
            DepositTime = 1.5f;
            Feedback?.Invoke(new(Vector2.Zero, "deposit", 40));
        }
        else if (DepositTime <= 0)
        {
            Phase = SlicePhase.Workshop;
        }
    }

    private void FinishDeposit()
    {
        Data.Recovered += Data.Hopper;
        Data.Scrap += Data.HopperScrap;
        Data.Components += Data.HopperComponents;
        Data.Hopper = Data.HopperScrap = 0;
        Data.HopperComponents = 0;
        Data.Trips++;
        CheckMilestone();
        Notify(Data.CoreProduced ? "RESTORATION READY. Enter the biodome at the Ark." : "Recycled. Organic life detected: please do not recycle.", "deposit");
    }

    private void CheckMilestone()
    {
        if (Data.Recovered >= 500)
        {
            Mark("500_kg");
        }
        if (Data.Recovered >= Config.TargetMass && !Data.CoreProduced && Data.Departures == 0)
        {
            Data.CoreProduced = true;
            Mark("1000_kg");
            Notify("ONE TONNE. One new beginning. A Bio-Core is waiting at the Ark.", "relic");
        }
    }

    public bool BuyUpgrade(int index)
    {
        if (Phase != SlicePhase.Workshop || index < 0 || index > 2 || Data.Upgrades[index] || Data.Scrap < Config.UpgradeCosts[index])
        {
            return false;
        }
        Data.Scrap -= Config.UpgradeCosts[index];
        Data.Upgrades[index] = true;
        Mark("first_upgrade");
        Notify(new[] { "Wider intake fitted. More sweep, less fuss.", "Hopper expanded to 160 kg. Room for ambition.", "Stronger motor online. Medium junk, meet your match." }[index], "upgrade");
        return true;
    }

    public bool BuildBot()
    {
        if (Phase != SlicePhase.Workshop || !Data.RoutingChip || Data.Scrap < Config.BotCost || Data.Components < 1 || Data.Bots.Count > 0)
        {
            return false;
        }
        Data.Scrap -= Config.BotCost;
        Data.Components--;
        Data.Bots.Add(new Collector { Position = new(135, 90) });
        Mark("collector_built");
        Notify("Collector deployed at the Ark pad. It has never been this employed.", "upgrade");
        return true;
    }

    private void UpdateBots(float dt)
    {
        foreach (var bot in Data.Bots)
        {
            if (bot.Pause > 0)
            {
                bot.Pause -= dt;
                continue;
            }
            var target = Data.Trash.Where(t => !t.Collected && !t.Hidden && !t.Buried && t.Mass <= 6 && t.Mass + bot.Load <= Config.BotCapacity)
                .MinBy(t => Vector2.DistanceSquared(t.Position, bot.Position));
            bot.Returning = bot.Load >= Config.BotCapacity - 1 || (target == null && bot.Load > 0) || bot.Returning;
            var destination = bot.Returning ? new Vector2(0, 100) : target?.Position ?? bot.Position;
            var delta = destination - bot.Position;
            if (delta.Length() > 8)
            {
                bot.Position += Vector2.Normalize(delta) * Math.Min(delta.Length(), Config.BotSpeed * dt);
            }
            else if (bot.Returning)
            {
                Data.Recovered += bot.Load;
                Data.BotMass += bot.Load;
                Data.Scrap += bot.Scrap;
                Data.Components += bot.Components;
                bot.Load = bot.Scrap = 0;
                bot.Components = 0;
                bot.Returning = false;
                bot.Pause = 1.4f;
                Mark("first_bot_deposit");
                CheckMilestone();
                Feedback?.Invoke(new(bot.Position, "bot", 20));
            }
            else if (target != null)
            {
                target.Collected = true;
                bot.Load += target.Mass;
                bot.Scrap += ScrapFor(target);
                bot.Components += target.Kind == 4 ? 1 : 0;
                bot.Pause = 1.1f;
                Feedback?.Invoke(new(bot.Position, "bot", 3));
            }
        }
    }

    public bool EnterBiodome()
    {
        if (Phase != SlicePhase.Workshop || !Data.CoreProduced)
        {
            return false;
        }
        Phase = SlicePhase.Biodome;
        return true;
    }

    public bool ChooseEcology(Ecology choice)
    {
        if (Phase != SlicePhase.Biodome || !Data.CoreProduced || Data.Ecology != Ecology.None || choice == Ecology.None)
        {
            return false;
        }
        Data.Ecology = choice;
        Mark("ecology_chosen");
        Notify("A little life. A permanent beginning.", "relic");
        return true;
    }

    public bool BeginDeparture()
    {
        if (Phase != SlicePhase.Biodome || Data.Ecology == Ecology.None || !Data.CoreProduced)
        {
            return false;
        }
        Phase = SlicePhase.Departure;
        _phaseTime = 0;
        Mark("departure");
        Feedback?.Invoke(new(Vector2.Zero, "departure", 100));
        return true;
    }

    private void CompleteDeparture()
    {
        Data.SiteRecovering = true;
        Data.Departures++;
        Data.Scrap = Data.Hopper = Data.HopperScrap = Data.Recovered = 0;
        Data.Components = Data.HopperComponents = 0;
        Data.Upgrades = new bool[3];
        Data.Bots.Clear();
        Data.CoreProduced = false;
        Player = new(-60, 150);
        Generate(true);
        Phase = SlicePhase.Teaser;
        Notify(Data.Ecology == Ecology.PaperFinch
            ? "NEW RESTORATION SITE. Your finches are already gathering paper. Follow them."
            : "NEW RESTORATION SITE. Scrub grass catches litter and makes your suction 60% faster.", "relic");
    }

    private void UpdateEcology(float dt)
    {
        if (Data.Ecology != Ecology.PaperFinch)
        {
            return;
        }
        var nest = new Vector2(60, 250);
        foreach (var t in Data.Trash.Where(t => t.Kind == 0 && !t.Collected))
        {
            var delta = nest + new Vector2(t.Id % 5 * 5, t.Id % 3 * 5) - t.Position;
            if (delta.Length() > 8 && Vector2.Distance(Player, t.Position) > 45)
            {
                t.Position += Vector2.Normalize(delta) * Math.Min(delta.Length(), 42 * dt);
            }
        }
    }

    public static string SavePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Aftergreen", "slice-save.json");

    public static SliceSave? LoadSave(string? path = null)
    {
        path ??= SavePath;
        if (!File.Exists(path))
        {
            return null;
        }
        var save = JsonSerializer.Deserialize<SliceSave>(File.ReadAllText(path));
        if (save == null || save.Version != 1 || save.Upgrades == null || save.Upgrades.Length != 3 || !Enum.IsDefined(save.Ecology)
            || save.Trash == null || save.Bots == null || save.Milestones == null)
        {
            throw new InvalidDataException("Unsupported AFTERGREEN save.");
        }
        return save;
    }

    public void Save(string? path = null)
    {
        path ??= SavePath;
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var json = JsonSerializer.Serialize(Data, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path + ".tmp", json);
        File.Move(path + ".tmp", path, true);
        File.WriteAllText(Path.Combine(Path.GetDirectoryName(path)!, "playtest.json"), JsonSerializer.Serialize(new
        {
            Data.Seconds,
            Data.Milestones,
            Data.Trips,
            Data.IdleSeconds,
            Data.BotMass,
            Ecology = Data.Ecology.ToString(),
            Data.Departures
        }, new JsonSerializerOptions { WriteIndented = true }));
    }
}
