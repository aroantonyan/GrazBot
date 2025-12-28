using TelegramBot.Models;

namespace TelegramBot.Abstractions;

public interface IGrazStore
{
    void Add(long chatId, Graz graz);
    IReadOnlyList<Graz> GetAll(long chatId);
    IReadOnlyList<Graz> GetInProgress(long chatId);
    Graz? GetById(long chatId, Guid grazId);
    void MarkWinner(long chatId, Guid grazId, int winningTeam);
    void MarkAsDeleted(long chatId, Graz graz);
    public IReadOnlyList<Graz> GetFinished(long chatId);
}