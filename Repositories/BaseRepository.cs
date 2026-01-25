using MySqlConnector;

namespace BowlingGame.API.Repositories;

public abstract class BaseRepository
{
    protected readonly string _connectionString;

    protected BaseRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
    }

    protected MySqlConnection CreateConnection() => new MySqlConnection(_connectionString);
}
