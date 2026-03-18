using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using DigitalAmnesia.Worker.Models;

namespace DigitalAmnesia.Worker.Services;

public sealed class WorkerApiClient
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _serializerOptions;

    public WorkerApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _serializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
    }

    public async Task<ScanJob?> ClaimNextQueuedJobAsync(string workerId, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsJsonAsync(
            "/internal/jobs/claim",
            new { workerId },
            _serializerOptions,
            cancellationToken
        );

        if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        var payload = await response.Content.ReadFromJsonAsync<ClaimJobResponse>(_serializerOptions, cancellationToken);

        return payload?.Job;
    }

    public async Task UpdateJobAsync(string jobId, object patch, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(new HttpMethod("PATCH"), $"/internal/jobs/{jobId}")
        {
            Content = new StringContent(
                JsonSerializer.Serialize(patch, _serializerOptions),
                Encoding.UTF8,
                "application/json"
            ),
        };

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new InvalidOperationException(
            string.IsNullOrWhiteSpace(body)
                ? $"Request failed with status {(int)response.StatusCode}."
                : body
        );
    }
}
