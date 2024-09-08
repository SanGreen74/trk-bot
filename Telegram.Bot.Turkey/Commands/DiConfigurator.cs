using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot.Turkey.Commands.AddExpense.Common;
using Telegram.Bot.Turkey.Commands.AddExpense.Personal;
using Telegram.Bot.Turkey.Commands.AddUser;
using Telegram.Bot.Turkey.Commands.RemoveUser;
using Telegram.Bot.Turkey.Commands.SetCurrency;
using Telegram.Bot.Turkey.Commands.Start;
using Telegram.Bot.Turkey.Commands.Transactions;

namespace Telegram.Bot.Turkey.Commands;

public static class DiConfigurator
{
    public static IServiceCollection ConfigureCommandHandlers(this IServiceCollection services)
        => services.AddSingleton<MainCommandHandler>()
            .AddSingleton<CommandHandler, AddUserCommandHandler>()
            .AddSingleton<CommandHandler, StartCommandHandler>()
            .AddSingleton<CommandHandler, RemoveUserCommandHandler>()
            .AddSingleton<CommandHandler, SetCurrencyCommandHandler>()
            .AddSingleton<CommandHandler, AddCommonExpenseCommandHandler>()
            .AddSingleton<CommandHandler, AddPersonalExpenseCommandHandler>()
            .AddSingleton<CommandHandler[]>(sp => sp.GetServices<CommandHandler>().ToArray())
            .AddSingleton<ITransactionUploader, TransactionUploader>();
}
