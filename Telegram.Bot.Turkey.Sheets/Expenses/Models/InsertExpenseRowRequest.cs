namespace Telegram.Bot.Turkey.Sheets.Expenses.Models;

public record InsertExpenseRowRequest
{
    public required DateOnly Date { get; init; }

    public required string Comment { get; init; }

    public required string WhoPaidName { get; init; }

    public required InsertExpenseRowParticipant[] Participants { get; init; }
}