namespace BowlingGame.API.Exceptions;

public class GameNotFoundException : Exception
{
    public int GameId { get; }

    public GameNotFoundException(int gameId)
        : base($"Game with ID {gameId} was not found.")
    {
        GameId = gameId;
    }
}
