using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Chisel.Generated;
using Graphite.Engine.Persistence;

namespace Graphite.Persistence.Tests;

internal static class EnumContractChecks
{
    internal static void Run()
    {
        var serializer = new SaveSerializer();
        var bytes = serializer.Serialize(Original.First);
        Check(Encoding.UTF8.GetString(bytes) == "\"weapon.first\"", "Mapped enums must save stable identities, not integer indexes or C# names");
        Check(serializer.Deserialize<Reordered>(bytes) == Reordered.RenamedFirst, "Reordered and renamed enum members retain their save identity");
        Check(serializer.Deserialize<Original>(serializer.Serialize(Original.DefaultName)) == Original.DefaultName, "EnumMember without Value uses the member name");
        Check(Encoding.UTF8.GetString(serializer.Serialize(Plain.First)) == "0", "Unannotated enums retain the existing serialization contract");

        foreach (var invalid in new[] { "0", "-1", "null", "true", "{}", "[]", "\"0\"", "\"First\"", "\"WEAPON.FIRST\"", "\"missing\"" })
        {
            Throws<JsonException>(() => serializer.Deserialize<Original>(Encoding.UTF8.GetBytes(invalid)));
        }
        Throws<JsonException>(() => serializer.Serialize(Original.Invalid));
        Throws<JsonException>(() => serializer.Serialize((Original)99));
        Throws<JsonException>(() => serializer.Serialize(Empty.Invalid));
        Throws<JsonException>(() => serializer.Deserialize<Empty>("\"anything\""u8));
        Throws<NotSupportedException>(() => serializer.Serialize(DuplicateNames.First));
        Throws<NotSupportedException>(() => serializer.Serialize(DuplicateValues.First));
        Check(serializer.Deserialize<Original?>("null"u8) is null, "Nullable mapped IDs support intentional absence");
        Check(serializer.Deserialize<Original?>(bytes) == Original.First, "Nullable mapped IDs use the same stable mapping");

        var data = new Container();
        data.Items.Add(Original.First);
        data.Groups["owned"] = [Original.DefaultName];
        var copy = serializer.Deserialize<Container>(serializer.Serialize(data));
        Check(copy.Items.SequenceEqual(data.Items) && copy.Groups["owned"].SequenceEqual(data.Groups["owned"])
            && copy.Hidden == Original.First && copy.Optional is null, "Mapped IDs work in private fields, properties, arrays and collections");
        var custom = new SaveSerializer(new OverrideConverter());
        Check(Encoding.UTF8.GetString(custom.Serialize(Original.First)) == "\"override\"", "Explicit converters retain precedence over built-in enum support");

        Console.WriteLine("Enum contract checks passed.");
    }

    internal static void GeneratedIds()
    {
        var serializer = new SaveSerializer();
        Check(Encoding.UTF8.GetString(serializer.Serialize(ChiselPilotId.STARTER_PILOT)) == "\"STARTER_PILOT\"",
            "Generated IDs serialize directly without a game-class converter");
        var ids = new[] { ChiselWeaponsId.AUTOCANNON, ChiselWeaponsId.AUTOCANNON };
        Check(serializer.Deserialize<ChiselWeaponsId[]>(serializer.Serialize(ids)).SequenceEqual(ids), "Generated ID arrays round trip automatically");
        Throws<JsonException>(() => serializer.Serialize(ChiselAssetId.Invalid));
        Throws<JsonException>(() => serializer.Serialize(ChiselLocalizationId.Invalid));
    }

    private static void Check(bool condition, string message)
    {
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
            return;
        }
        throw new InvalidOperationException($"Expected {typeof(T).Name}");
    }

    [DataContract]
    private enum Original
    {
        Invalid = -1,
        [EnumMember(Value = "weapon.first")] First = 0,
        [EnumMember] DefaultName = 1
    }

    [DataContract]
    private enum Reordered
    {
        [EnumMember(Value = "other")] Inserted = 0,
        [EnumMember(Value = "weapon.first")] RenamedFirst = 5
    }

    private enum Plain { First }

    [DataContract]
    private enum Empty { Invalid = -1 }

    [DataContract]
    private enum DuplicateNames
    {
        [EnumMember(Value = "same")] First,
        [EnumMember(Value = "same")] Second
    }

    [DataContract]
    private enum DuplicateValues
    {
        [EnumMember(Value = "first")] First = 0,
        [EnumMember(Value = "second")] Second = 0
    }

    [SaveContract("test.enum-container")]
    private sealed class Container
    {
        [SaveMember("hidden")] private Original _hidden = Original.First;
        [SaveMember("optional")] public Original? Optional { get; set; }
        [SaveMember("items")] public List<Original> Items = [];
        [SaveMember("groups")] public Dictionary<string, Original[]> Groups = [];
        public Original Hidden => _hidden;
    }

    private sealed class OverrideConverter : JsonConverter<Original>
    {
        public override Original Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => Original.First;
        public override void Write(Utf8JsonWriter writer, Original value, JsonSerializerOptions options) => writer.WriteStringValue("override");
    }
}
