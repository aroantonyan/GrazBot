using System.Collections.Concurrent;
using TelegramBot.Abstractions;
using TelegramBot.Models;

namespace TelegramBot.Services;

public sealed class InMemoryChatSettingsStore : IChatSettingsStore
{
    private readonly ConcurrentDictionary<long, ChatSettings> _chatSettings = new();
   
    public ChatSettings GetOrCreate(long chatId)
    {
        if (!_chatSettings.TryGetValue(chatId, out var settings))
            _chatSettings[chatId] = settings = new ChatSettings();
        return settings;
    }

    public void SetLanguage(long chatId, string languageCode)
    {
        var settings = GetOrCreate(chatId);
        settings.LanguageCode = languageCode;
    }
}