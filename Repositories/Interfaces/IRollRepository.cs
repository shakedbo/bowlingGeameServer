using BowlingGame.API.Models;

namespace BowlingGame.API.Repositories.Interfaces;

public interface IRollRepository
{
    Task AddRollAsync(int gameId, int pins, int rollIndex);
    Task<List<Roll>> GetRollsAsync(int gameId);
}
