namespace Telegram.Bot.Turkey.Commands.GetExpenses;

public interface IExpensesProvider
{
    Task<GetExpensesResponse> GetAsync(string tgUserName, CancellationToken ct);
}