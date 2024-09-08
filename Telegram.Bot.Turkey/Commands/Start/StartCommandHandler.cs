using Telegram.Bot.Turkey.Sheets.BotConfiguration;
using Telegram.Bot.Turkey.State;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace Telegram.Bot.Turkey.Commands.Start;

public class StartCommandHandler : CommandHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly IBotConfigurationRepository _botConfigurationRepository;
    private readonly IUserSessionState _sessionState;
    public override required string CommandName { get; init; } = TgCommands.Start;

    public StartCommandHandler(ITelegramBotClient botClient, IBotConfigurationRepository botConfigurationRepository,  IUserSessionState sessionState) : base(sessionState)
    {
        _botClient = botClient;
        _botConfigurationRepository = botConfigurationRepository;
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
            [TgCommands.Texts.AddPersonalExpenseText],
            [TgCommands.Texts.AddCommonExpenseText],
            [TgCommands.Texts.GetExpensesText]
        ])
        {
            ResizeKeyboard = true // Изменение размера клавиатуры
        };

        var botConfigurationDto = await _botConfigurationRepository.GetAsync(ct);
        var whoIs = userName != null 
            ? botConfigurationDto?.Participants.FirstOrDefault(x => x.TgName.Equals(userName, StringComparison.InvariantCultureIgnoreCase))?.Name
            : null;
        var text = whoIs != null
            ? $"Добро пожаловать, {whoIs}"
            : "Добро пожаловать";
        
        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: text,
            replyMarkup: replyKeyboard, cancellationToken: ct);
        
        OnComplete(userName);
    }

    public override Task HandleIntermediateMessage(Update update, CancellationToken ct)
    {
        throw new NotImplementedException();
    }
}
