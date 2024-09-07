using Telegram.Bot.Turkey.State;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace Telegram.Bot.Turkey.Commands.Start;

public class StartCommandHandler : CommandHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly IUserSessionState _sessionState;
    public override required string CommandName { get; init; } = TgCommands.Start;

    public StartCommandHandler(ITelegramBotClient botClient, IUserSessionState sessionState) : base(sessionState)
    {
        _botClient = botClient;
        _sessionState = sessionState;
    }
    
    public override async Task StartHandle(Update update, CancellationToken ct)
    {
        var message = update.Message!;
        var chatId = message.Chat.Id;
        var userName = message.From!.Username;

        if (!string.IsNullOrEmpty(userName))
        {
            _sessionState.Invalidate(userName);
        }
        
        var replyKeyboard = new ReplyKeyboardMarkup([
            ["Добавить личный расход"],
            ["Добавить общий расход"]
        ])
        {
            ResizeKeyboard = true // Изменение размера клавиатуры
        };

        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "Добро пожаловать", // TODO Welcome who
            replyMarkup: replyKeyboard, cancellationToken: ct);
        
        if (!string.IsNullOrEmpty(userName))
        {
            OnComplete(userName);
        }
    }

    public override Task HandleIntermediateMessage(Update update, CancellationToken ct)
    {
        throw new NotImplementedException();
    }
}
