using BowlingGame.API.DTOs.Requests;
using BowlingGame.API.DTOs.Responses;

namespace BowlingGame.API.Services.Interfaces;

public interface IBowlingService
{
    Task<GameResponse> StartGameAsync(CreateGameRequest request);
    Task<ScoreResponse> RollAsync(int gameId, AddRollRequest request);
    Task<ScoreResponse> GetScoreAsync(int gameId);
    Task<List<LeaderboardEntryResponse>> GetLeaderboardAsync(int count);
}
