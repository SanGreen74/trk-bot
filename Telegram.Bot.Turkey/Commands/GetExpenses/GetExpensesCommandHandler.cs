using System.Text;
using Telegram.Bot.Turkey.Commands.Access;
using Telegram.Bot.Turkey.Commands.AddExpense;
using Telegram.Bot.Turkey.Commands.Helpers;
using Telegram.Bot.Turkey.Sheets.BotConfiguration;
using Telegram.Bot.Turkey.State;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Telegram.Bot.Turkey.Commands.GetExpenses;

public class GetExpensesCommandHandler : CommandHandler
{
    public override required string CommandName { get; init; } = TgCommands.GetExpenses;

    private readonly ITelegramBotClient _botClient;
    private readonly IExpensesProvider _expensesProvider;
    private readonly IBotConfigurationRepository _configurationRepository;
    private readonly IUserAccessProvider _userAccessProvider;
    private readonly IUserSessionState _sessionState;
    
    public GetExpensesCommandHandler(ITelegramBotClient botClient, IExpensesProvider expensesProvider, IBotConfigurationRepository configurationRepository, IUserAccessProvider userAccessProvider, IUserSessionState sessionState) : base(sessionState)
    {
        _botClient = botClient;
        _expensesProvider = expensesProvider;
        _configurationRepository = configurationRepository;
        _userAccessProvider = userAccessProvider;
        _sessionState = sessionState;
    }

    public override async Task StartHandle(Update update, CancellationToken ct)
    {
        var message = update.Message;
        if (message!.Type != MessageType.Text || message.Text == null || message.From?.Username == null)
            return;

        var userName = message.From.Username;
        var chatId = message.Chat.Id;
        if (!await _userAccessProvider.CanAddExpensesAsync(userName, ct))
        {
            await _botClient.SendTextMessageAsync(chatId, $"Пользователь @{userName} не может просматривать расходы",
                cancellationToken: ct);
            OnComplete(userName);
            return;
        }

        var configuration = await _configurationRepository.GetAsync(ct);
        if (configuration == null)
        {
            await _botClient.SendTextMessageAsync(chatId, $"Отсутствует конфигурация бота",
                cancellationToken: ct);
            OnComplete(userName);
            return;
        }
        
        var inlineKeyboardButtons = configuration.Participants
            .Select(x => InlineKeyboardButton.WithCallbackData(x.Name, x.TgName))
            .ToArray();
        var inlineKeyboardMarkup = new InlineKeyboardMarkup(inlineKeyboardButtons);

        var state = new State{WaitAction = State.WaitActions.WaitPerson};
        _sessionState.SetState(userName, state);
        
        await _botClient.SendTextMessageAsync(chatId, "Выбери чьи расходы посмотреть",
            replyMarkup: inlineKeyboardMarkup, cancellationToken: ct);
    }

    public override async Task HandleIntermediateMessage(Update update, CancellationToken ct)
    {
        if (update is { Type: UpdateType.CallbackQuery, CallbackQuery: { Data: not null, Message: not null } } &&
            update.CallbackQuery.Message.From?.Username != null)
        {
            var query = update.CallbackQuery;
            var chatId = query.Message.Chat.Id;
            var userName = query.From.Username!;
            var state = _sessionState.GetState<State>(userName);
            if (state == null)
            {
                await _botClient.ReplySessionExpiredAsync(chatId, ct);
                OnComplete(userName);
                return;
            }

            var configuration = await _configurationRepository.GetAsync(ct);
            if (configuration == null)
            {
                await _botClient.ReplyConfigurationDamagedAsync(chatId, ct);
                OnComplete(userName);
                return;
            }

            if (state.WaitAction == State.WaitActions.WaitPerson)
            {
                await _botClient
                    .DeleteMessageAsync(chatId, query.Message.MessageId, ct);
                var expenses = await _expensesProvider.GetAsync(query.Data, ct);
                if (expenses.Transactions.Length == 0)
                {
                    await _botClient
                        .SendTextMessageAsync(chatId, $"Операции для {query.Data} не найдены", cancellationToken: ct);
                    OnComplete(userName);
                    return;
                }
                var groupedTransactions = expenses.Transactions
                    .GroupBy(x => x.Date)
                    .OrderBy(x => x.Key);
                foreach (var groupedTransaction in groupedTransactions)
                {
                    var date = groupedTransaction.Key;
                    var transactions = groupedTransaction.ToArray();
                    var text = FormatTransactions(date, transactions);
                    await _botClient
                        .SendTextMessageAsync(chatId, text, cancellationToken: ct);
                }

                await _botClient
                    .SendTextMessageAsync(chatId, $"Всего потрачено {expenses.TotalUsd} {CurrencyNames.Usd.Name}",
                        cancellationToken: ct);
                OnComplete(userName);
                return;
            }
        }
    }

    private static string FormatTransactions(DateOnly date, GetExpensesResponse.Transaction[] transactions)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Дата: {date.ToString("dd.MM.yyyy")}");
        foreach (var transaction in transactions)
        {
            sb.AppendLine($"— {transaction.AmountUsd.ToString("F2")} {CurrencyNames.Usd.Name}. {transaction.Comment}");
        }

        var totalPerDay = transactions.Sum(x => x.AmountUsd);
        sb.AppendLine($"Всего за день: {totalPerDay.ToString("F2")} {CurrencyNames.Usd.Name}");
        return sb.ToString();
    }

    private class State
    {
        public string WaitAction { get; set; }

        public static class WaitActions
        {
            public const string WaitPerson = "wait person";
        }
    }
}
