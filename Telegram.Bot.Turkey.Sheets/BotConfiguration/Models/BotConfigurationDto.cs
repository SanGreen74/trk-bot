namespace Telegram.Bot.Turkey.Sheets.BotConfiguration.Models;

public record BotConfigurationDto
{
    public Participant[] Participants { get; init; } = [];
    
    public Lira2UsdExchangeRate? Lira2UsdExchangeRate { get; init; }
}
