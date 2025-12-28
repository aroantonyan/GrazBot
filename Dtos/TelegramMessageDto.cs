using System.Text.Json.Serialization;

namespace TelegramBot.Dtos;

public sealed record TelegramMessageDto
{
    [JsonPropertyName("message_id")]
    public int MessageId { get; set; }
};