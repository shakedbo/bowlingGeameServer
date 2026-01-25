namespace BowlingGame.API.DTOs.Responses;

public class ScoreResponseDto
{
    public int GameId { get; set; }
    public string PlayerName { get; set; } = string.Empty;
    public int Score { get; set; }
    public bool IsComplete { get; set; }
    public int[] Rolls { get; set; } = Array.Empty<int>();
    public int CurrentFrame { get; set; }
}
