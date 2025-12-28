using TelegramBot.Abstractions;
using TelegramBot.Infrastructure.Abstractions;
using TelegramBot.UI;

namespace TelegramBot.Services;

public sealed class GrazSessionTimeoutWorker(
    IGrazSessionStore grazSessionStore,
    ITelegramApiClient telegramApi,
    GrazUi grazUi,
    ILogger<GrazSessionTimeoutWorker> logger)
    : BackgroundService
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(200);
    private static readonly TimeSpan CheckEvery = TimeSpan.FromSeconds(3);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var timer = new PeriodicTimer(CheckEvery);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            var now = DateTimeOffset.UtcNow;
            var sessions = grazSessionStore.GetAllSessions();

            foreach (var (chatId, session) in sessions)
            {
                var idle = now - session.LastInteractUtc;
                if (idle <= Timeout)
                    continue;

                if (!grazSessionStore.TryRemove(chatId, out var removed) || removed is null)
                    continue;
                if (!ReferenceEquals(removed, session))
                    continue;

                if (removed.WizardMessageId is not { } wizardMessageId)
                    continue;

                try
                {
                    await CloseWizardMessageAsync(chatId, wizardMessageId, stoppingToken);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to close timed-out session for chatId={ChatId}", chatId);
                }
            }
        }
    }

    private async Task CloseWizardMessageAsync(long chatId, int messageId, CancellationToken ct)
    {
        try
        {
            await telegramApi.EditMessageTextAsync(chatId, messageId, grazUi.SessionExpiredWarning(chatId), ct);
        }
        catch
        {
            // ignore
        }

        try
        {
            await Task.Delay(TimeSpan.FromSeconds(3), ct);
            await telegramApi.DeleteMessageAsync(chatId, messageId, ct);
        }
        catch (OperationCanceledException)
        {
        }
        catch
        {
            // ignore
        }
    }
}