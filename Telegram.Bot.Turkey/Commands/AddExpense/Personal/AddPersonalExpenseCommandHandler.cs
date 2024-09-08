using System.Globalization;
using System.Text;
using Telegram.Bot.Turkey.Commands.Access;
using Telegram.Bot.Turkey.Commands.Helpers;
using Telegram.Bot.Turkey.Commands.Transactions;
using Telegram.Bot.Turkey.Sheets.BotConfiguration;
using Telegram.Bot.Turkey.Sheets.BotConfiguration.Models;
using Telegram.Bot.Turkey.State;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Telegram.Bot.Turkey.Commands.AddExpense.Personal;

public class AddPersonalExpenseCommandHandler : CommandHandler
{
    public override required string CommandName { get; init; } = TgCommands.AddPersonalExpense;

    private readonly ITelegramBotClient _botClient;
    private readonly IUserAccessProvider _userAccessProvider;
    private readonly IUserSessionState _sessionState;
    private readonly IBotConfigurationRepository _configurationRepository;
    private readonly ITransactionUploader _transactionUploader;

    public AddPersonalExpenseCommandHandler(ITelegramBotClient botClient, IUserAccessProvider userAccessProvider,
        IUserSessionState sessionState, IBotConfigurationRepository configurationRepository,
        ITransactionUploader transactionUploader) : base(sessionState)
    {
        _botClient = botClient;
        _userAccessProvider = userAccessProvider;
        _sessionState = sessionState;
        _configurationRepository = configurationRepository;
        _transactionUploader = transactionUploader;
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
            await _botClient.ReplyNoAccessToAddExpenses(chatId, userName, ct);
            OnComplete(userName);
            return;
        }

        var state = new State();
        state.UpdateWaitAction(State.WaitActions.ChooseComment);
        await _botClient
            .SendTextMessageAsync(chatId, ExpenseTexts.InputPersonalComment, cancellationToken: ct);
        _sessionState.SetState(userName, state);
    }

    public override async Task HandleIntermediateMessage(Update update, CancellationToken ct)
    {
        var message = update.Message;
        if (message is { Text: not null, From.Username: not null, Type: MessageType.Text })
        {
            var userName = message.From.Username;
            var chatId = message.Chat.Id;
            var messageText = message.Text;
            await HandleInboxMessageAsync(chatId, userName, messageText, ct);
            return;
        }

        if (update is { Type: UpdateType.CallbackQuery, CallbackQuery: { Data: not null, Message: not null } } &&
            update.CallbackQuery.Message.From?.Username != null)
        {
            var query = update.CallbackQuery;
            var chatId = query.Message.Chat.Id;
            var userName = query.From.Username!;

            await HandleCallBackQueryAsync(chatId, userName, query.Data, query.Message.MessageId, ct);
        }
    }

    private async Task HandleInboxMessageAsync(long chatId, string userName, string messageText, CancellationToken ct)
    {
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

        // ChooseComment
        if (state.WaitAction == State.WaitActions.ChooseComment)
        {
            state.UpdateComment(messageText);
            state.UpdateWaitAction(State.WaitActions.ChooseCurrency);
            _sessionState.SetState(userName, state);
            var inlineKeyboardButtons = new[]
            {
                InlineKeyboardButton.WithCallbackData(CurrencyNames.Try.Text, CurrencyNames.Try.Name),
                InlineKeyboardButton.WithCallbackData(CurrencyNames.Usd.Text, CurrencyNames.Usd.Name)
            };
            var inlineKeyboardMarkup = new InlineKeyboardMarkup(inlineKeyboardButtons);
            await _botClient
                .SendTextMessageAsync(chatId, ExpenseTexts.ChooseCurrency, replyMarkup: inlineKeyboardMarkup,
                    cancellationToken: ct);
            return;
        }

        // AddSumTotal
        if (state.WaitAction == State.WaitActions.AddSumTotal)
        {
            var replaced = messageText.Replace(",", ".");
            if (!decimal.TryParse(replaced, CultureInfo.InvariantCulture, out var value))
            {
                await _botClient
                    .SendTextMessageAsync(chatId, $"Не удалось распарсить число {replaced}", cancellationToken: ct);
                OnComplete(userName);
                return;
            }

            state.UpdateSumTotal(value);
            state.UpdateWaitAction(State.WaitActions.ConfirmSave);
            _sessionState.SetState(userName, state);

            var saveButton = new[] { InlineKeyboardButton.WithCallbackData("Сохранить", "confirm_save") };
            var inlineKeyboardMarkup = new InlineKeyboardMarkup(saveButton);
            if (configuration.Usd2Lira2UsdExchangeRate == null)
            {
                await _botClient.ReplyConfigurationDamagedAsync(chatId, ct);
                OnComplete(userName);
                return;
            }

            await _botClient.SendTextMessageAsync(chatId, FormatState(configuration.Usd2Lira2UsdExchangeRate, state),
                replyMarkup: inlineKeyboardMarkup,
                cancellationToken: ct);
            return;
        }
    }

    private async Task HandleCallBackQueryAsync(long chatId, string userName, string queryData, int messageId,
        CancellationToken ct)
    {
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

        // Choose Currency
        if (state.WaitAction == State.WaitActions.ChooseCurrency)
        {
            if (queryData != CurrencyNames.Try.Name && queryData != CurrencyNames.Usd.Name)
            {
                await _botClient.SendTextMessageAsync(chatId, $"Неверный тип валюты: {queryData}",
                    cancellationToken: ct);
                OnComplete(userName);
                return;
            }

            state.UpdateCurrency(queryData);
            state.UpdateWaitAction(State.WaitActions.AddSumTotal);
            _sessionState.SetState(userName, state);

            var chosenCurrency = queryData switch
            {
                CurrencyNames.Try.Name => CurrencyNames.Try.Text,
                CurrencyNames.Usd.Name => CurrencyNames.Usd.Text,
                _ => throw new ArgumentOutOfRangeException(nameof(queryData), queryData, "Unexpected currency name")
            };

            var task1 = _botClient.SendTextMessageAsync(chatId,
                $"Введите сумму в {chosenCurrency} которую вы потратили", cancellationToken: ct);

            var task2 = _botClient.EditMessageTextAsync(chatId, messageId, $"Вы выбрали валюту: {chosenCurrency}",
                cancellationToken: ct);

            await Task.WhenAll(task1, task2);
            return;
        }

        if (state.WaitAction == State.WaitActions.ConfirmSave)
        {
            if (!state.CanSave())
            {
                await _botClient.ReplySessionExpiredAsync(chatId, ct);
                OnComplete(userName);
                return;
            }

            var transactionDto = new TransactionDto
            {
                Comment = state.Comment,
                Date = TurkeyDate.GetToday(),
                CurrencyType = state.Currency,
                WhoPaidTgName = userName,
                Participants =
                [
                    new TransactionParticipant
                    {
                        Amount = state.SumTotal,
                        TgName = userName
                    }
                ]
            };
            var task1 = _botClient.ReplyBeginSaveTransaction(chatId, transactionDto, configuration, ct);
            var task2 = _botClient
                .DeleteMessageAsync(chatId, messageId, cancellationToken: ct);
            await Task.WhenAll(task1, task2);
            var insertedIndex = await _transactionUploader.InsertOneAsync(transactionDto, cancellationToken: ct);
            await _botClient.ReplyTransactionWasAppended(chatId, insertedIndex, ct);
            OnComplete(userName);
            return;
        }
    }

    private static string FormatState(Lira2UsdExchangeRate exchangeRate, State state)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Дата транзакции: {TurkeyDate.GetToday().ToString("dd.MM.yyyy")}");
        sb.AppendLine($"Комментарий: {state.Comment}");
        sb.Append($"Сумма: {state.SumTotal.ToString("F2")} {state.Currency}");
        if (state.Currency.Equals(CurrencyNames.Try.Name))
        {
            var usdValue = CurrencyConverter.ConvertToUsdIfNeed(state.SumTotal, state.Currency, exchangeRate);
            sb.Append($" (~{usdValue.ToString("F2")} {CurrencyNames.Usd.Text})");
        }

        sb.AppendLine();
        return sb.ToString();
    }

    private class State
    {
        public string WaitAction { get; private set; }

        public string Currency { get; private set; }

        public string Comment { get; private set; }

        public decimal SumTotal { get; private set; }

        public void UpdateWaitAction(string waitAction) => WaitAction = waitAction;

        public void UpdateCurrency(string currency) => Currency = currency;

        public void UpdateComment(string comment) => Comment = comment;

        public void UpdateSumTotal(decimal sumTotal) => SumTotal = sumTotal;

        public bool CanSave() => SumTotal != 0
                                 && !string.IsNullOrEmpty(Comment)
                                 && !string.IsNullOrEmpty(Currency);

        public static class WaitActions
        {
            // Комментарий
            public const string ChooseComment = "choose_comment";

            // Валюта
            public const string ChooseCurrency = "choose_currency";

            // Сумма на всех поровну
            public const string AddSumTotal = "add_sum_total";

            // Подтвердить сохранение
            public const string ConfirmSave = "confirm_Save";
        }
    }
}
