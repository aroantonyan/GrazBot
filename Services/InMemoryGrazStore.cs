using System.Collections.Concurrent;
using TelegramBot.Abstractions;
using TelegramBot.Models;

namespace TelegramBot.Services;

public sealed class InMemoryGrazStore : IGrazStore
{
    private readonly ConcurrentDictionary<long, List<Graz>> _storage = new();

    public void Add(long chatId, Graz graz)
    {
        var list = _storage.GetOrAdd(chatId, _ => []);
        lock (list)
        {
            list.Add(graz);
        }
    }

    public IReadOnlyList<Graz> GetAll(long chatId)
    {
        if (!_storage.TryGetValue(chatId, out var list)) return [];
        lock (list)
        {
            return list.ToList(); 
        }

    }

    public IReadOnlyList<Graz> GetInProgress(long chatId)
    {
        if (!_storage.TryGetValue(chatId, out var list))
            return [];

        lock (list)
        {
            return list
                .Where(g => g is { IsFinished: false, IsDeleted: false })
                .ToList();
        }
    }

    public IReadOnlyList<Graz> GetFinished(long chatId)
    {
        if (!_storage.TryGetValue(chatId, out var list))
            return [];

        lock (list)
        {
            return list
                .Where(g => g is { IsFinished: true, IsDeleted: false, IsGiven: false })
                .ToList();
        }
    }
    
    public Graz? GetById(long chatId, Guid grazId)
    {
        return _storage.TryGetValue(chatId, out var list)
            ? list.FirstOrDefault(g => g.Id == grazId)
            : null;
    }
    public void MarkWinner(long chatId, Guid grazId, int winningTeam)
    {
        if (!_storage.TryGetValue(chatId, out var list)) return;

        var graz = list.FirstOrDefault(g => g.Id == grazId);
        if (graz is null) return;

        graz.WinnerTeam = winningTeam;
        graz.IsFinished = true;
        graz.FinishedAt = DateTime.UtcNow;
    }

    public void MarkAsDeleted(long chatId, Graz graz)
    {
        if (!_storage.TryGetValue(chatId, out var list))
            return;

        var existing = list.FirstOrDefault(g => g.Id == graz.Id);
        if (existing is null)
            return;

        existing.IsDeleted = true;
    }
}