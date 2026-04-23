using System.Text.Json;
using DocumentProcessing.Application.Messaging;
using DocumentProcessing.Domain.Entities;

namespace DocumentProcessing.Application.Outbox;

public static class OutboxMessageFactory
{
    public const string MessageType = "process-document-job";
    
    public static OutboxMessage Create(ProcessDocumentJobMessage jobMessage)
    {
        ArgumentNullException.ThrowIfNull(jobMessage);

        var content = JsonSerializer.Serialize(jobMessage);
        return OutboxMessage.Create(MessageType, content);
    }
}