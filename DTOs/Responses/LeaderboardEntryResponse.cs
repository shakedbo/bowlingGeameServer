namespace BowlingGame.API.DTOs.Responses;

public class LeaderboardEntryResponse
{
    public int Rank { get; set; }
    public string PlayerName { get; set; } = string.Empty;
    public int Score { get; set; }
    public DateTime Date { get; set; }
}
