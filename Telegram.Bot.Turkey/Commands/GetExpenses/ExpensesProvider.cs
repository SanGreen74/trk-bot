using Telegram.Bot.Turkey.Sheets.BotConfiguration;
using Telegram.Bot.Turkey.Sheets.Expenses;

namespace Telegram.Bot.Turkey.Commands.GetExpenses;

public class ExpensesProvider : IExpensesProvider
{
    private readonly IBotConfigurationRepository _configurationRepository;
    private readonly IExpensesService _expensesService;

    public ExpensesProvider(IBotConfigurationRepository configurationRepository, IExpensesService expensesService)
    {
        _configurationRepository = configurationRepository;
        _expensesService = expensesService;
    }
    
    public async Task<GetExpensesResponse> GetAsync(string tgUserName, CancellationToken ct)
    {
        var configuration = await _configurationRepository.GetAsync(ct);
        var participant = configuration?.Participants.First(x =>
            x.TgName.Equals(tgUserName, StringComparison.InvariantCultureIgnoreCase));
        if (participant == null)
        {
            return new GetExpensesResponse
            {
                Transactions = [],
                TotalUsd = 0
            };
        }

        try
        {
            var expensesResponse = await _expensesService.GetExpensesAsync(participant.Name, ct);
            return new GetExpensesResponse
            {
                Transactions = expensesResponse
                    .Items
                    .Select(x => new GetExpensesResponse.Transaction
                    {
                        Comment = x.Comment,
                        Date = x.Date,
                        AmountUsd = x.AmountUsd
                    }).ToArray(),
                TotalUsd = expensesResponse.Items.Sum(x => x.AmountUsd)
            };
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
            return new GetExpensesResponse
            {
                Transactions = [],
                TotalUsd = 0
            };
        }
    }
}