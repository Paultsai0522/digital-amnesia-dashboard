using System.Text.Json.Serialization;

namespace DigitalAmnesia.Worker.Models;

public sealed class GitHubSearchResponse
{
    [JsonPropertyName("items")]
    public List<GitHubUserSummary> Items { get; set; } = [];
}

public sealed class GitHubUserSummary
{
    [JsonPropertyName("login")]
    public string Login { get; set; } = string.Empty;

    [JsonPropertyName("html_url")]
    public string HtmlUrl { get; set; } = string.Empty;

    [JsonPropertyName("score")]
    public double Score { get; set; }
}

public sealed class GitHubUserProfile
{
    [JsonPropertyName("login")]
    public string Login { get; set; } = string.Empty;

    [JsonPropertyName("html_url")]
    public string HtmlUrl { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("bio")]
    public string? Bio { get; set; }

    [JsonPropertyName("company")]
    public string? Company { get; set; }

    [JsonPropertyName("location")]
    public string? Location { get; set; }

    [JsonPropertyName("blog")]
    public string? Blog { get; set; }

    [JsonPropertyName("followers")]
    public int Followers { get; set; }

    [JsonPropertyName("public_repos")]
    public int PublicRepos { get; set; }
}
