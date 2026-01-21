using BowlingGame.API.DTOs.Requests;
using BowlingGame.API.DTOs.Responses;
using BowlingGame.API.Exceptions;
using BowlingGame.API.Models;
using BowlingGame.API.Repositories.Interfaces;
using BowlingGame.API.Services.Interfaces;

namespace BowlingGame.API.Services;

public class BowlingService : IBowlingService
{
    private readonly IGameRepository _gameRepository;

    public BowlingService(IGameRepository gameRepository)
    {
        _gameRepository = gameRepository;
    }

    public async Task<GameResponse> StartGameAsync(CreateGameRequest request)
    {
        var game = await _gameRepository.CreateGameAsync(request.PlayerName);

        return new GameResponse
        {
            Id = game.Id,
            PlayerName = game.PlayerName,
            CreatedAt = game.CreatedAt,
            Status = game.Status.ToString()
        };
    }

    public async Task<ScoreResponse> RollAsync(int gameId, AddRollRequest request)
    {
        var game = await _gameRepository.GetGameAsync(gameId)
            ?? throw new GameNotFoundException(gameId);

        if (game.Status == GameStatus.Completed)
        {
            throw new GameCompletedException(gameId);
        }

        var rolls = await _gameRepository.GetRollsAsync(gameId);
        var pinsArray = rolls.Select(r => r.Pins).ToList();

        ValidateRoll(pinsArray, request.Pins);

        var rollIndex = rolls.Count;
        await _gameRepository.AddRollAsync(gameId, request.Pins, rollIndex);

        pinsArray.Add(request.Pins);

        var score = CalculateScore(pinsArray);
        var (currentFrame, isComplete) = GetGameState(pinsArray);

        if (isComplete)
        {
            game.Status = GameStatus.Completed;
            game.FinalScore = score;
            await _gameRepository.UpdateGameAsync(game);
        }

        return new ScoreResponse
        {
            GameId = gameId,
            PlayerName = game.PlayerName,
            Score = score,
            IsComplete = isComplete,
            Rolls = pinsArray.ToArray(),
            CurrentFrame = currentFrame
        };
    }

    public async Task<ScoreResponse> GetScoreAsync(int gameId)
    {
        var game = await _gameRepository.GetGameAsync(gameId)
            ?? throw new GameNotFoundException(gameId);

        var rolls = await _gameRepository.GetRollsAsync(gameId);
        var pinsArray = rolls.Select(r => r.Pins).ToList();

        var score = CalculateScore(pinsArray);
        var (currentFrame, isComplete) = GetGameState(pinsArray);

        return new ScoreResponse
        {
            GameId = gameId,
            PlayerName = game.PlayerName,
            Score = score,
            IsComplete = isComplete,
            Rolls = pinsArray.ToArray(),
            CurrentFrame = currentFrame
        };
    }

    public async Task<List<LeaderboardEntryResponse>> GetLeaderboardAsync(int count)
    {
        var topGames = await _gameRepository.GetTopScorersAsync(count);

        return topGames.Select((game, index) => new LeaderboardEntryResponse
        {
            Rank = index + 1,
            PlayerName = game.PlayerName,
            Score = game.FinalScore ?? 0,
            Date = game.CreatedAt
        }).ToList();
    }

    /// <summary>
    /// Validates if the roll is valid based on current game state.
    /// </summary>
    private void ValidateRoll(List<int> existingRolls, int pins)
    {
        if (pins < 0 || pins > 10)
        {
            throw new InvalidRollException(pins, "Pins must be between 0 and 10.");
        }

        var (currentFrame, isComplete) = GetGameState(existingRolls);

        if (isComplete)
        {
            throw new InvalidRollException("Game is already complete.");
        }

        // Determine position within current frame
        int rollsInCurrentFrame = GetRollsInCurrentFrame(existingRolls, currentFrame);

        if (currentFrame < 10)
        {
            // Frames 1-9: max 10 pins total per frame
            if (rollsInCurrentFrame == 1)
            {
                int firstRoll = GetFirstRollOfFrame(existingRolls, currentFrame);
                if (firstRoll < 10 && firstRoll + pins > 10)
                {
                    throw new InvalidRollException(pins, $"Cannot knock down {pins} pins. Only {10 - firstRoll} pins remaining in frame.");
                }
            }
        }
        else
        {
            // Frame 10: special rules
            ValidateTenthFrameRoll(existingRolls, pins, rollsInCurrentFrame);
        }
    }

    /// <summary>
    /// Validates rolls in the 10th frame with its special rules.
    /// </summary>
    private void ValidateTenthFrameRoll(List<int> existingRolls, int pins, int rollsInFrame)
    {
        var tenthFrameRolls = GetTenthFrameRolls(existingRolls);

        if (rollsInFrame == 1)
        {
            // Second roll in 10th frame
            int firstRoll = tenthFrameRolls[0];
            if (firstRoll < 10 && firstRoll + pins > 10)
            {
                throw new InvalidRollException(pins, $"Cannot knock down {pins} pins. Only {10 - firstRoll} pins remaining.");
            }
        }
        else if (rollsInFrame == 2)
        {
            // Third roll in 10th frame
            int firstRoll = tenthFrameRolls[0];
            int secondRoll = tenthFrameRolls[1];

            if (firstRoll == 10)
            {
                // First was strike
                if (secondRoll < 10 && secondRoll + pins > 10)
                {
                    throw new InvalidRollException(pins, $"Cannot knock down {pins} pins. Only {10 - secondRoll} pins remaining.");
                }
            }
            else if (firstRoll + secondRoll == 10)
            {
                // Spare - pins reset, any valid roll (0-10) is allowed
                // No additional validation needed
            }
        }
    }

    /// <summary>
    /// Calculates the bowling score based on standard rules.
    /// </summary>
    public int CalculateScore(List<int> rolls)
    {
        int score = 0;
        int rollIndex = 0;

        for (int frame = 1; frame <= 10; frame++)
        {
            if (rollIndex >= rolls.Count)
                break;

            if (frame < 10)
            {
                if (IsStrike(rolls, rollIndex))
                {
                    score += 10 + StrikeBonus(rolls, rollIndex);
                    rollIndex++;
                }
                else if (IsSpare(rolls, rollIndex))
                {
                    score += 10 + SpareBonus(rolls, rollIndex);
                    rollIndex += 2;
                }
                else
                {
                    score += GetFrameScore(rolls, rollIndex);
                    rollIndex += 2;
                }
            }
            else
            {
                // 10th frame: sum all remaining rolls (up to 3)
                while (rollIndex < rolls.Count)
                {
                    score += rolls[rollIndex];
                    rollIndex++;
                }
            }
        }

        return score;
    }

    /// <summary>
    /// Gets the current frame (1-10) and whether the game is complete.
    /// </summary>
    public (int Frame, bool IsComplete) GetGameState(List<int> rolls)
    {
        if (rolls.Count == 0)
            return (1, false);

        int rollIndex = 0;

        for (int frame = 1; frame <= 10; frame++)
        {
            if (rollIndex >= rolls.Count)
                return (frame, false);

            if (frame < 10)
            {
                if (IsStrike(rolls, rollIndex))
                {
                    rollIndex++;
                    continue;
                }
                else
                {
                    rollIndex += 2;
                    if (rollIndex > rolls.Count)
                        return (frame, false);
                }
            }
            else
            {
                // 10th frame
                int tenthFrameStart = rollIndex;
                int tenthFrameRolls = rolls.Count - tenthFrameStart;

                if (tenthFrameRolls == 0)
                    return (10, false);

                int firstRoll = rolls[tenthFrameStart];

                if (firstRoll == 10)
                {
                    // Strike in 10th: need 3 rolls total
                    return (10, tenthFrameRolls >= 3);
                }

                if (tenthFrameRolls < 2)
                    return (10, false);

                int secondRoll = rolls[tenthFrameStart + 1];

                if (firstRoll + secondRoll == 10)
                {
                    // Spare in 10th: need 3 rolls total
                    return (10, tenthFrameRolls >= 3);
                }

                // Open frame in 10th: only 2 rolls needed
                return (10, tenthFrameRolls >= 2);
            }
        }

        return (10, true);
    }

    private bool IsStrike(List<int> rolls, int rollIndex)
    {
        return rollIndex < rolls.Count && rolls[rollIndex] == 10;
    }

    private bool IsSpare(List<int> rolls, int rollIndex)
    {
        return rollIndex + 1 < rolls.Count && rolls[rollIndex] + rolls[rollIndex + 1] == 10;
    }

    private int StrikeBonus(List<int> rolls, int rollIndex)
    {
        int bonus = 0;
        if (rollIndex + 1 < rolls.Count)
            bonus += rolls[rollIndex + 1];
        if (rollIndex + 2 < rolls.Count)
            bonus += rolls[rollIndex + 2];
        return bonus;
    }

    private int SpareBonus(List<int> rolls, int rollIndex)
    {
        if (rollIndex + 2 < rolls.Count)
            return rolls[rollIndex + 2];
        return 0;
    }

    private int GetFrameScore(List<int> rolls, int rollIndex)
    {
        int score = 0;
        if (rollIndex < rolls.Count)
            score += rolls[rollIndex];
        if (rollIndex + 1 < rolls.Count)
            score += rolls[rollIndex + 1];
        return score;
    }

    private int GetRollsInCurrentFrame(List<int> rolls, int currentFrame)
    {
        int rollIndex = 0;

        for (int frame = 1; frame < currentFrame; frame++)
        {
            if (rollIndex >= rolls.Count)
                return 0;

            if (frame < 10)
            {
                if (rolls[rollIndex] == 10)
                {
                    rollIndex++;
                }
                else
                {
                    rollIndex += 2;
                }
            }
        }

        return rolls.Count - rollIndex;
    }

    private int GetFirstRollOfFrame(List<int> rolls, int targetFrame)
    {
        int rollIndex = 0;

        for (int frame = 1; frame < targetFrame; frame++)
        {
            if (rollIndex >= rolls.Count)
                return 0;

            if (frame < 10)
            {
                if (rolls[rollIndex] == 10)
                {
                    rollIndex++;
                }
                else
                {
                    rollIndex += 2;
                }
            }
        }

        return rollIndex < rolls.Count ? rolls[rollIndex] : 0;
    }

    private List<int> GetTenthFrameRolls(List<int> rolls)
    {
        int rollIndex = 0;

        for (int frame = 1; frame < 10; frame++)
        {
            if (rollIndex >= rolls.Count)
                return new List<int>();

            if (rolls[rollIndex] == 10)
            {
                rollIndex++;
            }
            else
            {
                rollIndex += 2;
            }
        }

        return rolls.Skip(rollIndex).ToList();
    }
}
