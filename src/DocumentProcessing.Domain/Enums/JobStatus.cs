namespace DocumentProcessing.Domain.Enums;

public enum JobStatus
{
    // created, not yet handed off
    Pending,
    // accepted and queue publication succeeded
    Queued,
    // worker has started
    Processing,
    // analysis finished successfully
    Completed,
    // processing failed
    Failed
}