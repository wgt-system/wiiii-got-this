using Microsoft.Data.Sqlite;
using WiiiiGotThis.Application;
using WiiiiGotThis.Contracts;
using WiiiiGotThis.Domain;

namespace WiiiiGotThis.Infrastructure;

public sealed class SqliteConnectionFactory(string connectionString)
{
    static SqliteConnectionFactory() => SQLitePCL.Batteries_V2.Init();

    public SqliteConnection Create() => new(connectionString);
}

public sealed class MigrationRunner(SqliteConnectionFactory connectionFactory)
{
    public async Task ApplyAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using (var metadata = connection.CreateCommand())
        {
            metadata.CommandText = "CREATE TABLE IF NOT EXISTS wgt_schema_migrations (version INTEGER NOT NULL PRIMARY KEY, applied_at_utc TEXT NOT NULL);";
            await metadata.ExecuteNonQueryAsync(cancellationToken);
        }

        await using var migration = connection.CreateCommand();
        migration.CommandText = "CREATE TABLE IF NOT EXISTS wgt_integration_publications (service_id TEXT NOT NULL PRIMARY KEY, display_name TEXT NOT NULL, published_at_utc TEXT NOT NULL);";
        await migration.ExecuteNonQueryAsync(cancellationToken);
        await using var marker = connection.CreateCommand();
        marker.CommandText = "INSERT OR IGNORE INTO wgt_schema_migrations(version, applied_at_utc) VALUES (1, $timestamp);";
        marker.Parameters.AddWithValue("$timestamp", DateTimeOffset.UtcNow.ToString("O"));
        await marker.ExecuteNonQueryAsync(cancellationToken);
    }
}

public sealed class SqliteIntegrationPublicationStore(SqliteConnectionFactory connectionFactory) : IIntegrationPublicationStore
{
    public async ValueTask SaveAsync(ServicePublication publication, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO wgt_integration_publications(service_id, display_name, published_at_utc) VALUES ($id, $name, $published) ON CONFLICT(service_id) DO UPDATE SET display_name = excluded.display_name, published_at_utc = excluded.published_at_utc;";
        command.Parameters.AddWithValue("$id", publication.ServiceId.Value);
        command.Parameters.AddWithValue("$name", publication.DisplayName);
        command.Parameters.AddWithValue("$published", publication.PublishedAtUtc.ToString("O"));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public ValueTask<ServicePublication?> LoadAsync(ServiceIdentity serviceIdentity, CancellationToken cancellationToken = default) => ValueTask.FromResult<ServicePublication?>(null);
}
