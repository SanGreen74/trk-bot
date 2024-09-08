namespace Telegram.Bot.Turkey.Sheets.Expenses.Models;

public record InsertExpenseRowResponse
{
    public required int InsertedInRow { get; init; }
}
