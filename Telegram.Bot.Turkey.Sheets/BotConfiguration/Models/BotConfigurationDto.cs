namespace Telegram.Bot.Turkey.Sheets.BotConfiguration.Models;

public record BotConfigurationDto
{
    public List<Participant> Participants { get; init; } = [];
    
    public Lira2UsdExchangeRate? Usd2Lira2UsdExchangeRate { get; init; }
}
