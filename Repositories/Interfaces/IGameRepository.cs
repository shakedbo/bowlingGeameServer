using BowlingGame.API.Models;

namespace BowlingGame.API.Repositories.Interfaces;

public interface IGameRepository
{
    Task<Game> CreateGameAsync(string playerName);
    Task<Game?> GetGameAsync(int gameId);
    Task UpdateGameAsync(Game game);
    Task<List<Game>> GetTopScorersAsync(int count);
}
