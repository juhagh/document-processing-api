using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using DocumentProcessing.Api.Contracts;
using DocumentProcessing.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Testing;

namespace DocumentProcessing.Api.Tests;

public class JobsControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string JobsRoute = "/api/jobs";
    private readonly HttpClient _client;
    
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };
    
    public JobsControllerTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateDocumentJob_WithValidInput_ReturnsAcceptedJob()
    {
        var request = new CreateJobRequest
        {
            InputText = "Test Input"
        };

        var response = await _client.PostAsJsonAsync(JobsRoute, request);
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        
        var jobResponse = await response.Content.ReadFromJsonAsync<JobResponse>(JsonOptions);
        Assert.NotNull(jobResponse);
        Assert.Equal("Test Input", jobResponse.InputText);
        Assert.Equal(JobStatus.Queued, jobResponse.Status);
        Assert.NotEqual(Guid.Empty, jobResponse.Id);
        Assert.Equal($"/api/jobs/{jobResponse.Id}", response.Headers.Location?.PathAndQuery);
    }

    [Fact]
    public async Task GetJobById_WhenJobExists_ReturnsJob()
    {
        var (_, job) = await CreateJobAndReadResponseAsync();
        Assert.NotNull(job);
        
        var response = await _client.GetAsync($"{JobsRoute}/{job.Id}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var jobDb = await response.Content.ReadFromJsonAsync<JobResponse>(JsonOptions);
        Assert.NotNull(jobDb);
        Assert.Equal(job.Id, jobDb.Id);
        Assert.Equal(job.InputText, jobDb.InputText);
    }

    [Fact]
    public async Task GetJobById_WhenJobDoesNotExist_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"{JobsRoute}/999999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
    
    [Fact]
    public async Task GetAllJobs_ReturnsJobsOrderedBySubmittedAtDescending()
    {
        var (_, jobFirst) = await CreateJobAndReadResponseAsync();
        var (_, jobSecond) = await CreateJobAndReadResponseAsync();
        var (_, jobThird) = await CreateJobAndReadResponseAsync();

        Assert.NotNull(jobFirst);
        Assert.NotNull(jobSecond);
        Assert.NotNull(jobThird);

        var response = await _client.GetAsync(JobsRoute);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var testedIds = new[]
        {
            jobFirst.Id,
            jobSecond.Id,
            jobThird.Id
        };

        var jobs = await response.Content.ReadFromJsonAsync<List<JobResponse>>(JsonOptions);
        Assert.NotNull(jobs);

        var testedJobs = jobs.Where(j => testedIds.Contains(j.Id)).ToList();

        Assert.Equal(3, testedJobs.Count);

        for (var i = 0; i < testedJobs.Count - 1; i++)
        {
            Assert.True(testedJobs[i].SubmittedAtUtc >= testedJobs[i + 1].SubmittedAtUtc);
        }
    }
    
    private async Task<(HttpResponseMessage Response, JobResponse Job)> CreateJobAndReadResponseAsync()
    {
        var request = new CreateJobRequest
        {
            InputText = "Test Input"
        };

        var response = await _client.PostAsJsonAsync(JobsRoute, request);
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        
        var job = await response.Content.ReadFromJsonAsync<JobResponse>(JsonOptions);
        Assert.NotNull(job);
 
        return (response, job);
    }
    
    private async Task<JobResponse> WaitForJobStatusAsync(
        Guid jobId,
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

            await Task.Delay(200);
        }

        throw new TimeoutException(
            $"Job {jobId} did not reach status {expectedStatus} within {timeout.TotalSeconds} seconds. " +
            $"Last observed status: {lastSeenJob?.Status.ToString() ?? "<none>"}");
    }
    
}