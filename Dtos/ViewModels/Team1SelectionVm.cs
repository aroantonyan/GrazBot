namespace TelegramBot.Dtos.ViewModels;

public sealed record Team1SelectionVm(IReadOnlyList<(long Id, string Name)> AvailableUsers,
    string? SelectedNames,
    string? Warning);