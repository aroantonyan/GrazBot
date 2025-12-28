// ReSharper disable All
namespace TelegramBot.State;

public enum ConversationState
{
    Idle = 0,                   
    CreatingGraz_Team1Members = 1,
    CreatingGraz_Team2Members = 2,
    CreatingGraz_Description = 3,
    CreatingGraz_Deadline = 4,
    CreatingGraz_Review = 5  
}