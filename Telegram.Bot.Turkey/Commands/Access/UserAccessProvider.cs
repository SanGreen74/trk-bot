namespace Telegram.Bot.Turkey.Commands.Access;

public class UserAccessProvider : IUserAccessProvider
{
    public Task<bool> IsAdminAsync(string userName)
    {
        return Task.FromResult(userName.Equals("sangreen74", StringComparison.OrdinalIgnoreCase));
    }
}