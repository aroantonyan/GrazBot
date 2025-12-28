using System.Collections.Concurrent;
using TelegramBot.Abstractions;
    using TelegramBot.Session;
using TelegramBot.State;

namespace TelegramBot.Services;

public sealed class InMemoryGrazSessionStore : IGrazSessionStore
{
    private readonly ConcurrentDictionary<long, GrazCreationSession> _sessions = new();

    public GrazCreationSession StartNew(long chatId, long creatorUserId, int? wizardMessageId)
    {
        var session = new GrazCreationSession
        {
            State = ConversationState.Idle,
            CreatorUserId = creatorUserId,
            WizardMessageId = wizardMessageId,
            LastInteractUtc = DateTimeOffset.UtcNow
        };

        _sessions[chatId] = session;
        return session;
    }

    public GrazCreationSession? Get(long chatId)
    {
        _sessions.TryGetValue(chatId, out var session);
        return session;
    }

    public void Reset(long chatId)  
    {
        _sessions.TryRemove(chatId, out _);
    }
    public void SetNewInteraction(long chatId)
    {
        if (_sessions.TryGetValue(chatId, out var session))
            session.LastInteractUtc = DateTimeOffset.UtcNow;
    }
    public KeyValuePair<long, GrazCreationSession>[] GetAllSessions()
    
        => _sessions.ToArray();
    
    public bool TryRemove(long chatId, out GrazCreationSession? removed)
    {
        return _sessions.TryRemove(chatId, out removed);
    }
}