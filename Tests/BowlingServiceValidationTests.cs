using BowlingGame.API.Models;
using BowlingGame.API.Services;
using BowlingGame.API.Repositories.Interfaces;
using BowlingGame.API.Exceptions;
using BowlingGame.API.DTOs.Requests;
using Moq;

namespace BowlingGame.Tests;

/// <summary>
/// Unit tests for roll validation and exception handling.
/// </summary>
public class BowlingServiceValidationTests
{
    private readonly Mock<IGameRepository> _mockRepository;
    private readonly Mock<IRollRepository> _mockRollRepository;
    private readonly BowlingService _service;

    public BowlingServiceValidationTests()
    {
        _mockRepository = new Mock<IGameRepository>();
        _mockRollRepository = new Mock<IRollRepository>();
        _service = new BowlingService(_mockRepository.Object, _mockRollRepository.Object);
    }

    #region RollAsync Exception Tests

    [Fact]
    public async Task RollAsync_NonExistentGame_ThrowsGameNotFoundException()
    {
        _mockRepository.Setup(r => r.GetGameAsync(It.IsAny<int>()))
            .ReturnsAsync((Game?)null);

        await Assert.ThrowsAsync<GameNotFoundException>(() => 
            _service.RollAsync(999, new AddRollRequestDto { Pins = 5 }));
    }

    [Fact]
    public async Task RollAsync_CompletedGame_ThrowsGameCompletedException()
    {
        var game = new Game { Id = 1, PlayerName = "Test", Status = GameStatus.Completed };
        _mockRepository.Setup(r => r.GetGameAsync(1)).ReturnsAsync(game);

        await Assert.ThrowsAsync<GameCompletedException>(() => 
            _service.RollAsync(1, new AddRollRequestDto { Pins = 5 }));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(11)]
    [InlineData(100)]
    public async Task RollAsync_InvalidPins_ThrowsInvalidRollException(int invalidPins)
    {
        var game = new Game { Id = 1, PlayerName = "Test", Status = GameStatus.Active };
        var rolls = new List<Roll>();
        
        _mockRepository.Setup(r => r.GetGameAsync(1)).ReturnsAsync(game);
        _mockRollRepository.Setup(r => r.GetRollsAsync(1)).ReturnsAsync(rolls);

        await Assert.ThrowsAsync<InvalidRollException>(() => 
            _service.RollAsync(1, new AddRollRequestDto { Pins = invalidPins }));
    }

    [Fact]
    public async Task RollAsync_ExceedsFrameTotal_ThrowsInvalidRollException()
    {
        var game = new Game { Id = 1, PlayerName = "Test", Status = GameStatus.Active };
        var rolls = new List<Roll>
        {
            new() { Pins = 7, RollIndex = 0 } // First roll knocked 7 pins
        };
        
        _mockRepository.Setup(r => r.GetGameAsync(1)).ReturnsAsync(game);
        _mockRollRepository.Setup(r => r.GetRollsAsync(1)).ReturnsAsync(rolls);

        // Trying to knock 5 pins when only 3 remain (7+5=12 > 10)
        await Assert.ThrowsAsync<InvalidRollException>(() => 
            _service.RollAsync(1, new AddRollRequestDto { Pins = 5 }));
    }

    #endregion

    #region RollAsync Success Tests

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    [InlineData(10)]
    public async Task RollAsync_ValidFirstRoll_ReturnsScore(int pins)
    {
        var game = new Game { Id = 1, PlayerName = "Test", Status = GameStatus.Active };
        var rolls = new List<Roll>();
        
        _mockRepository.Setup(r => r.GetGameAsync(1)).ReturnsAsync(game);
        _mockRollRepository.Setup(r => r.GetRollsAsync(1)).ReturnsAsync(rolls);
        _mockRollRepository.Setup(r => r.AddRollAsync(1, pins, 0)).Returns(Task.CompletedTask);

        var result = await _service.RollAsync(1, new AddRollRequestDto { Pins = pins });

        Assert.Equal(pins, result.Score);
        Assert.Equal(1, result.GameId);
        Assert.False(result.IsComplete);
        _mockRollRepository.Verify(r => r.AddRollAsync(1, pins, 0), Times.Once);
    }

    [Fact]
    public async Task RollAsync_AfterStrike_AllowsFullPinsInNextFrame()
    {
        var game = new Game { Id = 1, PlayerName = "Test", Status = GameStatus.Active };
        var rolls = new List<Roll>
        {
            new() { Pins = 10, RollIndex = 0 } // Strike in first frame
        };
        
        _mockRepository.Setup(r => r.GetGameAsync(1)).ReturnsAsync(game);
        _mockRollRepository.Setup(r => r.GetRollsAsync(1)).ReturnsAsync(rolls);
        _mockRollRepository.Setup(r => r.AddRollAsync(1, 8, 1)).Returns(Task.CompletedTask);

        var result = await _service.RollAsync(1, new AddRollRequestDto { Pins = 8 });

        Assert.Equal(2, result.CurrentFrame); // Now in frame 2
        _mockRollRepository.Verify(r => r.AddRollAsync(1, 8, 1), Times.Once);
    }

    [Fact]
    public async Task RollAsync_CompleteGame_UpdatesStatus()
    {
        var game = new Game { Id = 1, PlayerName = "Test", Status = GameStatus.Active };
        // 19 gutter balls before the final roll
        var rolls = Enumerable.Range(0, 19).Select(i => new Roll { Pins = 0, RollIndex = i }).ToList();
        
        _mockRepository.Setup(r => r.GetGameAsync(1)).ReturnsAsync(game);
        _mockRollRepository.Setup(r => r.GetRollsAsync(1)).ReturnsAsync(rolls);
        _mockRollRepository.Setup(r => r.AddRollAsync(1, 0, 19)).Returns(Task.CompletedTask);
        _mockRepository.Setup(r => r.UpdateGameAsync(It.Is<Game>(g => 
            g.Status == GameStatus.Completed && g.FinalScore == 0)))
            .Returns(Task.CompletedTask);

        var result = await _service.RollAsync(1, new AddRollRequestDto { Pins = 0 });

        Assert.True(result.IsComplete);
        _mockRepository.Verify(r => r.UpdateGameAsync(It.Is<Game>(g => 
            g.Status == GameStatus.Completed)), Times.Once);
    }

    #endregion

    #region GetScoreAsync Tests

    [Fact]
    public async Task GetScoreAsync_NonExistentGame_ThrowsGameNotFoundException()
    {
        _mockRepository.Setup(r => r.GetGameAsync(It.IsAny<int>()))
            .ReturnsAsync((Game?)null);

        await Assert.ThrowsAsync<GameNotFoundException>(() => _service.GetScoreAsync(999));
    }

    [Fact]
    public async Task GetScoreAsync_ExistingGame_ReturnsCorrectScore()
    {
        var game = new Game { Id = 1, PlayerName = "Test", Status = GameStatus.Active };
        var rolls = new List<Roll>
        {
            new() { Pins = 10, RollIndex = 0 },  // Strike
            new() { Pins = 3, RollIndex = 1 },
            new() { Pins = 4, RollIndex = 2 }
        };
        
        _mockRepository.Setup(r => r.GetGameAsync(1)).ReturnsAsync(game);
        _mockRollRepository.Setup(r => r.GetRollsAsync(1)).ReturnsAsync(rolls);

        var response = await _service.GetScoreAsync(1);

        Assert.Equal(24, response.Score); // (10+3+4) + 7 = 24
        Assert.Equal(3, response.CurrentFrame); // Strike completes frame 1, 3+4 completes frame 2, now in frame 3
        Assert.False(response.IsComplete);
    }

    #endregion

    #region StartGameAsync Tests

    [Fact]
    public async Task StartGameAsync_ValidPlayer_CreatesGame()
    {
        var newGame = new Game 
        { 
            Id = 42, 
            PlayerName = "TestPlayer", 
            Status = GameStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        
        _mockRepository.Setup(r => r.CreateGameAsync("TestPlayer")).ReturnsAsync(newGame);

        var result = await _service.StartGameAsync(new CreateGameRequestDro { PlayerName = "TestPlayer" });

        Assert.Equal(42, result.Id);
        Assert.Equal("TestPlayer", result.PlayerName);
        _mockRepository.Verify(r => r.CreateGameAsync("TestPlayer"), Times.Once);
    }

    #endregion

    #region GetLeaderboardAsync Tests

    [Fact]
    public async Task GetLeaderboardAsync_ReturnsFromRepository()
    {
        var topGames = new List<Game>
        {
            new() { Id = 1, PlayerName = "Player1", FinalScore = 200, Status = GameStatus.Completed, CreatedAt = DateTime.UtcNow },
            new() { Id = 2, PlayerName = "Player2", FinalScore = 180, Status = GameStatus.Completed, CreatedAt = DateTime.UtcNow }
        };
        
        _mockRepository.Setup(r => r.GetTopScorersAsync(10)).ReturnsAsync(topGames);

        var result = await _service.GetLeaderboardAsync(10);

        Assert.Equal(2, result.Count);
        Assert.Equal("Player1", result[0].PlayerName);
        Assert.Equal(200, result[0].Score);
        Assert.Equal(1, result[0].Rank);
        Assert.Equal("Player2", result[1].PlayerName);
        Assert.Equal(180, result[1].Score);
        Assert.Equal(2, result[1].Rank);
    }

    #endregion
}
