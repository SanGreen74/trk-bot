using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot.Turkey.Commands;
using Telegram.Bot.Turkey.Commands.AddUser;
using Telegram.Bot.Turkey.Commands.Start;
using Telegram.Bot.Turkey.State;

namespace Telegram.Bot.Turkey;

public static class DiConfiguration
{
    public static IServiceCollection ConfigureServices(this IServiceCollection services, IConfiguration configuration)
    {
        return services
            .ConfigureBot(configuration)
            .ConfigureMessageReceiver()
            .ConfigureCommandHandlers()
            .ConfigureSessionState();
    }

    private static IServiceCollection ConfigureBot(this IServiceCollection services, IConfiguration configuration)
    {
        const string token = "";
        var telegramBotClient = new TelegramBotClient(token);
        services.AddSingleton<ITelegramBotClient, TelegramBotClient>(_ => telegramBotClient);
        return services;
    }

    private static IServiceCollection ConfigureMessageReceiver(this IServiceCollection services)
        => services
            .AddSingleton<MessageReceiver>();

    private static IServiceCollection ConfigureCommandHandlers(this IServiceCollection services)
        => services.AddSingleton<MainCommandHandler>()
            .AddSingleton<CommandHandler, AddUserCommandHandler>()
            .AddSingleton<CommandHandler, StartCommandHandler>()
            .AddSingleton<CommandHandler[]>(sp => sp.GetServices<CommandHandler>().ToArray());

    private static IServiceCollection ConfigureSessionState(this IServiceCollection services)
        => services
            .AddMemoryCache()
            .AddSingleton<IUserSessionState, UserSessionState>();
}
