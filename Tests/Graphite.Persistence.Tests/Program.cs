using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Graphite.Engine.Configuration;
using Graphite.Engine.Persistence;
using Graphite.Game.Configuration;
using Chisel.Generated;
using Graphite.Game.Domain.Player;
using Graphite.Game.Sessions;

namespace Graphite.Persistence.Tests;

internal static class Program
{
    private static int _checks;

    private static void Main()
    {
        var root = Path.Combine(Path.GetTempPath(), "graphite-persistence-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            Throws<InvalidOperationException>(() => Storage.Load<Session>());
            Throws<InvalidOperationException>(() => Preferences.Get<bool>("muted"));
            ActiveSession();
            Serialization();
            EnumContractChecks.Run();
            EnumContractChecks.GeneratedIds();
            Loadouts();
            Slots(Path.Combine(root, "slots"));
            Migrations(Path.Combine(root, "migrations"));
            InterruptedWrites(Path.Combine(root, "writes"));
            PreferenceChecks(Path.Combine(root, "preferences"));
            Sessions(Path.Combine(root, "sessions"));
            Check(PersistencePaths.ForGame("deep-drive", SettingsEnvironment.Staging)
                != PersistencePaths.ForGame("deep-drive", SettingsEnvironment.Production), "Environments use separate directories");
            Throws<ArgumentException>(() => PersistencePaths.ForGame("../escape", SettingsEnvironment.Staging));
        }
        finally
        {
            Storage.Shutdown();
            Preferences.Shutdown();
            Directory.Delete(root, recursive: true);
        }
        Throws<InvalidOperationException>(() => Storage.Save(new Session()));
        Throws<InvalidOperationException>(() => Preferences.Set("muted", true));
        Console.WriteLine($"Persistence checks passed ({_checks} assertions).");
    }

    private static void Serialization()
    {
        var serializer = new SaveSerializer();
        var source = new Fixture();
        source.SetScore(12);
        source.Items.Add(new Item { Name = "glass", Amount = 3 });
        source.Counts["metal"] = 7;
        source.Tags.Add("coast");
        var bytes = serializer.Serialize(source);
        var text = Encoding.UTF8.GetString(bytes);
        Check(!text.Contains("RuntimeOnly") && !text.Contains("_score"), "Only explicit stable names are saved");
        var copy = serializer.Deserialize<Fixture>(bytes);
        Check(copy.Score == 12 && copy.Items[0].Name == "glass" && copy.Items[0].Amount == 3
            && copy.Counts["metal"] == 7 && copy.Tags.Contains("coast"), "Private fields and nested collections round trip");
        Check(!ReferenceEquals(source.Items, copy.Items), "Loaded objects are independent");
        var renamed = serializer.Deserialize<RenamedFixture>(bytes);
        Check(renamed.Points == 12, "C# type and member renames preserve saved keys");
        var empty = serializer.Deserialize<Fixture>("{\"items\":null,\"counts\":null,\"tags\":null,\"array\":null,\"readOnly\":null}"u8);
        Check(empty.Items.Count == 0 && empty.Counts.Count == 0 && empty.Tags.Count == 0
            && empty.Array.Length == 0 && empty.ReadOnly.Count == 0, "Explicit null collections become appropriate empty collections");
        Check(serializer.Deserialize<Fixture>("{}"u8).Items.Count == 0, "Missing fields preserve initialized defaults");
        Check(serializer.Deserialize<List<string>>("null"u8).Count == 0, "Null root collections become empty");
        Check(serializer.Deserialize<List<List<int>>>("[null,[1]]"u8)[0].Count == 0, "Nested list nulls become empty");
        Check(serializer.Deserialize<Dictionary<string, int[]>>("{\"items\":null}"u8)["items"].Length == 0, "Dictionary collection values cannot load as null");
        Check(serializer.Deserialize<HashSet<List<int>>>("[null]"u8).Single().Count == 0, "Nested set collections cannot load as null");
        Throws<NotSupportedException>(() => serializer.Serialize(new UnsupportedFixture()));
        Throws<NotSupportedException>(() => serializer.Serialize(new DuplicateNames()));
        Throws<NotSupportedException>(() => serializer.Serialize(new ReadonlyFixture()));
        Throws<NotSupportedException>(() => serializer.Serialize(new Dictionary<int, string> { [1] = "value" }));
        Throws<NotSupportedException>(() => serializer.Serialize<BaseFixture>(new DerivedFixture()));
        Throws<NotSupportedException>(() => serializer.Serialize(new List<BaseFixture> { new DerivedFixture() }));
        var cycle = new Cycle();
        cycle.Next = cycle;
        Throws<JsonException>(() => serializer.Serialize(cycle));
        Throws<InvalidDataException>(() => serializer.Deserialize<Fixture>("{\"score\":-1}"u8));
        Throws<InvalidDataException>(() => serializer.Deserialize<GuardedProperty>("{\"value\":-1}"u8));
        var inherited = serializer.Deserialize<InheritedPrivate>(serializer.Serialize(new InheritedPrivate()));
        Check(inherited.BaseValue == 4, "Private members of base classes are included");
        var custom = new SaveSerializer(new CoordinateConverter());
        var coordinate = custom.Deserialize<Coordinate>(custom.Serialize(new Coordinate(3, 7)));
        Check(coordinate == new Coordinate(3, 7), "Custom value converters extend serialization");
    }

    private static void Loadouts()
    {
        var serializer = new SaveSerializer();
        var source = new Loadout();
        var bytes = serializer.Serialize(source);
        var json = JsonNode.Parse(bytes)!.AsObject();
        Check(json.Count == 4 && json["chassis"]!.GetValue<string>() == "STARTER_MECH"
            && json["pilot"]!.GetValue<string>() == "STARTER_PILOT"
            && json["weaponLeft"]!.GetValue<string>() == "AUTOCANNON"
            && json["weaponRight"]!.GetValue<string>() == "AUTOCANNON",
            "Loadouts save stable Chisel slugs under the four explicit save keys, not dense enum indexes");
        var copy = serializer.Deserialize<Loadout>(bytes);
        Check(copy.ChassisId == source.ChassisId && copy.PilotId == source.PilotId
            && copy.WeaponLeftId == source.WeaponLeftId && copy.WeaponRightId == source.WeaponRightId,
            "All typed loadout selections round trip through stable slugs");
        copy.WeaponLeftId = ChiselWeaponsId.Invalid;
        Check(source.WeaponLeftId == ChiselWeaponsId.AUTOCANNON && copy.WeaponRightId == ChiselWeaponsId.AUTOCANNON,
            "Loaded loadouts and their two arm selections are independent");
        Throws<JsonException>(() => serializer.Serialize(copy));
        foreach (var key in new[] { "chassis", "pilot", "weaponLeft", "weaponRight" })
        {
            var unknown = JsonNode.Parse(bytes)!.AsObject();
            unknown[key] = "MISSING_CONTENT";
            Throws<JsonException>(() => serializer.Deserialize<Loadout>(JsonSerializer.SerializeToUtf8Bytes(unknown)));
            unknown[key] = 0;
            Throws<JsonException>(() => serializer.Deserialize<Loadout>(JsonSerializer.SerializeToUtf8Bytes(unknown)));
            unknown[key] = null;
            Throws<JsonException>(() => serializer.Deserialize<Loadout>(JsonSerializer.SerializeToUtf8Bytes(unknown)));
            unknown.Remove(key);
            var defaults = serializer.Deserialize<Loadout>(JsonSerializer.SerializeToUtf8Bytes(unknown));
            Check(defaults.ChassisId == source.ChassisId && defaults.PilotId == source.PilotId
                && defaults.WeaponLeftId == source.WeaponLeftId && defaults.WeaponRightId == source.WeaponRightId,
                "Missing loadout fields follow the engine's normal initialized-default contract");
        }
        Throws<InvalidDataException>(() => serializer.Deserialize<Loadout>("null"u8));
        Throws<JsonException>(() => serializer.Deserialize<Session>("{\"currentLoadout\":{\"pilot\":\"missing\"}}"u8));
        var oldSession = serializer.Deserialize<Session>("{\"clearedSectors\":[]}"u8);
        Check(oldSession.CurrentLoadout.ChassisId == ChiselChassisId.STARTER_MECH
            && oldSession.CurrentLoadout.PilotId == ChiselPilotId.STARTER_PILOT,
            "Sessions without an authored loadout retain the explicit starter defaults");
    }

    private static void Slots(string root)
    {
        Storage.Initialize(root);
        var id = Guid.NewGuid();
        Check((Storage.TryLoad<Fixture>(id)).Status == SaveStatus.NotFound, "Missing save is distinct");
        Check((Storage.ListSlots<Fixture>()).Count == 0, "An empty store lists no slots");
        Throws<FileNotFoundException>(() => Storage.Load<Fixture>(id));
        var value = new Fixture();
        value.SetScore(5);
        var first = Storage.Save(value, id, "Coast");
        Check(first.Name == "Coast", "Slot metadata is written");
        value.SetScore(8);
        Check((Storage.Save(value, id)).Status == SaveStatus.Success, "Slot can be overwritten");
        var loaded = Storage.TryLoad<Fixture>(id);
        Check(loaded.IsSuccess && loaded.Value!.Score == 8 && !loaded.RecoveredFromBackup, "Latest save loads");
        var slots = Storage.ListSlots<Fixture>();
        Check(slots.Count == 1 && slots[0].CreatedUtc == first.CreatedUtc && slots[0].Name == "Coast", "Slot identity and creation metadata persist");
        var path = Path.Combine(root, id.ToString("N") + ".json");
        File.WriteAllText(path, "{interrupted");
        loaded = Storage.TryLoad<Fixture>(id);
        Check(loaded.IsSuccess && loaded.RecoveredFromBackup && loaded.Value!.Score == 5, "Truncated primary recovers the prior save");
        Check((Storage.ListSlots<Fixture>())[0].RecoveredFromBackup, "Listing reports backup recovery");
        value.SetScore(10);
        Storage.Save(value, id);
        Rewrite(path, node => node["Data"]!["score"] = -10, updateChecksum: true);
        Check((Storage.TryLoad<Fixture>(id)).Value!.Score == 5, "Game validation rejects corrupt data and recovers backup");
        value.SetScore(11);
        Storage.Save(value, id);
        File.WriteAllText(path, "broken");
        Check((Storage.TryLoad<Fixture>(id)).Value!.Score == 5, "Saving after semantic corruption preserves the valid backup");
        File.Delete(path + ".bak");
        Check((Storage.TryLoad<Fixture>(id)).Status == SaveStatus.Corrupt, "Unrecoverable corruption is distinct");
        Storage.Save(value, id);
        Rewrite(path, node => node["SchemaVersion"] = 99);
        var before = File.ReadAllBytes(path);
        Check((Storage.TryLoad<Fixture>(id)).Status == SaveStatus.Incompatible, "Future saves are incompatible");
        Throws<InvalidDataException>(() => Storage.Save(value, id));
        Check(Enumerable.SequenceEqual(before, File.ReadAllBytes(path)), "Incompatible saves remain untouched");
        Check((Storage.ListSlots<Fixture>())[0].Status == SaveStatus.Incompatible, "Listing retains incompatible slots");
        Storage.Delete(id);
        Check(Storage.TryLoad<Fixture>(id).Status == SaveStatus.NotFound, "Delete removes primary and backup");
        Storage.Delete(id);
        var blocked = Path.Combine(root, "not-a-directory");
        File.WriteAllText(blocked, "x");
        Storage.Initialize(blocked);
        Throws<IOException>(() => Storage.Save(value));
        Storage.Initialize(root);
        Storage.Save(value, id);
        Rewrite(path, node => node["Data"]!["score"] = 999);
        Check((Storage.TryLoad<Fixture>(id)).Status == SaveStatus.Corrupt, "Checksum catches altered payloads");
        Storage.Save(value, id);
        Check(!File.Exists(path + ".bak"), "A corrupt primary never replaces a backup");
        File.Delete(path);
        Check((Storage.TryLoad<Fixture>(id)).Status == SaveStatus.NotFound, "No backup exists for a first write");
        Storage.Save(value, id);
        var copiedId = Guid.NewGuid();
        File.Copy(path, Path.Combine(root, copiedId.ToString("N") + ".json"));
        var mismatched = (Storage.ListSlots<Fixture>()).Single(slot => slot.Id == copiedId);
        Check(mismatched.Status == SaveStatus.Incompatible, "Mismatched metadata retains the actual file ID for deletion");
    }

    private static void Migrations(string root)
    {
        var id = Guid.NewGuid();
        Storage.Initialize(root);
        Storage.Save(new OldProfile { Name = "Ada" }, id);
        var path = Path.Combine(root, id.ToString("N") + ".json");
        var before = File.ReadAllBytes(path);
        Check(Storage.TryLoad<Profile>(id).Status == SaveStatus.Incompatible, "Missing migration is incompatible");
        var migration = new SaveMigration("test.profile", 1, data =>
        {
            data["displayName"] = data["oldName"]!.DeepClone();
            data.Remove("oldName");
            return data;
        });
        Storage.RegisterMigration(migration);
        Check(Storage.Load<Profile>(id).DisplayName == "Ada", "Migrations run before deserialization");
        Check(Enumerable.SequenceEqual(before, File.ReadAllBytes(path)), "Loading migrations never rewrites a save");
        Throws<ArgumentException>(() => Storage.RegisterMigration(migration));
        Storage.Initialize(root, migrations: [new SaveMigration("test.profile", 1, _ => throw new InvalidDataException("fixture failure"))]);
        Check(Storage.TryLoad<Profile>(id).Status == SaveStatus.Corrupt, "Failed migration reports failure");
        Check(Enumerable.SequenceEqual(before, File.ReadAllBytes(path)), "Failed migration leaves the file unchanged");
        Storage.Initialize(root, migrations: [migration]);
        Storage.Save(new Profile { DisplayName = "Bea" }, id);
        Check(Storage.TryLoad<OldProfile>(id).Status == SaveStatus.Incompatible, "Older code does not fall back over a newer primary");
        Check(Storage.TryLoad<Fixture>(id).Status == SaveStatus.Incompatible, "Wrong contract is rejected");
        Throws<InvalidDataException>(() => Storage.Save(new Fixture(), id));
    }

    private static void InterruptedWrites(string root)
    {
        Storage.Initialize(root);
        var source = new Fixture();
        source.SetScore(1);
        Storage.Save(source);
        source.SetScore(2);
        var path = Path.Combine(root, "default.json");
        Directory.CreateDirectory(path + ".bak");
        ThrowsFileFailure(() => Storage.Save(source));
        Check(Storage.Load<Fixture>().Score == 1, "A failed write preserves the current save");
        Check(!Directory.EnumerateFiles(root, "*.tmp*").Any(), "Failed writes clean temporary files");
        Directory.Delete(path + ".bak");
        Storage.Save(source);
        source.SetScore(3);
        Check(Storage.Load<Fixture>().Score == 2, "Saving captures the whole object before returning");
        Check(!Directory.EnumerateFiles(root, "*.lock").Any(), "Persistence creates no lock files");
    }

    private static void PreferenceChecks(string root)
    {
        var volume = PlayerPreferences.MasterVolume;
        var muted = new PreferenceKey<bool>("fixture.boolean", false);
        var music = PlayerPreferences.MusicVolume;
        var fx = PlayerPreferences.FxVolume;
        Check(Preferences.Initialize(root).Status == SaveStatus.NotFound && Preferences.Get(volume) == 1, "Missing preferences use typed defaults");
        Check(Preferences.Get(PlayerPreferences.CrtIntensity) == 1f, "Display preferences default to full aged CRT");
        Check(Preferences.Get(music) == 1 && Preferences.Get(fx) == 1, "Music and FX default to full volume");
        Preferences.Set(music, .3f);
        Preferences.Set(fx, .6f);
        foreach (var key in new[] { music, fx })
        {
            Throws<ArgumentOutOfRangeException>(() => Preferences.Set(key, -1f));
            Throws<ArgumentOutOfRangeException>(() => Preferences.Set(key, 2f));
            Throws<ArgumentOutOfRangeException>(() => Preferences.Set(key, float.NaN));
        }
        Check(Preferences.Get(RuntimePreferences.UiScale) == 1f, "Engine UI scale defaults to 100 percent");
        Check(RuntimePreferences.UiScaleOptions.SequenceEqual(new[] { .75f, 1f, 1.25f, 1.5f, 1.75f }),
            "UI scale supports exactly the five requested presets");
        foreach (var scale in RuntimePreferences.UiScaleOptions)
        {
            Preferences.Set(RuntimePreferences.UiScale, scale);
            Check(Preferences.Get(RuntimePreferences.UiScale) == scale, "Every UI-scale preset is accepted");
        }
        foreach (var scale in new[] { .5f, .74f, 1.1f, 1.76f, 2f })
        {
            Throws<ArgumentOutOfRangeException>(() => Preferences.Set(RuntimePreferences.UiScale, scale));
        }
        foreach (var (legacy, expected) in new[] { (.5f, .75f), (2f, 1.75f), (1.31f, 1.25f), (1.375f, 1.5f) })
        {
            Preferences.Set(RuntimePreferences.UiScale.Name, legacy);
            Preferences.Initialize(root);
            Check(RuntimePreferences.GetUiScale() == expected, "Legacy UI scale clamps and snaps to the nearest preset");
            Preferences.Initialize(root);
            Check(Preferences.Get(RuntimePreferences.UiScale) == expected, "Normalized UI scale is saved across reloads");
        }
        Check(Preferences.Get(RuntimePreferences.Display) == "", "Display defaults come from the selected environment");
        Preferences.Set(RuntimePreferences.Display, "BorderlessFullscreen:2560x1440");
        Throws<ArgumentOutOfRangeException>(() => Preferences.Set(RuntimePreferences.Display, "Unknown:2560x1440"));
        Throws<ArgumentOutOfRangeException>(() => Preferences.Set(RuntimePreferences.Display, "Windowed:0x720"));
        Throws<ArgumentOutOfRangeException>(() => Preferences.Set(RuntimePreferences.Display, "Fullscreen:1920x-1"));
        Throws<ArgumentOutOfRangeException>(() => Preferences.Set(RuntimePreferences.Display, "Windowed:1920x1080:extra"));
        var staging = new StagingSettings().Window;
        Check(staging.Width == 2560 && staging.Height == 1440 && staging.Fullscreen && staging.Borderless,
            "Staging defaults to 1440p borderless fullscreen");
        var production = new ProductionSettings().Window;
        Check(production.Width == 1280 && production.Height == 720 && !production.Fullscreen,
            "Production retains its original windowed defaults");
        Preferences.Set(RuntimePreferences.UiScale, 1.25f);
        Throws<ArgumentOutOfRangeException>(() => Preferences.Set(RuntimePreferences.UiScale, 0f));
        Throws<ArgumentOutOfRangeException>(() => Preferences.Set(RuntimePreferences.UiScale, 2.1f));
        Throws<ArgumentOutOfRangeException>(() => Preferences.Set(RuntimePreferences.UiScale, float.NaN));
        Preferences.Set(PlayerPreferences.CrtIntensity, .35f);
        Preferences.Initialize(root);
        Check(Preferences.Get(PlayerPreferences.CrtIntensity) == .35f, "CRT intensity persists immediately");
        Check(Preferences.Get(music) == .3f && Preferences.Get(fx) == .6f, "Music and FX volume persist independently");
        Check(Preferences.Get(RuntimePreferences.UiScale) == 1.25f, "Engine UI scale persists across reloads");
        Check(Preferences.Get(RuntimePreferences.Display) == "BorderlessFullscreen:2560x1440",
            "Display mode and resolution persist together across reloads");
        Check(Preferences.Get<string>("name") == "" && Preferences.Get<int>("quality") == 0
            && !Preferences.Get<bool>("muted") && Preferences.Get<float>("scale") == 0f
            && Preferences.Get<long>("count") == 0L && Preferences.Get<double>("time") == 0d, "String keys have correctly typed defaults");
        Check(Preferences.Get("quality", 3) == 3, "Callers can supply a default");
        Preferences.Set(volume, .4f);
        Check(File.Exists(Preferences.FilePath), "Set persists immediately");
        Preferences.Shutdown();
        Preferences.Initialize(root);
        Check(Preferences.Get(volume) == .4f, "Preferences persist across engine restarts");
        Preferences.Set(muted, true);
        Check(Preferences.Remove(volume) && Preferences.Get(volume) == 1 && Preferences.Get(muted), "Removing a key restores its default without erasing other keys");
        Check(!Preferences.Remove(volume), "Removing a missing key is idempotent");
        Preferences.Initialize(root);
        Check(Preferences.Get(volume) == 1 && Preferences.Get(muted), "Remove persists immediately");
        Preferences.Set("quality", 4);
        Preferences.Set("player", "Ada");
        Preferences.Set("visits", 9L);
        Preferences.Set("time", 2.5d);
        Preferences.Initialize(root);
        Check(Preferences.Get<int>("quality") == 4 && Preferences.Get<string>("player") == "Ada"
            && Preferences.Get<long>("visits") == 9L && Preferences.Get<double>("time") == 2.5d, "String keys round trip with exact types");
        Throws<InvalidOperationException>(() => Preferences.Get<float>("quality"));
        Throws<InvalidOperationException>(() => Preferences.Set("quality", "wrong"));
        Throws<InvalidOperationException>(() => Preferences.Remove<float>("quality"));
        Check(Preferences.Remove<int>("quality"), "String-key removal is typed");
        Throws<ArgumentOutOfRangeException>(() => Preferences.Set(volume, 2f));
        Throws<ArgumentOutOfRangeException>(() => Preferences.Set(volume, float.NaN));
        Throws<NotSupportedException>(() => Preferences.Set("unsupported", new Session()));
        Throws<ArgumentException>(() => Preferences.Get<int>(""));
        Preferences.Set(volume, .7f);
        Preferences.Set(volume, .8f);
        File.WriteAllText(Preferences.FilePath, "broken");
        Check(Preferences.Initialize(root).RecoveredFromBackup && Preferences.Get(volume) == .7f, "Preferences recover the previous file");
        Preferences.Set(volume, .9f);
        File.WriteAllText(Preferences.FilePath, "broken");
        Check(Preferences.Initialize(root).RecoveredFromBackup && Preferences.Get(volume) == .7f, "Saving after recovery preserves the valid backup");
        Preferences.Set(volume, .8f);
        Rewrite(Preferences.FilePath, node => node["SchemaVersion"] = 2);
        var before = File.ReadAllBytes(Preferences.FilePath);
        Check(Preferences.Initialize(root).Status == SaveStatus.Incompatible, "Future preferences are incompatible");
        Throws<InvalidDataException>(() => Preferences.Set(volume, .2f));
        Check(Enumerable.SequenceEqual(before, File.ReadAllBytes(Preferences.FilePath)), "Future preferences cannot be overwritten");
        Check(Preferences.Get(volume) == 1, "Failed writes do not change memory");

        var failures = Path.Combine(root, "failures");
        Preferences.Initialize(failures);
        Preferences.Set(volume, .3f);
        Directory.CreateDirectory(Preferences.FilePath + ".bak");
        ThrowsFileFailure(() => Preferences.Set(volume, .6f));
        ThrowsFileFailure(() => Preferences.Remove(volume));
        Check(Preferences.Get(volume) == .3f, "Failed sets and removals leave the cached value intact");
        Directory.Delete(Preferences.FilePath + ".bak");
        Preferences.Initialize(failures);
        Check(Preferences.Get(volume) == .3f, "Failed sets and removals leave the file intact");
        Preferences.Set(volume, .5f);
        Rewrite(Preferences.FilePath, node => node["Data"]![volume.Name]!["Type"] = "string", updateChecksum: true);
        Check(Preferences.Initialize(failures).RecoveredFromBackup && Preferences.Get(volume) == .3f, "Invalid entry types recover a valid backup");
    }

    private static void ActiveSession()
    {
        Throws<InvalidOperationException>(() => _ = SessionManager.ActiveSession);
        var first = new Session();
        var second = new Session();
        SessionManager.ActiveSession = first;
        Check(ReferenceEquals(SessionManager.ActiveSession, first), "The manager holds the supplied live session without persistence initialization");
        first.ClearSector("test-sector");
        Check(SessionManager.ActiveSession.ClearedSectors.Contains("test-sector"), "Active state is the same in-memory session, not a snapshot");
        Throws<ArgumentNullException>(() => first.ClearSector(null!));
        Throws<ArgumentException>(() => first.ClearSector(" "));
        Check(first.ClearedSectors.SequenceEqual(new[] { "test-sector" }), "Invalid sector IDs cannot silently enter session state");
        SessionManager.ActiveSession = second;
        Check(ReferenceEquals(SessionManager.ActiveSession, second), "The active session can be replaced");
        SessionManager.ActiveSession = null;
        Throws<InvalidOperationException>(() => _ = SessionManager.ActiveSession);
        var nullability = new System.Reflection.NullabilityInfoContext()
            .Create(typeof(SessionManager).GetProperty(nameof(SessionManager.ActiveSession))!);
        Check(nullability.ReadState == System.Reflection.NullabilityState.NotNull
            && nullability.WriteState == System.Reflection.NullabilityState.Nullable,
            "The compiler contract guarantees a non-null getter while allowing null assignments");
    }

    private static void Sessions(string root)
    {
        Storage.Initialize(root);
        var session = new Session();
        session.ClearSector("a-3-crystal-basin");
        Storage.Save(session);
        var loaded = Storage.Load<Session>();
        Check(loaded.ClearedSectors.SequenceEqual(new[] { "a-3-crystal-basin" }),
            "Save and load accept and return the whole campaign directly");
        Check(!ReferenceEquals(session, loaded), "Loaded session is independent of live state");
        Check(!ReferenceEquals(session.CurrentLoadout, loaded.CurrentLoadout)
            && loaded.CurrentLoadout.ChassisId == session.CurrentLoadout.ChassisId
            && loaded.CurrentLoadout.PilotId == session.CurrentLoadout.PilotId
            && loaded.CurrentLoadout.WeaponLeftId == session.CurrentLoadout.WeaponLeftId
            && loaded.CurrentLoadout.WeaponRightId == session.CurrentLoadout.WeaponRightId,
            "The active loadout persists as independent nested session state");
        var slotId = Guid.NewGuid();
        var second = new Session();
        second.ClearSector("b-1-iron-ridge");
        Storage.Save(second, slotId, "Expedition");
        Storage.Shutdown();
        Storage.Initialize(root);
        Check(Storage.Load<Session>().ClearedSectors.SequenceEqual(new[] { "a-3-crystal-basin" })
            && Storage.Load<Session>(slotId).ClearedSectors.SequenceEqual(new[] { "b-1-iron-ridge" }), "The default slot and explicit slots survive restart independently");
        Check(Storage.ListSlots<Session>().Count == 2 && Storage.ListSlots<Session>().Single(slot => slot.Id == slotId).Name == "Expedition", "Default and explicit slots can be listed");
        Storage.Delete();
        Check(Storage.TryLoad<Session>().Status == SaveStatus.NotFound
            && Storage.Load<Session>(slotId).ClearedSectors.SequenceEqual(new[] { "b-1-iron-ridge" }), "Deleting the default slot preserves other slots");
    }

    private static void Rewrite(string path, Action<JsonObject> edit, bool updateChecksum = false)
    {
        var node = JsonNode.Parse(File.ReadAllText(path))!.AsObject();
        edit(node);
        if (updateChecksum)
        {
            node["Checksum"] = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(node["Data"])));
        }

        File.WriteAllBytes(path, JsonSerializer.SerializeToUtf8Bytes(node));
    }

    private static void Check(bool condition, string message)
    {
        _checks++;
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
    private static void Throws<T>(Action action) where T : Exception
    {
        try
        {
            action();
        }
        catch (T)
        {
            _checks++;
            return;
        }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }
    private static void ThrowsFileFailure(Action action)
    {
        try
        {
            action();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            _checks++;
            return;
        }
        throw new InvalidOperationException("Expected a file-system failure.");
    }
    [SaveContract("test.fixture")]
    private sealed class Fixture : ISaveValidatable
    {
        [SaveMember("score")] private int _score;
        public int Score => _score;
        public Action RuntimeOnly { get; } = () => { };
        [SaveMember("items")] public List<Item> Items { get; private set; } = [];
        [SaveMember("counts")] public Dictionary<string, int> Counts { get; private set; } = [];
        [SaveMember("tags")] public HashSet<string> Tags { get; private set; } = [];
        [SaveMember("array")] public int[] Array { get; private set; } = [];
        [SaveMember("readOnly")] public IReadOnlyList<string> ReadOnly { get; private set; } = [];
        public void SetScore(int value) => _score = value;
        public void Validate()
        {
            if (_score < 0)
            {
                throw new InvalidDataException("Score cannot be negative.");
            }
        }
    }
    [SaveContract("test.item")]
    private sealed class Item
    {
        [SaveMember("name")] public string Name { get; set; } = "";
        [SaveMember("amount")]
        public int Amount
        {
            get; set;
        }
    }
    [SaveContract("test.fixture")]
    private sealed class RenamedFixture
    {
        [SaveMember("score")]
        public int Points
        {
            get; private set;
        }
    }
    [SaveContract("test.unsupported")]
    private sealed class UnsupportedFixture
    {
        [SaveMember("callback")] public Action Callback { get; set; } = () => { };
    }
    [SaveContract("test.duplicate")]
    private sealed class DuplicateNames
    {
        [SaveMember("value")]
        public int First
        {
            get; set;
        }
        [SaveMember("value")]
        public int Second
        {
            get; set;
        }
    }
    [SaveContract("test.readonly")]
    private sealed class ReadonlyFixture
    {
        [SaveMember("value")] public readonly int Value = 1;
    }
    [SaveContract("test.base")]
    private class BaseFixture
    {
        [SaveMember("value")]
        public int Value
        {
            get; set;
        }
    }
    [SaveContract("test.derived")]
    private sealed class DerivedFixture : BaseFixture;
    [SaveContract("test.cycle")]
    private sealed class Cycle
    {
        [SaveMember("next")]
        public Cycle? Next
        {
            get; set;
        }
    }
    [SaveContract("test.profile", Version = 1)]
    private sealed class OldProfile
    {
        [SaveMember("oldName")] public string Name { get; set; } = "";
    }
    [SaveContract("test.profile", Version = 2)]
    private sealed class Profile
    {
        [SaveMember("displayName")] public string DisplayName { get; set; } = "";
    }
    [SaveContract("test.guard")]
    private sealed class GuardedProperty
    {
        private int _value;
        [SaveMember("value")]
        public int Value
        {
            get => _value;
            private set => _value = value >= 0 ? value : throw new InvalidDataException("Negative value.");
        }
    }
    private class PrivateBase
    {
        [SaveMember("baseValue")] private int _value = 4;
        public int BaseValue => _value;
    }
    [SaveContract("test.inherited")]
    private sealed class InheritedPrivate : PrivateBase;
    private readonly record struct Coordinate(int X, int Y);
    private sealed class CoordinateConverter : JsonConverter<Coordinate>
    {
        public override Coordinate Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var parts = reader.GetString()!.Split(',');
            return new(int.Parse(parts[0]), int.Parse(parts[1]));
        }
        public override void Write(Utf8JsonWriter writer, Coordinate value, JsonSerializerOptions options)
            => writer.WriteStringValue($"{value.X},{value.Y}");
    }
}
