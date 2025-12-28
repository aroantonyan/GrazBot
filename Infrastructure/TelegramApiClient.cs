using System.Text.Json;
using Microsoft.Extensions.Options;
using TelegramBot.Configuration;
using TelegramBot.Dtos;
using TelegramBot.Infrastructure.Abstractions;
using TelegramBot.Models;

namespace TelegramBot.Infrastructure;

public sealed class TelegramApiClient(
    HttpClient httpClient,
    IOptions<TelegramOptions> options,
    ILogger<TelegramApiClient> logger)
    : ITelegramApiClient
{
    private readonly string _botToken = options.Value.BotToken;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);


    public async Task<int> SendMessageAsync(long chatId, UiScreen uiScreen, CancellationToken ct)
    {
        var url = BuildUrl("sendMessage");
        var payload = new
        {
            chat_id = chatId,
            text = uiScreen.Text,
            reply_markup = uiScreen.Keyboard
        };

        var msg = await PostAsync<TelegramMessageDto>(url, payload, ct);
        if (msg != null) return msg.MessageId;
        return -1;
    }

    public Task EditMessageTextAsync(long chatId, int messageId, UiScreen uiScreen, CancellationToken ct)
    {
        var url = BuildUrl("editMessageText");

        object payload = uiScreen.Keyboard is null
            ? new { chat_id = chatId, message_id = messageId, uiScreen.Text }
            : new { chat_id = chatId, message_id = messageId, uiScreen.Text, reply_markup = uiScreen.Keyboard };

        return PostAsync<TelegramMessage>(url, payload, ct);
    }

    public Task<bool> DeleteMessageAsync(long chatId, int messageId, CancellationToken ct)
    {
        var url = BuildUrl("deleteMessage");
        var payload = new { chat_id = chatId, message_id = messageId };
        return PostAsync<bool>(url, payload, ct);
    }

    public Task AnswerCallbackQueryAsync(string callbackQueryId, CancellationToken ct)
    {
        var url = BuildUrl("answerCallbackQuery");
        var payload = new { callback_query_id = callbackQueryId };
        return PostAsync<bool>(url, payload, ct);
    }

    public async Task AnswerCallbackQueryAsync(string callbackQueryId, string text, bool showAlert, CancellationToken ct)
    {
        var url = BuildUrl("answerCallbackQuery");
        
        var payload = new
        {
            callback_query_id = callbackQueryId,
            text,
            show_alert = showAlert
        };
        await PostAsync<bool>(url, payload, ct);
    }


    private string BuildUrl(string method) => $"https://api.telegram.org/bot{_botToken}/{method}";

    public async Task<T?> PostAsync<T>(string url, object payload, CancellationToken ct)
    {
        using var response = await httpClient.PostAsJsonAsync(url, payload, JsonOptions, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        TelegramResponse<T>? apiResponse;
        try
        {
            apiResponse = JsonSerializer.Deserialize<TelegramResponse<T>>(body, JsonOptions);
        }
        catch (JsonException)
        {
            logger.LogWarning("Telegram response deserialization failed. Body: {Body}", body);
            throw;
        }

        if (apiResponse is null)
            throw new InvalidOperationException($"Telegram returned empty response. Body: {body}");

        if (!apiResponse.Ok)
        {
            if (apiResponse.ErrorCode == 400 &&
                apiResponse.Description?.Contains("message is not modified", StringComparison.OrdinalIgnoreCase) == true)
            {
                logger.LogInformation("Telegram edit ignored (message not modified).");
                return default;
            }

            throw new InvalidOperationException($"Telegram call failed: {body}");
        }

        return apiResponse.Result;
    }
}