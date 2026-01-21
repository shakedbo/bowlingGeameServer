namespace BowlingGame.API.Exceptions;

public class GameCompletedException : Exception
{
    public int GameId { get; }

    public GameCompletedException(int gameId)
        : base($"Game with ID {gameId} is already completed. No more rolls allowed.")
    {
        GameId = gameId;
    }
}
