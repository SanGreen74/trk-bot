using System.Diagnostics.CodeAnalysis;

namespace Telegram.Bot.Turkey.Commands;

public static class TgCommands
{
    public const string AddUser = "/add_user";
    public const string RemoveUser = "/remove_user";
    public const string SetCurrency = "/set_currency";
    
    public const string AddPersonalExpense = "/personal_expense";
    public const string AddCommonExpense = "/common_expense";
    public const string GetExpenses = "/get_expenses";
    public const string Start = "/start";

    public static class Texts
    {
        public const string AddPersonalExpenseText = "Добавить мой расход";
        public const string AddCommonExpenseText = "Добавить общий расход";
        public const string GetExpensesText = "Посмотреть расходы";
        

        private static readonly IReadOnlyDictionary<string, string> Text2Command = new Dictionary<string, string>
        {
            [AddPersonalExpenseText] = AddPersonalExpense,
            [AddCommonExpenseText] = AddCommonExpense,
            [GetExpensesText] = GetExpenses,
        };

        public static bool TryGetCommand(string text, [NotNullWhen(true)] out string? command)
        {
            command = null;
            return Text2Command.TryGetValue(text, out command);
        }
    }
}
