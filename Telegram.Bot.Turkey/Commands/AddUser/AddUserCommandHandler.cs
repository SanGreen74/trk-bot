using Telegram.Bot.Turkey.State;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Telegram.Bot.Turkey.Commands.AddUser;

public class AddUserCommandHandler : CommandHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly IUserSessionState _userSessionState;
    public override required string CommandName { get; init; } = TgCommands.AddUser;

    public AddUserCommandHandler(ITelegramBotClient botClient, IUserSessionState userSessionState)
    {
        _botClient = botClient;
        _userSessionState = userSessionState;
    }
    
    public override async Task StartHandle(Update update, CancellationToken cancellationToken)
    {
        // TODO Get users from s3
        var users = new[] { "Первый", "Второй", "Третий" };
        var chatId = update.Message!.Chat.Id; // Todo check
        var userName = update.Message!.From!.Username; // Todo check

        var buttons = new InlineKeyboardMarkup([
            [InlineKeyboardButton.WithCallbackData("Пользователь 1", "user1")],
            [InlineKeyboardButton.WithCallbackData("Пользователь 2", "user2")],
            [InlineKeyboardButton.WithCallbackData("Пользователь 3", "user3")]
        ]);

        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "Выберите пользователя:",
            replyMarkup: buttons, 
            cancellationToken: cancellationToken);
    }

    public override async Task HandleIntermediateMessage(Update update, CancellationToken ct)
    {
        if (update is { Type: UpdateType.CallbackQuery, CallbackQuery: not null })
        {
            await HandleCallbackQueryAsync(update.CallbackQuery, ct);
            return;
        }

        if (update is { Type: UpdateType.Message, Message.Text: not null })
        {
            await HandleMessageAsync(update.Message, ct);
        }
    }

    private async Task HandleMessageAsync(Message update, CancellationToken ct)
    {
        var messageText = update.Text; 
        var chatId = update.Chat.Id;
        var userName = update.From!.Username!;
        var state = _userSessionState.GetState<State>(userName);
        if (state == null)
        {
            await _botClient.SendTextMessageAsync(chatId, "Сессия истекла, начните сначала", cancellationToken: ct);
            return;
        }
            
        // TODO Update users in s3
            
        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: $"Пользователь {state.Name} с логином {messageText} добавлен в список участников",
            cancellationToken: ct
        );

        _userSessionState.Invalidate(userName);
    }

    private async Task HandleCallbackQueryAsync(CallbackQuery query, CancellationToken ct)
    {
        var callbackData = query.Data;
        var queryId = query.Id;
        if (string.IsNullOrEmpty(callbackData))
        {
            await AnswerExceptionAsync(queryId, "Что-то пошло не так, callbackData not found", ct);
            return;
        }
        var userName = query.From.Username;
        if (string.IsNullOrEmpty(userName))
        {
            await AnswerExceptionAsync(queryId, "Что-то пошло не так, userName is null", ct);
            return;
        }
        var state = new State { Name = callbackData };
        _userSessionState.SetState(userName, state);

        var callbackAnswerText = $"Введите для {callbackData} логин пользователя в tg в формате @sangreen74";
        if (query.Message?.Chat.Id != null)
        {
            var chatId = query.Message.Chat.Id;
            await _botClient.DeleteMessageAsync(chatId, query.Message.MessageId, cancellationToken: ct);
            await _botClient.SendTextMessageAsync(chatId, callbackAnswerText, cancellationToken: ct);
        }
        else
        {
            await _botClient.AnswerCallbackQueryAsync(queryId, callbackAnswerText, cancellationToken: ct);
        }
    }

    private async Task AnswerExceptionAsync(string callbackQueryId, string text, CancellationToken cancellationToken)
    {
        await _botClient
            .AnswerCallbackQueryAsync(
                callbackQueryId,
                text,
                showAlert: true,
                cancellationToken: cancellationToken);
    }

    private class State
    {
        public required string Name { get; init; }
    }
}