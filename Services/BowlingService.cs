using BowlingGame.API.DTOs.Requests;
using BowlingGame.API.DTOs.Responses;
using BowlingGame.API.Exceptions;
using BowlingGame.API.Models;
using BowlingGame.API.Repositories.Interfaces;
using BowlingGame.API.Services.Interfaces;

namespace BowlingGame.API.Services;

/// <summary>
/// Service that implements the core bowling game logic including scoring, validation, and game state management.
/// Follows standard 10-pin bowling rules with special handling for the 10th frame.
/// </summary>
public class BowlingService : IBowlingService
{
    private const int TotalFrames = 10;
    private const int MaxPinsPerRoll = 10;

    private readonly IGameRepository _gameRepository;
    private readonly IRollRepository _rollRepository;

    public BowlingService(IGameRepository gameRepository, IRollRepository rollRepository)
    {
        _gameRepository = gameRepository;
        _rollRepository = rollRepository;
    }

    #region Public API Methods

    /// <summary>
    /// Creates a new bowling game for the specified player.
    /// </summary>
    /// <param name="request">Request containing the player's name.</param>
    /// <returns>A response containing the newly created game details.</returns>
    public async Task<GameResponseDto> StartGameAsync(CreateGameRequestDro request)
    {
        var game = await _gameRepository.CreateGameAsync(request.PlayerName);

        return new GameResponseDto
        {
            Id = game.Id,
            PlayerName = game.PlayerName,
            CreatedAt = game.CreatedAt,
            Status = game.Status.ToString()
        };
    }

    /// <summary>
    /// Records a roll (ball throw) for an active game and returns the updated score.
    /// Validates the roll, persists it, calculates the new score, and marks the game as complete if finished.
    /// </summary>
    /// <param name="gameId">The unique identifier of the game.</param>
    /// <param name="request">Request containing the number of pins knocked down.</param>
    /// <returns>Updated score response including current frame and completion status.</returns>
    /// <exception cref="GameNotFoundException">Thrown when the game doesn't exist.</exception>
    /// <exception cref="GameCompletedException">Thrown when trying to roll in a completed game.</exception>
    /// <exception cref="InvalidRollException">Thrown when the roll violates bowling rules.</exception>
    public async Task<ScoreResponseDto> RollAsync(int gameId, AddRollRequestDto request)
    {
        var game = await _gameRepository.GetGameAsync(gameId)
            ?? throw new GameNotFoundException(gameId);

        if (game.Status == GameStatus.Completed)
        {
            throw new GameCompletedException(gameId);
        }

        var rolls = await _rollRepository.GetRollsAsync(gameId);
        var pinsArray = rolls.Select(r => r.Pins).ToList();

        ValidateRoll(pinsArray, request.Pins);

        await _rollRepository.AddRollAsync(gameId, request.Pins, rolls.Count);
        pinsArray.Add(request.Pins);

        var score = CalculateScore(pinsArray);
        var (currentFrame, isComplete) = GetGameState(pinsArray);

        if (isComplete)
        {
            await MarkGameAsCompleted(game, score);
        }

        return BuildScoreResponse(gameId, game.PlayerName, pinsArray, score, currentFrame, isComplete);
    }

    /// <summary>
    /// Retrieves the current score and game state for a specific game.
    /// </summary>
    /// <param name="gameId">The unique identifier of the game.</param>
    /// <returns>Score response with current game state.</returns>
    /// <exception cref="GameNotFoundException">Thrown when the game doesn't exist.</exception>
    public async Task<ScoreResponseDto> GetScoreAsync(int gameId)
    {
        var game = await _gameRepository.GetGameAsync(gameId)
            ?? throw new GameNotFoundException(gameId);

        var rolls = await _rollRepository.GetRollsAsync(gameId);
        var pinsArray = rolls.Select(r => r.Pins).ToList();

        var score = CalculateScore(pinsArray);
        var (currentFrame, isComplete) = GetGameState(pinsArray);

        return BuildScoreResponse(gameId, game.PlayerName, pinsArray, score, currentFrame, isComplete);
    }

    /// <summary>
    /// Retrieves the top scores across all completed games.
    /// </summary>
    /// <param name="count">Maximum number of leaderboard entries to return.</param>
    /// <returns>List of leaderboard entries sorted by score (highest first).</returns>
    public async Task<List<LeaderboardEntryResponseDto>> GetLeaderboardAsync(int count)
    {
        var topGames = await _gameRepository.GetTopScorersAsync(count);

        return topGames.Select((game, index) => new LeaderboardEntryResponseDto
        {
            Rank = index + 1,
            PlayerName = game.PlayerName,
            Score = game.FinalScore ?? 0,
            Date = game.CreatedAt
        }).ToList();
    }

    #endregion

    #region Roll Validation

    /// <summary>
    /// Validates if a roll is legal based on bowling rules and current game state.
    /// Checks: pins range (0-10), game completion, and frame-specific pin limits.
    /// </summary>
    /// <param name="existingRolls">All rolls made so far in the game.</param>
    /// <param name="pins">Number of pins the player is attempting to knock down.</param>
    /// <exception cref="InvalidRollException">Thrown when the roll violates any bowling rule.</exception>
    private void ValidateRoll(List<int> existingRolls, int pins)
    {
        ValidatePinsInRange(pins);

        var (currentFrame, isComplete) = GetGameState(existingRolls);

        if (isComplete)
        {
            throw new InvalidRollException("Game is already complete.");
        }

        int rollsInCurrentFrame = CountRollsInCurrentFrame(existingRolls, currentFrame);

        if (currentFrame < TotalFrames)
        {
            ValidateRegularFrameRoll(existingRolls, pins, currentFrame, rollsInCurrentFrame);
        }
        else
        {
            ValidateTenthFrameRoll(existingRolls, pins, rollsInCurrentFrame);
        }
    }

    /// <summary>
    /// Ensures pins value is within the valid range of 0-10.
    /// </summary>
    private void ValidatePinsInRange(int pins)
    {
        if (pins < 0 || pins > MaxPinsPerRoll)
        {
            throw new InvalidRollException(pins, "Pins must be between 0 and 10.");
        }
    }

    /// <summary>
    /// Validates a roll in frames 1-9 where the sum of two rolls cannot exceed 10 pins.
    /// </summary>
    private void ValidateRegularFrameRoll(List<int> existingRolls, int pins, int currentFrame, int rollsInCurrentFrame)
    {
        if (rollsInCurrentFrame == 1)
        {
            int firstRoll = GetFirstRollOfFrame(existingRolls, currentFrame);
            int remainingPins = MaxPinsPerRoll - firstRoll;

            if (firstRoll < MaxPinsPerRoll && pins > remainingPins)
            {
                throw new InvalidRollException(pins, $"Cannot knock down {pins} pins. Only {remainingPins} pins remaining in frame.");
            }
        }
    }

    /// <summary>
    /// Validates rolls in the 10th frame which has special rules:
    /// - Strike allows 2 bonus rolls (pins reset after each strike)
    /// - Spare allows 1 bonus roll (pins reset)
    /// - Open frame (no strike/spare) ends after 2 rolls
    /// </summary>
    /// <param name="existingRolls">All rolls made so far in the game.</param>
    /// <param name="pins">Number of pins being knocked down.</param>
    /// <param name="rollsInFrame">Number of rolls already made in the 10th frame (0, 1, or 2).</param>
    private void ValidateTenthFrameRoll(List<int> existingRolls, int pins, int rollsInFrame)
    {
        var tenthFrameRolls = GetTenthFrameRolls(existingRolls);

        if (rollsInFrame == 1)
        {
            ValidateSecondRollInTenthFrame(tenthFrameRolls[0], pins);
        }
        else if (rollsInFrame == 2)
        {
            ValidateThirdRollInTenthFrame(tenthFrameRolls[0], tenthFrameRolls[1], pins);
        }
    }

    /// <summary>
    /// Validates the second roll in the 10th frame.
    /// If first roll wasn't a strike, remaining pins are limited.
    /// </summary>
    private void ValidateSecondRollInTenthFrame(int firstRoll, int pins)
    {
        if (firstRoll < MaxPinsPerRoll)
        {
            int remainingPins = MaxPinsPerRoll - firstRoll;
            if (pins > remainingPins)
            {
                throw new InvalidRollException(pins, $"Cannot knock down {pins} pins. Only {remainingPins} pins remaining.");
            }
        }
    }

    /// <summary>
    /// Validates the third roll in the 10th frame.
    /// Pins are reset after a strike or spare, otherwise third roll is not allowed (handled by game state).
    /// </summary>
    private void ValidateThirdRollInTenthFrame(int firstRoll, int secondRoll, int pins)
    {
        bool firstWasStrike = firstRoll == MaxPinsPerRoll;
        bool wasSpare = firstRoll + secondRoll == MaxPinsPerRoll;

        if (firstWasStrike && secondRoll < MaxPinsPerRoll)
        {
            int remainingPins = MaxPinsPerRoll - secondRoll;
            if (pins > remainingPins)
            {
                throw new InvalidRollException(pins, $"Cannot knock down {pins} pins. Only {remainingPins} pins remaining.");
            }
        }
        // If spare or second strike, pins are reset - any value 0-10 is valid (already checked by ValidatePinsInRange)
    }

    #endregion

    #region Score Calculation

    /// <summary>
    /// Calculates the total bowling score based on standard 10-pin bowling rules.
    /// 
    /// Scoring Rules:
    /// - Frames 1-9: Strike (10 pins on first roll) = 10 + next 2 rolls as bonus
    /// - Frames 1-9: Spare (10 pins in 2 rolls) = 10 + next 1 roll as bonus
    /// - Frames 1-9: Open frame = sum of the 2 rolls (no bonus)
    /// - Frame 10: Sum of all rolls (up to 3), no bonus calculation
    /// 
    /// Maximum possible score is 300 (perfect game: 12 strikes).
    /// </summary>
    /// <param name="rolls">List of all pins knocked down per roll.</param>
    /// <returns>Total calculated score.</returns>
    public int CalculateScore(List<int> rolls)
    {
        int score = 0;
        int rollIndex = 0;

        for (int frame = 1; frame <= TotalFrames && rollIndex < rolls.Count; frame++)
        {
            if (frame < TotalFrames)
            {
                score += CalculateRegularFrameScore(rolls, ref rollIndex);
            }
            else
            {
                score += CalculateTenthFrameScore(rolls, rollIndex);
            }
        }

        return score;
    }

    /// <summary>
    /// Calculates the score for frames 1-9, including strike and spare bonuses.
    /// Advances the rollIndex appropriately (1 for strike, 2 for non-strike).
    /// </summary>
    private int CalculateRegularFrameScore(List<int> rolls, ref int rollIndex)
    {
        if (IsStrike(rolls, rollIndex))
        {
            int frameScore = MaxPinsPerRoll + GetStrikeBonus(rolls, rollIndex);
            rollIndex++;
            return frameScore;
        }

        if (IsSpare(rolls, rollIndex))
        {
            int frameScore = MaxPinsPerRoll + GetSpareBonus(rolls, rollIndex);
            rollIndex += 2;
            return frameScore;
        }

        int openFrameScore = GetOpenFrameScore(rolls, rollIndex);
        rollIndex += 2;
        return openFrameScore;
    }

    /// <summary>
    /// Calculates the score for the 10th frame by summing all remaining rolls.
    /// No bonus calculation - just straight addition of pins.
    /// </summary>
    private int CalculateTenthFrameScore(List<int> rolls, int startIndex)
    {
        int score = 0;
        for (int i = startIndex; i < rolls.Count; i++)
        {
            score += rolls[i];
        }
        return score;
    }

    /// <summary>
    /// Checks if the roll at the given index is a strike (all 10 pins on first roll).
    /// </summary>
    private bool IsStrike(List<int> rolls, int rollIndex)
    {
        return rollIndex < rolls.Count && rolls[rollIndex] == MaxPinsPerRoll;
    }

    /// <summary>
    /// Checks if the two rolls starting at the given index form a spare (10 pins total).
    /// </summary>
    private bool IsSpare(List<int> rolls, int rollIndex)
    {
        return rollIndex + 1 < rolls.Count && 
               rolls[rollIndex] + rolls[rollIndex + 1] == MaxPinsPerRoll;
    }

    /// <summary>
    /// Gets the strike bonus: sum of the next 2 rolls after the strike.
    /// Returns partial bonus if not all bonus rolls have been made yet.
    /// </summary>
    private int GetStrikeBonus(List<int> rolls, int rollIndex)
    {
        int bonus = 0;
        if (rollIndex + 1 < rolls.Count) bonus += rolls[rollIndex + 1];
        if (rollIndex + 2 < rolls.Count) bonus += rolls[rollIndex + 2];
        return bonus;
    }

    /// <summary>
    /// Gets the spare bonus: the next roll after the spare.
    /// Returns 0 if the bonus roll hasn't been made yet.
    /// </summary>
    private int GetSpareBonus(List<int> rolls, int rollIndex)
    {
        return rollIndex + 2 < rolls.Count ? rolls[rollIndex + 2] : 0;
    }

    /// <summary>
    /// Gets the score for an open frame (no strike or spare): sum of both rolls.
    /// </summary>
    private int GetOpenFrameScore(List<int> rolls, int rollIndex)
    {
        int score = 0;
        if (rollIndex < rolls.Count) score += rolls[rollIndex];
        if (rollIndex + 1 < rolls.Count) score += rolls[rollIndex + 1];
        return score;
    }

    #endregion

    #region Game State Management

    /// <summary>
    /// Determines the current frame (1-10) and whether the game is complete.
    /// 
    /// Frame progression rules:
    /// - Frames 1-9: Advance after strike (1 roll) or 2 rolls
    /// - Frame 10: Complete after 2 rolls (open) or 3 rolls (strike/spare)
    /// </summary>
    /// <param name="rolls">List of all pins knocked down per roll.</param>
    /// <returns>Tuple of (current frame number, whether game is complete).</returns>
    public (int Frame, bool IsComplete) GetGameState(List<int> rolls)
    {
        if (rolls == null || rolls.Count == 0)
        {
            return (1, false);
        }

        int rollIndex = 0;

        // Process frames 1-9
        for (int frame = 1; frame < TotalFrames; frame++)
        {
            if (rollIndex >= rolls.Count)
            {
                return (frame, false);
            }

            rollIndex += IsStrike(rolls, rollIndex) ? 1 : 2;

            if (rollIndex > rolls.Count)
            {
                return (frame, false);
            }
        }

        // Process frame 10
        return EvaluateTenthFrameCompletion(rolls, rollIndex);
    }

    /// <summary>
    /// Evaluates whether the 10th frame is complete based on its special rules:
    /// - Strike: requires 3 total rolls
    /// - Spare: requires 3 total rolls  
    /// - Open (no strike/spare): requires only 2 rolls
    /// </summary>
    private (int Frame, bool IsComplete) EvaluateTenthFrameCompletion(List<int> rolls, int tenthFrameStart)
    {
        int tenthFrameRolls = rolls.Count - tenthFrameStart;

        if (tenthFrameRolls == 0)
        {
            return (TotalFrames, false);
        }

        int firstRoll = rolls[tenthFrameStart];
        bool firstWasStrike = firstRoll == MaxPinsPerRoll;

        if (firstWasStrike)
        {
            return (TotalFrames, tenthFrameRolls >= 3);
        }

        if (tenthFrameRolls < 2)
        {
            return (TotalFrames, false);
        }

        int secondRoll = rolls[tenthFrameStart + 1];
        bool wasSpare = firstRoll + secondRoll == MaxPinsPerRoll;

        if (wasSpare)
        {
            return (TotalFrames, tenthFrameRolls >= 3);
        }

        // Open frame in 10th: complete after 2 rolls
        return (TotalFrames, tenthFrameRolls >= 2);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Counts how many rolls have been made in the specified frame.
    /// Used to determine position within a frame for validation.
    /// </summary>
    private int CountRollsInCurrentFrame(List<int> rolls, int currentFrame)
    {
        int rollIndex = AdvanceToFrame(rolls, currentFrame);
        return rolls.Count - rollIndex;
    }

    /// <summary>
    /// Gets the pins knocked down on the first roll of the specified frame.
    /// Used for validating that second roll doesn't exceed remaining pins.
    /// </summary>
    private int GetFirstRollOfFrame(List<int> rolls, int targetFrame)
    {
        int rollIndex = AdvanceToFrame(rolls, targetFrame);
        return rollIndex < rolls.Count ? rolls[rollIndex] : 0;
    }

    /// <summary>
    /// Gets all rolls made in the 10th frame as a separate list.
    /// Used for special 10th frame validation logic.
    /// </summary>
    private List<int> GetTenthFrameRolls(List<int> rolls)
    {
        int rollIndex = AdvanceToFrame(rolls, TotalFrames);
        return rolls.Skip(rollIndex).ToList();
    }

    /// <summary>
    /// Advances through rolls to find the starting index of the specified frame.
    /// Handles both strikes (1 roll) and non-strikes (2 rolls) in frames 1-9.
    /// </summary>
    /// <param name="rolls">List of all pins knocked down per roll.</param>
    /// <param name="targetFrame">The frame to find (1-10).</param>
    /// <returns>The roll index where the target frame begins.</returns>
    private int AdvanceToFrame(List<int> rolls, int targetFrame)
    {
        int rollIndex = 0;

        for (int frame = 1; frame < targetFrame && frame < TotalFrames; frame++)
        {
            if (rollIndex >= rolls.Count)
            {
                break;
            }

            rollIndex += rolls[rollIndex] == MaxPinsPerRoll ? 1 : 2;
        }

        return rollIndex;
    }

    /// <summary>
    /// Marks the game as completed with the final score and persists the update.
    /// </summary>
    private async Task MarkGameAsCompleted(Game game, int finalScore)
    {
        game.Status = GameStatus.Completed;
        game.FinalScore = finalScore;
        await _gameRepository.UpdateGameAsync(game);
    }

    /// <summary>
    /// Builds a standardized score response DTO from game state data.
    /// </summary>
    private static ScoreResponseDto BuildScoreResponse(int gameId, string playerName, List<int> rolls, int score, int currentFrame, bool isComplete)
    {
        return new ScoreResponseDto
        {
            GameId = gameId,
            PlayerName = playerName,
            Score = score,
            IsComplete = isComplete,
            Rolls = rolls.ToArray(),
            CurrentFrame = currentFrame
        };
    }

    #endregion
}
