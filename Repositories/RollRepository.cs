using BowlingGame.API.Models;
using BowlingGame.API.Repositories.Interfaces;
using MySqlConnector;

namespace BowlingGame.API.Repositories;

public class RollRepository : BaseRepository, IRollRepository
{
    public RollRepository(IConfiguration configuration) : base(configuration)
    {
    }

    public async Task AddRollAsync(int gameId, int pins, int rollIndex)
    {
        const string sql = @"
            INSERT INTO Rolls (GameId, Pins, RollIndex)
            VALUES (@GameId, @Pins, @RollIndex)";

        using var connection = CreateConnection();
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

        using var connection = CreateConnection();
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
}
