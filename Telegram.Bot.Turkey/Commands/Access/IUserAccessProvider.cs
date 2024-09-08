namespace Telegram.Bot.Turkey.Commands.Access;

public interface IUserAccessProvider
{
    Task<bool> IsAdminAsync(string userName);

    Task<bool> CanAddExpensesAsync(string userName, CancellationToken cancellationToken);
}