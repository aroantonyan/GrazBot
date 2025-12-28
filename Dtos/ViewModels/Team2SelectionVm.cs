namespace TelegramBot.Dtos.ViewModels;

public sealed record Team2SelectionVm(
    string? Team1Names,
    string? Team2Names,
    IReadOnlyList<(long Id, string Name)> AvailableUsers,
    string? Warning
);