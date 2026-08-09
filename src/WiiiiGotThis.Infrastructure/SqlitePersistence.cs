using Microsoft.Data.Sqlite;
using WiiiiGotThis.Application;
using WiiiiGotThis.Contracts;
using WiiiiGotThis.Domain;

namespace WiiiiGotThis.Infrastructure;

public sealed class SqliteConnectionFactory(string connectionString)
{
    static SqliteConnectionFactory() => SQLitePCL.Batteries_V2.Init();
    public SqliteConnection Create()
    {
        var builder = new SqliteConnectionStringBuilder(connectionString) { ForeignKeys = true };
        return new SqliteConnection(builder.ToString());
    }
}

public sealed record MigrationScript(string FileName, string Sql);

public sealed class MigrationRunner
{
    private readonly SqliteConnectionFactory connectionFactory;
    private readonly MigrationScript[] migrations;

    public MigrationRunner(SqliteConnectionFactory connectionFactory)
        : this(connectionFactory, BuiltInMigrations.Create()) { }

    public MigrationRunner(SqliteConnectionFactory connectionFactory, IEnumerable<MigrationScript> migrations)
    {
        this.connectionFactory = connectionFactory;
        this.migrations = migrations.OrderBy(x => ParseVersion(x.FileName)).ToArray();
        if (this.migrations.Select(x => ParseVersion(x.FileName)).Distinct().Count() != this.migrations.Length)
            throw new ArgumentException("Migration versions must be unique.", nameof(migrations));
    }

    public async Task ApplyAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using (var metadata = connection.CreateCommand())
        {
            metadata.CommandText = "CREATE TABLE IF NOT EXISTS wgt_schema_migrations (version INTEGER NOT NULL PRIMARY KEY, applied_at_utc TEXT NOT NULL);";
            await metadata.ExecuteNonQueryAsync(cancellationToken);
        }

        foreach (var migration in migrations)
        {
            var version = ParseVersion(migration.FileName);
            if (await IsAppliedAsync(connection, version, cancellationToken)) continue;

            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
            try
            {
                await using var command = connection.CreateCommand();
                command.Transaction = (SqliteTransaction)transaction;
                command.CommandText = migration.Sql;
                await command.ExecuteNonQueryAsync(cancellationToken);
                command.CommandText = "INSERT INTO wgt_schema_migrations(version, applied_at_utc) VALUES ($version, $timestamp);";
                command.Parameters.Clear();
                command.Parameters.AddWithValue("$version", version);
                command.Parameters.AddWithValue("$timestamp", DateTimeOffset.UtcNow.ToString("O"));
                await command.ExecuteNonQueryAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(CancellationToken.None);
                throw;
            }
        }
    }

    private static async Task<bool> IsAppliedAsync(SqliteConnection connection, int version, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT EXISTS (SELECT 1 FROM wgt_schema_migrations WHERE version = $version);";
        command.Parameters.AddWithValue("$version", version);
        return Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken), System.Globalization.CultureInfo.InvariantCulture) != 0;
    }

    private static int ParseVersion(string fileName)
    {
        var name = Path.GetFileName(fileName);
        var separator = name.IndexOf('_');
        if (separator <= 0 || !int.TryParse(name[..separator], out var version))
            throw new FormatException($"Migration file name must start with a numeric version: '{fileName}'.");
        return version;
    }

    private static class BuiltInMigrations
    {
        public static MigrationScript[] Create() =>
        [
            new("0001_initial.sql", Read("WiiiiGotThis.Infrastructure.Migrations.0001_initial.sql")),
            new("0002_core_integration_state.sql", Read("WiiiiGotThis.Infrastructure.Migrations.0002_core_integration_state.sql"))
        ];

        private static string Read(string resourceName)
        {
            using var stream = typeof(MigrationRunner).Assembly.GetManifestResourceStream(resourceName) ?? throw new InvalidOperationException($"Migration resource not found: {resourceName}");
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
    }
}

internal static class PersistenceMapping
{
    public static string ToStorage(Enablement value) => value switch { Enablement.Enabled => "enabled", Enablement.Disabled => "disabled", _ => throw new ArgumentOutOfRangeException(nameof(value)) };
    public static Enablement ToEnablement(string value) => value switch { "enabled" => Enablement.Enabled, "disabled" => Enablement.Disabled, _ => throw new InvalidDataException($"Unknown enablement value '{value}'.") };
    public static DeviceIdentity Device(string value) => new(Guid.Parse(value));
    public static ServiceIdentity Service(string value) => new(value);
    public static CapabilityIdentity Capability(string value) => new(value);
    public static Version Version(string value) => System.Version.TryParse(value, out var version) && version is not null ? version : throw new InvalidDataException($"Invalid contract version '{value}'.");
}

public sealed class SqliteLocalDeviceStore(SqliteConnectionFactory connectionFactory) : ILocalDeviceStore
{
    public async ValueTask<LocalDeviceConfiguration?> LoadAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT device_identity, display_name FROM wgt_local_device WHERE singleton_key = 1;";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return !await reader.ReadAsync(cancellationToken) ? null : new LocalDeviceConfiguration(PersistenceMapping.Device(reader.GetString(0)), reader.GetString(1));
    }

    public async ValueTask SaveAsync(LocalDeviceConfiguration configuration, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        await using var connection = await OpenAsync(cancellationToken);
        await using (var existing = connection.CreateCommand())
        {
            existing.CommandText = "SELECT device_identity FROM wgt_local_device WHERE singleton_key = 1;";
            var storedIdentity = await existing.ExecuteScalarAsync(cancellationToken);
            if (storedIdentity is string value && !StringComparer.Ordinal.Equals(value, configuration.DeviceIdentity.Value.ToString("D")))
                throw new InvalidOperationException("The current local Device Identity cannot be replaced.");
        }
        await using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO wgt_local_device(singleton_key, device_identity, display_name) VALUES (1, $device, $name) ON CONFLICT(singleton_key) DO UPDATE SET display_name = excluded.display_name;";
        command.Parameters.AddWithValue("$device", configuration.DeviceIdentity.Value.ToString("D"));
        command.Parameters.AddWithValue("$name", configuration.DisplayName);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<SqliteConnection> OpenAsync(CancellationToken token) { var c = connectionFactory.Create(); await c.OpenAsync(token); return c; }
}

public sealed class SqliteServiceIntegrationStore(SqliteConnectionFactory connectionFactory) : IServiceIntegrationStore
{
    public async ValueTask<ServiceIntegration?> LoadAsync(ServiceIdentity serviceIdentity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(serviceIdentity);
        await using var connection = await OpenAsync(cancellationToken);
        return await ReadOneAsync(connection, serviceIdentity, cancellationToken);
    }

    public async ValueTask<IReadOnlyList<ServiceIntegration>> LoadAllAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT service_id FROM wgt_service_integrations ORDER BY service_id;";
        var serviceIds = new List<string>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) serviceIds.Add(reader.GetString(0));
        var result = new List<ServiceIntegration>(serviceIds.Count);
        foreach (var serviceId in serviceIds) result.Add(await ReadOneAsync(connection, PersistenceMapping.Service(serviceId), cancellationToken) ?? throw new InvalidDataException("Integration disappeared while loading."));
        return result;
    }

    public async ValueTask SaveAsync(ServiceIntegration integration, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(integration);
        await using var connection = await OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            await ExecuteAsync(connection, transaction, "INSERT INTO wgt_service_integrations(service_id, global_enablement) VALUES ($service, $enablement) ON CONFLICT(service_id) DO UPDATE SET global_enablement = excluded.global_enablement;", cancellationToken, ("$service", integration.ServiceIdentity.Value), ("$enablement", PersistenceMapping.ToStorage(integration.GlobalEnablement)));
            await ExecuteAsync(connection, transaction, "DELETE FROM wgt_service_integration_device_overrides WHERE service_id = $service;", cancellationToken, ("$service", integration.ServiceIdentity.Value));
            foreach (var pair in integration.DeviceOverrides)
                await ExecuteAsync(connection, transaction, "INSERT INTO wgt_service_integration_device_overrides(service_id, device_identity, enablement) VALUES ($service, $device, $enablement);", cancellationToken, ("$service", integration.ServiceIdentity.Value), ("$device", pair.Key.Value.ToString("D")), ("$enablement", PersistenceMapping.ToStorage(pair.Value)));
            await transaction.CommitAsync(cancellationToken);
        }
        catch { await transaction.RollbackAsync(CancellationToken.None); throw; }
    }

    private static async Task<ServiceIntegration?> ReadOneAsync(SqliteConnection connection, ServiceIdentity service, CancellationToken token)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT global_enablement FROM wgt_service_integrations WHERE service_id = $service;";
        command.Parameters.AddWithValue("$service", service.Value);
        var scalar = await command.ExecuteScalarAsync(token);
        if (scalar is null) return null;
        var integration = new ServiceIntegration(service);
        if (PersistenceMapping.ToEnablement((string)scalar) == Enablement.Enabled) integration.EnableGlobally(); else integration.DisableGlobally();
        await using var overrides = connection.CreateCommand();
        overrides.CommandText = "SELECT device_identity, enablement FROM wgt_service_integration_device_overrides WHERE service_id = $service ORDER BY device_identity;";
        overrides.Parameters.AddWithValue("$service", service.Value);
        await using var reader = await overrides.ExecuteReaderAsync(token);
        while (await reader.ReadAsync(token)) integration.SetDeviceOverride(PersistenceMapping.Device(reader.GetString(0)), PersistenceMapping.ToEnablement(reader.GetString(1)));
        return integration;
    }

    private async Task<SqliteConnection> OpenAsync(CancellationToken token) { var c = connectionFactory.Create(); await c.OpenAsync(token); return c; }
    private static async Task ExecuteAsync(SqliteConnection connection, System.Data.Common.DbTransaction transaction, string sql, CancellationToken cancellationToken, params (string Name, object Value)[] values) { await using var command = connection.CreateCommand(); command.Transaction = (SqliteTransaction)transaction; command.CommandText = sql; foreach (var (name, value) in values) command.Parameters.AddWithValue(name, value); await command.ExecuteNonQueryAsync(cancellationToken); }
}

public sealed class SqliteIntegrationPublicationStore(SqliteConnectionFactory connectionFactory) : IIntegrationPublicationStore
{
    public async ValueTask SaveAsync(ServicePublication publication, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(publication);
        await using var connection = connectionFactory.Create(); await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            await Exec(connection, transaction, "INSERT INTO wgt_integration_publications(service_id, display_name, published_at_utc) VALUES ($id, $name, $published) ON CONFLICT(service_id) DO UPDATE SET display_name = excluded.display_name, published_at_utc = excluded.published_at_utc;", cancellationToken, ("$id", publication.ServiceId.Value), ("$name", publication.DisplayName), ("$published", publication.PublishedAtUtc.ToString("O")));
            await Exec(connection, transaction, "DELETE FROM wgt_capability_publications WHERE service_id = $id;", cancellationToken, ("$id", publication.ServiceId.Value));
            for (var ordinal = 0; ordinal < publication.Capabilities.Count; ordinal++) { var capability = publication.Capabilities[ordinal]; await Exec(connection, transaction, "INSERT INTO wgt_capability_publications(service_id, capability_id, title, contract_version, ordinal) VALUES ($service, $capability, $title, $version, $ordinal);", cancellationToken, ("$service", publication.ServiceId.Value), ("$capability", capability.Id.Value), ("$title", capability.Title), ("$version", capability.ContractVersion.ToString()), ("$ordinal", ordinal)); }
            await transaction.CommitAsync(cancellationToken);
        }
        catch { await transaction.RollbackAsync(CancellationToken.None); throw; }
    }

    public async ValueTask<ServicePublication?> LoadAsync(ServiceIdentity serviceIdentity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(serviceIdentity); await using var connection = connectionFactory.Create(); await connection.OpenAsync(cancellationToken);
        await using var header = connection.CreateCommand(); header.CommandText = "SELECT display_name, published_at_utc FROM wgt_integration_publications WHERE service_id = $id;"; header.Parameters.AddWithValue("$id", serviceIdentity.Value);
        await using var reader = await header.ExecuteReaderAsync(cancellationToken); if (!await reader.ReadAsync(cancellationToken)) return null;
        var displayName = reader.GetString(0); if (!DateTimeOffset.TryParse(reader.GetString(1), System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind, out var publishedAt)) throw new InvalidDataException("Invalid publication timestamp.");
        await reader.DisposeAsync();
        await using var capabilities = connection.CreateCommand(); capabilities.CommandText = "SELECT capability_id, title, contract_version FROM wgt_capability_publications WHERE service_id = $id ORDER BY ordinal;"; capabilities.Parameters.AddWithValue("$id", serviceIdentity.Value);
        var list = new List<CapabilityPublication>(); await using var capabilityReader = await capabilities.ExecuteReaderAsync(cancellationToken); while (await capabilityReader.ReadAsync(cancellationToken)) list.Add(new CapabilityPublication(PersistenceMapping.Capability(capabilityReader.GetString(0)), capabilityReader.GetString(1), PersistenceMapping.Version(capabilityReader.GetString(2))));
        return new ServicePublication(serviceIdentity, displayName, list, publishedAt);
    }

    private static async Task Exec(SqliteConnection connection, System.Data.Common.DbTransaction transaction, string sql, CancellationToken cancellationToken, params (string Name, object Value)[] values) { await using var command = connection.CreateCommand(); command.Transaction = (SqliteTransaction)transaction; command.CommandText = sql; foreach (var (name, value) in values) command.Parameters.AddWithValue(name, value); await command.ExecuteNonQueryAsync(cancellationToken); }
}
