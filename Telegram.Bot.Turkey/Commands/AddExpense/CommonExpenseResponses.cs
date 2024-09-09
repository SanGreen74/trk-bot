using Telegram.Bot.Turkey.Commands.Transactions;
using Telegram.Bot.Turkey.Sheets.BotConfiguration.Models;
using Telegram.Bot.Types.Enums;

namespace Telegram.Bot.Turkey.Commands.AddExpense;

public static class CommonExpenseResponses
{
    public static async Task ReplyConfigurationDamagedAsync(this ITelegramBotClient botClient, long chatId,
        CancellationToken ct)
    {
        await botClient.SendTextMessageAsync(chatId, "Проблема с получением конфигурации я хуй знает че случилось",
            cancellationToken: ct);
    }

    public static async Task ReplySessionExpiredAsync(this ITelegramBotClient botClient, long chatId,
        CancellationToken ct)
    {
        await botClient.SendTextMessageAsync(chatId, "Cессия истекла или что-то пошло не так. Начните сначала",
            cancellationToken: ct);
    }

    public static async Task ReplyNoAccessToAddExpenses(this ITelegramBotClient botClient, long chatId, string userName,
        CancellationToken ct)
    {
        await botClient
            .SendTextMessageAsync(chatId, $"Пользователь {userName} не может добавлять расходы", cancellationToken: ct);
    }
    
    public static async Task ReplyTransactionWasAppended(this ITelegramBotClient botClient, long chatId, int rowNumber,
        CancellationToken ct)
    {
        var text = ExpenseTexts.TransactionWasAppended(rowNumber);
        await botClient
            .SendTextMessageAsync(chatId, text, parseMode: ParseMode.MarkdownV2, disableWebPagePreview: true, cancellationToken: ct);
    }

    public static async Task ReplyBeginSaveTransaction(this ITelegramBotClient botClient, long chatId, TransactionDto transactionDto, BotConfigurationDto configuration, CancellationToken ct)
    {
        await botClient
            .SendTextMessageAsync(chatId, ExpenseTexts.FormatTransactionTextMessage(transactionDto, configuration),
                cancellationToken: ct);
    }
}