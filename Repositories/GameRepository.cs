using BowlingGame.API.Models;
using BowlingGame.API.Repositories.Interfaces;
using MySqlConnector;

namespace BowlingGame.API.Repositories;

public class GameRepository : BaseRepository, IGameRepository
{
    public GameRepository(IConfiguration configuration) : base(configuration)
    {
    }

    public async Task<Game> CreateGameAsync(string playerName)
    {
        const string insertSql = @"
            INSERT INTO Games (PlayerName, CreatedAt, Status)
            VALUES (@PlayerName, UTC_TIMESTAMP(), @Status)";

        const string selectSql = @"
            SELECT Id, PlayerName, CreatedAt, Status, FinalScore
            FROM Games WHERE Id = LAST_INSERT_ID()";

        using var connection = CreateConnection();
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

        using var connection = CreateConnection();
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

    public async Task UpdateGameAsync(Game game)
    {
        const string sql = @"
            UPDATE Games
            SET Status = @Status, FinalScore = @FinalScore
            WHERE Id = @GameId";

        using var connection = CreateConnection();
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

        using var connection = CreateConnection();
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
