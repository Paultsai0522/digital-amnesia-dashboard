namespace DigitalAmnesia.Worker.Models;

public sealed class WorkerOptions
{
    public string WorkerId { get; init; } = $"worker_{Guid.NewGuid():N}"[..15];
    public string BackendApiUrl { get; init; } = "http://localhost:3001";
    public TimeSpan PollInterval { get; init; } = TimeSpan.FromSeconds(2);
    public TimeSpan StepDelay { get; init; } = TimeSpan.FromMilliseconds(900);

    public static WorkerOptions FromEnvironment() =>
        new()
        {
            WorkerId = GetWorkerId(),
            BackendApiUrl = Environment.GetEnvironmentVariable("BACKEND_API_URL")?.Trim() ?? "http://localhost:3001",
            PollInterval = TimeSpan.FromMilliseconds(GetPositiveInt("WORKER_POLL_INTERVAL_MS", 2000)),
            StepDelay = TimeSpan.FromMilliseconds(GetPositiveInt("WORKER_STEP_DELAY_MS", 900)),
        };

    private static string GetWorkerId()
    {
        var configured = Environment.GetEnvironmentVariable("WORKER_ID")?.Trim();

        return string.IsNullOrWhiteSpace(configured)
            ? $"worker_{Guid.NewGuid():N}"[..15]
            : configured;
    }

    private static int GetPositiveInt(string environmentVariable, int fallback)
    {
        var raw = Environment.GetEnvironmentVariable(environmentVariable);

        return int.TryParse(raw, out var parsed) && parsed > 0
            ? parsed
            : fallback;
    }
}
