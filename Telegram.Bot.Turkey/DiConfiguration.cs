using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot.Turkey.Commands;
using Telegram.Bot.Turkey.Commands.Access;
using Telegram.Bot.Turkey.State;

namespace Telegram.Bot.Turkey;

public static class DiConfiguration
{
    public static IServiceCollection ConfigureBotServices(this IServiceCollection services, IConfiguration configuration)
    {
        return services
            .ConfigureBot(configuration)
            .ConfigureMessageReceiver()
            .ConfigureCommandHandlers()
            .ConfigureSessionState()
            .ConfigureAccessProvider();
    }

    private static IServiceCollection ConfigureBot(this IServiceCollection services, IConfiguration configuration)
    {
        var botToken = configuration["BOT_TOKEN"];
        if (botToken is null)
        {
            throw new InvalidOperationException("Не удалось найти токен для бота");
        }
        var telegramBotClient = new TelegramBotClient(botToken);
        services.AddSingleton<ITelegramBotClient, TelegramBotClient>(_ => telegramBotClient);
        return services;
    }

    private static IServiceCollection ConfigureMessageReceiver(this IServiceCollection services)
        => services
            .AddSingleton<MessageReceiver>();

    private static IServiceCollection ConfigureSessionState(this IServiceCollection services)
        => services
            .AddMemoryCache()
            .AddSingleton<IUserSessionState, UserSessionState>();

    private static IServiceCollection ConfigureAccessProvider(this IServiceCollection services)
        => services
            .AddSingleton<IUserAccessProvider, UserAccessProvider>();
}
