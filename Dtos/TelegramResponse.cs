using System.Text.Json.Serialization;

namespace TelegramBot.Dtos;

public record TelegramResponse<T>
{
    [JsonPropertyName("ok")]
    public bool Ok { get; set; }

    [JsonPropertyName("result")]
    public T? Result { get; set; }

    // present when ok=false
    [JsonPropertyName("error_code")]
    public int? ErrorCode { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }
};