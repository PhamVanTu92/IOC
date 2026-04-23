using System.Text.Json.Serialization;

namespace IOC.Kafka.Events;

// ─────────────────────────────────────────────────────────────────────────────
// CloudEvent<T> — strongly-typed CloudEvents 1.0 wrapper
// Used for both producing and consuming messages
// ─────────────────────────────────────────────────────────────────────────────

public sealed record CloudEvent<T>
{
    [JsonPropertyName("specversion")]
    public string SpecVersion { get; init; } = "1.0";

    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("source")]
    public required string Source { get; init; }

    [JsonPropertyName("id")]
    public string Id { get; init; } = Guid.NewGuid().ToString();

    [JsonPropertyName("time")]
    public string Time { get; init; } = DateTime.UtcNow.ToString("O");

    [JsonPropertyName("datacontenttype")]
    public string DataContentType { get; init; } = "application/json";

    [JsonPropertyName("data")]
    public required T Data { get; init; }

    // ── Factories ─────────────────────────────────────────────────────────────

    public static CloudEvent<T> Create(string type, string source, T data) =>
        new() { Type = type, Source = source, Data = data };
}
