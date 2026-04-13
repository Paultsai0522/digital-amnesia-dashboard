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
    public string XApiBaseUrl { get; init; } = "https://api.x.com";
    public string? XBearerToken { get; init; }
    public int XSearchResultLimit { get; init; } = 5;
    public string XScannerMode { get; init; } = "auto";
    public bool UseLiveGitHubScanner => string.Equals(GitHubScannerMode, "live", StringComparison.OrdinalIgnoreCase);
    public bool UseLiveXScanner =>
        string.Equals(XScannerMode, "live", StringComparison.OrdinalIgnoreCase)
        || (string.Equals(XScannerMode, "auto", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(XBearerToken));

    public static WorkerOptions FromEnvironment()
    {
        var options = new WorkerOptions
        {
            WorkerId = GetWorkerId(),
            BackendApiUrl = GetBackendApiUrl(),
            PollInterval = TimeSpan.FromMilliseconds(GetPositiveInt("WORKER_POLL_INTERVAL_MS", 2000)),
            StepDelay = TimeSpan.FromMilliseconds(GetPositiveInt("WORKER_STEP_DELAY_MS", 900)),
            GitHubApiBaseUrl = GetAbsoluteUrl("GITHUB_API_BASE_URL", "https://api.github.com"),
            GitHubToken = GetOptionalString("GITHUB_TOKEN"),
            GitHubSearchResultLimit = GetPositiveInt("GITHUB_SEARCH_RESULT_LIMIT", 3),
            GitHubScannerMode = GetScannerMode("GITHUB_SCANNER_MODE", "live", "mock", "live"),
            XApiBaseUrl = GetAbsoluteUrl("X_API_BASE_URL", "https://api.x.com"),
            XBearerToken = GetOptionalString("X_BEARER_TOKEN"),
            XSearchResultLimit = GetPositiveInt("X_SEARCH_RESULT_LIMIT", 5),
            XScannerMode = GetScannerMode("X_SCANNER_MODE", "auto", "mock", "live"),
        };

        options.Validate();
        return options;
    }

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

    private void Validate()
    {
        if (string.Equals(XScannerMode, "live", StringComparison.OrdinalIgnoreCase)
            && string.IsNullOrWhiteSpace(XBearerToken))
        {
            throw new InvalidOperationException(
                "X_BEARER_TOKEN is required when X_SCANNER_MODE is set to 'live'."
            );
        }
    }

    private static string GetScannerMode(string environmentVariable, string fallback, params string[] allowedModes)
    {
        var configured = Environment.GetEnvironmentVariable(environmentVariable)?.Trim();

        if (string.IsNullOrWhiteSpace(configured))
        {
            return fallback;
        }

        var matchedMode = allowedModes.FirstOrDefault(mode => string.Equals(mode, configured, StringComparison.OrdinalIgnoreCase));
        if (matchedMode is not null)
        {
            return matchedMode;
        }

        throw new InvalidOperationException(
            $"{environmentVariable} must be one of: {string.Join(", ", allowedModes)}. Current value: '{configured}'."
        );
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
