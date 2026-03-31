using DocumentProcessing.Application.Messaging;

namespace DocumentProcessing.Application.Interfaces;

public interface IJobMessagePublisher
{
    Task PublishAsync(ProcessDocumentJobMessage message, CancellationToken cancellationToken = default);
}