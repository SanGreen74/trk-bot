namespace Telegram.Bot.Turkey.Commands.Transactions;

public record TransactionParticipant
{
    /// <summary>
    /// Имя участника
    /// </summary>
    public required string TgName { get; init; }

    /// <summary>
    /// Сколько потратил
    /// </summary>
    public required decimal Amount { get; init; }
}