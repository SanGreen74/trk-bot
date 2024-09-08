using Telegram.Bot.Turkey.Sheets.BotConfiguration;
using Telegram.Bot.Turkey.Sheets.BotConfiguration.Models;
using Telegram.Bot.Turkey.Sheets.Expenses;
using Telegram.Bot.Turkey.Sheets.Expenses.Models;

namespace Telegram.Bot.Turkey.Commands.Transactions;

public class TransactionUploader : ITransactionUploader
{
    private readonly IBotConfigurationRepository _configurationRepository;
    private readonly IExpensesService _expensesService;

    public TransactionUploader(IBotConfigurationRepository configurationRepository, IExpensesService expensesService)
    {
        _configurationRepository = configurationRepository;
        _expensesService = expensesService;
    }
    
    public async Task<int> InsertOneAsync(TransactionDto transaction, CancellationToken cancellationToken)
    {
        var configuration = await _configurationRepository.GetAsync(cancellationToken);
        if (configuration?.Usd2Lira2UsdExchangeRate?.ConversionRate == null)
        {
            throw new InvalidOperationException("Не найден конфиг");
        }

        var exchangeRate = configuration.Usd2Lira2UsdExchangeRate;
        var insertExpenseRowRequest = new InsertExpenseRowRequest
        {
            Comment = transaction.Comment,
            Date = transaction.Date,
            WhoPaidName = ResolveUserName(transaction.WhoPaidTgName, configuration.Participants),
            Participants = transaction.Participants
                .Select(x => new InsertExpenseRowParticipant
                {
                    Name = ResolveUserName(x.TgName, configuration.Participants),
                    Amount = CurrencyConverter.ConvertToUsdIfNeed(x.Amount, transaction.CurrencyType, exchangeRate)
                })
                .ToArray()
        };
        
        var response = await _expensesService.InsertExpenseRowAsync(insertExpenseRowRequest, cancellationToken);
        return response.InsertedInRow;
    }

    private static string ResolveUserName(string tgName, IEnumerable<Participant> participants)
    {
        return participants
            .First(x => x.TgName.Equals(tgName, StringComparison.OrdinalIgnoreCase))
            .Name;
    }
}
