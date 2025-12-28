using TelegramBot.Session;

namespace TelegramBot.Abstractions;

public interface IGrazSessionStore
{
    GrazCreationSession StartNew(long chatId, long creatorUserId, int? wizardMessageId);

    GrazCreationSession? Get(long chatId);

    void Reset(long chatId);

    public void SetNewInteraction(long chatId);
    public KeyValuePair<long, GrazCreationSession>[] GetAllSessions();

    public bool TryRemove(long chatId, out GrazCreationSession? removed);
}