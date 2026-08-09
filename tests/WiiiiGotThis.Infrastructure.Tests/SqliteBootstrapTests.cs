using WiiiiGotThis.Infrastructure;

namespace WiiiiGotThis.Infrastructure.Tests;

public sealed class SqliteBootstrapTests
{
    [Fact]
    public async Task Migration_runner_creates_only_wgt_bootstrap_tables()
    {
        var path = Path.Combine(Path.GetTempPath(), $"wgt-{Guid.NewGuid():N}.db");
        try
        {
            var factory = new SqliteConnectionFactory($"Data Source={path};Pooling=False");
            await new MigrationRunner(factory).ApplyAsync();
            {
                await using var connection = factory.Create();
                await connection.OpenAsync();
                await using var command = connection.CreateCommand();
                command.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table' ORDER BY name;";
                var names = new List<string>();
                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync()) names.Add(reader.GetString(0));
                Assert.Equal(["wgt_integration_publications", "wgt_schema_migrations"], names);
            }
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }
}
