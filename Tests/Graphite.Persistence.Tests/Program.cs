using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Graphite.Engine.Configuration;
using Graphite.Engine.Persistence;
using Graphite.Game.Preferences;
using Graphite.Game.Sessions;

namespace Graphite.Persistence.Tests;

internal static class Program
{
    private static int _checks;

    private static async Task Main()
    {
        var root = Path.Combine(Path.GetTempPath(), "graphite-persistence-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            Serialization();
            await Slots(Path.Combine(root, "slots"));
            await Migrations(Path.Combine(root, "migrations"));
            await InterruptedAndConcurrentWrites(Path.Combine(root, "writes"));
            await PreferenceChecks(Path.Combine(root, "preferences"));
            await Sessions(Path.Combine(root, "sessions"));
            Check(PersistencePaths.ForGame("aftergreen", SettingsEnvironment.Staging)
                != PersistencePaths.ForGame("aftergreen", SettingsEnvironment.Production), "Environments use separate directories");
            Throws<ArgumentException>(() => PersistencePaths.ForGame("../escape", SettingsEnvironment.Staging));
            Console.WriteLine($"Persistence checks passed ({_checks} assertions).");
        }
        finally { Directory.Delete(root, recursive: true); }
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
        var cycle = new Cycle(); cycle.Next = cycle;
        Throws<JsonException>(() => serializer.Serialize(cycle));
        Throws<InvalidDataException>(() => serializer.Deserialize<Fixture>("{\"score\":-1}"u8));
        Throws<InvalidDataException>(() => serializer.Deserialize<GuardedProperty>("{\"value\":-1}"u8));
        var inherited = serializer.Deserialize<InheritedPrivate>(serializer.Serialize(new InheritedPrivate()));
        Check(inherited.BaseValue == 4, "Private members of base classes are included");
        var custom = new SaveSerializer(new CoordinateConverter());
        var coordinate = custom.Deserialize<Coordinate>(custom.Serialize(new Coordinate(3, 7)));
        Check(coordinate == new Coordinate(3, 7), "Custom value converters extend serialization");
    }

    private static async Task Slots(string root)
    {
        var store = new SaveStore(root);
        var id = Guid.NewGuid();
        Check((await store.LoadAsync<Fixture>(id)).Status == SaveStatus.NotFound, "Missing save is distinct");
        Check((await store.ListSlotsAsync<Fixture>()).Count == 0, "An empty store lists no slots");
        var value = new Fixture(); value.SetScore(5);
        var first = await store.SaveAsync(id, value, "Coast");
        Check(first.IsSuccess && first.Value!.Name == "Coast", "Slot metadata is written");
        value.SetScore(8);
        Check((await store.SaveAsync(id, value)).IsSuccess, "Slot can be overwritten");
        var loaded = await store.LoadAsync<Fixture>(id);
        Check(loaded.IsSuccess && loaded.Value!.Score == 8 && !loaded.RecoveredFromBackup, "Latest save loads");
        var slots = await store.ListSlotsAsync<Fixture>();
        Check(slots.Count == 1 && slots[0].CreatedUtc == first.Value!.CreatedUtc && slots[0].Name == "Coast", "Slot identity and creation metadata persist");
        var path = Path.Combine(root, id.ToString("N") + ".json");
        await File.WriteAllTextAsync(path, "{interrupted");
        loaded = await store.LoadAsync<Fixture>(id);
        Check(loaded.IsSuccess && loaded.RecoveredFromBackup && loaded.Value!.Score == 5, "Truncated primary recovers the prior save");
        Check((await store.ListSlotsAsync<Fixture>())[0].RecoveredFromBackup, "Listing reports backup recovery");
        value.SetScore(10);
        await store.SaveAsync(id, value);
        await Rewrite(path, node => node["Data"]!["score"] = -10, updateChecksum: true);
        Check((await store.LoadAsync<Fixture>(id)).Value!.Score == 5, "Game validation rejects corrupt data and recovers backup");
        value.SetScore(11);
        await store.SaveAsync(id, value);
        await File.WriteAllTextAsync(path, "broken");
        Check((await store.LoadAsync<Fixture>(id)).Value!.Score == 5, "Saving after semantic corruption preserves the valid backup");
        File.Delete(path + ".bak");
        Check((await store.LoadAsync<Fixture>(id)).Status == SaveStatus.Corrupt, "Unrecoverable corruption is distinct");
        await store.SaveAsync(id, value);
        await Rewrite(path, node => node["SchemaVersion"] = 99);
        var before = await File.ReadAllBytesAsync(path);
        Check((await store.LoadAsync<Fixture>(id)).Status == SaveStatus.Incompatible, "Future saves are incompatible");
        Check((await store.SaveAsync(id, value)).Status == SaveStatus.Incompatible, "Saving cannot overwrite future saves");
        Check(Enumerable.SequenceEqual(before, await File.ReadAllBytesAsync(path)), "Incompatible saves remain untouched");
        Check((await store.ListSlotsAsync<Fixture>())[0].Status == SaveStatus.Incompatible, "Listing retains incompatible slots");
        Check((await store.DeleteAsync(id)).IsSuccess && (await store.LoadAsync<Fixture>(id)).Status == SaveStatus.NotFound, "Delete removes primary and backup");
        Check((await store.DeleteAsync(id)).IsSuccess, "Delete is idempotent");
        var blocked = Path.Combine(root, "not-a-directory"); await File.WriteAllTextAsync(blocked, "x");
        Check((await new SaveStore(blocked).SaveAsync(Guid.NewGuid(), value)).Status == SaveStatus.IoError, "I/O failures are distinct");
        using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
        await ThrowsAsync<OperationCanceledException>(() => store.SaveAsync(Guid.NewGuid(), value, cancellationToken: cancelled.Token));
        await store.SaveAsync(id, value);
        await Rewrite(path, node => node["Data"]!["score"] = 999);
        Check((await store.LoadAsync<Fixture>(id)).Status == SaveStatus.Corrupt, "Checksum catches altered payloads");
        await store.SaveAsync(id, value);
        File.Delete(path + ".bak");
        File.Delete(path);
        Check((await store.LoadAsync<Fixture>(id)).Status == SaveStatus.NotFound, "No backup exists for a first write");
        await store.SaveAsync(id, value);
        var copiedId = Guid.NewGuid();
        File.Copy(path, Path.Combine(root, copiedId.ToString("N") + ".json"));
        var mismatched = (await store.ListSlotsAsync<Fixture>()).Single(slot => slot.Id == copiedId);
        Check(mismatched.Status == SaveStatus.Incompatible, "Mismatched metadata retains the actual file ID for deletion");
    }

    private static async Task Migrations(string root)
    {
        var id = Guid.NewGuid();
        var old = new SaveStore(root);
        await old.SaveAsync(id, new OldProfile { Name = "Ada" });
        var path = Path.Combine(root, id.ToString("N") + ".json");
        var before = await File.ReadAllBytesAsync(path);
        var migration = new SaveMigration("test.profile", 1, data =>
        {
            data["displayName"] = data["oldName"]!.DeepClone(); data.Remove("oldName"); return data;
        });
        var current = new SaveStore(new AtomicFileStorage(root), new SaveSerializer(), migration);
        Check((await current.LoadAsync<Profile>(id)).Value!.DisplayName == "Ada", "Migrations run before deserialization");
        Check(Enumerable.SequenceEqual(before, await File.ReadAllBytesAsync(path)), "Loading migrations never rewrites a save");
        Check((await old.LoadAsync<Profile>(id)).Status == SaveStatus.Incompatible, "Missing migration is incompatible");
        var failed = new SaveStore(new AtomicFileStorage(root), new SaveSerializer(), new SaveMigration("test.profile", 1, _ => throw new InvalidDataException("fixture failure")));
        Check((await failed.LoadAsync<Profile>(id)).Status == SaveStatus.Corrupt, "Failed migration reports failure");
        Check(Enumerable.SequenceEqual(before, await File.ReadAllBytesAsync(path)), "Failed migration leaves the file unchanged");
        await current.SaveAsync(id, new Profile { DisplayName = "Bea" });
        Check((await old.LoadAsync<OldProfile>(id)).Status == SaveStatus.Incompatible, "Older code does not fall back over a newer primary");
        Check((await current.LoadAsync<Fixture>(id)).Status == SaveStatus.Incompatible, "Wrong contract is rejected");
    }

    private static async Task InterruptedAndConcurrentWrites(string root)
    {
        var normal = new SaveStore(root);
        var id = Guid.NewGuid();
        var source = new Fixture(); source.SetScore(1);
        await normal.SaveAsync(id, source);
        source.SetScore(2);
        var failed = new SaveStore(new FailingStorage(root), new SaveSerializer());
        Check((await failed.SaveAsync(id, source)).Status == SaveStatus.IoError, "Interrupted commit reports failure");
        Check((await normal.LoadAsync<Fixture>(id)).Value!.Score == 1, "Interrupted commit preserves the current save");
        Check(!Directory.EnumerateFiles(root, "*.tmp*").Any(), "Failed writes clean temporary files");
        var pause = new PausingStorage(root);
        var blocked = new SaveStore(pause, new SaveSerializer());
        var first = Task.Run(() => blocked.SaveAsync(id, source));
        await pause.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        source.SetScore(17);
        var queued = normal.SaveAsync(id, source);
        source.SetScore(29);
        pause.Resume.Set();
        Check((await first).IsSuccess && (await queued).IsSuccess, "Concurrent store instances serialize writes");
        Check((await normal.LoadAsync<Fixture>(id)).Value!.Score == 17, "Queued saves use the snapshot from the call boundary");
        var tasks = Enumerable.Range(1, 12).Select(score =>
        {
            var data = new Fixture(); data.SetScore(score); return normal.SaveAsync(id, data);
        }).ToArray();
        Check((await Task.WhenAll(tasks)).All(result => result.IsSuccess), "Overlapping saves all commit safely");
        Check((await normal.LoadAsync<Fixture>(id)).IsSuccess, "Concurrent writes leave a valid document");
        using var cancellation = new CancellationTokenSource();
        var cancelPause = new PausingStorage(root);
        var writing = Task.Run(() => new SaveStore(cancelPause, new SaveSerializer()).SaveAsync(id, source));
        await cancelPause.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var cancelPending = normal.SaveAsync(id, source, cancellationToken: cancellation.Token);
        cancellation.Cancel();
        await ThrowsAsync<OperationCanceledException>(() => cancelPending);
        cancelPause.Resume.Set();
        Check((await writing).IsSuccess, "Cancelling a queued write does not disturb another writer");
    }

    private static async Task PreferenceChecks(string root)
    {
        var volume = PlayerPreferences.MasterVolume;
        var muted = PlayerPreferences.Muted;
        var first = new Preferences(root);
        Check(first.Get(volume) == 1 && (await first.LoadAsync()).Status == SaveStatus.NotFound, "Missing preferences use typed defaults");
        first.Set(volume, .4f);
        Check(!File.Exists(first.FilePath), "Set does not write until flush");
        Check((await first.FlushAsync()).IsSuccess, "Preferences flush");
        var second = new Preferences(root);
        await second.LoadAsync();
        Check(second.Get(volume) == .4f, "Preferences persist across instances");
        first.Set(volume, .6f); second.Set(muted, true);
        await Task.WhenAll(first.FlushAsync(), second.FlushAsync());
        var read = new Preferences(root); await read.LoadAsync();
        Check(read.Get(volume) == .6f && read.Get(muted), "Concurrent preferences merge unrelated keys");
        second.Remove(volume); await second.FlushAsync(); await read.LoadAsync();
        Check(read.Get(volume) == 1 && read.Get(muted), "Removing a key restores its default without erasing other keys");
        read.Set(volume, .8f); await read.LoadAsync();
        Check(read.Get(volume) == .8f, "Reload preserves pending local edits");
        Throws<InvalidOperationException>(() => read.Get(new PreferenceKey<string>(volume.Name, "invalid")));
        Throws<ArgumentOutOfRangeException>(() => read.Set(volume, 2f));
        Throws<ArgumentOutOfRangeException>(() => read.Set(volume, float.NaN));
        await read.FlushAsync();
        await File.WriteAllTextAsync(read.FilePath, "broken");
        Check((await new Preferences(root).LoadAsync()).RecoveredFromBackup, "Preferences recover a previous file");
        read.Set(muted, false); await read.FlushAsync();
        await Rewrite(read.FilePath, node => node["SchemaVersion"] = 2);
        var future = new Preferences(root);
        Check((await future.LoadAsync()).Status == SaveStatus.Incompatible, "Future preferences are incompatible");
        future.Set(volume, .2f);
        Check((await future.FlushAsync()).Status == SaveStatus.Incompatible, "Future preferences cannot be overwritten");
        var inFlightRoot = Path.Combine(root, "in-flight");
        var pause = new PausingStorage(inFlightRoot);
        var pendingPreferences = new Preferences(pause);
        pendingPreferences.Set(volume, .3f);
        var flush = Task.Run(() => pendingPreferences.FlushAsync());
        await pause.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        pendingPreferences.Set(volume, .9f);
        pause.Resume.Set();
        await flush;
        Check(pendingPreferences.Get(volume) == .9f, "Preference changes during a flush stay in memory");
        await pendingPreferences.FlushAsync();
        var persisted = new Preferences(inFlightRoot); await persisted.LoadAsync();
        Check(persisted.Get(volume) == .9f, "Changes made during a flush persist on the next flush");
        await Rewrite(persisted.FilePath, node => node["Data"]![volume.Name]!["Type"] = "string", updateChecksum: true);
        await persisted.LoadAsync();
        Check(persisted.Get(volume) == volume.DefaultValue, "Wrong stored preference types use defaults");
    }

    private static async Task Sessions(string root)
    {
        var saves = new SaveStore(root);
        var manager = new SessionManager(saves);
        Check(manager.ActiveSession is null && manager.Slots.Count == 0, "No session is active by default");
        var session = manager.StartNew("Expedition"); session.AddPlayTime(42); session.CompleteSite("coast");
        var id = manager.ActiveSlotId!.Value;
        Check((await manager.SaveAsync()).IsSuccess && manager.Slots.Count == 1, "Session manager saves and refreshes slot metadata");
        manager.EndSession();
        Check((await manager.LoadAsync(id)).IsSuccess && manager.ActiveSession!.PlayTimeSeconds == 42
            && manager.ActiveSession.CompletedSites.Contains("coast"), "The whole session loads");
        var active = manager.ActiveSession;
        Check((await manager.LoadAsync(Guid.NewGuid())).Status == SaveStatus.NotFound && ReferenceEquals(active, manager.ActiveSession), "Failed loads preserve the active session");
        await manager.RefreshSlotsAsync();
        Check(manager.Slots.Single().Name == "Expedition", "Slot discovery preserves display metadata");
        var pause = new PausingStorage(root);
        var write = Task.Run(() => new SaveStore(pause, new SaveSerializer()).SaveAsync(id, active));
        await pause.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var loading = manager.LoadAsync(id);
        var replacement = manager.StartNew("New session");
        pause.Resume.Set();
        await write;
        await ThrowsAsync<OperationCanceledException>(() => loading);
        Check(ReferenceEquals(manager.ActiveSession, replacement), "A stale load cannot replace a newly started session");
    }

    private static async Task Rewrite(string path, Action<JsonObject> edit, bool updateChecksum = false)
    {
        var node = JsonNode.Parse(await File.ReadAllTextAsync(path))!.AsObject(); edit(node);
        if (updateChecksum)
        {
            node["Checksum"] = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(node["Data"])));
        }

        await File.WriteAllBytesAsync(path, JsonSerializer.SerializeToUtf8Bytes(node));
    }

    private static void Check(bool condition, string message)
    {
        _checks++;
        if (!condition) { throw new InvalidOperationException(message); }
    }
    private static void Throws<T>(Action action) where T : Exception
    {
        try { action(); } catch (T) { _checks++; return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }
    private static async Task ThrowsAsync<T>(Func<Task> action) where T : Exception
    {
        try { await action(); } catch (T) { _checks++; return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
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
        public void Validate() { if (_score < 0) { throw new InvalidDataException("Score cannot be negative."); } }
    }
    [SaveContract("test.item")]
    private sealed class Item
    {
        [SaveMember("name")] public string Name { get; set; } = "";
        [SaveMember("amount")] public int Amount { get; set; }
    }
    [SaveContract("test.fixture")]
    private sealed class RenamedFixture { [SaveMember("score")] public int Points { get; private set; } }
    [SaveContract("test.unsupported")]
    private sealed class UnsupportedFixture { [SaveMember("callback")] public Action Callback { get; set; } = () => { }; }
    [SaveContract("test.duplicate")]
    private sealed class DuplicateNames
    {
        [SaveMember("value")] public int First { get; set; }
        [SaveMember("value")] public int Second { get; set; }
    }
    [SaveContract("test.readonly")]
    private sealed class ReadonlyFixture { [SaveMember("value")] public readonly int Value = 1; }
    [SaveContract("test.base")]
    private class BaseFixture { [SaveMember("value")] public int Value { get; set; } }
    [SaveContract("test.derived")]
    private sealed class DerivedFixture : BaseFixture;
    [SaveContract("test.cycle")]
    private sealed class Cycle { [SaveMember("next")] public Cycle? Next { get; set; } }
    [SaveContract("test.profile", Version = 1)]
    private sealed class OldProfile { [SaveMember("oldName")] public string Name { get; set; } = ""; }
    [SaveContract("test.profile", Version = 2)]
    private sealed class Profile { [SaveMember("displayName")] public string DisplayName { get; set; } = ""; }
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
            var parts = reader.GetString()!.Split(','); return new(int.Parse(parts[0]), int.Parse(parts[1]));
        }
        public override void Write(Utf8JsonWriter writer, Coordinate value, JsonSerializerOptions options)
            => writer.WriteStringValue($"{value.X},{value.Y}");
    }
    private sealed class FailingStorage(string directory) : AtomicFileStorage(directory)
    {
        protected override void Commit(string temporaryPath, string destinationPath) => throw new IOException("Simulated interruption before commit.");
    }
    private sealed class PausingStorage(string directory) : AtomicFileStorage(directory)
    {
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public ManualResetEventSlim Resume { get; } = new(false);
        protected override void Commit(string temporaryPath, string destinationPath)
        {
            Entered.TrySetResult();
            if (!Resume.Wait(TimeSpan.FromSeconds(5))) { throw new IOException("Timed out waiting for test writer."); }
            base.Commit(temporaryPath, destinationPath);
        }
    }
}
