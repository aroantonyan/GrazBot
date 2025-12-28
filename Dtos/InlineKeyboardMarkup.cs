using System.Text.Json.Serialization;

namespace TelegramBot.Dtos;

public sealed class InlineKeyboardMarkup
{
    [JsonPropertyName("inline_keyboard")]
    public List<List<InlineKeyboardButton>> InlineKeyboard { get; set; } = [];
}

