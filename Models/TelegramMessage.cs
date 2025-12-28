using System.Text.Json.Serialization;

namespace TelegramBot.Models;

public sealed class TelegramMessage
{
    [JsonPropertyName("message_id")]
    public int MessageId { get; init; }

    [JsonPropertyName("chat")]
    public TelegramChat? Chat { get; init; }

    [JsonPropertyName("text")]
    public string? Text { get; init; }

    [JsonPropertyName("from")]
    public TelegramUser? From { get; init; }
}