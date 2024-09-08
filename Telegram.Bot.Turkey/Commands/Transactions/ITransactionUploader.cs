namespace Telegram.Bot.Turkey.Commands.Transactions;

public interface ITransactionUploader
{
    Task<int> InsertOneAsync(TransactionDto transaction, CancellationToken cancellationToken);
}
