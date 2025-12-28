using TelegramBot.Dtos;

namespace TelegramBot.Infrastructure.Abstractions;

public interface ITelegramApiClient
{
    Task<int> SendMessageAsync(long chatId, UiScreen uiScreen, CancellationToken ct);
    Task EditMessageTextAsync(long chatId, int messageId, UiScreen uiScreen, CancellationToken ct);
    Task<bool> DeleteMessageAsync(long chatId, int messageId, CancellationToken ct);
    Task AnswerCallbackQueryAsync(string callbackQueryId, CancellationToken ct);
    Task AnswerCallbackQueryAsync(string callbackQueryId, string text, bool showAlert, CancellationToken ct);

}