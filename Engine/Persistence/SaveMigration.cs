using System.Text.Json.Nodes;

namespace Graphite.Engine.Persistence;

/// <summary>Transforms one version into the next. Migrations never write files.</summary>
public sealed record SaveMigration(string ContractId, int FromVersion, Func<JsonObject, JsonObject> Upgrade);
