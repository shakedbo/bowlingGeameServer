using BowlingGame.API.DTOs.Requests;
using BowlingGame.API.DTOs.Responses;

namespace BowlingGame.API.Services.Interfaces;

public interface IBowlingService
{
    Task<GameResponseDto> StartGameAsync(CreateGameRequestDro request);
    Task<ScoreResponseDto> RollAsync(int gameId, AddRollRequestDto request);
    Task<ScoreResponseDto> GetScoreAsync(int gameId);
    Task<List<LeaderboardEntryResponseDto>> GetLeaderboardAsync(int count);
}
