using BowlingGame.API.Models;

namespace BowlingGame.API.Repositories.Interfaces;

public interface IApiLogRepository
{
    Task InsertAsync(ApiLog log);
}
