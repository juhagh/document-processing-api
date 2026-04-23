namespace DocumentProcessing.Application.Interfaces;

public interface IUnitOfWork
{
    IJobRepository JobRepository{ get; }
    IOutboxRepository OutboxRepository { get; }
    Task<int> CommitAsync(CancellationToken cancellationToken);
}