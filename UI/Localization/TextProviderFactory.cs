using System.Globalization;

namespace TelegramBot.UI.Localization;

public sealed class TextProviderFactory : ITextProviderFactory
{
    private static readonly CultureInfo En = CultureInfo.GetCultureInfo("en");
    private static readonly CultureInfo Hy = CultureInfo.GetCultureInfo("hy"); 

    public ITextProvider Create(string? languageCode)
    {
        languageCode = (languageCode ?? "en").Trim().ToLowerInvariant();

        var culture = languageCode switch
        {
            "hy"  => Hy,
            _ => En
        };

        return new ResxTextProvider(culture);
    }
}