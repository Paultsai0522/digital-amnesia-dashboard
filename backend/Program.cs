using System.Text.Json;
using DigitalAmnesia.Backend.Models;
using DigitalAmnesia.Backend.Services;

var builder = WebApplication.CreateBuilder(args);
var port = int.TryParse(Environment.GetEnvironmentVariable("PORT"), out var configuredPort)
    ? configuredPort
    : 3001;

builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod());
});
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});
builder.Services.AddSingleton<JobStore>();

var app = builder.Build();

app.UseCors();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapPost("/api/jobs", async (CreateJobRequest payload, JobStore store) =>
{
    var query = JobMapper.NormalizeQuery(payload);

    if (!JobMapper.HasScanInput(query))
    {
        return Results.BadRequest(new ErrorResponse
        {
            Error = "Provide at least one identity signal before starting a scan.",
        });
    }

    var job = await store.CreateJobAsync(query);
    return Results.Json(new { jobId = job.Id, job }, statusCode: StatusCodes.Status201Created);
});

app.MapGet("/api/jobs/{jobId}", async (string jobId, JobStore store) =>
{
    var job = await store.GetJobByIdAsync(jobId);

    return job is null
        ? Results.NotFound(new ErrorResponse { Error = "Job not found." })
        : Results.Ok(job);
});

app.MapPost("/internal/jobs/claim", async (ClaimJobRequest payload, JobStore store) =>
{
    var job = await store.ClaimNextQueuedJobAsync(payload.WorkerId);

    return job is null
        ? Results.NoContent()
        : Results.Ok(new { job });
});

app.MapMethods("/internal/jobs/{jobId}", ["PATCH"], async (string jobId, JsonElement patch, JobStore store) =>
{
    var job = await store.UpdateJobAsync(jobId, patch);

    return job is null
        ? Results.NotFound(new ErrorResponse { Error = "Job not found." })
        : Results.Ok(job);
});

app.Run();
