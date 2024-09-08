using Telegram.Bot.Turkey.Commands.Helpers;
using Telegram.Bot.Turkey.Sheets.BotConfiguration.Models;

namespace Telegram.Bot.Turkey.Commands.Transactions;

public static class CurrencyConverter
{
    public static decimal ConvertToUsdIfNeed(decimal value, string currency, Lira2UsdExchangeRate exchangeRate)
    {
        if (currency.Equals(CurrencyNames.Usd.Name, StringComparison.InvariantCultureIgnoreCase))
        {
            return value;
        }

        if (currency.Equals(CurrencyNames.Try.Name, StringComparison.InvariantCultureIgnoreCase))
        {
            return value / exchangeRate.ConversionRate!.Value;
        }
        
        throw new ArgumentOutOfRangeException(nameof(currency), currency, "Unsupported currency");
    }

}
