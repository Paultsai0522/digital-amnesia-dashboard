using System.Text.Json.Serialization;

namespace DigitalAmnesia.Worker.Models;

public sealed class XUserLookupResponse
{
    [JsonPropertyName("data")]
    public XUserProfile? Data { get; set; }

    [JsonPropertyName("errors")]
    public List<XApiError> Errors { get; set; } = [];
}

public sealed class XUserSearchResponse
{
    [JsonPropertyName("data")]
    public List<XUserProfile> Data { get; set; } = [];

    [JsonPropertyName("meta")]
    public XSearchMetadata? Meta { get; set; }

    [JsonPropertyName("errors")]
    public List<XApiError> Errors { get; set; } = [];
}

public sealed class XUserProfile
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("verified")]
    public bool Verified { get; set; }

    [JsonPropertyName("protected")]
    public bool IsProtected { get; set; }

    [JsonPropertyName("profile_image_url")]
    public string? ProfileImageUrl { get; set; }

    [JsonPropertyName("public_metrics")]
    public XUserPublicMetrics? PublicMetrics { get; set; }
}

public sealed class XUserPublicMetrics
{
    [JsonPropertyName("followers_count")]
    public int FollowersCount { get; set; }

    [JsonPropertyName("following_count")]
    public int FollowingCount { get; set; }

    [JsonPropertyName("tweet_count")]
    public int TweetCount { get; set; }

    [JsonPropertyName("listed_count")]
    public int ListedCount { get; set; }

    [JsonPropertyName("like_count")]
    public int LikeCount { get; set; }

    [JsonPropertyName("media_count")]
    public int MediaCount { get; set; }
}

public sealed class XSearchMetadata
{
    [JsonPropertyName("result_count")]
    public int ResultCount { get; set; }
}

public sealed class XApiError
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("detail")]
    public string? Detail { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}
