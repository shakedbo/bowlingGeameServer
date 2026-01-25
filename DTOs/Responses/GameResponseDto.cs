namespace BowlingGame.API.DTOs.Responses;

public class GameResponseDto
{
    public int Id { get; set; }
    public string PlayerName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string Status { get; set; } = string.Empty;
}
