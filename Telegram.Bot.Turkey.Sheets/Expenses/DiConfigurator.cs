namespace Telegram.Bot.Turkey.Sheets.Expenses;

internal static class DiConfigurator
{
    public static IServiceCollection ConfigureExpense(this IServiceCollection services)
        => services.AddSingleton<IExpensesService, ExpensesService>();
}
