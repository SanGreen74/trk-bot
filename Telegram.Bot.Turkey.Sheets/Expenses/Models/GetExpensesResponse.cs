namespace Telegram.Bot.Turkey.Sheets.Expenses.Models;

public record GetExpensesResponse
{
    public required Item[] Items { get; init; }
    
    public record Item
    {
        public required decimal AmountUsd { get; init; }

        public required DateOnly Date { get; init; }

        public required string Comment { get; init; }
    }
}
