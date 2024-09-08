namespace Telegram.Bot.Turkey.Commands.Helpers;

public static class TurkeyDate
{
    public static DateOnly GetToday() => DateOnly.FromDateTime(DateTime.UtcNow.AddHours(3));
}