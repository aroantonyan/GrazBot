using System.Collections.Concurrent;
using TelegramBot.Abstractions;
using TelegramBot.Models;

namespace TelegramBot.Services;

public sealed class InMemoryTelegramUserRegistry : ITelegramUserRegistry
{
    private readonly ConcurrentDictionary<long, ConcurrentDictionary<long, TelegramUser>> _chatUsers = new();

    public void RegisterIfNeeded(long chatId, TelegramUser user)
    {
        var usersInChat = _chatUsers.GetOrAdd(chatId, _ => new ConcurrentDictionary<long, TelegramUser>());

        usersInChat.AddOrUpdate(
            user.Id,
            user,
            (_, existing) => existing);
    }

    public IReadOnlyCollection<TelegramUser> GetAll(long chatId)
    {
        if (_chatUsers.TryGetValue(chatId, out var usersInChat))
        {
            return (IReadOnlyCollection<TelegramUser>)usersInChat.Values;
        }

        return [];
    }

    public bool TryGet(long chatId, long telegramUserId, out TelegramUser user)
    {
        user = null!;
        return _chatUsers.TryGetValue(chatId, out var usersInChat) && usersInChat.TryGetValue(telegramUserId, out user);
    }
}