using System.Text.RegularExpressions;
using DigitalAmnesia.Worker.Models;

namespace DigitalAmnesia.Worker.Services;

public sealed partial class XScanner(XApiClient apiClient, WorkerOptions options)
{
    public async Task<PlatformScanOutcome> ScanAsync(ScanQuery query, int existingResultCount, CancellationToken cancellationToken)
    {
        var candidates = new List<XUserProfile>();
        var seenIds = new HashSet<string>(StringComparer.Ordinal);
        var seenUsernames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var normalizedUsername = NormalizeUsername(query.Username);
        if (IsValidUsername(normalizedUsername))
        {
            var exactUser = await apiClient.GetUserByUsernameAsync(normalizedUsername, cancellationToken);
            TryAddCandidate(candidates, seenIds, seenUsernames, exactUser);
        }

        var searchQuery = BuildSearchQuery(query, includeUsername: string.IsNullOrWhiteSpace(normalizedUsername));
        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            var searchResults = await apiClient.SearchUsersAsync(searchQuery, options.XSearchResultLimit, cancellationToken);
            foreach (var candidate in searchResults)
            {
                if (!TryAddCandidate(candidates, seenIds, seenUsernames, candidate))
                {
                    continue;
                }

                if (candidates.Count >= options.XSearchResultLimit)
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
            .Take(options.XSearchResultLimit)
            .ToList();

        return new PlatformScanOutcome
        {
            Status = "completed",
            Message = results.Count > 0
                ? $"{results.Count} live X match{(results.Count == 1 ? string.Empty : "es")} found"
                : "No public X matches found",
            Results = results,
        };
    }

    private static bool TryAddCandidate(
        ICollection<XUserProfile> candidates,
        ISet<string> seenIds,
        ISet<string> seenUsernames,
        XUserProfile? candidate
    )
    {
        if (candidate is null
            || candidate.IsProtected
            || string.IsNullOrWhiteSpace(candidate.Id)
            || string.IsNullOrWhiteSpace(candidate.Username))
        {
            return false;
        }

        if (seenIds.Contains(candidate.Id) || seenUsernames.Contains(candidate.Username))
        {
            return false;
        }

        seenIds.Add(candidate.Id);
        seenUsernames.Add(candidate.Username);
        candidates.Add(candidate);
        return true;
    }

    private static ScanResult BuildCandidateResult(XUserProfile profile, ScanQuery query, int sequence)
    {
        var reasons = new List<string>();
        var score = 0;
        var normalizedProfileUsername = NormalizeUsername(profile.Username);
        var normalizedQueryUsername = NormalizeUsername(query.Username);
        var normalizedProfileName = Normalize(profile.Name);
        var normalizedQueryDisplayName = Normalize(query.DisplayName);

        if (!string.IsNullOrWhiteSpace(normalizedQueryUsername))
        {
            if (string.Equals(normalizedProfileUsername, normalizedQueryUsername, StringComparison.Ordinal))
            {
                score += 60;
                reasons.Add("Exact X handle match");
            }
            else if (normalizedProfileUsername.Contains(normalizedQueryUsername, StringComparison.Ordinal)
                || normalizedQueryUsername.Contains(normalizedProfileUsername, StringComparison.Ordinal))
            {
                score += 20;
                reasons.Add("X handle closely matches supplied username");
            }
        }

        if (!string.IsNullOrWhiteSpace(normalizedQueryDisplayName))
        {
            if (string.Equals(normalizedProfileName, normalizedQueryDisplayName, StringComparison.Ordinal))
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

        if (profile.Verified)
        {
            score += 6;
            reasons.Add("Verified X account");
        }

        var followersCount = profile.PublicMetrics?.FollowersCount ?? 0;
        if (followersCount > 0)
        {
            score += followersCount >= 1000 ? 6 : 3;
            reasons.Add($"Followers: {followersCount}");
        }

        var tweetCount = profile.PublicMetrics?.TweetCount ?? 0;
        if (tweetCount > 0)
        {
            score += tweetCount >= 100 ? 4 : 2;
            reasons.Add($"Tweets: {tweetCount}");
        }

        score = Math.Clamp(score, 0, 100);

        return new ScanResult
        {
            Id = $"res_x_{sequence}",
            Platform = "X",
            ProfileUrl = $"https://x.com/{profile.Username}",
            Username = profile.Username,
            DisplayName = string.IsNullOrWhiteSpace(profile.Name) ? profile.Username : profile.Name,
            Bio = BuildProfileSummary(profile),
            MatchLevel = score >= 75 ? "high" : score >= 45 ? "medium" : "low",
            MatchScore = score,
            MatchReasons = reasons.Count > 0 ? reasons : ["Public X profile found"],
        };
    }

    private static string BuildSearchQuery(ScanQuery query, bool includeUsername)
    {
        var parts = new List<string>();

        if (includeUsername)
        {
            var username = NormalizeUsername(query.Username);
            if (!string.IsNullOrWhiteSpace(username))
            {
                parts.Add(username);
            }
        }

        if (!string.IsNullOrWhiteSpace(query.DisplayName))
        {
            parts.Add(query.DisplayName);
        }

        parts.AddRange(query.Keywords.Take(2));

        var sanitized = SearchQuerySanitizer().Replace(string.Join(" ", parts), " ");
        sanitized = WhitespaceRegex().Replace(sanitized, " ").Trim();

        return sanitized.Length switch
        {
            0 => string.Empty,
            > 50 => sanitized[..50].Trim(),
            _ => sanitized,
        };
    }

    private static IEnumerable<string> FindKeywordMatches(XUserProfile profile, IReadOnlyCollection<string> keywords)
    {
        if (keywords.Count == 0)
        {
            return [];
        }

        var haystack = Normalize(
            string.Join(
                " ",
                [
                    profile.Name,
                    profile.Description ?? string.Empty,
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

    private static string BuildProfileSummary(XUserProfile profile)
    {
        var segments = new List<string>();

        if (!string.IsNullOrWhiteSpace(profile.Description))
        {
            segments.Add(profile.Description.Trim());
        }

        if (profile.Verified)
        {
            segments.Add("Verified X account");
        }

        if ((profile.PublicMetrics?.FollowersCount ?? 0) > 0)
        {
            segments.Add($"Followers: {profile.PublicMetrics!.FollowersCount}");
        }

        if ((profile.PublicMetrics?.TweetCount ?? 0) > 0)
        {
            segments.Add($"Tweets: {profile.PublicMetrics!.TweetCount}");
        }

        return segments.Count > 0
            ? string.Join(" | ", segments)
            : $"Public X profile @{profile.Username}";
    }

    private static bool IsValidUsername(string username) =>
        !string.IsNullOrWhiteSpace(username) && UsernameRegex().IsMatch(username);

    private static string NormalizeUsername(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return value.Trim().TrimStart('@').ToLowerInvariant();
    }

    private static string Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : WhitespaceRegex().Replace(value.Trim().ToLowerInvariant(), " ");

    [GeneratedRegex("^[A-Za-z0-9_]{1,15}$", RegexOptions.Compiled)]
    private static partial Regex UsernameRegex();

    [GeneratedRegex("[a-z0-9]+", RegexOptions.Compiled)]
    private static partial Regex WordRegex();

    [GeneratedRegex("[^A-Za-z0-9_' ]+", RegexOptions.Compiled)]
    private static partial Regex SearchQuerySanitizer();

    [GeneratedRegex("\\s+", RegexOptions.Compiled)]
    private static partial Regex WhitespaceRegex();
}
