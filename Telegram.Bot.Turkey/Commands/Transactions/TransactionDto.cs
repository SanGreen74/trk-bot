namespace Telegram.Bot.Turkey.Commands.Transactions;

public record TransactionDto
{
    /// <summary>
    /// Дата транзакции
    /// </summary>
    public required DateOnly Date { get; init; }

    /// <summary>
    /// Комментарий
    /// </summary>
    public required string Comment { get; init; }

    /// <summary>
    /// Имя того кто заплатил
    /// </summary>
    public required string WhoPaidTgName { get; init; }

    /// <summary>
    /// Валюта
    /// </summary>
    public required string CurrencyType { get; init; }
    
    /// <summary>
    /// Список участников транзакции
    /// </summary>
    public TransactionParticipant[] Participants { get; init; }
}