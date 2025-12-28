using System.Text.Json.Serialization;

namespace TelegramBot.Dtos;

public sealed class InlineKeyboardButton
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = null!;

    [JsonPropertyName("callback_data")]
    public string CallbackData { get; set; } = null!;
}