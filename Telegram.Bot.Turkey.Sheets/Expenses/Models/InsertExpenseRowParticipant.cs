namespace Telegram.Bot.Turkey.Sheets.Expenses.Models;

public record InsertExpenseRowParticipant
{
    public required string Name { get; init; }

    public required decimal Amount { get; init; }
}