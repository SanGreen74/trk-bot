
namespace Telegram.Bot.Turkey.Commands.Transactions;

public record TransactionDto
{
    public required DateOnly Date { get; init; }

    public required string Comment { get; init; }

    public required string WhoPaidTgName { get; init; }

    public required string CurrencyType { get; init; }
    
    public TransactionParticipant[] Participants { get; init; }
}

public record TransactionParticipant
{
    public required string TgName { get; init; }

    public required decimal Amount { get; init; }
}

public class TransactionUploader
{
    public async Task InsertOneAsync(TransactionDto transaction, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
