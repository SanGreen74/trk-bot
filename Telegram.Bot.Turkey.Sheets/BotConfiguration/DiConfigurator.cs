namespace Telegram.Bot.Turkey.Sheets.BotConfiguration;

internal static class DiConfigurator
{
    public static IServiceCollection ConfigureBotConfigurationRepository(this IServiceCollection services)
        => services.AddSingleton<IBotConfigurationRepository, BotConfigurationRepository>();
}
