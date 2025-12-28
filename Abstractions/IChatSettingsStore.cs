using TelegramBot.Models;

namespace TelegramBot.Abstractions;

public interface IChatSettingsStore
{
    ChatSettings GetOrCreate(long chatId);
    void SetLanguage(long chatId, string languageCode);
}