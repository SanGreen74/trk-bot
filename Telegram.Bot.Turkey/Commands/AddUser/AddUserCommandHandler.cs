using Telegram.Bot.Turkey.State;
using Telegram.Bot.Types;
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

    public override async Task HandleIntermediateMessage(Update update, CancellationToken cancellationToken)
    {
        if (update.CallbackQuery != null)
        {
            var callbackData = update.CallbackQuery.Data; // todo validate
            var chatId = update.CallbackQuery.Message.Chat.Id;
            var userName = update.CallbackQuery.From.Username;

            var state = new State { Name = callbackData };
            _userSessionState.SetState(userName, state);
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "Введите логин пользователя в tg в формате @sangreen74",
                cancellationToken: cancellationToken
            );
        }

        if (update.Message != null)
        {
            var messageText = update.Message.Text; 
            var chatId = update.Message.Chat.Id;
            var userName = update.Message.From.Username;
            var state = _userSessionState.GetState<State>(userName);
            if (state == null)
            {
                // TODO Validation
                return;
            }
            
            // TODO Update users in s3
            
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: $"Пользователь {state.Name} с логином {messageText} добавлен в список участников",
                cancellationToken: cancellationToken
            );
        }
    }

    private class State
    {
        public required string Name { get; init; }
        
        public string? Login { get; init; }
    }
}