using BowlingGame.API.DTOs.Requests;
using BowlingGame.API.DTOs.Responses;
using BowlingGame.API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BowlingGame.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GamesController : ControllerBase
{
    private readonly IBowlingService _bowlingService;

    public GamesController(IBowlingService bowlingService)
    {
        _bowlingService = bowlingService;
    }

    /// <summary>
    /// Starts a new bowling game.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(GameResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GameResponse>> StartGame([FromBody] CreateGameRequest request)
    {
        var game = await _bowlingService.StartGameAsync(request);
        return CreatedAtAction(nameof(GetScore), new { id = game.Id }, game);
    }

    /// <summary>
    /// Records a roll for a game.
    /// </summary>
    [HttpPost("{id}/roll")]
    [ProducesResponseType(typeof(ScoreResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ScoreResponse>> Roll(int id, [FromBody] AddRollRequest request)
    {
        var score = await _bowlingService.RollAsync(id, request);
        return Ok(score);
    }

    /// <summary>
    /// Gets the current score for a game.
    /// </summary>
    [HttpGet("{id}/score")]
    [ProducesResponseType(typeof(ScoreResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ScoreResponse>> GetScore(int id)
    {
        var score = await _bowlingService.GetScoreAsync(id);
        return Ok(score);
    }

    /// <summary>
    /// Gets the leaderboard of top scorers (completed games only).
    /// </summary>
    [HttpGet("leaderboard")]
    [ProducesResponseType(typeof(List<LeaderboardEntryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<LeaderboardEntryResponse>>> GetLeaderboard([FromQuery] int count = 10)
    {
        if (count <= 0)
        {
            count = 10;
        }
        else if (count > 100)
        {
            count = 100;
        }

        var leaderboard = await _bowlingService.GetLeaderboardAsync(count);
        return Ok(leaderboard);
    }
}
