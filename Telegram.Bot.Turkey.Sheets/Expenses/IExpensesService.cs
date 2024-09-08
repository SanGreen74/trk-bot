using Telegram.Bot.Turkey.Sheets.Expenses.Models;

namespace Telegram.Bot.Turkey.Sheets.Expenses;

public interface IExpensesService
{
    Task<string[]> GetUsersAsync(CancellationToken cancellationToken);

    Task<InsertExpenseRowResponse> InsertExpenseRowAsync(InsertExpenseRowRequest request, CancellationToken cancellationToken);
}