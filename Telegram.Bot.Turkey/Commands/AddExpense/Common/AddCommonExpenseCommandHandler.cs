using System.Globalization;
using System.Text;
using Telegram.Bot.Turkey.Commands.Access;
using Telegram.Bot.Turkey.Commands.Helpers;
using Telegram.Bot.Turkey.Commands.Transactions;
using Telegram.Bot.Turkey.Sheets.BotConfiguration;
using Telegram.Bot.Turkey.State;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Telegram.Bot.Turkey.Commands.AddExpense.Common;

public class AddCommonExpenseCommandHandler : CommandHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly IBotConfigurationRepository _configurationRepository;
    private readonly IUserAccessProvider _userAccessProvider;
    private readonly IUserSessionState _sessionState;
    private readonly ITransactionUploader _transactionUploader;
    public override required string CommandName { get; init; } = TgCommands.AddCommonExpense;

    public AddCommonExpenseCommandHandler(ITelegramBotClient botClient,
        IBotConfigurationRepository configurationRepository, IUserAccessProvider userAccessProvider,
        IUserSessionState sessionState, ITransactionUploader transactionUploader) : base(sessionState)
    {
        _botClient = botClient;
        _configurationRepository = configurationRepository;
        _userAccessProvider = userAccessProvider;
        _sessionState = sessionState;
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
            .SendTextMessageAsync(chatId, ExpenseTexts.InputComment, cancellationToken: ct);
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

        // AddParticipantAmount
        if (state.WaitAction == State.WaitActions.AddParticipantAmount)
        {
            var replaced = messageText.Replace(",", ".");
            var value = decimal.Parse(replaced, CultureInfo.InvariantCulture);
                
            state.Participants.Last().Amount = value;
            state.UpdateWaitAction(State.WaitActions.AddParticipant);
            _sessionState.SetState(userName, state);
                
            var inlineKeyboardButtons = configuration.Participants
                .Where(x => !state.Participants.Any(y => y.TgName.Equals(x.TgName)))
                .Select(x => InlineKeyboardButton.WithCallbackData(x.Name, x.TgName))
                .ToArray();
            var saveButton = new[] { InlineKeyboardButton.WithCallbackData("Сохранить", "confirm_save") };
            var inlineKeyboardMarkup = inlineKeyboardButtons.Length > 0
                ? new InlineKeyboardMarkup([inlineKeyboardButtons, saveButton])
                : new InlineKeyboardMarkup([saveButton]);

            var text = inlineKeyboardButtons.Length > 0
                ? "Выбери кого еще добавить в счет?"
                : "Сохранить транзакцию?";
            await _botClient.SendTextMessageAsync(chatId, text, replyMarkup: inlineKeyboardMarkup,
                cancellationToken: ct);
            return;
        }

        // AddParticipantAmountTotal
        if (state.WaitAction == State.WaitActions.AddParticipantAmountTotal)
        {
            var replaced = messageText.Replace(",", ".");
            var value = decimal.Parse(replaced, CultureInfo.InvariantCulture);
            var amountPerUser = value / configuration.Participants.Count;
            foreach (var participant in configuration.Participants)
            {
                state.AddParticipant(new State.Participant { TgName = participant.TgName, Amount = amountPerUser });
            }
                
            state.UpdateWaitAction(State.WaitActions.AddParticipant); // hack
            _sessionState.SetState(userName, state);

            var saveButton = new[] { InlineKeyboardButton.WithCallbackData("Сохранить", "confirm_save") };
            var inlineKeyboardMarkup = new InlineKeyboardMarkup(saveButton);
            await _botClient.SendTextMessageAsync(chatId, FormatParticipantsTextMessage(state), cancellationToken: ct);    
            await _botClient.SendTextMessageAsync(chatId, "Сохранить транзакцию?", replyMarkup: inlineKeyboardMarkup,
                cancellationToken: ct);
            return;
        }
    }

    private async Task HandleCallBackQueryAsync(long chatId, string userName, string queryData, int messageId, CancellationToken ct)
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
            state.UpdateWaitAction(State.WaitActions.ChooseWhoPaid);
            _sessionState.SetState(userName, state);
            
            var chosenCurrency = queryData switch
            {
                CurrencyNames.Try.Name => CurrencyNames.Try.Text,
                CurrencyNames.Usd.Name => CurrencyNames.Usd.Text,
                _ => throw new ArgumentOutOfRangeException(nameof(queryData), queryData, "Unexpected currency name")
            };
            
            var inlineKeyboardButtons = configuration.Participants
                .Select(x => InlineKeyboardButton.WithCallbackData(x.Name, x.TgName))
                .ToArray();
            var inlineKeyboardMarkup = new InlineKeyboardMarkup(inlineKeyboardButtons);
            
            var task1 = _botClient.SendTextMessageAsync(chatId, "Выбери того, кто оплатил весь счет",
                replyMarkup: inlineKeyboardMarkup, cancellationToken: ct);
            
            var task2 = _botClient.EditMessageTextAsync(chatId, messageId, $"Вы выбрали валюту: {chosenCurrency}",
                cancellationToken: ct);

            await Task.WhenAll(task1, task2);
            return;
        }

        // ChooseWhoPaid
        if (state.WaitAction == State.WaitActions.ChooseWhoPaid)
        {
            state.UpdateWaitAction(State.WaitActions.AddParticipant);
            state.UpdateWhoPaidTg(queryData);
            _sessionState.SetState(userName, state);

            var inlineKeyboardButtons = configuration.Participants
                .Select(x => InlineKeyboardButton.WithCallbackData(x.Name, x.TgName))
                .ToArray();
            var allButton = new[]
                { InlineKeyboardButton.WithCallbackData("Разделить на всех поровну", "На всех") };
            var inlineKeyboardMarkup = new InlineKeyboardMarkup([inlineKeyboardButtons, allButton]);
                
            var task1 = _botClient.SendTextMessageAsync(chatId, "Выбери по очереди с кем делится счет, а затем укажи их долг/сумму",
                replyMarkup: inlineKeyboardMarkup, cancellationToken: ct);
            var task2 = _botClient.EditMessageTextAsync(chatId, messageId, $"Счет оплатил: {queryData}",
                cancellationToken: ct);
            
            await Task.WhenAll(task1, task2);
            return;
        }
            
        // AddParticipant
        if (state.WaitAction == State.WaitActions.AddParticipant)
        {
            if (queryData == "На всех")
            {
                state.UpdateWaitAction(State.WaitActions.AddParticipantAmountTotal);
                _sessionState.SetState(userName, state);

                var task1 = _botClient
                    .SendTextMessageAsync(chatId, "Введи общую сумму чека, которая будет поровну разделена",
                        cancellationToken: ct);
                var task2 = _botClient
                    .EditMessageTextAsync(chatId, messageId, "На кого разделить чек: На всех поровну", cancellationToken: ct);
                await Task.WhenAll(task1, task2);
                return;
            }

            if (queryData == "confirm_save")
            {
                if (!state.CanSave())
                {
                    await _botClient
                        .SendTextMessageAsync(chatId, "Братан, ет не удалось сохранить. Давай еще раз",
                            cancellationToken: ct);
                    OnComplete(userName);
                    return;
                }
                
                var deleteConfirmationTask = _botClient
                    .DeleteMessageAsync(chatId, messageId, cancellationToken: ct);

                var transactionDto = new TransactionDto
                {
                    Comment = state.Comment,
                    Date = TurkeyDate.GetToday(),
                    CurrencyType = state.Currency,
                    WhoPaidTgName = state.WhoPaidTg,
                    Participants = state.Participants
                        .Select(x => new TransactionParticipant
                        {
                            TgName = x.TgName,
                            Amount = x.Amount
                        })
                        .ToArray()
                };

                var approvedTask = _botClient.ReplyBeginSaveTransaction(chatId, transactionDto, configuration, ct);
                await Task.WhenAll(deleteConfirmationTask, approvedTask);
                var insertedIndex = await _transactionUploader.InsertOneAsync(transactionDto, cancellationToken: ct);
                await _botClient.ReplyTransactionWasAppended(chatId, insertedIndex, ct);
                OnComplete(userName);
                return;
            }
                
            state.UpdateWaitAction(State.WaitActions.AddParticipantAmount);
            state.AddParticipant(new State.Participant { TgName = queryData });
            _sessionState.SetState(userName, state);
                
            var participant = configuration.Participants.First(x => x.TgName.Equals(queryData));
            var participantsTextMessage = FormatParticipantsTextMessage(state);
            var task_1 = _botClient
                .SendTextMessageAsync(chatId, $"Введи сумму, которую потратил(a) {participant.Name}",
                    cancellationToken: ct);
            var task_2 = _botClient.EditMessageTextAsync(chatId, messageId, participantsTextMessage, cancellationToken: ct);
            await Task.WhenAll(task_1, task_2);
            return;
        }
    }

    private static string FormatParticipantsTextMessage(State state)
    {
        var sb = new StringBuilder();
        sb.AppendLine("На кого делится счет:");
        foreach (var participant in state.Participants)
        {
            sb.AppendLine($"— @{participant.TgName}, {participant.Amount.ToString("F2")} {state.Currency}");
        }

        sb.AppendLine($"Итого: {state.Participants.Sum(x => x.Amount).ToString("F2")}");
        return sb.ToString();
    }

    private class State
    {
        public string WaitAction { get; private set; }
        
        public string Currency { get; private set; }
        
        public string WhoPaidTg { get; private set; }

        public string Comment { get; private set; }

        public List<Participant> Participants { get; private set; } = new();
        
        public void UpdateWaitAction(string waitAction) => WaitAction = waitAction;
        
        public void UpdateCurrency(string currency) => Currency = currency;
        
        public void UpdateWhoPaidTg(string whoPaidTg) => WhoPaidTg = whoPaidTg;
        
        public void UpdateComment(string comment) => Comment = comment;
        
        public void AddParticipant(Participant participant) => Participants.Add(participant);
        
        public bool CanSave() => Participants.Count > 0 
                                 && !string.IsNullOrEmpty(WhoPaidTg)
                                 && !string.IsNullOrEmpty(Comment)
                                 && !string.IsNullOrEmpty(Currency);
        
        public class Participant
        {
            public string TgName { get; set; }
            
            public decimal Amount { get; set; }
        }

        public static class WaitActions
        {
            // Кто заплатил
            public const string ChooseWhoPaid = "choose_who_paid_tg";
            
            // Комментарий
            public const string ChooseComment = "choose_comment";
            
            // Валюта
            public const string ChooseCurrency = "choose_currency";
            
            // Выбрать человека на кого делится счет
            public const string AddParticipant = "add_participant";
            
            // Сумма на человека
            public const string AddParticipantAmount = "add_participant_amount";

            // Сумма на всех поровну
            public const string AddParticipantAmountTotal = "add_participant_amount_total";
        }
    }
}
