namespace DigitalAmnesia.Worker.Models;

public sealed class PlatformScanOutcome
{
    public string Status { get; init; } = "completed";
    public string Message { get; init; } = string.Empty;
    public List<ScanResult> Results { get; init; } = [];
    public string? Error { get; init; }
}
