using TelegramBot.Abstractions;
using TelegramBot.Models;

namespace TelegramBot.Services;

public sealed class TelegramUserTracker(
    ITelegramUserRegistry userRegistry,
    ILogger<TelegramUserTracker> logger) : ITelegramUserTracker
{
    public void Track(TelegramUpdate update)
    {
        if (!TryExtract(update, out var chatId, out var user)) return;
        userRegistry.RegisterIfNeeded(chatId, user);

        logger.LogInformation("Seen user {Id} ({Name}) in chat {ChatId}",
            user.Id,
            user.FirstName ?? user.Username ?? "<no name>",
            chatId);
    }

    private static bool TryExtract(TelegramUpdate update, out long chatId, out TelegramUser user)
    {
        if (update.Message is { From: { IsBot: false } fromUser, Chat.Id: var msgChatId })
        {
            chatId = msgChatId;
            user = fromUser;
            return true;
        }

        if (update.CallbackQuery is { From: { IsBot: false } cbUser, Message.Chat.Id: var cbChatId })
        {
            chatId = cbChatId;
            user = cbUser;
            return true;
        }

        chatId = 0;
        user = null!;
        return false;
    }
}