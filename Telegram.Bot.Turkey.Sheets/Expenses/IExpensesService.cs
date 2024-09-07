namespace Telegram.Bot.Turkey.Sheets.Expenses;

public interface IExpensesService
{
    Task<string[]> GetUsersAsync(CancellationToken cancellationToken);
}
