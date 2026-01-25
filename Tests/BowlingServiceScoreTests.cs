using BowlingGame.API.Services;
using BowlingGame.API.Repositories.Interfaces;
using Moq;

namespace BowlingGame.Tests;

/// <summary>
/// Unit tests for bowling score calculation logic.
/// </summary>
public class BowlingServiceScoreTests
{
    private readonly BowlingService _service;
    private readonly Mock<IGameRepository> _mockRepository;
    private readonly Mock<IRollRepository> _mockRollRepository;

    public BowlingServiceScoreTests()
    {
        _mockRepository = new Mock<IGameRepository>();
        _mockRollRepository = new Mock<IRollRepository>();
        _service = new BowlingService(_mockRepository.Object, _mockRollRepository.Object);
    }

    #region Perfect Game Tests

    [Fact]
    public void CalculateScore_PerfectGame_Returns300()
    {
        // 12 strikes = 300 (perfect game)
        var rolls = new List<int> { 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10 };

        var score = _service.CalculateScore(rolls);

        Assert.Equal(300, score);
    }

    #endregion

    #region All Gutter Tests

    [Fact]
    public void CalculateScore_AllGutters_Returns0()
    {
        // 20 gutter balls = 0
        var rolls = new List<int> { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

        var score = _service.CalculateScore(rolls);

        Assert.Equal(0, score);
    }

    #endregion

    #region All Ones Tests

    [Fact]
    public void CalculateScore_AllOnes_Returns20()
    {
        // 20 rolls of 1 = 20
        var rolls = new List<int> { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };

        var score = _service.CalculateScore(rolls);

        Assert.Equal(20, score);
    }

    #endregion

    #region Spare Tests

    [Fact]
    public void CalculateScore_SingleSpare_CalculatesBonus()
    {
        // Spare in first frame (5+5), then 3, rest zeros
        // Frame 1: 10 + 3 (bonus) = 13
        // Frame 2: 3 + 0 = 3
        // Total: 16
        var rolls = new List<int> { 5, 5, 3, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

        var score = _service.CalculateScore(rolls);

        Assert.Equal(16, score);
    }

    [Fact]
    public void CalculateScore_AllSpares_Returns150()
    {
        // All 5s = spare every frame
        // Each frame: 10 + 5 (next roll bonus) = 15
        // 10 frames * 15 = 150
        var rolls = new List<int> { 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5 };

        var score = _service.CalculateScore(rolls);

        Assert.Equal(150, score);
    }

    [Fact]
    public void CalculateScore_SpareInTenthFrame_Gets3Rolls()
    {
        // 9 frames of gutters, then spare + bonus roll
        var rolls = new List<int> { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 5, 5, 10 };

        var score = _service.CalculateScore(rolls);

        Assert.Equal(20, score); // 5 + 5 + 10 = 20
    }

    #endregion

    #region Strike Tests

    [Fact]
    public void CalculateScore_SingleStrike_CalculatesBonus()
    {
        // Strike, then 3+4, rest zeros
        // Frame 1: 10 + 3 + 4 = 17
        // Frame 2: 3 + 4 = 7
        // Total: 24
        var rolls = new List<int> { 10, 3, 4, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

        var score = _service.CalculateScore(rolls);

        Assert.Equal(24, score);
    }

    [Fact]
    public void CalculateScore_TwoStrikesInRow_CalculatesDoubleBonus()
    {
        // Two strikes, then 3+4, rest zeros
        // Frame 1: 10 + 10 + 3 = 23
        // Frame 2: 10 + 3 + 4 = 17
        // Frame 3: 3 + 4 = 7
        // Total: 47
        var rolls = new List<int> { 10, 10, 3, 4, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

        var score = _service.CalculateScore(rolls);

        Assert.Equal(47, score);
    }

    [Fact]
    public void CalculateScore_ThreeStrikesInRow_Turkey()
    {
        // Three strikes (turkey), then zeros
        // Frame 1: 10 + 10 + 10 = 30
        // Frame 2: 10 + 10 + 0 = 20
        // Frame 3: 10 + 0 + 0 = 10
        // Total: 60
        var rolls = new List<int> { 10, 10, 10, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

        var score = _service.CalculateScore(rolls);

        Assert.Equal(60, score);
    }

    [Fact]
    public void CalculateScore_StrikeInTenthFrame_Gets3Rolls()
    {
        // 9 frames of gutters, then strike + 2 bonus rolls
        var rolls = new List<int> { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 10, 3, 4 };

        var score = _service.CalculateScore(rolls);

        Assert.Equal(17, score); // 10 + 3 + 4 = 17
    }

    [Fact]
    public void CalculateScore_ThreeStrikesInTenthFrame_Returns30()
    {
        // 9 frames of gutters, then 3 strikes in 10th
        var rolls = new List<int> { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 10, 10, 10 };

        var score = _service.CalculateScore(rolls);

        Assert.Equal(30, score);
    }

    #endregion

    #region Mixed Game Tests

    [Fact]
    public void CalculateScore_MixedGame_CalculatesCorrectly()
    {
        // Real-world example game
        // Frame 1: 3 + 6 = 9
        // Frame 2: 7 + 2 = 9
        // Frame 3: Strike (10 + 4 + 5) = 19
        // Frame 4: 4 + 5 = 9
        // Frame 5: 8 + spare (10 + 10) = 20
        // Frame 6: Strike (10 + 0 + 0) = 10
        // Frame 7: 0 + 0 = 0
        // Frame 8: 6 + 3 = 9
        // Frame 9: 9 + spare (10 + 5) = 15
        // Frame 10: 5 + 4 = 9
        // Total: 109
        var rolls = new List<int> { 3, 6, 7, 2, 10, 4, 5, 8, 2, 10, 0, 0, 6, 3, 9, 1, 5, 4 };

        var score = _service.CalculateScore(rolls);

        Assert.Equal(109, score);
    }

    #endregion

    #region Incomplete Game Tests

    [Fact]
    public void CalculateScore_EmptyGame_Returns0()
    {
        var rolls = new List<int>();

        var score = _service.CalculateScore(rolls);

        Assert.Equal(0, score);
    }

    [Fact]
    public void CalculateScore_PartialGame_ReturnsPartialScore()
    {
        // Only first frame completed
        var rolls = new List<int> { 3, 4 };

        var score = _service.CalculateScore(rolls);

        Assert.Equal(7, score);
    }

    [Fact]
    public void CalculateScore_StrikeWithNoFollowUp_Returns10()
    {
        // Strike but no bonus rolls yet
        var rolls = new List<int> { 10 };

        var score = _service.CalculateScore(rolls);

        Assert.Equal(10, score);
    }

    #endregion
}
