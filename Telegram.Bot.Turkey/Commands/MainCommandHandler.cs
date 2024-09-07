using System.Diagnostics.CodeAnalysis;
using Telegram.Bot.Turkey.State;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Telegram.Bot.Turkey.Commands;

public class MainCommandHandler
{
    private readonly IUserSessionState _userSessionState;
    private readonly ITelegramBotClient _botClient;
    private readonly CommandHandler[] _commandHandlers;
    
    public MainCommandHandler(IUserSessionState userSessionState, ITelegramBotClient botClient, CommandHandler[] commandHandlers)
    {
        _userSessionState = userSessionState;
        _botClient = botClient;
        _commandHandlers = commandHandlers;
    }

    public async Task Handle(Update update, CancellationToken cancellationToken)
    {
        if (update is { Type: UpdateType.Message, Message: not null })
        {
            await HandleMessageAsync(update, cancellationToken);
            return;
        }

        if (update.CallbackQuery != null)
        {
            await HandleCallbackQueryAsync(update, cancellationToken);
        }
    }

    private async Task HandleCallbackQueryAsync(Update update, CancellationToken cancellationToken)
    {
        var userName = update.CallbackQuery!.From.Username;
        var chatId = update.CallbackQuery.Message!.Chat.Id;
        if (string.IsNullOrEmpty(userName))
        {
            await DeleteConnectedMessageAsync();
            await ReplyAuthorNotFoundAsync(chatId, cancellationToken);
            return;
        }

        var commandNameMaybe = _userSessionState.GetCommandName(userName);
        if (string.IsNullOrEmpty(commandNameMaybe))
        {
            await DeleteConnectedMessageAsync();
            await ReplyProcessingNotFoundAsync(chatId, cancellationToken);
            _userSessionState.Invalidate(userName);
            return;
        }

        await _commandHandlers.First(x =>
                string.Equals(x.CommandName, commandNameMaybe, StringComparison.CurrentCultureIgnoreCase))
            .HandleIntermediateMessage(update, cancellationToken);
        return;

        async Task DeleteConnectedMessageAsync()
        {
            await _botClient.DeleteMessageAsync(chatId, update.CallbackQuery.Message.MessageId,
                cancellationToken: cancellationToken);
        }
    }

    private async Task HandleMessageAsync(Update update, CancellationToken cancellationToken)
    {
        var chatId = update.Message!.Chat.Id;
        var messageText = update.Message?.Text;
        if (string.IsNullOrEmpty(messageText))
        {
            await ReplyIncorrectMessageAsync(chatId, cancellationToken);
            return;
        }

        var userName = update.Message?.From?.Username;
        if (string.IsNullOrEmpty(userName))
        {
            await ReplyAuthorNotFoundAsync(chatId, cancellationToken);
            return;
        }

        if (TryGetUserCommand(messageText, out var commandName))
        {
            await HandleStartCommandAsync(update, chatId, userName, commandName, cancellationToken);
            return;
        }

        var commandNameMaybe = _userSessionState.GetCommandName(userName);
        if (string.IsNullOrEmpty(commandNameMaybe))
        {
            await ReplyProcessingNotFoundAsync(chatId, cancellationToken);
            return;
        }

        var commandHandler = _commandHandlers.FirstOrDefault(x =>
            string.Equals(x.CommandName, commandNameMaybe, StringComparison.CurrentCultureIgnoreCase));
        if (commandHandler == null)
        {
            await ReplyCommandNotFoundAsync(chatId, commandNameMaybe, cancellationToken);
            return;
        }

        await commandHandler.HandleIntermediateMessage(update, cancellationToken);
    }

    private static bool TryGetUserCommand(string messageText, [NotNullWhen(true)] out string? commandName)
    {
        commandName = null;
        if (string.IsNullOrEmpty(messageText))
        {
            return false;
        }

        if (messageText.StartsWith("/"))
        {
            commandName = messageText;
            return true;
        }

        return TgCommands.Texts.TryGetCommand(messageText, out commandName);
    }

    private async Task HandleStartCommandAsync(Update update, long chatId, string userName, string commandName,
        CancellationToken cancellationToken)
    {
        _userSessionState.Invalidate(userName);
        _userSessionState.SetCommandName(userName, commandName);
        var commandHandler = _commandHandlers
            .FirstOrDefault(x => string.Equals(x.CommandName, commandName, StringComparison.CurrentCultureIgnoreCase));
        if (commandHandler == null)
        {
            await ReplyCommandNotFoundAsync(chatId, commandName, cancellationToken);
            return;
        }
        
        await commandHandler
            .StartHandle(update, cancellationToken);
    }

    private async Task ReplyAuthorNotFoundAsync(long chatId, CancellationToken cancellationToken)
    {
        await _botClient
            .SendTextMessageAsync(
                chatId: chatId,
                text: "Не удалось определить пользователя, который отправил сообщение :(",
                cancellationToken: cancellationToken);
    }

    private async Task ReplyProcessingNotFoundAsync(long chatId, CancellationToken cancellationToken)
    {
        await _botClient
            .SendTextMessageAsync(
                chatId: chatId,
                text: "Произошла ошибка. Начните сначала",
                cancellationToken: cancellationToken);
    }

    private async Task ReplyCommandNotFoundAsync(long chatId, string commandName, CancellationToken cancellationToken)
    {
        await _botClient
            .SendTextMessageAsync(
                chatId: chatId,
                text: $"Команда {commandName} не поддерживается",
                cancellationToken: cancellationToken);
    }

    private async Task ReplyIncorrectMessageAsync(long chatId, CancellationToken cancellationToken)
    {
        await _botClient
            .SendTextMessageAsync(
                chatId: chatId,
                text: $"Некорректное сообщение",
                cancellationToken: cancellationToken);
    }
}
