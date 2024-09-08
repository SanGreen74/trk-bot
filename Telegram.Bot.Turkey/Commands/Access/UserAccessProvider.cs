using Telegram.Bot.Turkey.Sheets.BotConfiguration;

namespace Telegram.Bot.Turkey.Commands.Access;

public class UserAccessProvider : IUserAccessProvider
{
    private readonly IBotConfigurationRepository _configurationRepository;

    public UserAccessProvider(IBotConfigurationRepository configurationRepository)
    {
        _configurationRepository = configurationRepository;
    }
    
    public Task<bool> IsAdminAsync(string userName)
    {
        return Task.FromResult(userName.Equals("sangreen74", StringComparison.OrdinalIgnoreCase));
    }

    public async Task<bool> CanAddExpensesAsync(string userName, CancellationToken cancellationToken)
    {
        var configuration = await _configurationRepository.GetAsync(cancellationToken);
        return configuration?
            .Participants
            .Any(x => x.TgName.Equals(userName)) ?? false;
    }

    public async Task<bool> CanViewExpensesAsync(string userName, CancellationToken cancellationToken)
    {
        var configuration = await _configurationRepository.GetAsync(cancellationToken);
        return configuration?
            .Participants
            .Any(x => x.TgName.Equals(userName)) ?? false;
    }
}