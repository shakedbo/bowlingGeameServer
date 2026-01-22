using BowlingGame.API.Services;
using BowlingGame.API.Repositories.Interfaces;
using Moq;

namespace BowlingGame.Tests;

/// <summary>
/// Unit tests for game state (frame tracking and completion).
/// </summary>
public class BowlingServiceGameStateTests
{
    private readonly BowlingService _service;

    public BowlingServiceGameStateTests()
    {
        var mockRepository = new Mock<IGameRepository>();
        _service = new BowlingService(mockRepository.Object);
    }

    #region Empty/Initial State Tests

    [Fact]
    public void GetGameState_EmptyGame_ReturnsFrame1NotComplete()
    {
        var (frame, isComplete) = _service.GetGameState(new List<int>());

        Assert.Equal(1, frame);
        Assert.False(isComplete);
    }

    #endregion

    #region Frame Progression Tests

    [Fact]
    public void GetGameState_AfterFirstRoll_StillInFrame1()
    {
        var rolls = new List<int> { 5 };

        var (frame, isComplete) = _service.GetGameState(rolls);

        Assert.Equal(1, frame);
        Assert.False(isComplete);
    }

    [Fact]
    public void GetGameState_AfterTwoRolls_MovesToFrame2()
    {
        var rolls = new List<int> { 3, 4 };

        var (frame, isComplete) = _service.GetGameState(rolls);

        Assert.Equal(2, frame);
        Assert.False(isComplete);
    }

    [Fact]
    public void GetGameState_AfterStrike_MovesToNextFrame()
    {
        var rolls = new List<int> { 10 };

        var (frame, isComplete) = _service.GetGameState(rolls);

        Assert.Equal(2, frame);
        Assert.False(isComplete);
    }

    [Fact]
    public void GetGameState_AfterSpare_MovesToNextFrame()
    {
        var rolls = new List<int> { 7, 3 };

        var (frame, isComplete) = _service.GetGameState(rolls);

        Assert.Equal(2, frame);
        Assert.False(isComplete);
    }

    [Fact]
    public void GetGameState_NineStrikes_AtFrame10()
    {
        var rolls = new List<int> { 10, 10, 10, 10, 10, 10, 10, 10, 10 };

        var (frame, isComplete) = _service.GetGameState(rolls);

        Assert.Equal(10, frame);
        Assert.False(isComplete);
    }

    #endregion

    #region 10th Frame Completion Tests

    [Fact]
    public void GetGameState_OpenTenthFrame_CompletesAfter2Rolls()
    {
        // 9 frames of gutters (18 rolls), then open 10th frame
        var rolls = new List<int> { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 3, 4 };

        var (frame, isComplete) = _service.GetGameState(rolls);

        Assert.Equal(10, frame);
        Assert.True(isComplete);
    }

    [Fact]
    public void GetGameState_StrikeInTenth_NeedsThreeRolls()
    {
        // 9 frames of gutters, then strike in 10th with only 1 bonus roll
        var rolls = new List<int> { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 10, 5 };

        var (frame, isComplete) = _service.GetGameState(rolls);

        Assert.Equal(10, frame);
        Assert.False(isComplete); // Needs 3rd roll
    }

    [Fact]
    public void GetGameState_StrikeInTenthWithTwoBonusRolls_IsComplete()
    {
        var rolls = new List<int> { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 10, 5, 3 };

        var (frame, isComplete) = _service.GetGameState(rolls);

        Assert.Equal(10, frame);
        Assert.True(isComplete);
    }

    [Fact]
    public void GetGameState_SpareInTenth_NeedsThreeRolls()
    {
        var rolls = new List<int> { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 5, 5 };

        var (frame, isComplete) = _service.GetGameState(rolls);

        Assert.Equal(10, frame);
        Assert.False(isComplete); // Needs 3rd roll
    }

    [Fact]
    public void GetGameState_SpareInTenthWithBonus_IsComplete()
    {
        var rolls = new List<int> { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 5, 5, 7 };

        var (frame, isComplete) = _service.GetGameState(rolls);

        Assert.Equal(10, frame);
        Assert.True(isComplete);
    }

    [Fact]
    public void GetGameState_ThreeStrikesInTenth_IsComplete()
    {
        var rolls = new List<int> { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 10, 10, 10 };

        var (frame, isComplete) = _service.GetGameState(rolls);

        Assert.Equal(10, frame);
        Assert.True(isComplete);
    }

    [Fact]
    public void GetGameState_PerfectGame_IsComplete()
    {
        var rolls = new List<int> { 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10 };

        var (frame, isComplete) = _service.GetGameState(rolls);

        Assert.Equal(10, frame);
        Assert.True(isComplete);
    }

    [Fact]
    public void GetGameState_AllGutters_IsComplete()
    {
        var rolls = new List<int> { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

        var (frame, isComplete) = _service.GetGameState(rolls);

        Assert.Equal(10, frame);
        Assert.True(isComplete);
    }

    #endregion

    #region Mid-Game State Tests

    [Fact]
    public void GetGameState_MidGame_ReturnsCorrectFrame()
    {
        // 3 frames completed (strike, 3+4, 5+5)
        var rolls = new List<int> { 10, 3, 4, 5, 5 };

        var (frame, isComplete) = _service.GetGameState(rolls);

        Assert.Equal(4, frame);
        Assert.False(isComplete);
    }

    [Fact]
    public void GetGameState_Frame5FirstRoll_ReturnsFrame5()
    {
        // 4 complete frames + 1 roll
        var rolls = new List<int> { 3, 4, 5, 2, 10, 8, 1, 6 };

        var (frame, isComplete) = _service.GetGameState(rolls);

        Assert.Equal(5, frame);
        Assert.False(isComplete);
    }

    #endregion
}
