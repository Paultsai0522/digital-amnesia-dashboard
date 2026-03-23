namespace DigitalAmnesia.Worker.Models;

public sealed class WorkerOptions
{
    public string WorkerId { get; init; } = $"worker_{Guid.NewGuid():N}"[..15];
    public string BackendApiUrl { get; init; } = "http://localhost:3001";
    public TimeSpan PollInterval { get; init; } = TimeSpan.FromSeconds(2);
    public TimeSpan StepDelay { get; init; } = TimeSpan.FromMilliseconds(900);
    public string GitHubApiBaseUrl { get; init; } = "https://api.github.com";
    public string? GitHubToken { get; init; }
    public int GitHubSearchResultLimit { get; init; } = 3;
    public string GitHubScannerMode { get; init; } = "live";
    public bool UseLiveGitHubScanner => string.Equals(GitHubScannerMode, "live", StringComparison.OrdinalIgnoreCase);

    public static WorkerOptions FromEnvironment() =>
        new()
        {
            WorkerId = GetWorkerId(),
            BackendApiUrl = GetBackendApiUrl(),
            PollInterval = TimeSpan.FromMilliseconds(GetPositiveInt("WORKER_POLL_INTERVAL_MS", 2000)),
            StepDelay = TimeSpan.FromMilliseconds(GetPositiveInt("WORKER_STEP_DELAY_MS", 900)),
            GitHubApiBaseUrl = GetAbsoluteUrl("GITHUB_API_BASE_URL", "https://api.github.com"),
            GitHubToken = GetOptionalString("GITHUB_TOKEN"),
            GitHubSearchResultLimit = GetPositiveInt("GITHUB_SEARCH_RESULT_LIMIT", 3),
            GitHubScannerMode = GetScannerMode(),
        };

    private static string GetBackendApiUrl()
    {
        const string fallback = "http://localhost:3001";
        var configured = Environment.GetEnvironmentVariable("BACKEND_API_URL")?.Trim();

        if (string.IsNullOrWhiteSpace(configured))
        {
            return fallback;
        }

        if (configured.Contains("${{", StringComparison.Ordinal) || configured.Contains("}}", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "BACKEND_API_URL contains an unresolved Railway reference variable. " +
                "Use a resolved value such as 'http://backend.railway.internal' or a Railway reference like " +
                "'${{backend.RAILWAY_PRIVATE_DOMAIN}}' wrapped with 'http://' in the Variables UI."
            );
        }

        if (Uri.TryCreate(configured, UriKind.Absolute, out var absoluteUri))
        {
            return absoluteUri.ToString().TrimEnd('/');
        }

        if (Uri.TryCreate($"http://{configured}", UriKind.Absolute, out var hostnameUri))
        {
            return hostnameUri.ToString().TrimEnd('/');
        }

        throw new InvalidOperationException(
            $"BACKEND_API_URL must be a valid absolute URL or hostname. Current value: '{configured}'."
        );
    }

    private static string GetAbsoluteUrl(string environmentVariable, string fallback)
    {
        var configured = Environment.GetEnvironmentVariable(environmentVariable)?.Trim();

        if (string.IsNullOrWhiteSpace(configured))
        {
            return fallback;
        }

        if (Uri.TryCreate(configured, UriKind.Absolute, out var absoluteUri))
        {
            return absoluteUri.ToString().TrimEnd('/');
        }

        throw new InvalidOperationException(
            $"{environmentVariable} must be a valid absolute URL. Current value: '{configured}'."
        );
    }

    private static string? GetOptionalString(string environmentVariable)
    {
        var value = Environment.GetEnvironmentVariable(environmentVariable)?.Trim();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static string GetScannerMode()
    {
        var configured = Environment.GetEnvironmentVariable("GITHUB_SCANNER_MODE")?.Trim();

        return string.Equals(configured, "mock", StringComparison.OrdinalIgnoreCase)
            ? "mock"
            : "live";
    }

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
