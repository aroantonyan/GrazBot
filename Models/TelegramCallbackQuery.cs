using System.Text.Json.Serialization;

namespace TelegramBot.Models;

public sealed class TelegramCallbackQuery
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = null!;

    [JsonPropertyName("from")]
    public TelegramUser From { get; init; } = null!;

    [JsonPropertyName("message")]
    public TelegramMessage? Message { get; init; }

    [JsonPropertyName("data")]
    public string? Data { get; init; }
}