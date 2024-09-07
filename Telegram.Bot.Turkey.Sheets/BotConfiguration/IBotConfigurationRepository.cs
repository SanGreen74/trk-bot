using Telegram.Bot.Turkey.Sheets.BotConfiguration.Models;

namespace Telegram.Bot.Turkey.Sheets.BotConfiguration;

public interface IBotConfigurationRepository
{
    Task<BotConfigurationDto?> GetAsync(CancellationToken cancellationToken);
    
    Task SetAsync(BotConfigurationDto botConfiguration, CancellationToken cancellationToken);
}
