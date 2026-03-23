using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using DigitalAmnesia.Worker.Models;

namespace DigitalAmnesia.Worker.Services;

public sealed class GitHubApiClient(HttpClient httpClient, WorkerOptions options)
{
    private readonly JsonSerializerOptions _serializerOptions = new(JsonSerializerDefaults.Web);

    public async Task<GitHubUserProfile?> GetUserAsync(string username, CancellationToken cancellationToken)
    {
        using var request = CreateRequest(HttpMethod.Get, $"/users/{Uri.EscapeDataString(username)}");
        if (!string.IsNullOrWhiteSpace(options.GitHubToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.GitHubToken);
        }

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<GitHubUserProfile>(_serializerOptions, cancellationToken);
    }

    public async Task<IReadOnlyList<GitHubUserSummary>> SearchUsersAsync(string query, int limit, CancellationToken cancellationToken)
    {
        var perPage = Math.Clamp(limit, 1, 10);
        using var request = CreateRequest(
            HttpMethod.Get,
            $"/search/users?q={Uri.EscapeDataString(query)}&per_page={perPage}"
        );

        // Search users is public-only. Avoid sending auth to keep behavior aligned with the public endpoint.
        using var response = await httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        var payload = await response.Content.ReadFromJsonAsync<GitHubSearchResponse>(_serializerOptions, cancellationToken);
        return payload?.Items ?? [];
    }

    private static HttpRequestMessage CreateRequest(HttpMethod method, string path)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Accept.ParseAdd("application/vnd.github+json");
        request.Headers.Add("X-GitHub-Api-Version", "2026-03-10");
        request.Headers.UserAgent.ParseAdd("DigitalAmnesiaDashboardWorker/1.0");
        return request;
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var rawBody = await response.Content.ReadAsStringAsync(cancellationToken);
        var message = TryExtractMessage(rawBody);

        if (response.StatusCode == HttpStatusCode.Forbidden
            && response.Headers.TryGetValues("X-RateLimit-Remaining", out var remainingValues)
            && remainingValues.Contains("0"))
        {
            throw new InvalidOperationException("GitHub API rate limit exceeded for the current worker.");
        }

        throw new InvalidOperationException(
            string.IsNullOrWhiteSpace(message)
                ? $"GitHub API request failed with status {(int)response.StatusCode}."
                : $"GitHub API request failed: {message}"
        );
    }

    private static string? TryExtractMessage(string rawBody)
    {
        if (string.IsNullOrWhiteSpace(rawBody))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(rawBody);
            return document.RootElement.TryGetProperty("message", out var messageElement)
                ? messageElement.GetString()
                : null;
        }
        catch (JsonException)
        {
            return rawBody.Trim();
        }
    }
}
