namespace DigitalAmnesia.Worker.Models;

public sealed class ScanQuery
{
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public List<string> Keywords { get; set; } = [];
}

public sealed class ScanTarget
{
    public string Platform { get; set; } = string.Empty;
    public string Status { get; set; } = "queued";
    public string Message { get; set; } = string.Empty;
}

public sealed class ScanResult
{
    public string Id { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public string ProfileUrl { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    public string MatchLevel { get; set; } = "low";
    public int MatchScore { get; set; }
    public List<string> MatchReasons { get; set; } = [];
}

public sealed class ScanJob
{
    public string Id { get; set; } = string.Empty;
    public string Status { get; set; } = "queued";
    public int Progress { get; set; }
    public ScanQuery Query { get; set; } = new();
    public List<ScanTarget> Targets { get; set; } = [];
    public List<ScanResult> Results { get; set; } = [];
    public string? Error { get; set; }
    public string? WorkerId { get; set; }
    public string CreatedAt { get; set; } = string.Empty;
    public string UpdatedAt { get; set; } = string.Empty;
    public string? StartedAt { get; set; }
    public string? CompletedAt { get; set; }
}

public sealed class ClaimJobResponse
{
    public ScanJob? Job { get; set; }
}
