using BowlingGame.API.Models;
using BowlingGame.API.Repositories.Interfaces;
using MySqlConnector;

namespace BowlingGame.API.Repositories;

public class ApiLogRepository : BaseRepository, IApiLogRepository
{
    public ApiLogRepository(IConfiguration configuration) : base(configuration)
    {
    }

    public async Task InsertAsync(ApiLog log)
    {
        const string sql = @"
            INSERT INTO ApiLogs (Timestamp, HttpMethod, Path, StatusCode, RequestBody, ResponseBody, DurationMs, ExceptionMessage)
            VALUES (@Timestamp, @HttpMethod, @Path, @StatusCode, @RequestBody, @ResponseBody, @DurationMs, @ExceptionMessage)";

        using var connection = CreateConnection();
        await connection.OpenAsync();

        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Timestamp", log.Timestamp);
        command.Parameters.AddWithValue("@HttpMethod", log.HttpMethod);
        command.Parameters.AddWithValue("@Path", log.Path);
        command.Parameters.AddWithValue("@StatusCode", log.StatusCode);
        command.Parameters.AddWithValue("@RequestBody", (object?)log.RequestBody ?? DBNull.Value);
        command.Parameters.AddWithValue("@ResponseBody", (object?)log.ResponseBody ?? DBNull.Value);
        command.Parameters.AddWithValue("@DurationMs", log.DurationMs);
        command.Parameters.AddWithValue("@ExceptionMessage", (object?)log.ExceptionMessage ?? DBNull.Value);

        await command.ExecuteNonQueryAsync();
    }
}
