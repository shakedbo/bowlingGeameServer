namespace BowlingGame.API.Models;

public class Roll
{
    public int Id { get; set; }
    public int GameId { get; set; }
    public int Pins { get; set; }
    public int RollIndex { get; set; }
}
