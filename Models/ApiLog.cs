namespace BowlingGame.API.Models;

public class ApiLog
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string HttpMethod { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public string? RequestBody { get; set; }
    public string? ResponseBody { get; set; }
    public long DurationMs { get; set; }
    public string? ExceptionMessage { get; set; }
}
