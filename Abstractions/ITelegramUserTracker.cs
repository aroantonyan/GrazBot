using TelegramBot.Models;

namespace TelegramBot.Abstractions;

public interface ITelegramUserTracker
{
    void Track(TelegramUpdate update);
}