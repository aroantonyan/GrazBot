namespace TelegramBot.Dtos.ViewModels;

public sealed record GrazSummaryVm(
    string Team1Names,
    string Team2Names,
    string? Description,
    string? Deadline
);