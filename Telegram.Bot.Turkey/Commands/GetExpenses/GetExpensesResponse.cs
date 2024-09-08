namespace Telegram.Bot.Turkey.Commands.GetExpenses;

public record GetExpensesResponse
{
    public required Transaction[] Transactions { get; init; }
    
    public required decimal TotalUsd { get; init; }
    
    public record Transaction
    {
        public required DateOnly Date { get; init; }
        
        public required string Comment { get; init; }

        public required decimal AmountUsd { get; init; }
    }
}