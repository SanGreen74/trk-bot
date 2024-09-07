namespace Telegram.Bot.Turkey.Sheets.BotConfiguration.Models;

public record Participant
{
    public required string Name { get; init; }

    public required string TgName { get; init; }
}