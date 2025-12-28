using System.Globalization;
using System.Resources;

namespace TelegramBot.UI.Localization;



public sealed class ResxTextProvider : ITextProvider
{
    private static readonly ResourceManager ResourceManager =
        new ResourceManager("TelegramBot.UI.Resources.GrazBotTexts", typeof(ResxTextProvider).Assembly);

    public CultureInfo Culture { get; }

    public ResxTextProvider(CultureInfo culture)
        => Culture = culture;

    public string this[string key]
        => ResourceManager.GetString(key, Culture) ?? $"[[{key}]]";

    public string Format(string key, params object[] args)
        => string.Format(Culture, this[key], args);
}