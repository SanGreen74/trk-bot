using Telegram.Bot.Turkey.Sheets.BotConfiguration;
using Telegram.Bot.Turkey.Sheets.Expenses;
using Telegram.Bot.Turkey.Sheets.GoogleClient;

namespace Telegram.Bot.Turkey.Sheets;

public static class DiConfigurator
{
    public static IServiceCollection ConfigureSheets(this IServiceCollection services, IConfiguration configuration)
        => services
            .ConfigureGoogleClient(configuration)
            .ConfigureBotConfigurationRepository()
            .ConfigureExpense();
}
