using TelegramBot.State;

namespace TelegramBot.Session;

public sealed class GrazCreationSession
{
    public ConversationState? State { get; set; } = ConversationState.Idle;
    public long CreatorUserId { get; init; }     
    public int? WizardMessageId { get; set; }     
    public DateTimeOffset LastInteractUtc { get; set; }
    public HashSet<long> Team1UserIds { get; set; } = [];
    public HashSet<long> Team2UserIds { get; set; } = [];
    public string? Description { get; set; }
    public string? Deadline { get; set; }
}