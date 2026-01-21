using BowlingGame.API.Models;
using BowlingGame.API.Repositories.Interfaces;
using MySqlConnector;

namespace BowlingGame.API.Repositories;

public class GameRepository : IGameRepository
{
    private readonly string _connectionString;

    public GameRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
    }

    public async Task<Game> CreateGameAsync(string playerName)
    {
        const string insertSql = @"
            INSERT INTO Games (PlayerName, CreatedAt, Status)
            VALUES (@PlayerName, UTC_TIMESTAMP(), @Status)";

        const string selectSql = @"
            SELECT Id, PlayerName, CreatedAt, Status, FinalScore
            FROM Games WHERE Id = LAST_INSERT_ID()";

        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        using var insertCommand = new MySqlCommand(insertSql, connection);
        insertCommand.Parameters.AddWithValue("@PlayerName", playerName);
        insertCommand.Parameters.AddWithValue("@Status", (byte)GameStatus.Active);
        await insertCommand.ExecuteNonQueryAsync();

        using var selectCommand = new MySqlCommand(selectSql, connection);
        using var reader = await selectCommand.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new Game
            {
                Id = reader.GetInt32(0),
                PlayerName = reader.GetString(1),
                CreatedAt = reader.GetDateTime(2),
                Status = (GameStatus)(byte)reader.GetInt16(3),
                FinalScore = reader.IsDBNull(4) ? null : reader.GetInt32(4)
            };
        }

        throw new InvalidOperationException("Failed to create game.");
    }

    public async Task<Game?> GetGameAsync(int gameId)
    {
        const string sql = @"
            SELECT Id, PlayerName, CreatedAt, Status, FinalScore
            FROM Games
            WHERE Id = @GameId";

        using var connection = new MySqlConnection(_connectionString);
        using var command = new MySqlCommand(sql, connection);
        
        command.Parameters.AddWithValue("@GameId", gameId);

        await connection.OpenAsync();
        
        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new Game
            {
                Id = reader.GetInt32(0),
                PlayerName = reader.GetString(1),
                CreatedAt = reader.GetDateTime(2),
                Status = (GameStatus)(byte)reader.GetInt16(3),
                FinalScore = reader.IsDBNull(4) ? null : reader.GetInt32(4)
            };
        }

        return null;
    }

    public async Task AddRollAsync(int gameId, int pins, int rollIndex)
    {
        const string sql = @"
            INSERT INTO Rolls (GameId, Pins, RollIndex)
            VALUES (@GameId, @Pins, @RollIndex)";

        using var connection = new MySqlConnection(_connectionString);
        using var command = new MySqlCommand(sql, connection);
        
        command.Parameters.AddWithValue("@GameId", gameId);
        command.Parameters.AddWithValue("@Pins", (byte)pins);
        command.Parameters.AddWithValue("@RollIndex", rollIndex);

        await connection.OpenAsync();
        await command.ExecuteNonQueryAsync();
    }

    public async Task<List<Roll>> GetRollsAsync(int gameId)
    {
        const string sql = @"
            SELECT Id, GameId, Pins, RollIndex
            FROM Rolls
            WHERE GameId = @GameId
            ORDER BY RollIndex";

        var rolls = new List<Roll>();

        using var connection = new MySqlConnection(_connectionString);
        using var command = new MySqlCommand(sql, connection);
        
        command.Parameters.AddWithValue("@GameId", gameId);

        await connection.OpenAsync();
        
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            rolls.Add(new Roll
            {
                Id = reader.GetInt32(0),
                GameId = reader.GetInt32(1),
                Pins = (int)reader.GetInt16(2),
                RollIndex = reader.GetInt32(3)
            });
        }

        return rolls;
    }

    public async Task UpdateGameAsync(Game game)
    {
        const string sql = @"
            UPDATE Games
            SET Status = @Status, FinalScore = @FinalScore
            WHERE Id = @GameId";

        using var connection = new MySqlConnection(_connectionString);
        using var command = new MySqlCommand(sql, connection);
        
        command.Parameters.AddWithValue("@GameId", game.Id);
        command.Parameters.AddWithValue("@Status", (byte)game.Status);
        command.Parameters.AddWithValue("@FinalScore", game.FinalScore.HasValue ? game.FinalScore.Value : DBNull.Value);

        await connection.OpenAsync();
        await command.ExecuteNonQueryAsync();
    }

    public async Task<List<Game>> GetTopScorersAsync(int count)
    {
        const string sql = @"
            SELECT Id, PlayerName, CreatedAt, Status, FinalScore
            FROM Games
            WHERE Status = @Status AND FinalScore IS NOT NULL
            ORDER BY FinalScore DESC, CreatedAt ASC
            LIMIT @Count";

        var games = new List<Game>();

        using var connection = new MySqlConnection(_connectionString);
        using var command = new MySqlCommand(sql, connection);
        
        command.Parameters.AddWithValue("@Count", count);
        command.Parameters.AddWithValue("@Status", (byte)GameStatus.Completed);

        await connection.OpenAsync();
        
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            games.Add(new Game
            {
                Id = reader.GetInt32(0),
                PlayerName = reader.GetString(1),
                CreatedAt = reader.GetDateTime(2),
                Status = (GameStatus)(byte)reader.GetInt16(3),
                FinalScore = reader.IsDBNull(4) ? null : reader.GetInt32(4)
            });
        }

        return games;
    }
}
