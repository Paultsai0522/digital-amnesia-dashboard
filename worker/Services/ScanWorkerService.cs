using System.Text.RegularExpressions;
using DigitalAmnesia.Worker.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DigitalAmnesia.Worker.Services;

public sealed partial class ScanWorkerService(
    WorkerApiClient apiClient,
    GitHubScanner gitHubScanner,
    WorkerOptions options,
    ILogger<ScanWorkerService> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Worker {WorkerId} polling for queued jobs at {BackendApiUrl}", options.WorkerId, options.BackendApiUrl);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var job = await apiClient.ClaimNextQueuedJobAsync(options.WorkerId, stoppingToken);

                if (job is null)
                {
                    await Task.Delay(options.PollInterval, stoppingToken);
                    continue;
                }

                logger.LogInformation("Claimed {JobId}", job.Id);
                await ProcessJobAsync(job, stoppingToken);
                logger.LogInformation("Completed {JobId}", job.Id);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Worker iteration failed");
                await Task.Delay(options.PollInterval, stoppingToken);
            }
        }
    }

    private async Task ProcessJobAsync(ScanJob job, CancellationToken cancellationToken)
    {
        var liveTargets = job.Targets
            .Select(target => new ScanTarget
            {
                Platform = target.Platform,
                Status = target.Status,
                Message = target.Message,
            })
            .ToList();

        var liveResults = job.Results
            .Select(CloneResult)
            .ToList();
        var targetErrors = new List<string>();

        try
        {
            for (var index = 0; index < liveTargets.Count; index += 1)
            {
                var target = liveTargets[index];

                liveTargets = UpdateTarget(liveTargets, target.Platform, new ScanTarget
                {
                    Platform = target.Platform,
                    Status = "running",
                    Message = $"Scanning {target.Platform} for public identity signals",
                });

                await apiClient.UpdateJobAsync(
                    job.Id,
                    new
                    {
                        status = "running",
                        progress = (int)Math.Round((double)index / liveTargets.Count * 100),
                        targets = liveTargets,
                        results = liveResults,
                        error = (string?)null,
                    },
                    cancellationToken
                );

                await Task.Delay(options.StepDelay, cancellationToken);

                PlatformScanOutcome outcome;
                try
                {
                    outcome = await ScanPlatformAsync(job, target.Platform, liveResults.Count, cancellationToken);
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    logger.LogWarning(exception, "Platform scan failed for {Platform} on {JobId}", target.Platform, job.Id);
                    outcome = new PlatformScanOutcome
                    {
                        Status = "failed",
                        Message = exception.Message,
                        Error = exception.Message,
                    };
                }

                liveResults.AddRange(outcome.Results);
                liveTargets = UpdateTarget(liveTargets, target.Platform, new ScanTarget
                {
                    Platform = target.Platform,
                    Status = outcome.Status,
                    Message = outcome.Message,
                });

                if (!string.IsNullOrWhiteSpace(outcome.Error))
                {
                    targetErrors.Add($"{target.Platform}: {outcome.Error}");
                }

                var isLastTarget = index == liveTargets.Count - 1;

                await apiClient.UpdateJobAsync(
                    job.Id,
                    new
                    {
                        status = isLastTarget ? GetTerminalJobStatus(liveTargets) : "running",
                        progress = (int)Math.Round((double)(index + 1) / liveTargets.Count * 100),
                        targets = liveTargets,
                        results = liveResults,
                        completedAt = isLastTarget ? DateTimeOffset.UtcNow.ToString("O") : null,
                        error = isLastTarget ? BuildTerminalError(targetErrors) : null,
                    },
                    cancellationToken
                );
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            await apiClient.UpdateJobAsync(
                job.Id,
                new
                {
                    status = "failed",
                    targets = liveTargets,
                    results = liveResults,
                    error = exception.Message,
                },
                cancellationToken
            );

            throw;
        }
    }

    private async Task<PlatformScanOutcome> ScanPlatformAsync(
        ScanJob job,
        string platform,
        int existingResultCount,
        CancellationToken cancellationToken
    )
    {
        if (string.Equals(platform, "GitHub", StringComparison.Ordinal) && options.UseLiveGitHubScanner)
        {
            return await gitHubScanner.ScanAsync(job.Query, existingResultCount, cancellationToken);
        }

        var results = BuildMockResultsForPlatform(job, platform, existingResultCount);
        return new PlatformScanOutcome
        {
            Status = "completed",
            Message = results.Count > 0
                ? $"{results.Count} match{(results.Count == 1 ? string.Empty : "es")} found"
                : $"No public {platform} matches found",
            Results = results,
        };
    }

    private static string GetTerminalJobStatus(IReadOnlyCollection<ScanTarget> targets)
    {
        var failedTargets = targets.Count(target => string.Equals(target.Status, "failed", StringComparison.Ordinal));
        var completedTargets = targets.Count(target => string.Equals(target.Status, "completed", StringComparison.Ordinal));

        return failedTargets > 0 && completedTargets == 0
            ? "failed"
            : "completed";
    }

    private static string? BuildTerminalError(IEnumerable<string> targetErrors)
    {
        var errors = targetErrors
            .Where(error => !string.IsNullOrWhiteSpace(error))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        return errors.Count == 0
            ? null
            : $"Some platform scans failed: {string.Join("; ", errors)}";
    }

    private static List<ScanTarget> UpdateTarget(List<ScanTarget> targets, string platform, ScanTarget patch) =>
        targets.Select(target =>
        {
            if (!string.Equals(target.Platform, platform, StringComparison.Ordinal))
            {
                return new ScanTarget
                {
                    Platform = target.Platform,
                    Status = target.Status,
                    Message = target.Message,
                };
            }

            return new ScanTarget
            {
                Platform = patch.Platform,
                Status = patch.Status,
                Message = patch.Message,
            };
        }).ToList();

    private static List<ScanResult> BuildMockResultsForPlatform(ScanJob job, string platform, int existingResultCount)
    {
        var query = job.Query;
        var baseUsername = BuildBaseUsername(query);
        var displayName = FormatDisplayName(query);

        if (string.Equals(platform, "GitHub", StringComparison.Ordinal))
        {
            var results = new List<ScanResult>
            {
                new()
                {
                    Id = $"res_github_{existingResultCount + 1}",
                    Platform = platform,
                    ProfileUrl = $"https://github.com/{baseUsername}",
                    Username = baseUsername,
                    DisplayName = displayName,
                    Bio = $"Public repositories and commits connected to {displayName}.",
                    MatchLevel = "high",
                    MatchScore = 87,
                    MatchReasons = BuildReasons(query, "Exact handle match on GitHub"),
                },
            };

            if (!string.IsNullOrWhiteSpace(query.DisplayName) || query.Keywords.Count > 0)
            {
                results.Add(new ScanResult
                {
                    Id = $"res_github_{existingResultCount + 2}",
                    Platform = platform,
                    ProfileUrl = $"https://github.com/{baseUsername}-dev",
                    Username = $"{baseUsername}-dev",
                    DisplayName = !string.IsNullOrWhiteSpace(query.DisplayName) ? query.DisplayName : $"{displayName} Dev",
                    Bio = "Secondary developer profile with overlapping bio terms.",
                    MatchLevel = "medium",
                    MatchScore = 63,
                    MatchReasons = BuildReasons(query, "Profile bio overlaps with supplied identity signals"),
                });
            }

            return results;
        }

        if (string.Equals(platform, "Reddit", StringComparison.Ordinal))
        {
            return
            [
                new ScanResult
                {
                    Id = $"res_reddit_{existingResultCount + 1}",
                    Platform = platform,
                    ProfileUrl = $"https://www.reddit.com/user/{baseUsername}",
                    Username = baseUsername,
                    DisplayName = displayName,
                    Bio = "Comment history suggests overlap with supplied keywords.",
                    MatchLevel = query.Keywords.Count > 0 ? "medium" : "low",
                    MatchScore = query.Keywords.Count > 0 ? 55 : 42,
                    MatchReasons = BuildReasons(query, "Handle similarity found on Reddit"),
                },
            ];
        }

        if (string.IsNullOrWhiteSpace(query.Username) && query.Keywords.Count == 0)
        {
            return [];
        }

        return
        [
            new ScanResult
            {
                Id = $"res_x_{existingResultCount + 1}",
                Platform = platform,
                ProfileUrl = $"https://x.com/{baseUsername}",
                Username = baseUsername,
                DisplayName = displayName,
                Bio = "Public profile contains matching name fragments and topic signals.",
                MatchLevel = "low",
                MatchScore = 38,
                MatchReasons = BuildReasons(query, "Loose public profile match on X"),
            },
        ];
    }

    private static List<string> BuildReasons(ScanQuery query, string leadingReason)
    {
        var reasons = new List<string> { leadingReason };
        reasons.AddRange(query.Keywords.Take(2).Select(keyword => $"Keyword overlap: {keyword}"));
        return reasons;
    }

    private static string BuildBaseUsername(ScanQuery query)
    {
        if (!string.IsNullOrWhiteSpace(query.Username))
        {
            return query.Username;
        }

        var slug = SlugRegex().Replace((query.DisplayName ?? "identity-signal").ToLowerInvariant(), "-").Trim('-');
        return string.IsNullOrWhiteSpace(slug) ? "identity-signal" : slug;
    }

    private static string FormatDisplayName(ScanQuery query)
    {
        if (!string.IsNullOrWhiteSpace(query.DisplayName))
        {
            return query.DisplayName;
        }

        if (!string.IsNullOrWhiteSpace(query.Username))
        {
            return string.Join(
                " ",
                query.Username
                    .Split(['-', '_', '.'], StringSplitOptions.RemoveEmptyEntries)
                    .Select(part => char.ToUpperInvariant(part[0]) + part[1..])
            );
        }

        return "Unknown Identity";
    }

    private static ScanResult CloneResult(ScanResult result) =>
        new()
        {
            Id = result.Id,
            Platform = result.Platform,
            ProfileUrl = result.ProfileUrl,
            Username = result.Username,
            DisplayName = result.DisplayName,
            Bio = result.Bio,
            MatchLevel = result.MatchLevel,
            MatchScore = result.MatchScore,
            MatchReasons = [.. result.MatchReasons],
        };

    [GeneratedRegex("[^a-z0-9]+", RegexOptions.Compiled)]
    private static partial Regex SlugRegex();
}
