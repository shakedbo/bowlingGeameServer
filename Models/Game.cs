namespace BowlingGame.API.Models;

public class Game
{
    public int Id { get; set; }
    public string PlayerName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public GameStatus Status { get; set; }
    public int? FinalScore { get; set; }
}

public enum GameStatus : byte
{
    Active = 0,
    Completed = 1
}
