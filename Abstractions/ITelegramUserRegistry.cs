using TelegramBot.Models;

namespace TelegramBot.Abstractions;

public interface ITelegramUserRegistry
{
    
    void RegisterIfNeeded(long chatId, TelegramUser user);

    IReadOnlyCollection<TelegramUser> GetAll(long chatId);
}