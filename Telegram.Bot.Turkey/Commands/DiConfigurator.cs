using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot.Turkey.Commands.AddUser;
using Telegram.Bot.Turkey.Commands.RemoveUser;
using Telegram.Bot.Turkey.Commands.SetCurrency;
using Telegram.Bot.Turkey.Commands.Start;

namespace Telegram.Bot.Turkey.Commands;

public static class DiConfigurator
{
    public static IServiceCollection ConfigureCommandHandlers(this IServiceCollection services)
        => services.AddSingleton<MainCommandHandler>()
            .AddSingleton<CommandHandler, AddUserCommandHandler>()
            .AddSingleton<CommandHandler, StartCommandHandler>()
            .AddSingleton<CommandHandler, RemoveUserCommandHandler>()
            .AddSingleton<CommandHandler, SetCurrencyCommandHandler>()
            // .AddSingleton<CommandHandler, SetCurrencyCommandHandler>()
            .AddSingleton<CommandHandler[]>(sp => sp.GetServices<CommandHandler>().ToArray());
}
