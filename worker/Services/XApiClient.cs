using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using DigitalAmnesia.Worker.Models;

namespace DigitalAmnesia.Worker.Services;

public sealed class XApiClient(HttpClient httpClient, WorkerOptions options)
{
    private const string UserFields = "description,verified,protected,public_metrics,profile_image_url";
    private readonly JsonSerializerOptions _serializerOptions = new(JsonSerializerDefaults.Web);

    public async Task<XUserProfile?> GetUserByUsernameAsync(string username, CancellationToken cancellationToken)
    {
        EnsureTokenConfigured();

        using var request = CreateRequest(
            HttpMethod.Get,
            $"/2/users/by/username/{Uri.EscapeDataString(username)}?user.fields={Uri.EscapeDataString(UserFields)}"
        );

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        var payload = await response.Content.ReadFromJsonAsync<XUserLookupResponse>(_serializerOptions, cancellationToken);
        return payload?.Data;
    }

    public async Task<IReadOnlyList<XUserProfile>> SearchUsersAsync(string query, int limit, CancellationToken cancellationToken)
    {
        EnsureTokenConfigured();

        var maxResults = Math.Clamp(limit, 1, 10);
        using var request = CreateRequest(
            HttpMethod.Get,
            $"/2/users/search?query={Uri.EscapeDataString(query)}&max_results={maxResults}&user.fields={Uri.EscapeDataString(UserFields)}"
        );

        using var response = await httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        var payload = await response.Content.ReadFromJsonAsync<XUserSearchResponse>(_serializerOptions, cancellationToken);
        return payload?.Data ?? [];
    }

    private void EnsureTokenConfigured()
    {
        if (string.IsNullOrWhiteSpace(options.XBearerToken))
        {
            throw new InvalidOperationException("X_BEARER_TOKEN is required for live X scanning.");
        }
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string path)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Accept.ParseAdd("application/json");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.XBearerToken);
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

        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            throw new InvalidOperationException("X API rate limit exceeded for the current worker.");
        }

        throw new InvalidOperationException(
            string.IsNullOrWhiteSpace(message)
                ? $"X API request failed with status {(int)response.StatusCode}."
                : $"X API request failed: {message}"
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

            if (document.RootElement.TryGetProperty("detail", out var detailElement))
            {
                return detailElement.GetString();
            }

            if (document.RootElement.TryGetProperty("title", out var titleElement))
            {
                return titleElement.GetString();
            }

            if (document.RootElement.TryGetProperty("errors", out var errorsElement)
                && errorsElement.ValueKind == JsonValueKind.Array
                && errorsElement.GetArrayLength() > 0)
            {
                var firstError = errorsElement[0];
                if (firstError.TryGetProperty("message", out var errorMessageElement))
                {
                    return errorMessageElement.GetString();
                }

                if (firstError.TryGetProperty("detail", out var errorDetailElement))
                {
                    return errorDetailElement.GetString();
                }
            }

            return rawBody.Trim();
        }
        catch (JsonException)
        {
            return rawBody.Trim();
        }
    }
}
