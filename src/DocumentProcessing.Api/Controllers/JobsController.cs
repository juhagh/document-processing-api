using DocumentProcessing.Api.Contracts;
using DocumentProcessing.Application.DTOs;
using DocumentProcessing.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DocumentProcessing.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class JobsController : ControllerBase
{
    private readonly IJobService _jobService;

    public JobsController(IJobService jobService)
    {
        _jobService = jobService;
    }
    
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<JobResponse>>> GetAllJobs(CancellationToken cancellationToken)
    {
        var jobs = await _jobService.GetAllAsync(cancellationToken);
        var jobList = jobs
            .Select(MapToResponse)
            .ToList();

        return Ok(jobList);
    }
    
    [HttpGet("{id:int}")]
    public async Task<ActionResult<JobResponse>> GetJobById(int id, CancellationToken cancellationToken)
    {
        var job = await _jobService.GetByIdAsync(id, cancellationToken);

        if (job is null)
            return NotFound();

        return Ok(MapToResponse(job));
    }
    
    [HttpPost]
    public async Task<ActionResult<JobResponse>> CreateDocumentJob([FromBody] CreateJobRequest createJobRequest,
        CancellationToken cancellationToken)
    {
        var dto = new CreateJobRequestDto
        {
            InputText = createJobRequest.InputText,
        };
        
        var job = await _jobService.CreateAsync(dto, cancellationToken);
        
        return CreatedAtAction(nameof(GetJobById), new { id = job.Id }, MapToResponse(job));

    }
    
    private static JobResponse MapToResponse(JobResponseDto job)
    {
        return new JobResponse
        {
            Id = job.Id,
            Status = job.Status,
            InputText = job.InputText,
            SubmittedAtUtc = job.SubmittedAtUtc,
            UpdatedAtUtc = job.UpdatedAtUtc,
            CompletedAtUtc = job.CompletedAtUtc,
            ErrorMessage = job.ErrorMessage,
            WordCount = job.WordCount,
            CharacterCount = job.CharacterCount,
            LineCount = job.LineCount,
            KeywordHits = job.KeywordHits,
            Category = job.Category,
            Summary = job.Summary
        };
    }
}