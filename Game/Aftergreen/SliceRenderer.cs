using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Myra;

namespace Graphite.Game.Aftergreen;

public sealed class SliceRenderer : IDisposable
{
    private static readonly Color Ink = new(24, 43, 43);
    private static readonly Color Cream = new(241, 237, 211);
    private static readonly Color Mint = new(172, 218, 135);
    private static readonly Color Gold = new(242, 190, 98);
    private readonly SliceState _state;
    private readonly SpriteBatch _batch;
    private readonly Texture2D _pixel;
    private readonly Texture2D _circle;
    private readonly SpriteFontBase _font;
    private readonly List<(Rectangle Bounds, string Action)> _buttons = [];
    private readonly List<Particle> _particles = [];
    private readonly Random _random = new(18);
    private Vector2 _camera;
    private float _scale = 1;
    private Vector2 _offset;
    public bool ResetConfirmation { get; set; }
    public bool Muted { get; set; }
    private sealed class Particle
    {
        public Vector2 P;
        public Vector2 V;
        public float Life;
        public Color Color;
    }

    public SliceRenderer(SliceState state)
    {
        _state = state;
        var device = MyraEnvironment.Game.GraphicsDevice;
        _batch = new(device);
        _pixel = new(device, 1, 1);
        _pixel.SetData(new[] { Color.White });
        _circle = new(device, 64, 64);
        var pixels = new Color[64 * 64];
        for (var y = 0; y < 64; y++)
        {
            for (var x = 0; x < 64; x++)
            {
                pixels[y * 64 + x] = new Color(255, 255, 255) * Math.Clamp(32 - Vector2.Distance(new(x + .5f, y + .5f), new(32)), 0, 1);
            }
        }
        _circle.SetData(pixels);
        _font = DefaultAssets.DebugFont;
        _camera = state.Player;
    }

    public void Update(float dt)
    {
        _camera = Vector2.Lerp(_camera, _state.Player + _state.Aim * 35, Math.Min(1, dt * 6));
        foreach (var p in _particles)
        {
            p.P += p.V * dt;
            p.V *= MathF.Pow(.2f, dt);
            p.Life -= dt;
        }
        _particles.RemoveAll(p => p.Life <= 0);
    }

    public void Burst(Feedback feedback)
    {
        for (var i = 0; i < feedback.Count; i++)
        {
            var angle = (float)_random.NextDouble() * MathF.Tau;
            _particles.Add(new Particle
            {
                P = feedback.Position,
                V = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * _random.Next(20, 130),
                Life = .4f + (float)_random.NextDouble(),
                Color = feedback.Kind is "relic" or "upgrade" ? Mint : Gold
            });
        }
    }

    public Vector2 ScreenToWorld(Vector2 p) => (p - _offset) / _scale - new Vector2(640, 380) + _camera;
    public string? Hit(Vector2 p)
    {
        var logical = (p - _offset) / _scale;
        return _buttons.LastOrDefault(b => b.Bounds.Contains(logical)).Action;
    }
    private Vector2 World(Vector2 p) => p - _camera + new Vector2(640, 380);
    private void Box(float x, float y, float w, float h, Color color) => _batch.Draw(_pixel, new Rectangle((int)x, (int)y, Math.Max(1, (int)w), Math.Max(1, (int)h)), color);
    private void Disc(Vector2 p, float radius, Color color) => _batch.Draw(_circle, p, null, color, 0, new(32), radius / 32, SpriteEffects.None, 0);
    private void Line(Vector2 a, Vector2 b, Color color, float width = 2)
    {
        var d = b - a;
        _batch.Draw(_pixel, a, null, color, MathF.Atan2(d.Y, d.X), new(0, .5f), new Vector2(d.Length(), width), SpriteEffects.None, 0);
    }
    private void Text(string text, float x, float y, Color? color = null, float size = 1)
        => _font.DrawText(_batch, text, new(x, y), color ?? Cream, scale: new Vector2(size * .72f));
    private void Button(string action, string label, int x, int y, int width, bool enabled = true)
    {
        var bounds = new Rectangle(x, y, width, 44);
        Box(x, y, width, 44, enabled ? Mint : new Color(66, 79, 72));
        var labelScale = Math.Min(1, (width - 32) / (_font.MeasureString(label).X * .72f));
        Text(label, x + 16, y + 12, enabled ? Ink : new Color(154, 165, 147), labelScale);
        if (enabled)
        {
            _buttons.Add((bounds, action));
        }
    }

    public void Draw(string? capturePath = null)
    {
        var device = MyraEnvironment.Game.GraphicsDevice;
        using var target = capturePath == null ? null : new RenderTarget2D(device, 1280, 720);
        if (target != null)
        {
            device.SetRenderTarget(target);
        }
        var vp = device.Viewport;
        _scale = Math.Min(vp.Width / 1280f, vp.Height / 720f);
        _offset = new((vp.Width - 1280 * _scale) / 2, (vp.Height - 720 * _scale) / 2);
        _buttons.Clear();
        _batch.Begin(blendState: BlendState.AlphaBlend, samplerState: SamplerState.LinearClamp,
            transformMatrix: Matrix.CreateScale(_scale) * Matrix.CreateTranslation(_offset.X, _offset.Y, 0));
        DrawExterior();
        DrawHud();
        switch (_state.Phase)
        {
            case SlicePhase.Workshop: DrawWorkshop(); break;
            case SlicePhase.Biodome: DrawBiodome(); break;
            case SlicePhase.Departure: DrawDeparture(); break;
            case SlicePhase.Complete: DrawComplete(); break;
            case SlicePhase.Paused: DrawPause(); break;
        }
        if (_state.Card != null)
        {
            DrawCard();
        }
        _batch.End();
        if (target != null)
        {
            device.SetRenderTarget(null);
            using var stream = File.Create(capturePath!);
            target.SaveAsPng(stream, target.Width, target.Height);
        }
    }

    private void DrawExterior()
    {
        var teaser = _state.Data.Departures > 0;
        Box(0, 0, 1280, 720, teaser ? new Color(139, 150, 117) : new Color(184, 160, 119));
        // Deterministic ground marks and dune contours are rendered without physics.
        for (var i = 0; i < 330; i++)
        {
            var p = World(new Vector2((i * 173 % 2400) - 1200, (i * 317 % 2400) - 1200));
            if (p.X < -80 || p.X > 1360 || p.Y < -80 || p.Y > 800)
            {
                continue;
            }
            if (i % 9 == 0)
            {
                Line(p, p + new Vector2(55, -8), new Color(156, 137, 104) * .4f, 2);
                Line(p + new Vector2(55, -8), p + new Vector2(75, 4), new Color(156, 137, 104) * .4f, 2);
            }
            else
            {
                Disc(p, 1 + i % 3, new Color(131, 119, 90) * .35f);
            }
        }
        for (var i = 0; i < 100; i++)
        {
            var a = i * MathF.Tau / 100;
            var p = World(new Vector2(MathF.Cos(a), MathF.Sin(a)) * (1000 + i % 4 * 17));
            Disc(p + new Vector2(4, 8), 32, Ink * .22f);
            Box(p.X - 20, p.Y - 16, 42, 27, i % 2 == 0 ? new Color(100, 103, 87) : new Color(133, 111, 83));
            Line(p - new Vector2(17, 9), p + new Vector2(17, 9), Cream * .15f, 3);
        }
        if (!teaser)
        {
            for (var h = 0; h < 6; h++)
            {
                var p = World(SliceState.Heaps[h]);
                var hidden = _state.Data.Trash.Any(t => t.Group == h && t.Hidden);
                if (hidden)
                {
                    Disc(p + new Vector2(4, 14), 84, Ink * .18f);
                    for (var j = 0; j < 9; j++)
                    {
                        var offset = new Vector2((j * 37 % 100) - 50, (j * 23 % 65) - 35);
                        Box(p.X + offset.X - 15, p.Y + offset.Y, 40, 24, j % 2 == 0 ? new Color(126, 113, 91) : new Color(100, 117, 106));
                        Line(p + offset, p + offset + new Vector2(28, 12), Cream * .25f, 2);
                    }
                }
                Text(new[] { "01 / TIRE ANCHOR", "02 / RELIC HEAP", "03 / PARKR-7", "04 / METAL CACHE", "05 / SCRAP RIDGE", "06 / THE BIG ONE" }[h], p.X - 85, p.Y - 108, Ink, .85f);
                if (h == 2)
                {
                    Robot(p + new Vector2(100, 0), Gold, Vector2.UnitY, false);
                    if (Vector2.Distance(_state.Player, SliceState.Heaps[h]) < 260)
                    {
                        Text(_state.Data.NpcFreed ? "PUBLIC SERVICE RESUMED." : "PARKING LIMIT: 2 HOURS.\nCURRENT DURATION: 43,812 DAYS.", p.X - 90, p.Y + 85, Ink, .85f);
                    }
                }
            }
        }
        if (teaser)
        {
            DrawEcology();
        }
        foreach (var t in _state.Data.Trash.OrderBy(t => t.Y))
        {
            if (!t.Collected && !t.Hidden)
            {
                DrawTrash(t);
            }
        }
        DrawArk();
        foreach (var bot in _state.Data.Bots)
        {
            var p = World(bot.Position);
            Robot(p, Mint, new Vector2(MathF.Sin(_state.AnimationTime * (bot.Pause > 0 ? 8 : 1)), 1), true);
            Text(bot.Returning ? $"HOME / {bot.Load:0} kg" : "VERY EMPLOYED", p.X - 45, p.Y - 42, Ink, .7f);
        }
        var player = World(_state.Player);
        var pointer = Microsoft.Xna.Framework.Input.Mouse.GetState();
        var hovered = _state.Data.Trash.Where(t => !t.Hidden && !t.Collected
            && Vector2.Distance(t.Position, ScreenToWorld(new(pointer.X, pointer.Y))) < 20).FirstOrDefault();
        if (hovered != null && !_state.VacuumActive && _state.Phase is SlicePhase.Exterior or SlicePhase.Teaser)
        {
            var names = new[] { "FINAL FINAL NOTICE", "FOREVER BOTTLE", "ARTISAN AIR / EMPTY", "PREMIUM RUST", "SMART TOASTER / OFFLINE", "RETIREMENT PLAN", "EXECUTIVE SEATING", "PLANNED OBSOLESCENCE" };
            Text($"{names[hovered.Kind]} / {hovered.Mass:0.#} kg", 34, 149, Ink, .8f);
        }
        if (_state.VacuumActive)
        {
            var angle = MathF.Atan2(_state.Aim.Y, _state.Aim.X);
            for (var j = 0; j < 26; j++)
            {
                var a = angle - _state.Cone + _state.Cone * 2 * j / 25;
                var ray = new Vector2(MathF.Cos(a), MathF.Sin(a));
                Line(player + ray * 22, player + ray * _state.Range, Cream * .035f, 7);
                var progress = 1 - ((_state.AnimationTime * 1.3f + j * .137f) % 1);
                var p = player + ray * (22 + progress * (_state.Range - 22));
                Line(p, p + ray * (4 + progress * 11), Cream * (1 - progress * .7f), 1.5f);
            }
        }
        if (_state.LatchTarget is { } latch)
        {
            Line(player, World(latch.Position), Ink, 5);
            Line(player, World(latch.Position), Gold, 2);
            Text("KEEP PULLING AWAY", player.X - 85, player.Y + 43, Ink, .8f);
        }
        else if (_state.FindLatchTarget() != null)
        {
            Text("RMB + MOVE AWAY / LATCH", player.X - 120, player.Y + 43, Ink, .8f);
        }
        Robot(player, Cream, _state.Aim, false);
        foreach (var p in _particles)
        {
            Disc(World(p.P), 2 + p.Life * 3, p.Color * Math.Min(1, p.Life));
        }
        var ark = World(new Vector2(0, 80));
        if (ark.X < 100 || ark.X > 1180 || ark.Y < 140 || ark.Y > 570)
        {
            var delta = Vector2.Normalize(ark - player);
            var marker = player + delta * 240;
            Disc(marker, 26, Ink);
            Text("ARK", marker.X - 18, marker.Y - 9, Mint, .8f);
            Line(marker + delta * 28, marker + delta * 42, Cream, 3);
        }
    }

    private void DrawTrash(Debris t)
    {
        var p = World(t.Position);
        if (p.X < -60 || p.X > 1340 || p.Y < -60 || p.Y > 780)
        {
            return;
        }
        var size = t.Mass <= 1 ? 9 : t.Mass <= 6 ? 14 : 25;
        Disc(p + new Vector2(3, 5), size * .8f, Ink * .18f);
        var color = t.Kind switch { 0 => Cream, 1 => new Color(125, 171, 157), 2 => new Color(190, 107, 69), 3 => new Color(117, 132, 121), 4 => new Color(81, 113, 119), _ => new Color(108, 100, 83) };
        if (t.Kind == 5)
        {
            Disc(p, 22, Ink);
            Disc(p, 10, new Color(173, 151, 112));
            Disc(p, 6, Ink * .5f);
        }
        else
        {
            _batch.Draw(_pixel, p, null, color, t.Rotation, new(.5f), new Vector2(size * 1.5f, size), SpriteEffects.None, 0);
            Line(p - new Vector2(size * .4f, 0), p + new Vector2(size * .4f, 0), Cream * .45f, 2);
            if (t.Kind == 4)
            {
                Disc(p, 3, Mint);
            }
            if (t.Kind >= 6)
            {
                Box(p.X - 10, p.Y - 8, 18, 12, Ink * .6f);
            }
        }
        if (t.Buried)
        {
            Disc(p + new Vector2(0, 15), 12 * (1 - Math.Min(.8f, t.Loosened)), new Color(177, 149, 106));
            Disc(p + new Vector2(0, -29), 4, Gold);
        }
    }

    private void Robot(Vector2 p, Color color, Vector2 aim, bool small)
    {
        var s = small ? .75f : 1;
        Disc(p + new Vector2(3, 10), 23 * s, Ink * .2f);
        Box(p.X - 24 * s, p.Y - 14 * s, 10 * s, 32 * s, Ink);
        Box(p.X + 14 * s, p.Y - 14 * s, 10 * s, 32 * s, Ink);
        Disc(p, 21 * s, color);
        Disc(p - new Vector2(3, 3), 15 * s, Color.Lerp(color, Color.White, .25f));
        Box(p.X - 13 * s, p.Y - 9 * s, 26 * s, 14 * s, Ink);
        Disc(p + new Vector2(-6 * s, -3 * s), 2.5f * s, Mint);
        Disc(p + new Vector2(6 * s, -3 * s), 2.5f * s, Mint);
        Line(p + new Vector2(0, -18 * s), p + new Vector2(3, -29 * s), Ink, 2);
        Disc(p + new Vector2(3, -29 * s), 3, Gold);
        if (!small)
        {
            Line(p + aim * 12, p + aim * 33, Ink, 13);
            Line(p + aim * 14, p + aim * 35, new Color(143, 163, 146), 8);
            Disc(p + aim * 35, 6, Ink);
        }
    }

    private void DrawArk()
    {
        var p = World(Vector2.Zero);
        Disc(p + new Vector2(8, 28), 116, Ink * .2f);
        Box(p.X - 108, p.Y - 67, 216, 145, Ink);
        Box(p.X - 98, p.Y - 82, 196, 139, new Color(222, 216, 185));
        Box(p.X - 83, p.Y - 68, 166, 101, new Color(196, 196, 164));
        Disc(p - new Vector2(0, 23), 49, new Color(90, 133, 118));
        Disc(p - new Vector2(7, 31), 34, new Color(136, 172, 143));
        Line(p + new Vector2(-40, -23), p + new Vector2(40, -23), Cream * .35f, 3);
        Line(p + new Vector2(0, -64), p + new Vector2(0, 18), Cream * .35f, 3);
        Box(p.X - 112, p.Y - 28, 27, 75, new Color(91, 118, 109));
        Box(p.X + 85, p.Y - 28, 27, 75, new Color(91, 118, 109));
        Box(p.X - 52, p.Y + 41, 104, 40, Ink);
        for (var i = 0; i < 6; i++)
        {
            Box(p.X - 43 + i * 16, p.Y + 49, 10, 5, _state.DepositTime > 0 ? Gold : Mint);
        }
        Text("A R K  /  0 1", p.X - 53, p.Y + 20, Ink, .8f);
        if (_state.Data.CoreProduced)
        {
            Disc(p + new Vector2(0, 62), 12 + MathF.Sin(_state.AnimationTime * 3) * 3, Mint);
        }
        if (_state.NearArk)
        {
            Text(_state.DepositTime > 0 ? "RECYCLING..." : _state.Data.Hopper > 0 ? "[E] RECYCLE HOPPER" : "[E] WORKSHOP", p.X - 83, p.Y + 114, Ink, .9f);
        }
    }

    private void DrawEcology()
    {
        for (var i = 0; i < 24; i++)
        {
            var p = World(new Vector2((i * 47 % 320) - 60, 170 + i * 31 % 200));
            Line(p, p + new Vector2(-6, -12), new Color(70, 107, 74), 2);
            Line(p, p + new Vector2(6, -16), new Color(89, 129, 76), 2);
        }
        if (_state.Data.Ecology == Ecology.PaperFinch)
        {
            for (var i = 0; i < 6; i++)
            {
                var a = _state.AnimationTime * .8f + i;
                var p = World(new Vector2(85 + MathF.Cos(a) * (40 + i * 14), 250 + MathF.Sin(a * 1.3f) * 60));
                Disc(p, 5, Gold);
                var flap = MathF.Sin(_state.AnimationTime * 16 + i) * 8;
                Line(p, p + new Vector2(-11, flap), Cream, 3);
                Line(p, p + new Vector2(11, flap), Cream, 3);
            }
        }
    }

    private void DrawHud()
    {
        var d = _state.Data;
        Box(22, 20, 1236, 80, Ink);
        Text("AFTERGREEN", 42, 32, Cream, 1.4f);
        Text(d.Departures == 0 ? "01 / JUNK DESERT" : "02 / THE QUIET REACH", 43, 66, Mint, .8f);
        Text($"{d.Recovered:0} / {_state.Config.TargetMass:0} kg", 420, 31, Cream, 1.3f);
        Box(420, 68, 390, 7, new Color(68, 84, 71));
        Box(420, 68, 390 * Math.Min(1, d.Recovered / _state.Config.TargetMass), 7, Mint);
        Text($"SCRAP  {d.Scrap:0}     COMPONENTS  {d.Components}", 878, 39, Gold, .9f);
        Text(d.RoutingChip ? "ROUTING CHIP / PERMANENT" : "RESTORATION UNIT / ONLINE", 878, 67, Mint, .7f);
        Box(22, 610, 292, 87, Ink);
        Text($"HOPPER   {d.Hopper:0} / {_state.Capacity:0} kg", 40, 623, Cream);
        Box(40, 653, 250, 12, new Color(66, 83, 70));
        Box(40, 653, 250 * d.Hopper / _state.Capacity, 12, d.Hopper >= _state.Capacity ? Gold : Mint);
        Text($"+{d.Hopper:0} kg awaiting recycling", 40, 678, Mint, .7f);
        Box(970, 540, 288, 157, Ink);
        DrawMap();
        if (_state.MessageTime > 0)
        {
            Box(335, 612, 615, 43, Ink * .95f);
            Text(_state.Message, 350, 624, Cream, .74f);
        }
        Text("WASD move   /   Mouse aim   /   Hold LMB vacuum", 335, 665, Ink, .9f);
        Text($"RMB + move away: pull   /   E recycle   /   Esc pause   /   M {(Muted ? "unmute" : "mute")}", 335, 685, Ink, .8f);
        var objective = d.Departures > 0 ? "Sweep the litter helped by your new habitat."
            : d.CoreProduced ? "A new beginning is ready. Visit the Ark biodome."
            : !d.Milestones.ContainsKey("first_pickup") ? "Hold the left mouse button. Sweep your first patch."
            : !d.Milestones.ContainsKey("first_excavation") ? "Golden dots mark buried objects. Latch, then move away."
            : !d.RoutingChip ? "Explore the Relic Heap, southeast of the Ark."
            : d.Bots.Count == 0 ? "Build your Collector Bot at the Ark workshop."
            : "Work together. Recover one tonne. Grow something new.";
        Text(objective, 34, 118, Ink, .95f);
    }

    private void DrawMap()
    {
        Text("SITE SURVEY", 987, 550, Mint, .7f);
        var center = new Vector2(1110, 624);
        foreach (var t in _state.Data.Trash.Where(t => !t.Collected))
        {
            Disc(center + t.Position * .064f, t.Buried ? 2 : 1, t.Buried ? Gold : Cream * .5f);
        }
        Disc(center, 4, Mint);
        Disc(center + _state.Player * .064f, 4, Color.White);
        if (_state.Data.NpcFreed && _state.Data.Trash.Any(t => t.Group == 3 && !t.Collected))
        {
            var cache = center + SliceState.Heaps[3] * .064f;
            Disc(cache, 6, Gold);
            Disc(cache, 3, Ink);
        }
        foreach (var b in _state.Data.Bots)
        {
            Disc(center + b.Position * .064f, 3, Mint);
        }
        Text($"BOT RECOVERED  {_state.Data.BotMass:0} kg", 987, 678, Cream, .65f);
    }

    private void Panel(string eyebrow, string title)
    {
        Box(0, 0, 1280, 720, Ink * .65f);
        Box(210, 135, 860, 465, Ink);
        Text(eyebrow, 250, 165, Mint, .85f);
        Text(title, 250, 195, Cream, 1.8f);
    }

    private void DrawWorkshop()
    {
        Panel("ARK / FIELD FABRICATION", "A little more capable.");
        Text($"{_state.Data.Scrap:0} scrap  /  {_state.Data.Components} components", 750, 174, Gold);
        var names = new[] { "WIDER INTAKE", "BIGGER HOPPER", "STRONGER MOTOR" };
        var desc = new[] { "37% wider suction cone", "100 kg > 160 kg capacity", "2.3x pull / longer reach" };
        for (var i = 0; i < 3; i++)
        {
            var x = 250 + i * 263;
            Text(names[i], x, 260, Cream);
            Text(desc[i], x, 291, Mint, .8f);
            Button("upgrade" + i, _state.Data.Upgrades[i] ? "FITTED" : $"[{i + 1}] FIT / {_state.Config.UpgradeCosts[i]} scrap", x, 325, 244,
                !_state.Data.Upgrades[i] && _state.Data.Scrap >= _state.Config.UpgradeCosts[i]);
        }
        Text("COLLECTOR BOT", 250, 402, Cream);
        Text("Finds litter. Carries it home. Loves the job.", 250, 431, Mint, .85f);
        Button("bot", _state.Data.Bots.Count > 0 ? "HAPPILY EMPLOYED" : !_state.Data.RoutingChip ? "FIND THE ROUTING CHIP" : $"[4] BUILD / {_state.Config.BotCost} scrap + 1 component", 635, 402, 392,
            _state.Data.RoutingChip && _state.Data.Bots.Count == 0 && _state.Data.Scrap >= _state.Config.BotCost && _state.Data.Components > 0);
        Button("back", "BACK TO THE DUST", 250, 520, 240);
        Button("dome", _state.Data.CoreProduced ? "[B] ENTER BIODOME" : "BIODOME / NEEDS 1 TONNE", 660, 520, 367, _state.Data.CoreProduced);
    }

    private void DrawBiodome()
    {
        Box(0, 0, 1280, 720, new Color(34, 58, 48));
        Disc(new(640, 330), 285, new Color(49, 78, 58));
        Disc(new(640, 300), 210, new Color(64, 93, 64));
        for (var i = 0; i < 7; i++)
        {
            Line(new(320 + i * 108, 110), new(220 + i * 138, 480), Cream * .08f, 3);
        }
        Text("ARK / BIODOME", 70, 45, Mint);
        Text("Room for a beginning.", 70, 82, Cream, 2.3f);
        Text("One tonne of yesterday. One small chance for tomorrow.", 72, 133, Cream, .9f);
        Disc(new(640, 375), 145, new Color(28, 43, 35));
        Disc(new(640, 359), 132, new Color(105, 83, 55));
        var grown = _state.Data.Ecology != Ecology.None;
        var height = grown ? 115 : 34;
        Line(new(640, 385), new(640, 385 - height), Mint, 5);
        for (var i = 0; i < (grown ? 9 : 2); i++)
        {
            var p = new Vector2(640 + (i % 2 == 0 ? -20 : 20), 375 - i * 11);
            Disc(p, grown ? 18 : 9, i % 2 == 0 ? Mint : new Color(103, 150, 85));
        }
        Disc(new(445, 365), 28, Ink);
        Disc(new(445, 355), 15 + MathF.Sin(_state.AnimationTime * 2) * 2, Mint);
        Text(grown ? "BIO-CORE / ROOTED" : "BIO-CORE / READY", 357, 408, Mint, .8f);
        if (_state.Data.Ecology == Ecology.PaperFinch)
        {
            Box(760, 288, 46, 60, Gold);
            Disc(new(783, 312), 11, Ink);
            for (var i = 0; i < 3; i++)
            {
                var p = new Vector2(770 + MathF.Sin(_state.AnimationTime + i * 2) * 70, 250 + i * 25);
                Disc(p, 6, Gold);
                Line(p, p + new Vector2(12, MathF.Sin(_state.AnimationTime * 12) * 8), Cream, 3);
            }
        }
        if (_state.Data.Ecology == Ecology.ScrubGrass)
        {
            for (var i = 0; i < 28; i++)
            {
                var p = new Vector2(550 + i * 47 % 185, 340 + i * 31 % 70);
                Line(p, p + new Vector2(MathF.Sin(i) * 10, -28), Mint, 3);
            }
        }
        Box(55, 480, 1170, 215, Ink);
        if (!grown)
        {
            Text("CHOOSE YOUR FIRST PERMANENT HABITAT", 82, 500, Mint, .85f);
            Text("Paper Finch Habitat", 82, 532, Cream, 1.2f);
            Text("Birds gather paper into satisfying little piles at future sites.", 82, 565, Cream, .85f);
            Button("finch", "[1] PLANT / PAPER FINCH", 82, 617, 470);
            Text("Scrub Grass", 676, 532, Cream, 1.2f);
            Text("Catches litter. Makes future suction 60% faster.", 676, 565, Cream, .85f);
            Button("grass", "[2] PLANT / SCRUB GRASS", 676, 617, 470);
        }
        else
        {
            Text("STAYS HERE", 82, 502, Gold);
            Text("Field upgrades, scrap, components and Collector Bot.\nThey remain to finish the job.", 82, 536, Cream, .9f);
            Text("TRAVELS WITH YOU", 676, 502, Mint);
            Text(($"{(_state.Data.RoutingChip ? "Routing Chip + automation blueprint" : "Your restoration unit")}\n{(_state.Data.Ecology == Ecology.PaperFinch ? "Paper Finch Habitat" : "Scrub Grass")} / permanent"), 676, 536, Cream, .9f);
            Button("back", "KEEP CLEANING", 82, 617, 300);
            Button("depart", "CONFIRM DEPARTURE", 676, 617, 470);
        }
    }

    private void DrawPause()
    {
        Panel("A RESTORATION EXPEDITION / PLAYABLE PROTOTYPE", "AFTERGREEN");
        Text("You are a tiny machine. This is a very big mess.", 250, 257, Mint, 1.1f);
        Text("Sweep loose litter. Pull the past out of the sand.\nBuild a helper. Turn one tonne of waste into life.\nThen carry that life somewhere new.", 250, 305, Cream, 1.05f);
        Text("WASD move  /  Mouse aim  /  Hold LMB vacuum\nHold RMB and move away to excavate  /  E recycle\nEsc pause  /  M sound  /  Progress saves automatically", 250, 405, Cream, .9f);
        Button("resume", "[ENTER] BEGIN / CONTINUE", 250, 520, 340);
        Button("reset", ResetConfirmation ? "CONFIRM ERASE & RESTART" : "NEW EXPEDITION", 650, 520, 377);
    }

    private void DrawCard()
    {
        var relic = _state.Card == "relic";
        Panel(relic ? "DISCOVERY / PERMANENT" : "PUBLIC SERVICE ANNOUNCEMENT", relic ? "Municipal Routing Chip" : "PARKR-7 / back on duty");
        Text(relic ? "Designed to coordinate municipal maintenance units.\nSomehow still believes the city is operating normally."
            : "VIOLATION DETECTED.\nPARKING LIMIT: 2 HOURS.\nCURRENT DURATION: 43,812 DAYS.\nPENALTY CALCULATION FAILED.", 250, 285, Cream, 1.1f);
        Text(relic ? "AUTOMATION TECHNOLOGY UNLOCKED\nFabricate a Collector Bot at the Ark. Blueprint travels with you."
            : "Thank you, citizen. High-value metal marked west of the Ark.\nPUBLIC SERVICE RESUMED.", 250, 415, Mint, 1);
        Button("close", "[ENTER] CONTINUE", 250, 520, 777);
    }

    private void DrawDeparture()
    {
        Box(0, 0, 1280, 720, Ink * .85f);
        Text("SITE RECOVERY INITIATED", 350, 240, Mint, 1.8f);
        Text("The tools stay. Life takes root. You carry the beginning.", 350, 300, Cream);
        for (var i = 0; i < 18; i++)
        {
            var p = new Vector2(310 + i * 38, 440);
            Line(p, p + new Vector2(MathF.Sin(i) * 12, -30 - MathF.Sin(_state.AnimationTime + i) * 12), Mint, 3);
        }
        Text("NEXT / THE QUIET REACH", 470, 520, Gold);
    }

    private void DrawComplete()
    {
        Panel("VERTICAL SLICE / EXPEDITION COMPLETE", "Leave it better.");
        Text("Your habitat travelled with you. Nature is helping clean again.\nThe first site is recovering. This one is a new beginning.", 250, 285, Cream, 1.1f);
        Text($"{_state.Data.Seconds / 60:0.0} minutes played   /   {_state.Data.Trips} recycler trips\nCollector contribution: {_state.Data.BotMass:0} kg\nPermanent habitat: {_state.Data.Ecology}", 250, 371, Mint);
        Text("Would you willingly restore another site?", 250, 467, Gold, 1.1f);
        Button("back", "LOOK AROUND THE NEW SITE", 250, 520, 777);
    }

    public void Dispose()
    {
        _batch.Dispose();
        _pixel.Dispose();
        _circle.Dispose();
    }
}
