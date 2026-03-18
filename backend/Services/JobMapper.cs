using System.Text.Json;
using DigitalAmnesia.Backend.Models;

namespace DigitalAmnesia.Backend.Services;

public static class JobMapper
{
    public static readonly string[] ScanPlatforms = ["GitHub", "Reddit", "X"];

    public static ScanQuery NormalizeQuery(CreateJobRequest? payload) =>
        NormalizeQuery(new ScanQuery
        {
            Username = payload?.Username ?? string.Empty,
            DisplayName = payload?.DisplayName ?? string.Empty,
            Keywords = payload?.Keywords ?? [],
        });

    public static ScanQuery NormalizeQuery(ScanQuery? query)
    {
        var rawKeywords = query?.Keywords ?? [];

        return new ScanQuery
        {
            Username = CleanString(query?.Username),
            DisplayName = CleanString(query?.DisplayName),
            Keywords = rawKeywords
                .Select(static keyword => CleanString(keyword))
                .Where(static keyword => !string.IsNullOrWhiteSpace(keyword))
                .ToList(),
        };
    }

    public static bool HasScanInput(ScanQuery query) =>
        !string.IsNullOrWhiteSpace(query.Username)
        || !string.IsNullOrWhiteSpace(query.DisplayName)
        || query.Keywords.Count > 0;

    public static ScanJob CreateQueuedJob(ScanQuery query)
    {
        var timestamp = Timestamp();

        return new ScanJob
        {
            Id = $"job_{Guid.NewGuid()}",
            Status = "queued",
            Progress = 0,
            Query = NormalizeQuery(query),
            Targets = ScanPlatforms
                .Select(static platform => new ScanTarget
                {
                    Platform = platform,
                    Status = "queued",
                    Message = "Queued for worker pickup",
                })
                .ToList(),
            Results = [],
            Error = null,
            WorkerId = null,
            CreatedAt = timestamp,
            UpdatedAt = timestamp,
            StartedAt = null,
            CompletedAt = null,
        };
    }

    public static ScanJob CloneJob(ScanJob job) =>
        new()
        {
            Id = job.Id,
            Status = job.Status,
            Progress = job.Progress,
            Query = CloneQuery(job.Query),
            Targets = job.Targets.Select(CloneTarget).ToList(),
            Results = job.Results.Select(CloneResult).ToList(),
            Error = job.Error,
            WorkerId = job.WorkerId,
            CreatedAt = job.CreatedAt,
            UpdatedAt = job.UpdatedAt,
            StartedAt = job.StartedAt,
            CompletedAt = job.CompletedAt,
        };

    public static ScanJob MergeJobPatch(ScanJob job, JsonElement patch)
    {
        var timestamp = Timestamp();
        var nextStatus = TryReadString(patch, "status") ?? job.Status;
        var nextProgress = TryReadInt(patch, "progress") is int progress
            ? Math.Clamp((int)Math.Round((double)progress), 0, 100)
            : job.Progress;

        var nextQuery = patch.TryGetProperty("query", out var queryElement)
            ? ReadQuery(queryElement)
            : CloneQuery(job.Query);

        var nextTargets = patch.TryGetProperty("targets", out var targetsElement)
            ? ReadTargets(targetsElement)
            : job.Targets.Select(CloneTarget).ToList();

        var nextResults = patch.TryGetProperty("results", out var resultsElement)
            ? ReadResults(resultsElement)
            : job.Results.Select(CloneResult).ToList();

        string? nextError;
        if (patch.TryGetProperty("error", out var errorElement))
        {
            nextError = errorElement.ValueKind == JsonValueKind.Null
                ? null
                : errorElement.GetString();
        }
        else if (string.Equals(nextStatus, "failed", StringComparison.Ordinal))
        {
            nextError = job.Error ?? "Worker failed unexpectedly";
        }
        else
        {
            nextError = null;
        }

        string? nextWorkerId;
        if (patch.TryGetProperty("workerId", out var workerIdElement))
        {
            nextWorkerId = workerIdElement.ValueKind == JsonValueKind.Null
                ? null
                : NullIfEmpty(CleanString(workerIdElement.GetString()));
        }
        else
        {
            nextWorkerId = job.WorkerId;
        }

        string? nextStartedAt;
        if (patch.TryGetProperty("startedAt", out var startedAtElement))
        {
            nextStartedAt = startedAtElement.ValueKind == JsonValueKind.Null
                ? null
                : startedAtElement.GetString();
        }
        else if (!string.IsNullOrWhiteSpace(job.StartedAt))
        {
            nextStartedAt = job.StartedAt;
        }
        else if (string.Equals(nextStatus, "running", StringComparison.Ordinal))
        {
            nextStartedAt = timestamp;
        }
        else
        {
            nextStartedAt = null;
        }

        string? nextCompletedAt;
        if (patch.TryGetProperty("completedAt", out var completedAtElement))
        {
            nextCompletedAt = completedAtElement.ValueKind == JsonValueKind.Null
                ? null
                : completedAtElement.GetString();
        }
        else if (string.Equals(nextStatus, "completed", StringComparison.Ordinal))
        {
            nextCompletedAt = job.CompletedAt ?? timestamp;
        }
        else if (string.Equals(nextStatus, "failed", StringComparison.Ordinal))
        {
            nextCompletedAt = job.CompletedAt;
        }
        else
        {
            nextCompletedAt = null;
        }

        return new ScanJob
        {
            Id = job.Id,
            Status = nextStatus,
            Progress = nextProgress,
            Query = nextQuery,
            Targets = nextTargets,
            Results = nextResults,
            Error = nextError,
            WorkerId = nextWorkerId,
            CreatedAt = job.CreatedAt,
            UpdatedAt = timestamp,
            StartedAt = nextStartedAt,
            CompletedAt = nextCompletedAt,
        };
    }

    private static ScanQuery CloneQuery(ScanQuery query) =>
        new()
        {
            Username = query.Username,
            DisplayName = query.DisplayName,
            Keywords = [.. query.Keywords],
        };

    private static ScanTarget CloneTarget(ScanTarget target) =>
        new()
        {
            Platform = CleanString(target.Platform),
            Status = CleanString(target.Status, "queued"),
            Message = CleanString(target.Message),
        };

    private static ScanResult CloneResult(ScanResult result) =>
        new()
        {
            Id = CleanString(result.Id),
            Platform = CleanString(result.Platform),
            ProfileUrl = CleanString(result.ProfileUrl),
            Username = CleanString(result.Username),
            DisplayName = CleanString(result.DisplayName),
            Bio = CleanString(result.Bio),
            MatchLevel = CleanString(result.MatchLevel, "low"),
            MatchScore = result.MatchScore,
            MatchReasons = result.MatchReasons
                .Select(static reason => CleanString(reason))
                .Where(static reason => !string.IsNullOrWhiteSpace(reason))
                .ToList(),
        };

    private static ScanQuery ReadQuery(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return NormalizeQuery(new ScanQuery());
        }

        var keywords = new List<string>();

        if (element.TryGetProperty("keywords", out var keywordsElement))
        {
            if (keywordsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var keywordElement in keywordsElement.EnumerateArray())
                {
                    if (keywordElement.ValueKind == JsonValueKind.String)
                    {
                        keywords.Add(keywordElement.GetString() ?? string.Empty);
                    }
                }
            }
            else if (keywordsElement.ValueKind == JsonValueKind.String)
            {
                keywords.AddRange((keywordsElement.GetString() ?? string.Empty).Split(','));
            }
        }

        return NormalizeQuery(new ScanQuery
        {
            Username = TryReadString(element, "username") ?? string.Empty,
            DisplayName = TryReadString(element, "displayName") ?? string.Empty,
            Keywords = keywords,
        });
    }

    private static List<ScanTarget> ReadTargets(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return element
            .EnumerateArray()
            .Where(static item => item.ValueKind == JsonValueKind.Object)
            .Select(static item => new ScanTarget
            {
                Platform = CleanString(item.TryGetProperty("platform", out var platform) ? platform.GetString() : null),
                Status = CleanString(item.TryGetProperty("status", out var status) ? status.GetString() : null, "queued"),
                Message = CleanString(item.TryGetProperty("message", out var message) ? message.GetString() : null),
            })
            .ToList();
    }

    private static List<ScanResult> ReadResults(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return element
            .EnumerateArray()
            .Where(static item => item.ValueKind == JsonValueKind.Object)
            .Select(static item => new ScanResult
            {
                Id = CleanString(item.TryGetProperty("id", out var id) ? id.GetString() : null),
                Platform = CleanString(item.TryGetProperty("platform", out var platform) ? platform.GetString() : null),
                ProfileUrl = CleanString(item.TryGetProperty("profileUrl", out var profileUrl) ? profileUrl.GetString() : null),
                Username = CleanString(item.TryGetProperty("username", out var username) ? username.GetString() : null),
                DisplayName = CleanString(item.TryGetProperty("displayName", out var displayName) ? displayName.GetString() : null),
                Bio = CleanString(item.TryGetProperty("bio", out var bio) ? bio.GetString() : null),
                MatchLevel = CleanString(item.TryGetProperty("matchLevel", out var matchLevel) ? matchLevel.GetString() : null, "low"),
                MatchScore = TryGetInt(item, "matchScore"),
                MatchReasons = ReadStringArray(item, "matchReasons"),
            })
            .ToList();
    }

    private static List<string> ReadStringArray(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var arrayElement) || arrayElement.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return arrayElement
            .EnumerateArray()
            .Where(static item => item.ValueKind == JsonValueKind.String)
            .Select(static item => CleanString(item.GetString()))
            .Where(static item => !string.IsNullOrWhiteSpace(item))
            .ToList();
    }

    private static int TryGetInt(JsonElement element, string propertyName) =>
        TryReadInt(element, propertyName) ?? 0;

    private static int? TryReadInt(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        return property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out var value)
            ? value
            : null;
    }

    private static string? TryReadString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        return property.ValueKind == JsonValueKind.Null ? null : CleanString(property.GetString());
    }

    private static string CleanString(string? value, string fallback = "") =>
        string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

    private static string? NullIfEmpty(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;

    private static string Timestamp() => DateTimeOffset.UtcNow.ToString("O");
}
