namespace TelegramBot.Models;

public sealed class Graz
{
    public Guid Id { get; } = Guid.NewGuid();
    public List<string> Team1Members { get; init; } = [];
    public List<string> Team2Members { get; init; } = [];
    public string Description { get; init; } = null!;
    public string? Deadline { get; init; }
    public bool IsFinished { get; set; }
    public bool IsGiven { get; set; }
    public DateTime CreatedAt { get; init; }
    public int? WinnerTeam { get; set; } 
    public DateTime? FinishedAt { get; set; }
    public bool IsDeleted { get; set; }
    public List<long> Team1UserIds { get; init; } = [];
    public List<long> Team2UserIds { get; init; } = [];


}