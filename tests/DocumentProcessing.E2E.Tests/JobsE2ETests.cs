using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using DocumentProcessing.Api.Contracts;
using DocumentProcessing.Domain.Enums;

namespace DocumentProcessing.E2E.Tests;

[Trait("Category", "E2E")]
public class JobsE2ETests
{
    private const string JobsRoute = "/api/jobs";
    private const string BaseUrl = "http://localhost:8080";
    
    private readonly HttpClient _client;
    
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };
    
    public JobsE2ETests()
    {
        _client = new HttpClient { BaseAddress = new Uri(BaseUrl) };
    }
    
    [Fact]
    public async Task FullJobLifecycle_WithValidInput_EventuallyCompletesWithResults()
    {
        // Arrange
        var request = new CreateJobRequest
        {
            InputText = "This is a full end to end test.\nIt spans multiple lines.\nThe worker should process this successfully."
        };
        
        // Act — submit job
        var createResponse = await _client.PostAsJsonAsync(JobsRoute, request);
        Assert.Equal(HttpStatusCode.Accepted, createResponse.StatusCode);

        var createdJob = await createResponse.Content.ReadFromJsonAsync<JobResponse>(JsonOptions);
        Assert.NotNull(createdJob);
        Assert.Equal(JobStatus.Queued, createdJob.Status);

        // Act — wait for completion
        var completedJob = await WaitForJobStatusAsync(
            createdJob.Id,
            JobStatus.Completed,
            TimeSpan.FromSeconds(15));

        // Assert
        Assert.Equal(JobStatus.Completed, completedJob.Status);
        Assert.NotNull(completedJob.WordCount);
        Assert.NotNull(completedJob.CharacterCount);
        Assert.NotNull(completedJob.LineCount);
        Assert.NotNull(completedJob.Summary);
        Assert.NotNull(completedJob.CompletedAtUtc);
        Assert.Null(completedJob.ErrorMessage);
    }
    
    [Fact]
    public async Task FullJobLifecycle_WithTriggerFailure_EventuallyReturnsFailed()
    {
        // Arrange
        var request = new CreateJobRequest
        {
            InputText = "TRIGGER_FAILURE"
        };

        // Act — submit job
        var createResponse = await _client.PostAsJsonAsync(JobsRoute, request);
        Assert.Equal(HttpStatusCode.Accepted, createResponse.StatusCode);

        var createdJob = await createResponse.Content.ReadFromJsonAsync<JobResponse>(JsonOptions);
        Assert.NotNull(createdJob);

        // Act — wait for failure
        var failedJob = await WaitForJobStatusAsync(
            createdJob.Id,
            JobStatus.Failed,
            TimeSpan.FromSeconds(15));

        // Assert
        Assert.Equal(JobStatus.Failed, failedJob.Status);
        Assert.False(string.IsNullOrWhiteSpace(failedJob.ErrorMessage));
        Assert.Null(failedJob.CompletedAtUtc);
    }
    
    private async Task<JobResponse> WaitForJobStatusAsync(
        int jobId,
        JobStatus expectedStatus,
        TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        JobResponse? lastSeenJob = null;

        while (DateTime.UtcNow < deadline)
        {
            var response = await _client.GetAsync($"{JobsRoute}/{jobId}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var job = await response.Content.ReadFromJsonAsync<JobResponse>(JsonOptions);
            Assert.NotNull(job);

            lastSeenJob = job;

            if (job.Status == expectedStatus)
                return job;

            var isTerminal =
                job.Status == JobStatus.Completed ||
                job.Status == JobStatus.Failed;

            if (isTerminal && job.Status != expectedStatus)
            {
                throw new Xunit.Sdk.XunitException(
                    $"Job {jobId} reached terminal status {job.Status} " +
                    $"but expected {expectedStatus}. " +
                    $"ErrorMessage: {job.ErrorMessage ?? "<null>"}");
            }

            await Task.Delay(500);
        }

        throw new TimeoutException(
            $"Job {jobId} did not reach status {expectedStatus} within {timeout.TotalSeconds}s. " +
            $"Last observed status: {lastSeenJob?.Status.ToString() ?? "<none>"}");
    }
}
    