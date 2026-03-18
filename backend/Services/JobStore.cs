using System.Text.Json;
using DigitalAmnesia.Backend.Models;

namespace DigitalAmnesia.Backend.Services;

public sealed class JobStore
{
    private readonly string _dataFilePath;
    private readonly JsonSerializerOptions _serializerOptions;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public JobStore(IWebHostEnvironment environment)
    {
        var dataDirectory = Path.Combine(environment.ContentRootPath, "data");
        _dataFilePath = Path.Combine(dataDirectory, "jobs.json");
        _serializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            WriteIndented = true,
        };
    }

    public async Task<ScanJob> CreateJobAsync(ScanQuery query)
    {
        await _gate.WaitAsync();
        try
        {
            var data = await ReadDataAsync();
            var job = JobMapper.CreateQueuedJob(query);

            data.Jobs.Add(job);
            await WriteDataAsync(data);

            return JobMapper.CloneJob(job);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<ScanJob?> GetJobByIdAsync(string jobId)
    {
        await _gate.WaitAsync();
        try
        {
            var data = await ReadDataAsync();
            var job = data.Jobs.FirstOrDefault(job => string.Equals(job.Id, jobId, StringComparison.Ordinal));
            return job is null ? null : JobMapper.CloneJob(job);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<ScanJob?> ClaimNextQueuedJobAsync(string? workerId)
    {
        await _gate.WaitAsync();
        try
        {
            var data = await ReadDataAsync();
            var job = data.Jobs.FirstOrDefault(job => string.Equals(job.Status, "queued", StringComparison.Ordinal));

            if (job is null)
            {
                return null;
            }

            using var patch = JsonDocument.Parse(
                $$"""
                {
                  "status": "running",
                  "workerId": {{JsonSerializer.Serialize(workerId?.Trim(), _serializerOptions)}}
                }
                """
            );

            var updatedJob = JobMapper.MergeJobPatch(job, patch.RootElement);
            ReplaceJob(data.Jobs, updatedJob);
            await WriteDataAsync(data);

            return JobMapper.CloneJob(updatedJob);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<ScanJob?> UpdateJobAsync(string jobId, JsonElement patch)
    {
        await _gate.WaitAsync();
        try
        {
            var data = await ReadDataAsync();
            var job = data.Jobs.FirstOrDefault(job => string.Equals(job.Id, jobId, StringComparison.Ordinal));

            if (job is null)
            {
                return null;
            }

            var updatedJob = JobMapper.MergeJobPatch(job, patch);
            ReplaceJob(data.Jobs, updatedJob);
            await WriteDataAsync(data);

            return JobMapper.CloneJob(updatedJob);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<JobDataFile> ReadDataAsync()
    {
        await EnsureDataFileAsync();
        var raw = await File.ReadAllTextAsync(_dataFilePath);

        return JsonSerializer.Deserialize<JobDataFile>(raw, _serializerOptions) ?? new JobDataFile();
    }

    private async Task WriteDataAsync(JobDataFile data)
    {
        await EnsureDataFileAsync();
        var raw = JsonSerializer.Serialize(data, _serializerOptions);
        await File.WriteAllTextAsync(_dataFilePath, raw);
    }

    private async Task EnsureDataFileAsync()
    {
        var dataDirectory = Path.GetDirectoryName(_dataFilePath)!;
        Directory.CreateDirectory(dataDirectory);

        if (!File.Exists(_dataFilePath))
        {
            var raw = JsonSerializer.Serialize(new JobDataFile(), _serializerOptions);
            await File.WriteAllTextAsync(_dataFilePath, raw);
        }
    }

    private static void ReplaceJob(List<ScanJob> jobs, ScanJob updatedJob)
    {
        var index = jobs.FindIndex(job => string.Equals(job.Id, updatedJob.Id, StringComparison.Ordinal));
        if (index >= 0)
        {
            jobs[index] = updatedJob;
        }
    }
}
