using System.Text.Json;
using TelegramBot.Abstractions;
using TelegramBot.Dtos.ViewModels;
using TelegramBot.Infrastructure.Abstractions;
using TelegramBot.Models;
using TelegramBot.Session;
using TelegramBot.State;
using TelegramBot.UI;

namespace TelegramBot.Services;

public class BotService(
    ILogger<BotService> logger,
    ITelegramUserRegistry userRegistry,
    IGrazSessionStore grazSessionStore,
    IGrazStore grazStore,
    ITelegramApiClient telegramApi,
    IChatSettingsStore chatSettingsStore,
    GrazUi grazUi,
    ITelegramUserTracker userTracker)
    : IBotService
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };


    public async Task HandleUpdateAsync(TelegramUpdate update, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(update, Options);
        logger.LogInformation("===== TELEGRAM UPDATE (MODEL) ====={NewLine}{Json}", Environment.NewLine, json);

        userTracker.Track(update);


        if (update.CallbackQuery is not null)
        {
            await HandleCallbackQueryAsync(update.CallbackQuery, ct);
            return;
        }

        if (update.Message is not null)
        {
            await HandleMessageAsync(update.Message, ct);
        }
    }

    private async Task HandleMessageAsync(TelegramMessage message, CancellationToken ct)
    {
        if (message.Chat != null)
        {
            var chatId = message.Chat.Id;
            var fromUser = message.From;
            if (fromUser!.IsBot)
            {
                return;
            }

            if (IsForeignSession(chatId, fromUser.Id, out var currentSession))
            {
                return;
            }

            var text = message.Text?.Trim();
            if (string.IsNullOrWhiteSpace(text)) return;

            if (text.StartsWith("/graz", StringComparison.OrdinalIgnoreCase) && grazSessionStore.Get(chatId) == null)
            {
                var session = grazSessionStore.StartNew(chatId, fromUser.Id, null);
                await telegramApi.DeleteMessageAsync(chatId, message.MessageId, ct);
                var messageId = await SendMainMenuAsync(chatId, ct);
                session.WizardMessageId = messageId;
                return;
            }

            await TryHandleGrazCreationTextAsync(chatId, message.MessageId, currentSession, text, ct);
        }
    }

    private async Task<int> SendMainMenuAsync(long chatId, CancellationToken ct)
    {
        var uiScreen = grazUi.MainMenuScreen(chatId);
        return await telegramApi.SendMessageAsync(chatId, uiScreen, ct);
    }


    private async Task HandleCallbackQueryAsync(TelegramCallbackQuery callbackQuery, CancellationToken ct)
    {
        var message = callbackQuery.Message;
        var data = callbackQuery.Data;
        var fromUser = callbackQuery.From;
        var telegramChat = message!.Chat;
        if (telegramChat != null)
        {
            var chatId = telegramChat.Id;
            var messageId = message.MessageId;

            if (string.IsNullOrWhiteSpace(data) || IsForeignSession(chatId, fromUser.Id, out _))
            {
                await telegramApi.AnswerCallbackQueryAsync(callbackQuery.Id, ct);
                return;
            }


            grazSessionStore.SetNewInteraction(telegramChat.Id);

            if (data == "graz_play_new_graz")
            {
                await StartTeam1SelectionAsync(chatId, messageId, ct);
            }
            else if (data == "graz_in_progress_games")
            {
                await ShowInProgressGamesAsync(chatId, messageId, ct);
            }
            else if (data == "graz_back_to_menu")
            {
                await telegramApi.EditMessageTextAsync(chatId, messageId, grazUi.MainMenuScreen(chatId), ct);
            }
            else if (data.StartsWith("graz_team1_add:", StringComparison.OrdinalIgnoreCase))
            {
                await HandleTeam1AddAsync(chatId, messageId, data, ct);
            }
            else if (data == "graz_team1_done")
            {
                await HandleTeam1DoneAsync(chatId, messageId, ct);
            }
            else if (data.StartsWith("graz_team2_add:", StringComparison.OrdinalIgnoreCase))
            {
                await HandleTeam2AddAsync(chatId, messageId, data, ct);
            }
            else if (data == "graz_team2_done")
            {
                await HandleTeam2DoneAsync(chatId, messageId, ct);
            }
            else if (data == "graz_confirm")
            {
                await HandleGrazConfirmAsync(chatId, messageId, ct);
            }
            else if (data == "graz_cancel")
            {
                await HandleGrazCancelAsync(chatId, messageId, ct);
            }
            else if (data == "graz_back")
            {
                await HandleGrazBackAsync(chatId, messageId, callbackQuery.From.Id, ct);
            }
            else if (data.StartsWith("graz_inprogress_open:", StringComparison.OrdinalIgnoreCase))
            {
                await HandleGrazInprogressOpenAsync(chatId, messageId, data, ct);
            }
            else if (data == "graz_back_to_inprogress")
            {
                await ShowInProgressGamesAsync(chatId, messageId, ct);
            }
            else if (data.StartsWith("graz_winner_team1:", StringComparison.OrdinalIgnoreCase))
            {
                if (TryParseGrazId(data, "graz_winner_team1:", out var grazId))
                {
                    await HandleGrazWinnerAsync(chatId, messageId, grazId, winningTeam: 1, actorUserId: fromUser.Id,
                        callbackQueryId: callbackQuery.Id, ct);
                }
            }
            else if (data.StartsWith("graz_winner_team2:", StringComparison.OrdinalIgnoreCase))
            {
                if (TryParseGrazId(data, "graz_winner_team2:", out var grazId))
                {
                    await HandleGrazWinnerAsync(chatId, messageId, grazId, winningTeam: 2, actorUserId: fromUser.Id,
                        callbackQueryId: callbackQuery.Id, ct);
                }
            }
            else if (data == "graz_history")
            {
                await ShowGrazHistoryAsync(chatId, messageId, ct);
            }
            else if (data.StartsWith("graz_delete:", StringComparison.OrdinalIgnoreCase))
            {
                await HandleGrazDeleteAsync(chatId, messageId, data, fromUser, callbackQuery, ct);
            }
            else if (data == "graz_close_bot")
            {
                await CloseSessionAsync(chatId, messageId, ct);
            }
            else if (data == "graz_language")
            {
                await StartLanguageSelectionAsync(chatId, messageId, ct);
            }
            else if (data.StartsWith($"graz_set_lang:", StringComparison.OrdinalIgnoreCase))
            {
                await ChangeLanguageAsync(chatId, data, messageId, ct);
            }
            else if (data.StartsWith("graz_finished_open:", StringComparison.OrdinalIgnoreCase))
            {
                await HandleGrazFinishedOpenAsync(chatId, messageId, data, ct);
            }
            else if (data == "graz_back_to_finished")
            {
                await ShowGrazHistoryAsync(chatId, messageId, ct);
            }
            else if (data.StartsWith("graz_finished_notgiven", StringComparison.OrdinalIgnoreCase))
            {
                await telegramApi.AnswerCallbackQueryAsync(callbackQuery.Id, grazUi.NotGivenNotAllowed(chatId), showAlert: true, ct);
            }
            else if (data.StartsWith("graz_finished_given:", StringComparison.OrdinalIgnoreCase))
            {
                await HandleGrazGivenAsync(chatId, messageId, data, callbackQuery.Id,fromUser.Id, ct);
            }
        }

        await telegramApi.AnswerCallbackQueryAsync(callbackQuery.Id, ct);
    }

    private async Task HandleGrazGivenAsync(long chatId, int messageId, string data, string callbackQueryId, long fromUserId, CancellationToken ct)
    {
        const string prefix = "graz_finished_given:";
        if (!data.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return;

        var idPart = data[prefix.Length..];
        if (!Guid.TryParse(idPart, out var grazId))
            return;

        var graz = grazStore.GetById(chatId, grazId); if (graz is null) return;

        var winnerUserIds = graz.WinnerTeam == 1 ? graz.Team1UserIds : graz.Team2UserIds;
            
        if (!winnerUserIds.Contains(fromUserId))
        {
            await telegramApi.AnswerCallbackQueryAsync(callbackQueryId, grazUi.GivenConfirmOnlyWinnerWarning(chatId), true, ct);

            return;
        }
        graz.IsGiven = true;
        
        var winnerNames = graz.WinnerTeam == 1
            ? string.Join(", ", graz.Team1Members)
            : string.Join(", ", graz.Team2Members);

        var loserNames = graz.WinnerTeam == 1
            ? string.Join(", ", graz.Team2Members)
            : string.Join(", ", graz.Team1Members);

        var uiScreen = grazUi.GrazGivenConfirmationScreen(chatId, winnerNames, loserNames);
        
        await telegramApi.EditMessageTextAsync(chatId, messageId, uiScreen, ct);
    }
    
    
    private async Task HandleGrazInprogressOpenAsync(long chatId, int messageId, string data, CancellationToken ct)
    {
        const string prefix = "graz_inprogress_open:";
        if (!data.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return;

        var grazIdStr = data[prefix.Length..];
        if (!Guid.TryParse(grazIdStr, out var grazId))
            return;

        var graz = grazStore
            .GetInProgress(chatId)
            .FirstOrDefault(g => g.Id == grazId);

        if (graz is null)
        {
            await telegramApi.EditMessageTextAsync(chatId, messageId, grazUi.GrazNotFound(chatId), ct);
            grazSessionStore.Reset(chatId);
            return;
        }

        var screen = grazUi.GrazOpenInprogressScreen(chatId, graz);

        await telegramApi.EditMessageTextAsync(chatId, messageId, screen, ct);
    }

    private async Task HandleGrazFinishedOpenAsync(long chatId, int messageId, string data, CancellationToken ct)
    {
        const string prefix = "graz_finished_open:";
        if (!data.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return;

        var grazIdStr = data[prefix.Length..];
        if (!Guid.TryParse(grazIdStr, out var grazId))
            return;

        var graz = grazStore.GetFinished(chatId).FirstOrDefault(g => g.Id == grazId);
        if (graz is null)
        {
            await telegramApi.EditMessageTextAsync(chatId, messageId, grazUi.GrazNotFound(chatId), ct);
            grazSessionStore.Reset(chatId);
            return;
        }

        var uiScreen = grazUi.GrazOpenFinishedScreen(chatId, graz);

        await telegramApi.EditMessageTextAsync(chatId, messageId, uiScreen, ct);
    }


    private async Task ChangeLanguageAsync(long chatId, string data, int messageId, CancellationToken ct)
    {
        const string prefix = "graz_set_lang:";
        var language = data[prefix.Length..];
        chatSettingsStore.SetLanguage(chatId, language);
        var uiScreen = grazUi.LanguageChangedScreen(chatId);
        await telegramApi.EditMessageTextAsync(chatId, messageId, uiScreen, ct);
    }

    private async Task StartLanguageSelectionAsync(long chatId, int messageId, CancellationToken ct)
    {
        var uiScreen = grazUi.LanguageMenu(chatId);
        await telegramApi.EditMessageTextAsync(chatId, messageId, uiScreen, ct);
    }

    private async Task StartTeam1SelectionAsync(long chatId, int messageId, CancellationToken ct)
    {
        var session = grazSessionStore.Get(chatId);
        if (session == null) Console.WriteLine(@"session is null (StartTeam1SelectionAsync)");

        session!.State = ConversationState.CreatingGraz_Team1Members;
        session.Team1UserIds.Clear();
        session.Team2UserIds.Clear();
        session.WizardMessageId = messageId;

        await ShowTeam1SelectionMessageAsync(chatId, messageId, session, ct);
    }

    private async Task ShowTeam1SelectionMessageAsync(
        long chatId,
        int messageId,
        GrazCreationSession session,
        CancellationToken ct,
        string? warning = null)
    {
        var allUsers = userRegistry.GetAll(chatId);

        var selectedUsers = allUsers
            .Where(u => session.Team1UserIds.Contains(u.Id))
            .ToList();

        var availableUsers = allUsers
            .Where(u => !session.Team1UserIds.Contains(u.Id))
            .ToList();

        var selectedNames = selectedUsers.Count == 0
            ? null
            : string.Join(", ", selectedUsers.Select(u => u.FirstName ?? u.Username ?? u.Id.ToString()));

        var availableVm = availableUsers
            .Select(u => (u.Id, u.FirstName ?? u.Username ?? u.Id.ToString()))
            .ToList();

        var model = new Team1SelectionVm(
            AvailableUsers: availableVm,
            SelectedNames: selectedNames,
            Warning: warning
        );

        var uiScreen = grazUi.Team1SelectionScreen(chatId, model);

        await telegramApi.EditMessageTextAsync(chatId, messageId, uiScreen, ct);
    }


    private async Task HandleTeam1AddAsync(long chatId, int messageId, string data, CancellationToken ct)
    {
        var allUsers = userRegistry.GetAll(chatId);

        const string prefix = "graz_team1_add:";

        if (!data.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return;

        var idPart = data[prefix.Length..];
        if (!long.TryParse(idPart, out var selectedUserId)) return;

        var session = grazSessionStore.Get(chatId);

        var availableUsers = allUsers
            .Where(u => !session!.Team1UserIds.Contains(u.Id))
            .ToList();

        if (availableUsers.Count == 1)
        {
            await ShowTeam1SelectionMessageAsync(chatId, messageId, session!, ct,
                warning: grazUi.Team1OverlapWarning(chatId));
            return;
        }


        if (session!.State != ConversationState.CreatingGraz_Team1Members)
        {
            return;
        }

        session.Team1UserIds.Add(selectedUserId);

        await ShowTeam1SelectionMessageAsync(chatId, messageId, session, ct);
    }

    private async Task StartTeam2SelectionAsync(long chatId, int messageId, CancellationToken ct)
    {
        var session = grazSessionStore.Get(chatId);

        session!.State = ConversationState.CreatingGraz_Team2Members;
        session.Team2UserIds.Clear();

        await ShowTeam2SelectionMessageAsync(chatId, messageId, session, ct);
    }

    private async Task HandleTeam2AddAsync(long chatId, int messageId, string data, CancellationToken ct)
    {
        const string prefix = "graz_team2_add:";
        if (!data.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return;

        var idPart = data[prefix.Length..];
        if (!long.TryParse(idPart, out var selectedUserId))
            return;

        var session = grazSessionStore.Get(chatId);

        if (session!.State != ConversationState.CreatingGraz_Team2Members)
            return;

        if (session.Team1UserIds.Contains(selectedUserId))
            return;

        session.Team2UserIds.Add(selectedUserId);

        await ShowTeam2SelectionMessageAsync(chatId, messageId, session, ct);
    }

    private async Task ShowTeam2SelectionMessageAsync(
        long chatId,
        int messageId,
        GrazCreationSession session,
        CancellationToken ct,
        string? warning = null)
    {
        var allUsers = userRegistry.GetAll(chatId);

        var team1Users = allUsers.Where(u => session.Team1UserIds.Contains(u.Id)).ToList();
        var team2Users = allUsers.Where(u => session.Team2UserIds.Contains(u.Id)).ToList();

        var availableUsers = allUsers
            .Where(u => !session.Team1UserIds.Contains(u.Id) && !session.Team2UserIds.Contains(u.Id))
            .ToList();

        var team1Names = team1Users.Count == 0 ? null : string.Join(", ", team1Users.Select(DisplayName));
        var team2Names = team2Users.Count == 0 ? null : string.Join(", ", team2Users.Select(DisplayName));

        var availableVm = availableUsers
            .Select(u => (u.Id, DisplayName(u)))
            .ToList();

        var vm = new Team2SelectionVm(
            Team1Names: team1Names,
            Team2Names: team2Names,
            AvailableUsers: availableVm,
            Warning: warning
        );

        var screen = grazUi.Team2SelectionScreen(chatId, vm);

        await telegramApi.EditMessageTextAsync(chatId, messageId, screen, ct);
        return;

        static string DisplayName(TelegramUser u)
            => u.FirstName ?? u.Username ?? u.Id.ToString();
    }

    private async Task StartGrazDescriptionStepAsync(
        long chatId,
        int messageId,
        CancellationToken ct)
    {
        var session = grazSessionStore.Get(chatId);
        if (session is null)
            return;

        session.State = ConversationState.CreatingGraz_Description;
        session.Description = null;

        await ShowGrazSummaryStepAsync(
            chatId: chatId,
            fallbackMessageId: messageId,
            stepState: ConversationState.CreatingGraz_Description,
            ct: ct);
    }
    
    private async Task TryHandleGrazCreationTextAsync(
        long chatId,
        int messageId,
        GrazCreationSession? session,
        string text,
        CancellationToken ct)
    {
        if (session is null)
            return;

        switch (session.State)
        {
            case ConversationState.CreatingGraz_Description:
            {
                session.Description = text;

                await telegramApi.DeleteMessageAsync(chatId, messageId, ct);
                grazSessionStore.SetNewInteraction(chatId);

                await ShowGrazSummaryStepAsync(
                    chatId: chatId,
                    fallbackMessageId: session.WizardMessageId ?? messageId,
                    stepState: ConversationState.CreatingGraz_Deadline,
                    ct: ct);

                return;
            }

            case ConversationState.CreatingGraz_Deadline:
            {
                session.Deadline = text;

                await telegramApi.DeleteMessageAsync(chatId, messageId, ct);
                grazSessionStore.SetNewInteraction(chatId);

                await ShowGrazSummaryStepAsync(
                    chatId: chatId,
                    fallbackMessageId: session.WizardMessageId ?? messageId,
                    stepState: ConversationState.CreatingGraz_Review,
                    ct: ct);

                return;
            }
        }
    }


    private async Task HandleGrazConfirmAsync(long chatId, int messageId, CancellationToken ct)
    {
        var session = grazSessionStore.Get(chatId);
        if (session is null)
            return;

        var allUsers = userRegistry.GetAll(chatId)
            .ToDictionary(u => u.Id, u => u);

        var team1Names = session.Team1UserIds.Select(ResolveName).ToList();
        var team2Names = session.Team2UserIds.Select(ResolveName).ToList();

        var graz = new Graz
        {
            Team1Members = team1Names,
            Team2Members = team2Names,

            Team1UserIds = session.Team1UserIds.ToList(),
            Team2UserIds = session.Team2UserIds.ToList(),

            Description = session.Description ?? string.Empty,
            Deadline = session.Deadline,
            IsFinished = false,
            CreatedAt = DateTime.UtcNow,
        };

        grazStore.Add(chatId, graz);
        grazSessionStore.Reset(chatId);
        var vm = BuildGrazSummaryVm(chatId, session);
        var savedScreen = grazUi.GrazSavedScreen(chatId, vm);

        await telegramApi.EditMessageTextAsync(chatId, messageId, savedScreen, ct);
        return;

        string ResolveName(long userId)
        {
            if (allUsers.TryGetValue(userId, out var u))
                return u.FirstName ?? u.Username ?? userId.ToString();

            return userId.ToString();
        }
    }

    private async Task HandleGrazCancelAsync(long chatId, int messageId, CancellationToken ct)
    {
        await telegramApi.EditMessageTextAsync(chatId, messageId, grazUi.GrazCancelledScreen(chatId), ct);

        grazSessionStore.Reset(chatId);

        try
        {
            await Task.Delay(TimeSpan.FromSeconds(3), ct);
            await telegramApi.DeleteMessageAsync(chatId, messageId, ct);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task ShowInProgressGamesAsync(long chatId, int messageId, CancellationToken ct)
    {
        var games = grazStore.GetInProgress(chatId).ToList();

        var screen = grazUi.InProgressScreen(chatId, games);

        await telegramApi.EditMessageTextAsync(chatId, messageId, screen, ct);
    }

    private bool IsForeignSession(long chatId, long currentUserId, out GrazCreationSession? session)
    {
        session = grazSessionStore.Get(chatId);

        if (session is null) return false;
        return session.CreatorUserId != currentUserId;
    }

    private async Task HandleGrazBackAsync(long chatId, int messageId, long userId, CancellationToken ct)
    {
        var session = grazSessionStore.Get(chatId);
        if (session is null)
            return;

        if (session.CreatorUserId != userId)
            return;

        switch (session.State)
        {
            case ConversationState.CreatingGraz_Team1Members:
                session.State = ConversationState.Idle;
                await SendMainMenuAsync(chatId, ct);
                break;

            case ConversationState.CreatingGraz_Team2Members:
                session.Team1UserIds.Clear();
                session.Team2UserIds.Clear();
                session.State = ConversationState.CreatingGraz_Team1Members;
                await ShowTeam1SelectionMessageAsync(chatId, messageId, session, ct);
                break;

            case ConversationState.CreatingGraz_Description:
                session.Team2UserIds.Clear();
                session.State = ConversationState.CreatingGraz_Team2Members;
                await ShowTeam2SelectionMessageAsync(chatId, messageId, session, ct);
                break;

            case ConversationState.CreatingGraz_Deadline:
            {
                await ShowGrazSummaryStepAsync(
                    chatId: chatId,
                    fallbackMessageId: messageId,
                    stepState: ConversationState.CreatingGraz_Description,
                    ct: ct);
                break;
            }

            case ConversationState.CreatingGraz_Review:
            {
                await ShowGrazSummaryStepAsync(
                    chatId: chatId,
                    fallbackMessageId: messageId,
                    stepState: ConversationState.CreatingGraz_Deadline,
                    ct: ct);
                break;
            }
        }
    }

    private static bool TryParseGrazId(string data, string prefix, out Guid grazId)
    {
        var idPart = data[prefix.Length..];
        return Guid.TryParse(idPart, out grazId);
    }

    private async Task HandleGrazWinnerAsync(
        long chatId,
        int messageId,
        Guid grazId,
        int winningTeam,
        long actorUserId,
        string callbackQueryId,
        CancellationToken ct)
    {
        var graz = grazStore.GetById(chatId, grazId);
        if (graz is null)
        {
            await telegramApi.EditMessageTextAsync(chatId, messageId, grazUi.GrazNotFound(chatId), ct);
            return;
        }

        var isParticipant =
            graz.Team1UserIds.Contains(actorUserId) ||
            graz.Team2UserIds.Contains(actorUserId);

        if (!isParticipant)
        {
            await telegramApi.AnswerCallbackQueryAsync(
                callbackQueryId,
                grazUi.DeleteNotAllowedWarning(chatId),
                showAlert: true,
                ct);
            return;
        }

        grazStore.MarkWinner(chatId, grazId, winningTeam);

        var screen = grazUi.GrazFinishedScreen(chatId, graz, winningTeam);

        await telegramApi.EditMessageTextAsync(chatId, messageId, screen, ct);
    }

    private async Task ShowGrazHistoryAsync(long chatId, int messageId, CancellationToken ct)
    {
        var history = grazStore.GetFinished(chatId).ToList();

        var screen = grazUi.GrazHistoryScreen(chatId, history);

        await telegramApi.EditMessageTextAsync(chatId, messageId, screen, ct);
    }

    private async Task HandleGrazDeleteAsync(
        long chatId,
        int messageId,
        string data,
        TelegramUser? fromUser,
        TelegramCallbackQuery callbackQuery,
        CancellationToken ct)
    {
        const string prefix = "graz_delete:";

        var idPart = data[prefix.Length..];

        if (!Guid.TryParse(idPart, out var grazId))
        {
            logger.LogWarning("Invalid graz id for delete: {IdPart}", idPart);
            await ShowInProgressGamesAsync(chatId, messageId, ct);
            return;
        }

        var graz = grazStore.GetById(chatId, grazId);
        if (fromUser != null && graz != null && !IsGrazMember(fromUser, graz))
        {
            await telegramApi.AnswerCallbackQueryAsync(callbackQuery.Id, grazUi.DeleteNotAllowedWarning(chatId),
                showAlert: true, ct);
            return;
        }

        grazStore.MarkAsDeleted(chatId, graz!);
        await ShowGrazHistoryAsync(chatId, messageId, ct);
    }

    private async Task CloseSessionAsync(long chatId, int messageId, CancellationToken ct)
    {
        try
        {
            await telegramApi.EditMessageTextAsync(
                chatId,
                messageId,
                grazUi.BotClosedScreen(chatId),
                ct);
        }
        catch
        {
            // ignored
        }

        grazSessionStore.Reset(chatId);

        try
        {
            await Task.Delay(TimeSpan.FromSeconds(3), ct);
            await telegramApi.DeleteMessageAsync(chatId, messageId, ct);
        }
        catch
        {
            // ignored
        }
    }

    private async Task HandleTeam1DoneAsync(long chatId, int messageId, CancellationToken ct)
    {
        var session = grazSessionStore.Get(chatId);
        if (session is null)
            return;

        if (session.State != ConversationState.CreatingGraz_Team1Members)
        {
            await ShowTeam1SelectionMessageAsync(chatId, messageId, session, ct);
            return;
        }

        if (session.Team1UserIds.Count < 1)
        {
            await ShowTeam1SelectionMessageAsync(
                chatId,
                messageId,
                session,
                ct,
                warning: grazUi.NoMemberForTeamWarning(chatId));
            return;
        }

        await StartTeam2SelectionAsync(chatId, messageId, ct);
    }

    private async Task HandleTeam2DoneAsync(long chatId, int messageId, CancellationToken ct)
    {
        var session = grazSessionStore.Get(chatId);
        if (session is null)
            return;

        if (session.State != ConversationState.CreatingGraz_Team2Members)
        {
            await ShowTeam2SelectionMessageAsync(chatId, messageId, session, ct);
            return;
        }

        if (session.Team2UserIds.Count < 1)
        {
            await ShowTeam2SelectionMessageAsync(
                chatId,
                messageId,
                session,
                ct,
                warning: grazUi.NoMemberForTeamWarning(chatId));
            return;
        }

        await StartGrazDescriptionStepAsync(chatId, messageId, ct);
    }

    private static bool IsGrazMember(TelegramUser user, Graz graz)
    {
        if (graz.Team1UserIds.Count > 0 || graz.Team2UserIds.Count > 0)
            return graz.Team1UserIds.Contains(user.Id) || graz.Team2UserIds.Contains(user.Id);
        return false;
    }

    private GrazSummaryVm BuildGrazSummaryVm(long chatId, GrazCreationSession session)
    {
        var allUsers = userRegistry.GetAll(chatId);
        var userById = allUsers.ToDictionary(u => u.Id, u => u);

        return new GrazSummaryVm(
            Team1Names: FormatNames(session.Team1UserIds),
            Team2Names: FormatNames(session.Team2UserIds),
            Description: session.Description,
            Deadline: session.Deadline
        );

        string FormatNames(IEnumerable<long> ids)
            => string.Join(", ", ids.Select(id =>
            {
                if (userById.TryGetValue(id, out var u))
                    return u.FirstName ?? u.Username ?? u.Id.ToString();

                return id.ToString();
            }));
    }

    private async Task ShowGrazSummaryStepAsync(
        long chatId,
        int fallbackMessageId,
        ConversationState stepState,
        CancellationToken ct)
    {
        var session = grazSessionStore.Get(chatId);
        if (session is null)
            return;

        var targetMessageId = session.WizardMessageId ?? fallbackMessageId;

        switch (stepState)
        {
            case ConversationState.CreatingGraz_Description:
                session.State = ConversationState.CreatingGraz_Description;
                break;

            case ConversationState.CreatingGraz_Deadline:
                session.State = ConversationState.CreatingGraz_Deadline;
                break;

            case ConversationState.CreatingGraz_Review:
                session.State = ConversationState.CreatingGraz_Review;
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(stepState), stepState, @"Not a summary step");
        }

        var vm = BuildGrazSummaryVm(chatId, session);

        var screen = grazUi.GrazSummaryStepScreen(chatId, stepState, vm);

        await telegramApi.EditMessageTextAsync(chatId, targetMessageId, screen, ct);
    }
}