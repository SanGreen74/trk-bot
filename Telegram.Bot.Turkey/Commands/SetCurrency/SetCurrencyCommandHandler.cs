using System.Globalization;
using System.Text.RegularExpressions;
using Telegram.Bot.Turkey.Commands.Access;
using Telegram.Bot.Turkey.Sheets.BotConfiguration;
using Telegram.Bot.Turkey.Sheets.BotConfiguration.Models;
using Telegram.Bot.Turkey.State;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Telegram.Bot.Turkey.Commands.SetCurrency;

public class SetCurrencyCommandHandler : CommandHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly IBotConfigurationRepository _botConfigurationRepository;
    private readonly IUserAccessProvider _userAccessProvider;
    private static readonly Regex CurrencyRegex = new(@"(?<=1USD\s=\s)(\d{1,3}([.,]\d{2})?)TRY", RegexOptions.Compiled);
    
    public override required string CommandName { get; init; } = TgCommands.SetCurrency;

    public SetCurrencyCommandHandler(ITelegramBotClient botClient,
        IBotConfigurationRepository botConfigurationRepository, IUserSessionState sessionState,
        IUserAccessProvider userAccessProvider) : base(sessionState)
    {
        _botClient = botClient;
        _botConfigurationRepository = botConfigurationRepository;
        _userAccessProvider = userAccessProvider;
    }

    public override async Task StartHandle(Update update, CancellationToken ct)
    {
        if (update.Message!.Type != MessageType.Text || update.Message.Text == null)
            return;

        var chatId = update.Message.Chat.Id;
        var userName = update.Message.From?.Username;
        if (!await HasAccessToEditCurrencyAsync(userName))
        {
            await ReplyNotAccessToEditCurrencyAsync(chatId, ct);
            OnComplete(userName);
            return;
        }
        
        await _botClient
            .SendTextMessageAsync(chatId, "Введи курс валют в формате 1USD = 25,25TRY", cancellationToken: ct);
    }

    private async Task<bool> HasAccessToEditCurrencyAsync(string? userName)
    {
        return userName != null && await _userAccessProvider.IsAdminAsync(userName);
    }

    public override async Task HandleIntermediateMessage(Update update, CancellationToken ct)
    {
        if (update.Message!.Type != MessageType.Text || update.Message.Text == null)
            return;
        
        var chatId = update.Message.Chat.Id;
        var userName = update.Message.From?.Username;
        if (!await HasAccessToEditCurrencyAsync(userName))
        {
            await ReplyNotAccessToEditCurrencyAsync(chatId, ct);
            OnComplete(userName);
            return;
        }

        var currencyString = update.Message.Text.Replace(",", ".");
        var match = CurrencyRegex.Match(currencyString);
        if (!match.Success)
        {
            await _botClient
                .SendTextMessageAsync(chatId, "Не удалось распарсить значение из строки", cancellationToken: ct);
            OnComplete(userName);
            return;
        }

        var tryValueString = match.Groups[1].Value;
        var result = decimal.Parse(tryValueString, CultureInfo.InvariantCulture);
        
        var configurationDto = await _botConfigurationRepository.GetAsync(ct);
        if (configurationDto == null)
        {
            await _botClient
                .SendTextMessageAsync(chatId, "Проблемы с конфигом брат", cancellationToken: ct);
            OnComplete(userName);
            return;
        }

        configurationDto = configurationDto with
        {
            Usd2Lira2UsdExchangeRate = new Lira2UsdExchangeRate { ConversionRate = result }
        };
        await _botConfigurationRepository.SetAsync(configurationDto, ct);
        await _botClient
            .SendTextMessageAsync(chatId, $"Курс валют обновлен. Текущий курс 1USD = {result}TRY",
                cancellationToken: ct);
        OnComplete(userName);
    }

    private async Task ReplyNotAccessToEditCurrencyAsync(long chatId, CancellationToken ct)
    {
        await _botClient
            .SendTextMessageAsync(chatId, "Нет доступа к редактированию курса валюты", cancellationToken: ct);
    }
}
