namespace TelegramBot.UI.Localization;

public interface ITextProviderFactory
{
    ITextProvider Create(string? languageCode);
}