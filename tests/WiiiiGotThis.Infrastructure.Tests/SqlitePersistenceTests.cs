using Microsoft.Data.Sqlite;
using WiiiiGotThis.Application;
using WiiiiGotThis.Contracts;
using WiiiiGotThis.Domain;
using WiiiiGotThis.Infrastructure;

namespace WiiiiGotThis.Infrastructure.Tests;

public sealed class SqlitePersistenceTests
{
    [Fact]
    public async Task Factory_enables_foreign_keys_on_every_connection()
    {
        await using var db = TestDatabase.Create();
        await using var connection = db.Factory.Create();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA foreign_keys;";
        Assert.Equal(1L, (long)(await command.ExecuteScalarAsync())!);
    }

    [Fact]
    public async Task Foreign_key_rejects_orphan_capability_publication()
    {
        await using var db = TestDatabase.Create(); await db.Runner.ApplyAsync();
        await using var connection = db.Factory.Create(); await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO wgt_capability_publications(service_id, capability_id, title, contract_version, ordinal) VALUES ('missing', 'capability', 'Capability', '1.0', 0);";
        await Assert.ThrowsAsync<SqliteException>(() => command.ExecuteNonQueryAsync());
    }

    [Fact]
    public async Task Fresh_migrations_are_idempotent_and_create_only_wgt_tables()
    {
        await using var db = TestDatabase.Create();
        await db.Runner.ApplyAsync();
        await db.Runner.ApplyAsync();
        var tables = await db.TableNamesAsync();
        Assert.Equal(["wgt_capability_publications", "wgt_integration_publications", "wgt_local_device", "wgt_publication_refresh_states", "wgt_schema_migrations", "wgt_service_integration_device_overrides", "wgt_service_integrations"], tables);
        Assert.Equal([1, 2, 3], await db.MigrationVersionsAsync());
    }

    [Fact]
    public async Task Existing_bootstrap_database_receives_only_migration_two()
    {
        await using var db = TestDatabase.Create();
        await using (var connection = db.Factory.Create())
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = "CREATE TABLE wgt_schema_migrations(version INTEGER PRIMARY KEY, applied_at_utc TEXT NOT NULL); INSERT INTO wgt_schema_migrations VALUES (1, '2026-08-09T00:00:00Z'); CREATE TABLE wgt_integration_publications(service_id TEXT PRIMARY KEY, display_name TEXT NOT NULL, published_at_utc TEXT NOT NULL); INSERT INTO wgt_integration_publications VALUES ('bootstrap-service', 'Existing', '2026-08-09T00:00:00Z');";
            await command.ExecuteNonQueryAsync();
        }
        await db.Runner.ApplyAsync();
        Assert.Equal([1, 2, 3], await db.MigrationVersionsAsync());
        var service = new ServiceIdentity("bootstrap-service");
        var existing = await new SqliteIntegrationPublicationStore(db.Factory).LoadAsync(service);
        Assert.Equal("Existing", existing.Publication!.DisplayName); Assert.False(existing.RefreshObservation.HasAttempted);
    }

    [Fact]
    public async Task Migration_failure_rolls_back_schema_and_marker()
    {
        await using var db = TestDatabase.Create();
        var runner = new MigrationRunner(db.Factory, [new MigrationScript("0007_failure.sql", "CREATE TABLE should_rollback (id INTEGER); INSERT INTO definitely_missing_table VALUES (1);")]);
        await Assert.ThrowsAnyAsync<Exception>(() => runner.ApplyAsync());
        Assert.DoesNotContain("should_rollback", await db.TableNamesAsync());
        Assert.Empty(await db.MigrationVersionsAsync());
    }

    [Fact]
    public async Task Local_device_is_a_singleton_and_survives_restart()
    {
        await using var db = TestDatabase.Create(); await db.Runner.ApplyAsync();
        var identity = DeviceIdentity.New();
        var first = new SqliteLocalDeviceStore(db.Factory);
        await first.SaveAsync(new LocalDeviceConfiguration(identity, "  Desktop  "));
        await first.SaveAsync(new LocalDeviceConfiguration(identity, "Renamed"));
        await using var restarted = db.Reopen();
        var loaded = await new SqliteLocalDeviceStore(restarted.Factory).LoadAsync();
        Assert.NotNull(loaded); Assert.Equal(identity, loaded!.DeviceIdentity); Assert.Equal("Renamed", loaded.DisplayName);
        Assert.Equal(1, await restarted.ScalarAsync("SELECT COUNT(*) FROM wgt_local_device;"));
    }

    [Fact]
    public async Task Local_device_identity_cannot_be_replaced()
    {
        await using var db = TestDatabase.Create(); await db.Runner.ApplyAsync(); var store = new SqliteLocalDeviceStore(db.Factory);
        var identity = DeviceIdentity.New(); var replacement = DeviceIdentity.New();
        await store.SaveAsync(new LocalDeviceConfiguration(identity, "First"));
        await store.SaveAsync(new LocalDeviceConfiguration(identity, "Renamed"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => store.SaveAsync(new LocalDeviceConfiguration(replacement, "Replacement")).AsTask());
        var loaded = await store.LoadAsync(); Assert.NotNull(loaded); Assert.Equal(identity, loaded!.DeviceIdentity); Assert.Equal("Renamed", loaded.DisplayName);
    }

    [Fact]
    public async Task Service_integration_round_trips_global_state_and_overrides_atomically()
    {
        await using var db = TestDatabase.Create(); await db.Runner.ApplyAsync();
        var service = new ServiceIntegration(new ServiceIdentity("service-a")); service.EnableGlobally();
        var deviceA = DeviceIdentity.New(); var deviceB = DeviceIdentity.New();
        service.SetDeviceOverride(deviceA, Enablement.Disabled); service.SetDeviceOverride(deviceB, Enablement.Enabled);
        var store = new SqliteServiceIntegrationStore(db.Factory); await store.SaveAsync(service);
        await using var restarted = db.Reopen(); var loaded = await new SqliteServiceIntegrationStore(restarted.Factory).LoadAsync(service.ServiceIdentity);
        Assert.NotNull(loaded); Assert.Equal(Enablement.Enabled, loaded!.GlobalEnablement); Assert.Equal(Enablement.Disabled, loaded.GetEffectiveEnablement(deviceA)); Assert.Equal(Enablement.Enabled, loaded.GetEffectiveEnablement(deviceB));
        service.ClearDeviceOverride(deviceA); await store.SaveAsync(service);
        Assert.Equal(Enablement.Enabled, (await store.LoadAsync(service.ServiceIdentity))!.GetEffectiveEnablement(deviceA));
    }

    [Fact]
    public async Task Service_integration_global_disabled_round_trips()
    {
        await using var db = TestDatabase.Create(); await db.Runner.ApplyAsync();
        var service = new ServiceIntegration(new ServiceIdentity("disabled-service"));
        await new SqliteServiceIntegrationStore(db.Factory).SaveAsync(service);
        await using var restarted = db.Reopen();
        var loaded = await new SqliteServiceIntegrationStore(restarted.Factory).LoadAsync(service.ServiceIdentity);
        Assert.NotNull(loaded); Assert.Equal(Enablement.Disabled, loaded!.GlobalEnablement);
    }

    [Fact]
    public async Task Service_integrations_are_isolated_and_load_all_is_deterministic()
    {
        await using var db = TestDatabase.Create(); await db.Runner.ApplyAsync(); var store = new SqliteServiceIntegrationStore(db.Factory);
        await store.SaveAsync(new ServiceIntegration(new ServiceIdentity("z-service"))); await store.SaveAsync(new ServiceIntegration(new ServiceIdentity("a-service")));
        Assert.Equal(["a-service", "z-service"], (await store.LoadAllAsync()).Select(x => x.ServiceIdentity.Value));
    }

    [Fact]
    public async Task Publication_round_trip_preserves_order_and_replacement()
    {
        await using var db = TestDatabase.Create(); await db.Runner.ApplyAsync(); var store = new SqliteIntegrationPublicationStore(db.Factory); var service = new ServiceIdentity("publication-service");
        var first = new ServicePublication(service, "First", [new(new CapabilityIdentity("two"), "Two", new Version(2, 0)), new(new CapabilityIdentity("one"), "One", new Version(1, 0))], DateTimeOffset.UtcNow);
        await store.SaveAsync(new IntegrationPublicationState(service, first, PublicationRefreshObservation.Completed(first.PublishedAtUtc, IntegrationRefreshStatus.Refreshed, first.PublishedAtUtc))); var loaded = (await store.LoadAsync(service)).Publication; Assert.NotNull(loaded); Assert.Equal(["two", "one"], loaded!.Capabilities.Select(x => x.Id.Value)); Assert.Equal(first.PublishedAtUtc, loaded.PublishedAtUtc);
        var second = new ServicePublication(service, "Second", [new(new CapabilityIdentity("one"), "Updated", new Version(3, 0))], first.PublishedAtUtc.AddMinutes(1)); await store.SaveAsync(new IntegrationPublicationState(service, second, PublicationRefreshObservation.Completed(second.PublishedAtUtc, IntegrationRefreshStatus.Refreshed, second.PublishedAtUtc)));
        loaded = (await store.LoadAsync(service)).Publication; Assert.Equal("Second", loaded!.DisplayName); Assert.Single(loaded.Capabilities); Assert.Equal("Updated", loaded.Capabilities[0].Title); Assert.Equal(new Version(3, 0), loaded.Capabilities[0].ContractVersion);
    }

    [Fact]
    public async Task Publication_snapshot_survives_restart()
    {
        await using var db = TestDatabase.Create(); await db.Runner.ApplyAsync();
        var service = new ServiceIdentity("restart-publication");
        var publication = new ServicePublication(service, "Restart", [new(new CapabilityIdentity("cap"), "Capability", new Version(1, 2))], DateTimeOffset.UtcNow);
        await new SqliteIntegrationPublicationStore(db.Factory).SaveAsync(new IntegrationPublicationState(service, publication, PublicationRefreshObservation.Completed(publication.PublishedAtUtc, IntegrationRefreshStatus.Refreshed, publication.PublishedAtUtc)));
        await using var restarted = db.Reopen(); await restarted.Runner.ApplyAsync();
        var loaded = (await new SqliteIntegrationPublicationStore(restarted.Factory).LoadAsync(service)).Publication;
        Assert.NotNull(loaded); Assert.Equal("Restart", loaded!.DisplayName); Assert.Equal("cap", loaded.Capabilities.Single().Id.Value);
    }

    [Fact]
    public async Task Publication_and_refresh_metadata_survive_restart_and_failed_refresh_retains_snapshot()
    {
        await using var db = TestDatabase.Create(); await db.Runner.ApplyAsync(); var store = new SqliteIntegrationPublicationStore(db.Factory); var service = new ServiceIdentity("lifecycle-service"); var firstAt = DateTimeOffset.UtcNow;
        var publication = new ServicePublication(service, "Retained", [new(new("cap"), "Capability", new(1, 0))], firstAt.AddMinutes(-1));
        await store.SaveAsync(new IntegrationPublicationState(service, publication, PublicationRefreshObservation.Completed(firstAt, IntegrationRefreshStatus.Refreshed, firstAt)));
        await using var restarted = db.Reopen(); await restarted.Runner.ApplyAsync(); var restartedStore = new SqliteIntegrationPublicationStore(restarted.Factory);
        var loaded = await restartedStore.LoadAsync(service); Assert.Equal("Retained", loaded.Publication!.DisplayName); Assert.Equal(IntegrationRefreshStatus.Refreshed, loaded.RefreshObservation.LatestResult); Assert.Equal(firstAt, loaded.RefreshObservation.LastSuccessfulRefreshAtUtc);
        var failedAt = firstAt.AddHours(1); await restartedStore.SaveAsync(new IntegrationPublicationState(service, null, PublicationRefreshObservation.Completed(failedAt, IntegrationRefreshStatus.AdapterFailed, firstAt)));
        loaded = await restartedStore.LoadAsync(service); Assert.Equal("Retained", loaded.Publication!.DisplayName); Assert.Equal(IntegrationRefreshStatus.AdapterFailed, loaded.RefreshObservation.LatestResult); Assert.Equal(firstAt, loaded.RefreshObservation.LastSuccessfulRefreshAtUtc);
    }

    [Fact]
    public async Task Other_publications_are_unaffected_by_replacement()
    {
        await using var db = TestDatabase.Create(); await db.Runner.ApplyAsync(); var store = new SqliteIntegrationPublicationStore(db.Factory);
        var a = new ServiceIdentity("a"); var b = new ServiceIdentity("b"); var timestamp = DateTimeOffset.UtcNow;
        await store.SaveAsync(new IntegrationPublicationState(a, new ServicePublication(a, "A", [new(new CapabilityIdentity("a-cap"), "A", new Version(1, 0))], timestamp), PublicationRefreshObservation.Completed(timestamp, IntegrationRefreshStatus.Refreshed, timestamp))); await store.SaveAsync(new IntegrationPublicationState(b, new ServicePublication(b, "B", [new(new CapabilityIdentity("b-cap"), "B", new Version(1, 0))], timestamp), PublicationRefreshObservation.Completed(timestamp, IntegrationRefreshStatus.Refreshed, timestamp)));
        await store.SaveAsync(new IntegrationPublicationState(a, new ServicePublication(a, "A2", [], timestamp.AddDays(1)), PublicationRefreshObservation.Completed(timestamp.AddDays(1), IntegrationRefreshStatus.Refreshed, timestamp.AddDays(1))));
        Assert.Single((await store.LoadAsync(b)).Publication!.Capabilities); Assert.Equal("B", (await store.LoadAsync(b)).Publication!.DisplayName);
    }

    [Fact]
    public async Task Failed_publication_replacement_rolls_back_header_and_capability_changes()
    {
        await using var db = TestDatabase.Create(); await db.Runner.ApplyAsync(); var store = new SqliteIntegrationPublicationStore(db.Factory); var service = new ServiceIdentity("transactional-service"); var timestamp = DateTimeOffset.UtcNow;
        var original = new ServicePublication(service, "Original", [new(new("original"), "Original", new(1, 0))], timestamp);
        await store.SaveAsync(new IntegrationPublicationState(service, original, PublicationRefreshObservation.Completed(timestamp, IntegrationRefreshStatus.Refreshed, timestamp)));
        var invalid = new ServicePublication(service, "Should Roll Back", [new(new("duplicate"), "One", new(1, 0)), new(new("duplicate"), "Two", new(1, 0))], timestamp.AddMinutes(1));
        var saveInvalid = new IntegrationPublicationState(service, invalid, PublicationRefreshObservation.Completed(timestamp.AddMinutes(1), IntegrationRefreshStatus.Refreshed, timestamp.AddMinutes(1)));
        await Assert.ThrowsAsync<SqliteException>(() => store.SaveAsync(saveInvalid).AsTask());
        var retained = (await store.LoadAsync(service)).Publication!; Assert.Equal("Original", retained.DisplayName); Assert.Equal("original", retained.Capabilities.Single().Id.Value);
    }

    private sealed class TestDatabase : IAsyncDisposable
    {
        private TestDatabase(string path) { Path = path; Factory = new SqliteConnectionFactory($"Data Source={path};Pooling=False"); Runner = new MigrationRunner(Factory); }
        public string Path { get; } public SqliteConnectionFactory Factory { get; } public MigrationRunner Runner { get; }
        public static TestDatabase Create() => new(System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"wgt-{Guid.NewGuid():N}.db"));
        public TestDatabase Reopen() => new(Path);
        public async Task<List<string>> TableNamesAsync() { await using var c = Factory.Create(); await c.OpenAsync(); await using var cmd = c.CreateCommand(); cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name;"; var result = new List<string>(); await using var r = await cmd.ExecuteReaderAsync(); while (await r.ReadAsync()) result.Add(r.GetString(0)); return result; }
        public async Task<List<int>> MigrationVersionsAsync() { await using var c = Factory.Create(); await c.OpenAsync(); await using var cmd = c.CreateCommand(); cmd.CommandText = "SELECT version FROM wgt_schema_migrations ORDER BY version;"; var result = new List<int>(); await using var r = await cmd.ExecuteReaderAsync(); while (await r.ReadAsync()) result.Add(r.GetInt32(0)); return result; }
        public async Task<long> ScalarAsync(string sql) { await using var c = Factory.Create(); await c.OpenAsync(); await using var cmd = c.CreateCommand(); cmd.CommandText = sql; return (long)(await cmd.ExecuteScalarAsync())!; }
        public ValueTask DisposeAsync() { if (File.Exists(Path)) File.Delete(Path); return ValueTask.CompletedTask; }
    }
}
