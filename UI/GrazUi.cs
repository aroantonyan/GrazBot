using System.Text;
using TelegramBot.Abstractions;
using TelegramBot.Dtos;
using TelegramBot.Dtos.ViewModels;
using TelegramBot.Models;
using TelegramBot.State;
using TelegramBot.UI.Localization;

namespace TelegramBot.UI;

public class GrazUi(ITextProviderFactory factory, IChatSettingsStore chatSettingsStore)
{
    private ITextProvider TextProvider(long chatId)
    {
        var lang = chatSettingsStore.GetOrCreate(chatId).LanguageCode;
        return factory.Create(lang);
    }

    public UiScreen MainMenuScreen(long chatId)
    {
        var textProvider = TextProvider(chatId);

        var keyboard = new InlineKeyboardMarkup
        {
            InlineKeyboard =
            [
                [
                    new InlineKeyboardButton
                        { Text = textProvider["PlayNewButton"], CallbackData = "graz_play_new_graz" },
                    new InlineKeyboardButton
                        { Text = textProvider["InProgressButton"], CallbackData = "graz_in_progress_games" }
                ],
                [
                    new InlineKeyboardButton
                        { Text = textProvider["StatisticsButton"], CallbackData = "graz_statistics" },
                    new InlineKeyboardButton { Text = textProvider["HistoryButton"], CallbackData = "graz_history" }
                ],
                [
                    new InlineKeyboardButton { Text = textProvider["LanguageButton"], CallbackData = "graz_language" },
                    new InlineKeyboardButton
                        { Text = textProvider["GivenGrazesButton"], CallbackData = "graz_given" }
                ],
                [
                    new InlineKeyboardButton { Text = textProvider["CloseBotButton"], CallbackData = "graz_close_bot" }
                ]
            ]
        };

        return new UiScreen(textProvider["ChooseOptionText"], keyboard);
    }

    public UiScreen LanguageChangedScreen(long chatId)
    {
        var textProvider = TextProvider(chatId);
        var keyboard = BackToMenu(chatId);
        var text = textProvider["LanguageChangedText"];
        return new UiScreen(text, keyboard);
    }


    private InlineKeyboardMarkup BackToMenu(long chatId)
    {
        var textProvider = TextProvider(chatId);
        return new InlineKeyboardMarkup
        {
            InlineKeyboard =
            [
                [new InlineKeyboardButton { Text = textProvider["BackButton"], CallbackData = "graz_back_to_menu" }]
            ]
        };
    }

    public UiScreen LanguageMenu(long chatId)
    {
        var settings = chatSettingsStore.GetOrCreate(chatId);
        var current = (settings.LanguageCode).Trim().ToLowerInvariant();

        var textProvider = TextProvider(chatId);

        var header = textProvider["SelectLanguageText"];

        var switchTo = current == "hy" ? "en" : "hy";

        var switchButtonText = switchTo == "hy" ? "Armenian | Հայերեն 🇦🇲" : "English 🏴󠁧󠁢󠁥󠁮󠁧󠁿";

        var keyboard = new InlineKeyboardMarkup
        {
            InlineKeyboard =
            [
                [
                    new InlineKeyboardButton
                    {
                        Text = switchButtonText,
                        CallbackData = $"graz_set_lang:{switchTo}"
                    }
                ],
                [
                    new InlineKeyboardButton
                    {
                        Text = textProvider["BackButton"],
                        CallbackData = "graz_back_to_menu"
                    }
                ]
            ]
        };
        return new UiScreen(header, keyboard);
    }


    private InlineKeyboardMarkup BackToInProgress(long chatId)
    {
        var textProvider = TextProvider(chatId);

        return new InlineKeyboardMarkup
        {
            InlineKeyboard =
            [
                [
                    new InlineKeyboardButton
                        { Text = textProvider["BackButton"], CallbackData = "graz_back_to_inprogress" }
                ]
            ]
        };
    }

    private InlineKeyboardMarkup BackCancel(long chatId)
    {
        var textProvider = TextProvider(chatId);

        return new InlineKeyboardMarkup
        {
            InlineKeyboard =
            [
                [
                    new InlineKeyboardButton { Text = textProvider["BackButton"], CallbackData = "graz_back" },
                    new InlineKeyboardButton { Text = textProvider["CancelButton"], CallbackData = "graz_cancel" }
                ]
            ]
        };
    }


    private InlineKeyboardMarkup Review(long chatId)
    {
        var textProvider = TextProvider(chatId);

        return new InlineKeyboardMarkup
        {
            InlineKeyboard =
            [
                [
                    new InlineKeyboardButton { Text = textProvider["BackButton"], CallbackData = "graz_back" },
                    new InlineKeyboardButton { Text = textProvider["ConfirmButton"], CallbackData = "graz_confirm" },
                    new InlineKeyboardButton { Text = textProvider["CancelButton"], CallbackData = "graz_cancel" }
                ]
            ]
        };
    }

    public UiScreen InProgressScreen(long chatId, IReadOnlyList<Graz> games)
    {
        var t = TextProvider(chatId);

        if (games.Count == 0)
            return new UiScreen(t["InProgressEmptyText"], BackToMenu(chatId));

        var rows = new List<List<InlineKeyboardButton>>();

        for (var i = 0; i < games.Count; i++)
        {
            var graz = games[i];
            var team1 = string.Join(", ", graz.Team1Members);
            var team2 = string.Join(", ", graz.Team2Members);

            var line = $"{i + 1}) {team1} vs {team2}";

            rows.Add(
            [
                new InlineKeyboardButton
                {
                    Text = line,
                    CallbackData = $"graz_inprogress_open:{graz.Id}"
                }
            ]);
        }

        rows.Add(
        [
            new InlineKeyboardButton { Text = t["BackButton"], CallbackData = "graz_back_to_menu" }
        ]);

        var keyboard = new InlineKeyboardMarkup { InlineKeyboard = rows };

        return new UiScreen(t["InProgressHeaderText"], keyboard);
    }

    private List<InlineKeyboardButton> Team1Selection(long chatId)
    {
        var textProvider = TextProvider(chatId);

        return
        [
            new InlineKeyboardButton { Text = textProvider["DoneButton"], CallbackData = "graz_team1_done" },
            new InlineKeyboardButton { Text = textProvider["BackButton"], CallbackData = "graz_back_to_menu" },
            new InlineKeyboardButton { Text = textProvider["CancelButton"], CallbackData = "graz_cancel" }
        ];
    }

    private List<InlineKeyboardButton> Team2Selection(long chatId)
    {
        var textProvider = TextProvider(chatId);

        return
        [
            new InlineKeyboardButton { Text = textProvider["DoneButton"], CallbackData = "graz_team2_done" },
            new InlineKeyboardButton { Text = textProvider["BackButton"], CallbackData = "graz_play_new_graz" },
            new InlineKeyboardButton { Text = textProvider["CancelButton"], CallbackData = "graz_cancel" }
        ];
    }

    public UiScreen GrazOpenFinishedScreen(long chatId, Graz graz)
    {
        var t = TextProvider(chatId);

        var team1Names = string.Join(", ", graz.Team1Members);
        var team2Names = string.Join(", ", graz.Team2Members);

        var team1Cup = graz.WinnerTeam == 1 ? "🏆 " : string.Empty;
        var team2Cup = graz.WinnerTeam == 2 ? "🏆 " : string.Empty;

        var description = string.IsNullOrWhiteSpace(graz.Description)
            ? t["NoDescriptionText"]
            : graz.Description;

        var deadline = string.IsNullOrWhiteSpace(graz.Deadline)
            ? t["NoDeadlineText"]
            : graz.Deadline;

        var playedAt = graz.CreatedAt.ToString("yyyy-MM-dd HH:mm");

        var text =
            $"{t["ChooseOptionText"]}\n\n" +
            $"{team1Cup}{team1Names} {t["VsText"]} {team2Cup}{team2Names}\n" +
            $"{t["GrazLabel"]} - {description}\n" +
            $"{t["DeadlineLabel"]} - {deadline}\n" +
            $"{t["PlayedAtLabel"]} - {playedAt}\n";

        var keyboard = new InlineKeyboardMarkup
        {
            InlineKeyboard =
            [
                [
                    new InlineKeyboardButton
                    {
                        Text = t["GivenButton"],
                        CallbackData = $"graz_finished_given:{graz.Id}"
                    },
                    new InlineKeyboardButton
                    {
                        Text = t["NotGivenButton"],
                        CallbackData = $"graz_finished_notgiven:{graz.Id}"
                    }
                ],
                [
                    new InlineKeyboardButton
                    {
                        Text = t["BackButton"],
                        CallbackData = "graz_back_to_finished"
                    }
                ]
            ]
        };

        return new UiScreen(text, keyboard);
    }

    public UiScreen GrazOpenInprogressScreen(long chatId, Graz graz)
    {
        var textProvider = TextProvider(chatId);

        var team1Names = string.Join(", ", graz.Team1Members);
        var team2Names = string.Join(", ", graz.Team2Members);

        var grazLine = GrazDisplayLine(chatId, index: null, graz);

        var text =
            $"{textProvider["ChooseOptionText"]}\n\n" +
            $"{textProvider["SelectedGrazPrefixText"]} : {grazLine}";

        var keyboard = new InlineKeyboardMarkup
        {
            InlineKeyboard =
            [
                [
                    new InlineKeyboardButton
                    {
                        Text = $"{textProvider["WinnerPrefix"]} : {team1Names}",
                        CallbackData = $"graz_winner_team1:{graz.Id}"
                    }
                ],
                [
                    new InlineKeyboardButton
                    {
                        Text = $"{textProvider["WinnerPrefix"]} : {team2Names}",
                        CallbackData = $"graz_winner_team2:{graz.Id}"
                    }
                ],
                [
                    new InlineKeyboardButton
                    {
                        Text = textProvider["DeleteButton"],
                        CallbackData = $"graz_delete:{graz.Id}"
                    }
                ],
                [
                    new InlineKeyboardButton
                    {
                        Text = textProvider["BackButton"],
                        CallbackData = "graz_back_to_inprogress"
                    }
                ]
            ]
        };

        return new UiScreen(text, keyboard);
    }


    public UiScreen Team1SelectionScreen(long chatId, Team1SelectionVm vm)
    {
        var textProvider = TextProvider(chatId);

        var text = BuildTeam1SelectionText(textProvider, vm.SelectedNames, vm.Warning);

        var keyboard = BuildTeam1SelectionKeyboard(chatId, vm.AvailableUsers);

        return new UiScreen(text, keyboard);
    }

    private static string BuildTeam1SelectionText(ITextProvider t, string? selectedNames, string? warning)
    {
        var sb = new StringBuilder();

        sb.AppendLine(t["Team1SelectionHeader"]);

        if (!string.IsNullOrWhiteSpace(selectedNames))
        {
            sb.AppendLine();
            sb.Append(t["Team1Label"] + " :");
            sb.Append(' ');
            sb.Append(selectedNames);
        }

        if (string.IsNullOrWhiteSpace(warning)) return sb.ToString().TrimEnd();
        sb.AppendLine();
        sb.AppendLine();
        sb.Append(warning);

        return sb.ToString().TrimEnd();
    }

    private InlineKeyboardMarkup BuildTeam1SelectionKeyboard(
        long chatId,
        IReadOnlyList<(long Id, string Name)> availableUsers)
    {
        var rows = new List<List<InlineKeyboardButton>>();

        for (var i = 0; i < availableUsers.Count; i += 2)
        {
            var row = new List<InlineKeyboardButton>();

            var u1 = availableUsers[i];
            row.Add(new InlineKeyboardButton
            {
                Text = u1.Name,
                CallbackData = $"graz_team1_add:{u1.Id}"
            });

            if (i + 1 < availableUsers.Count)
            {
                var u2 = availableUsers[i + 1];
                row.Add(new InlineKeyboardButton
                {
                    Text = u2.Name,
                    CallbackData = $"graz_team1_add:{u2.Id}"
                });
            }

            rows.Add(row);
        }

        rows.Add(Team1Selection(chatId));

        return new InlineKeyboardMarkup { InlineKeyboard = rows };
    }

    public UiScreen Team2SelectionScreen(long chatId, Team2SelectionVm vm)
    {
        var t = TextProvider(chatId);

        var text = BuildTeam2Text(t, vm.Team1Names, vm.Team2Names, vm.Warning);
        var keyboard = BuildTeam2Keyboard(chatId, vm.AvailableUsers);

        return new UiScreen(text, keyboard);
    }

    private static string BuildTeam2Text(
        ITextProvider t,
        string? team1Names,
        string? team2Names,
        string? warning)
    {
        var sb = new StringBuilder();

        sb.AppendLine(t["Team2SelectionHeader"]);
        sb.AppendLine();

        if (!string.IsNullOrWhiteSpace(team1Names))
            sb.AppendLine($"{t["Team1Label"]} : {team1Names}");

        if (!string.IsNullOrWhiteSpace(team2Names))
            sb.AppendLine($"{t["Team2Label"]} : {team2Names}");

        if (string.IsNullOrWhiteSpace(warning)) return sb.ToString().TrimEnd();
        sb.AppendLine();
        sb.AppendLine(warning);

        return sb.ToString().TrimEnd();
    }

    private InlineKeyboardMarkup BuildTeam2Keyboard(
        long chatId,
        IReadOnlyList<(long Id, string Name)> availableUsers)
    {
        var rows = new List<List<InlineKeyboardButton>>();

        for (var i = 0; i < availableUsers.Count; i += 2)
        {
            var row = new List<InlineKeyboardButton>();

            var u1 = availableUsers[i];
            row.Add(new InlineKeyboardButton
            {
                Text = u1.Name,
                CallbackData = $"graz_team2_add:{u1.Id}"
            });

            if (i + 1 < availableUsers.Count)
            {
                var u2 = availableUsers[i + 1];
                row.Add(new InlineKeyboardButton
                {
                    Text = u2.Name,
                    CallbackData = $"graz_team2_add:{u2.Id}"
                });
            }

            rows.Add(row);
        }

        rows.Add(Team2Selection(chatId));

        return new InlineKeyboardMarkup { InlineKeyboard = rows };
    }

    public UiScreen GrazCancelledScreen(long chatId)
    {
        var textProvider = TextProvider(chatId);
        return new UiScreen(textProvider["GrazCancelledText"], null);
    }

    public UiScreen BotClosedScreen(long chatId)
    {
        var textProvider = TextProvider(chatId);
        return new UiScreen(textProvider["BotClosed"], null);
    }

    public string DeleteNotAllowedWarning(long chatId)
    {
        var textProvider = TextProvider(chatId);
        return textProvider["DeleteNotAllowedWarning"];
    }

    public string NotGivenNotAllowed(long chatId)
    {
        var textProvider = TextProvider(chatId);
        return textProvider["NotGivenNotAllowed"];
    }

    public string NoMemberForTeamWarning(long chatId)
    {
        var textProvider = TextProvider(chatId);
        return textProvider["NoMemberForTeamWarning"];
    }

    public string GivenConfirmOnlyWinnerWarning(long chatId)
    {
        var textProvider = TextProvider(chatId);
        return textProvider["GivenConfirmOnlyWinnerWarning"];
    }

    public string Team1OverlapWarning(long chatId)
    {
        var textProvider = TextProvider(chatId);
        return textProvider["Team1OverlapWarning"];
    }

    public UiScreen SessionExpiredWarning(long chatId)
    {
        var textProvider = TextProvider(chatId);
        return new UiScreen(textProvider["SessionExpiredWarning"], null);
    }

    public UiScreen GrazNotFound(long chatId)
    {
        var textProvider = TextProvider(chatId);
        return new UiScreen(textProvider["GrazNotFound"], null);
    }

    private static string GrazSummaryText(
        ITextProvider t,
        string headerText,
        GrazSummaryVm vm,
        bool includeDescription,
        bool includeDeadline)
    {
        var sb = new StringBuilder();

        sb.AppendLine(headerText);
        sb.AppendLine();

        sb.AppendLine($"{t["Team1Label"]} : {vm.Team1Names}");
        sb.AppendLine($"{t["Team2Label"]} : {vm.Team2Names}");

        if (includeDescription)
        {
            var desc = !string.IsNullOrWhiteSpace(vm.Description)
                ? vm.Description
                : t["GrazSummaryNoDescription"];
            sb.AppendLine($"{t["GrazSummaryDescriptionLabel"]} : {desc}");
        }

        if (!includeDeadline) return sb.ToString().TrimEnd();
        var deadline = !string.IsNullOrWhiteSpace(vm.Deadline)
            ? vm.Deadline
            : t["GrazSummaryNoDeadline"];
        sb.AppendLine($"{t["DeadlineLabel"]} : {deadline}");

        return sb.ToString().TrimEnd();
    }

    public UiScreen GrazSummaryStepScreen(long chatId, ConversationState step, GrazSummaryVm vm)
    {
        var textProvider = TextProvider(chatId);

        string headerText;
        bool includeDescription;
        bool includeDeadline;
        InlineKeyboardMarkup keyboard;

        switch (step)
        {
            case ConversationState.CreatingGraz_Description:
                headerText = textProvider["DescriptionHeaderText"];
                includeDescription = false;
                includeDeadline = false;
                keyboard = BackCancel(chatId);
                break;

            case ConversationState.CreatingGraz_Deadline:
                headerText = textProvider["DeadlineLabel"];
                includeDescription = true;
                includeDeadline = false;
                keyboard = BackCancel(chatId);
                break;

            case ConversationState.CreatingGraz_Review:
                headerText = textProvider["ReviewHeaderText"];
                includeDescription = true;
                includeDeadline = true;
                keyboard = Review(chatId);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(step), step, @"Not a summary step");
        }

        var text = GrazSummaryText(textProvider, headerText, vm, includeDescription, includeDeadline);
        return new UiScreen(text, keyboard);
    }


    private string GrazDisplayLine(long chatId, int? index, Graz graz)
    {
        var t = TextProvider(chatId);

        var team1 = string.Join(", ", graz.Team1Members);
        var team2 = string.Join(", ", graz.Team2Members);

        var description = string.IsNullOrWhiteSpace(graz.Description)
            ? t["NoDescriptionText"]
            : graz.Description;

        var deadline = string.IsNullOrWhiteSpace(graz.Deadline)
            ? t["NoDeadlineText"]
            : graz.Deadline;

        var prefix = index.HasValue ? $"{index.Value}) " : string.Empty;

        var sb = new StringBuilder();

        sb.Append(prefix);
        sb.Append(team1);
        sb.Append(' ');
        sb.Append(t["VsText"]);
        sb.Append(' ');
        sb.AppendLine(team2);

        sb.Append(t["GrazLabel"]);
        sb.Append(": ");
        sb.AppendLine(description);

        sb.Append(t["DeadlineLabel"]);
        sb.Append(": ");
        sb.Append(deadline);

        return sb.ToString().TrimEnd();
    }

    public UiScreen GrazFinishedScreen(long chatId, Graz graz, int winningTeam)
    {
        var textProvider = TextProvider(chatId);

        var winnerNames = winningTeam == 1
            ? string.Join(", ", graz.Team1Members)
            : string.Join(", ", graz.Team2Members);

        var text =
            $"{textProvider["AnnounceGrazFinishedText"]}\n\n" +
            $"{GrazDisplayLine(chatId, index: null, graz)}\n\n" +
            $"{textProvider["WinnerPrefix"]} - {winnerNames}";

        return new UiScreen(text, BackToInProgress(chatId));
    }
    
    public UiScreen GrazHistoryScreen(long chatId, IReadOnlyList<Graz> history)
    {
        var t = TextProvider(chatId);

        if (history.Count == 0)
            return new UiScreen(t["HistoryEmptyText"], BackToMenu(chatId));

        var rows = new List<List<InlineKeyboardButton>>();

        foreach (var graz in history)
        {
            rows.Add(
            [
                new InlineKeyboardButton
                {
                    Text = GrazHistoryButtonText(chatId, graz),
                    CallbackData = $"graz_finished_open:{graz.Id}"
                }
            ]);
        }

        rows.Add(
        [
            new InlineKeyboardButton
            {
                Text = t["BackButton"],
                CallbackData = "graz_back_to_menu"
            }
        ]);

        var keyboard = new InlineKeyboardMarkup { InlineKeyboard = rows };

        return new UiScreen(t["HistoryHeaderText"], keyboard);
    }

    private string GrazHistoryButtonText(long chatId, Graz graz)
    {
        var textProvider = TextProvider(chatId);

        var team1 = string.Join(", ", graz.Team1Members);
        var team2 = string.Join(", ", graz.Team2Members);

        var description = string.IsNullOrWhiteSpace(graz.Description)
            ? textProvider["NoDescriptionShort"]
            : graz.Description;

        var team1Mark = graz.WinnerTeam == 1 ? " 🏆" : string.Empty;
        var team2Mark = graz.WinnerTeam == 2 ? " 🏆" : string.Empty;

        return
            $"{team1}{team1Mark} {textProvider["VsText"]} {team2}{team2Mark} : {textProvider["GrazLabel"]} - {description}";
    }

    public UiScreen GrazSavedScreen(long chatId, GrazSummaryVm vm)
    {
        var t = TextProvider(chatId);

        var text = GrazSummaryText(
            t: t,
            headerText: t["SavedHeaderText"],
            vm: vm,
            includeDescription: true,
            includeDeadline: true);

        return new UiScreen(text, null);
    }

    public UiScreen GrazGivenConfirmationScreen(long chatId, string winnerNames, string loserNames)
    {
        var textProvider = TextProvider(chatId);

        var text =
            $"🏆{textProvider["GrazGivenTitleText"]}\n\n" +
            $"{textProvider["ThanksToWinnersText"]}\n" +
            $"💪 {winnerNames}\n\n" +
            $"{textProvider["ThanksToLosersText"]}\n" +
            $"🥹 {loserNames}";

        var keyboard = new InlineKeyboardMarkup
        {
            InlineKeyboard =
            [
                [
                    new InlineKeyboardButton
                    {
                        Text = textProvider["BackButton"],
                        CallbackData = "graz_back_to_finished"
                    }
                ]
            ]
        };

        return new UiScreen(text, keyboard);
    }
}