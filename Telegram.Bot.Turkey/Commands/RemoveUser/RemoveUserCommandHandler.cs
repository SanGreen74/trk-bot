using Telegram.Bot.Turkey.Sheets.BotConfiguration;
using Telegram.Bot.Turkey.State;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Telegram.Bot.Turkey.Commands.RemoveUser;

public class RemoveUserCommandHandler : CommandHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly IBotConfigurationRepository _configurationRepository;
    public override required string CommandName { get; init; } = TgCommands.RemoveUser;

    public RemoveUserCommandHandler(ITelegramBotClient botClient, IBotConfigurationRepository configurationRepository,
        IUserSessionState sessionState) : base(sessionState)
    {
        _botClient = botClient;
        _configurationRepository = configurationRepository;
    }

    public override async Task StartHandle(Update update, CancellationToken ct)
    {
        var configuration = await _configurationRepository.GetAsync(ct);
        if (configuration == null)
        {
            await _botClient.SendTextMessageAsync(update.Message!.Chat.Id, "Проблемы с конфигом!", cancellationToken: ct);
            if (update.Message.From?.Username != null)
            {
                OnComplete(update.Message.From.Username);
            }

            return;
        }

        var inlineKeyboardButtons = configuration.Participants
            .Select(x => InlineKeyboardButton.WithCallbackData($"{x.Name}-{x.TgName}", x.TgName))
            .ToArray();
        var chatId = update.Message!.Chat.Id;

        var buttons = new InlineKeyboardMarkup([inlineKeyboardButtons]);

        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "Выберите пользователя для удаления:",
            replyMarkup: buttons, 
            cancellationToken: ct);
    }

    public override async Task HandleIntermediateMessage(Update update, CancellationToken ct)
    {
        if (update is { Type: UpdateType.CallbackQuery, CallbackQuery: not null })
        {
            await HandleCallbackQueryAsync(update.CallbackQuery, ct);
            return;
        }
    }

    private async Task HandleCallbackQueryAsync(CallbackQuery query, CancellationToken ct)
    {
        var callbackData = query.Data;
        if (callbackData == null)
        {
            await AnswerExceptionAsync(query.Id, "Callback data not found", ct);
            OnCompleteIfPossible();
            return;
        }

        var configurationDto = await _configurationRepository.GetAsync(ct);
        if (configurationDto == null)
        {
            await AnswerExceptionAsync(query.Id, "Не найдена конфигурация", ct);
            OnCompleteIfPossible();
            return;
        }

        configurationDto = configurationDto with
        {
            Participants = configurationDto.Participants
                .Where(x => !x.TgName.Equals(callbackData))
                .ToList()
        };
        
        await _configurationRepository.SetAsync(configurationDto, ct);
        if (query.Message?.Chat.Id != null && query.Message?.MessageId != null)
        {
            var deleteTask =
                _botClient.DeleteMessageAsync(query.Message.Chat.Id, query.Message.MessageId, cancellationToken: ct);
            var sendTextMessageTask = _botClient.SendTextMessageAsync(query.Message.Chat.Id,
                $"Пользователь {callbackData} удален", cancellationToken: ct);
            await Task.WhenAll(deleteTask, sendTextMessageTask);
        }
        
        OnCompleteIfPossible();
        return;

        void OnCompleteIfPossible()
        {
            if (query.Message?.From?.Username != null)
            {
                OnComplete(query.Message.From.Username);
            }
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
}
