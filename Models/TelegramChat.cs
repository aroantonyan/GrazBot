using System.Text.Json.Serialization;

namespace TelegramBot.Models;

public sealed class TelegramChat
{
    [JsonPropertyName("id")]
    public long Id { get; init; }
}