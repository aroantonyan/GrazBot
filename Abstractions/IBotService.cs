using TelegramBot.Models;

namespace TelegramBot.Abstractions;

public interface IBotService
{
    Task HandleUpdateAsync(TelegramUpdate update, CancellationToken cancellationToken = default);
}