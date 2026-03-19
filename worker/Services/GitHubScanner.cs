using System.Text.RegularExpressions;
using DigitalAmnesia.Worker.Models;

namespace DigitalAmnesia.Worker.Services;

public sealed partial class GitHubScanner(GitHubApiClient apiClient, WorkerOptions options)
{
    public async Task<PlatformScanOutcome> ScanAsync(ScanQuery query, int existingResultCount, CancellationToken cancellationToken)
    {
        var candidates = new List<GitHubUserProfile>();
        var seenLogins = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(query.Username))
        {
            var exactUser = await apiClient.GetUserAsync(query.Username, cancellationToken);
            if (exactUser is not null)
            {
                candidates.Add(exactUser);
                seenLogins.Add(exactUser.Login);
            }
        }

        var searchQuery = BuildSearchQuery(query, candidates.Count == 0);
        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            var summaries = await apiClient.SearchUsersAsync(searchQuery, options.GitHubSearchResultLimit, cancellationToken);
            foreach (var summary in summaries)
            {
                if (!seenLogins.Add(summary.Login))
                {
                    continue;
                }

                var profile = await apiClient.GetUserAsync(summary.Login, cancellationToken);
                if (profile is null)
                {
                    continue;
                }

                candidates.Add(profile);

                if (candidates.Count >= options.GitHubSearchResultLimit)
                {
                    break;
                }
            }
        }

        var results = candidates
            .Select((candidate, index) => BuildCandidateResult(candidate, query, existingResultCount + index + 1))
            .Where(candidate => candidate.MatchScore >= 20)
            .OrderByDescending(candidate => candidate.MatchScore)
            .ThenBy(candidate => candidate.Username, StringComparer.OrdinalIgnoreCase)
            .Take(options.GitHubSearchResultLimit)
            .ToList();

        return new PlatformScanOutcome
        {
            Status = "completed",
            Message = results.Count > 0
                ? $"{results.Count} live GitHub match{(results.Count == 1 ? string.Empty : "es")} found"
                : "No public GitHub matches found",
            Results = results,
        };
    }

    private static ScanResult BuildCandidateResult(GitHubUserProfile profile, ScanQuery query, int sequence)
    {
        var reasons = new List<string>();
        var score = 0;
        var normalizedLogin = Normalize(profile.Login);
        var normalizedUsername = Normalize(query.Username);
        var normalizedDisplayName = Normalize(query.DisplayName);
        var normalizedProfileName = Normalize(profile.Name);

        if (!string.IsNullOrWhiteSpace(normalizedUsername))
        {
            if (string.Equals(normalizedLogin, normalizedUsername, StringComparison.Ordinal))
            {
                score += 60;
                reasons.Add("Exact GitHub handle match");
            }
            else if (normalizedLogin.Contains(normalizedUsername, StringComparison.Ordinal)
                || normalizedUsername.Contains(normalizedLogin, StringComparison.Ordinal))
            {
                score += 22;
                reasons.Add("GitHub handle closely matches supplied username");
            }
        }

        if (!string.IsNullOrWhiteSpace(normalizedDisplayName))
        {
            if (string.Equals(normalizedProfileName, normalizedDisplayName, StringComparison.Ordinal))
            {
                score += 24;
                reasons.Add("Profile name matches supplied display name");
            }
            else if (HasWordOverlap(profile.Name, query.DisplayName))
            {
                score += 12;
                reasons.Add("Profile name overlaps with supplied display name");
            }
        }

        var keywordMatches = FindKeywordMatches(profile, query.Keywords).ToList();
        if (keywordMatches.Count > 0)
        {
            score += Math.Min(keywordMatches.Count * 8, 16);
            reasons.AddRange(keywordMatches.Select(keyword => $"Keyword overlap: {keyword}"));
        }

        if (profile.PublicRepos > 0)
        {
            score += 4;
            reasons.Add($"Public repositories: {profile.PublicRepos}");
        }

        if (profile.Followers > 0)
        {
            score += Math.Min(profile.Followers >= 100 ? 6 : 3, 6);
            reasons.Add($"Public followers: {profile.Followers}");
        }

        score = Math.Clamp(score, 0, 100);

        return new ScanResult
        {
            Id = $"res_github_{sequence}",
            Platform = "GitHub",
            ProfileUrl = profile.HtmlUrl,
            Username = profile.Login,
            DisplayName = string.IsNullOrWhiteSpace(profile.Name) ? profile.Login : profile.Name,
            Bio = BuildProfileSummary(profile),
            MatchLevel = score >= 75 ? "high" : score >= 45 ? "medium" : "low",
            MatchScore = score,
            MatchReasons = reasons.Count > 0 ? reasons : ["Public GitHub profile found"],
        };
    }

    private static string BuildSearchQuery(ScanQuery query, bool includeUsername)
    {
        var parts = new List<string>();

        if (includeUsername && !string.IsNullOrWhiteSpace(query.Username))
        {
            parts.Add(query.Username);
        }

        if (!string.IsNullOrWhiteSpace(query.DisplayName))
        {
            parts.Add($"\"{query.DisplayName}\"");
        }

        parts.AddRange(query.Keywords.Take(2));

        return string.Join(" ", parts).Trim();
    }

    private static IEnumerable<string> FindKeywordMatches(GitHubUserProfile profile, IReadOnlyCollection<string> keywords)
    {
        if (keywords.Count == 0)
        {
            return [];
        }

        var haystack = Normalize(
            string.Join(
                " ",
                [
                    profile.Bio ?? string.Empty,
                    profile.Company ?? string.Empty,
                    profile.Location ?? string.Empty,
                    profile.Blog ?? string.Empty,
                ]
            )
        );

        if (string.IsNullOrWhiteSpace(haystack))
        {
            return [];
        }

        return keywords
            .Where(keyword => !string.IsNullOrWhiteSpace(keyword))
            .Select(keyword => keyword.Trim())
            .Where(keyword => haystack.Contains(Normalize(keyword), StringComparison.Ordinal))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(2);
    }

    private static bool HasWordOverlap(string? left, string? right)
    {
        var leftWords = SplitWords(left);
        var rightWords = SplitWords(right);
        return leftWords.Overlaps(rightWords);
    }

    private static HashSet<string> SplitWords(string? input) =>
        WordRegex()
            .Matches(Normalize(input))
            .Select(match => match.Value)
            .Where(word => word.Length > 1)
            .ToHashSet(StringComparer.Ordinal);

    private static string BuildProfileSummary(GitHubUserProfile profile)
    {
        var segments = new List<string>();

        if (!string.IsNullOrWhiteSpace(profile.Bio))
        {
            segments.Add(profile.Bio.Trim());
        }

        if (!string.IsNullOrWhiteSpace(profile.Company))
        {
            segments.Add($"Company: {profile.Company.Trim()}");
        }

        if (!string.IsNullOrWhiteSpace(profile.Location))
        {
            segments.Add($"Location: {profile.Location.Trim()}");
        }

        if (!string.IsNullOrWhiteSpace(profile.Blog))
        {
            segments.Add($"Blog: {profile.Blog.Trim()}");
        }

        if (segments.Count == 0)
        {
            return $"GitHub profile with {profile.PublicRepos} public repos and {profile.Followers} followers.";
        }

        return string.Join(" | ", segments);
    }

    private static string Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : WhitespaceRegex().Replace(value.Trim().ToLowerInvariant(), " ");

    [GeneratedRegex("[a-z0-9]+", RegexOptions.Compiled)]
    private static partial Regex WordRegex();

    [GeneratedRegex("\\s+", RegexOptions.Compiled)]
    private static partial Regex WhitespaceRegex();
}
