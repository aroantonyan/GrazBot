using System.Globalization;

namespace TelegramBot.UI.Localization;

public interface ITextProvider
{
    CultureInfo Culture { get; }

    string this[string key] { get; }

    string Format(string key, params object[] args);
}