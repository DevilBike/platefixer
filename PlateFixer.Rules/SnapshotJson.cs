using System.Text.Json;
using System.Text.Json.Serialization;
using PlateFixer.Rules.Model;

namespace PlateFixer.Rules;

/// <summary>Reads and writes snapshots as JSON, so a tool outside the game can review them.</summary>
public static class SnapshotJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
    };

    public static string Serialize(PlateSnapshot snapshot) => JsonSerializer.Serialize(snapshot, Options);

    public static PlateSnapshot Deserialize(string json) =>
        JsonSerializer.Deserialize<PlateSnapshot>(json, Options)
        ?? throw new JsonException("Snapshot JSON was empty.");
}
