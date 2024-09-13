using System.Text;
using Telegram.Bot.Turkey.Commands.Helpers;
using Telegram.Bot.Turkey.Commands.Transactions;
using Telegram.Bot.Turkey.Sheets.BotConfiguration.Models;

namespace Telegram.Bot.Turkey.Commands.AddExpense;

public static class ExpenseTexts
{
    public const string InputComment = "Введи комментарий покупки (например: ресторан/АЗС/...)";
    public const string InputPersonalComment = "Введи комментарий личной покупки (например: ресторан/АЗС/...)";
    public const string ChooseCurrency = "Выберите валюту операции";

    public static string TransactionWasAppended(int rowNumber)
    {
        return
            $"Транзакция успешно добавлена в [таблицу](https://docs.google.com/spreadsheets/d/1rYMbIKz_8lBW0_usvkQJmc46TclYfXm47OVKnw1rX6I/edit?gid=0#gid=0) в строчку №{rowNumber}";
    }
    
    public static string FormatTransactionTextMessage(TransactionDto transaction, BotConfigurationDto configuration)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Сохраняю транзакцию:");
        sb.AppendLine($"Дата: {transaction.Date.ToString("dd.MM.yyyy")}");
        sb.AppendLine($"Заплатил за чек: @{transaction.WhoPaidTgName}");
        sb.AppendLine($"Комментарий: {transaction.Comment}");
        foreach (var participant in transaction.Participants)
        {
            sb.Append($"— @{participant.TgName}: {participant.Amount.ToString("F2")} {transaction.CurrencyType}");
            if (transaction.CurrencyType.Equals(CurrencyNames.Try.Name))
            {
                var usdValue = CurrencyConverter.ConvertToUsdIfNeed(participant.Amount, transaction.CurrencyType,
                    configuration.Usd2Lira2UsdExchangeRate!);
                sb.Append($" (~{usdValue.ToString("F2")} {CurrencyNames.Usd.Text})");
            }
            sb.AppendLine();
        }

        var totalPrice = transaction.Participants.Sum(x => x.Amount);
        sb.Append($"Общая стоимость: {totalPrice.ToString("F2")} {transaction.CurrencyType}");
        
        if (transaction.CurrencyType.Equals(CurrencyNames.Try.Name))
        {
            var usdValue = CurrencyConverter.ConvertToUsdIfNeed(totalPrice, transaction.CurrencyType,
                configuration.Usd2Lira2UsdExchangeRate!);
            sb.Append($" (~{usdValue.ToString("F2")} {CurrencyNames.Usd.Text})");
        }

        sb.AppendLine();
        
        return sb.ToString();
    }

}
