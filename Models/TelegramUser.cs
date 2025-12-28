using System.Text.Json.Serialization;

namespace TelegramBot.Models;

public sealed class TelegramUser
{
    [JsonPropertyName("id")]
    public long Id { get; init; }

    [JsonPropertyName("first_name")]
    public string? FirstName { get; init; }
    
    [JsonPropertyName("username")]
    public string? Username { get; init; }
    
    [JsonPropertyName("is_bot")]
    public bool IsBot { get; init; }
}