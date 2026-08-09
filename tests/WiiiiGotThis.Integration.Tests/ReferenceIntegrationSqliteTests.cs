using WiiiiGotThis.Application;
using WiiiiGotThis.Domain;
using WiiiiGotThis.Infrastructure;
using WiiiiGotThis.Integrations.Reference;

namespace WiiiiGotThis.Integration.Tests;

public sealed class ReferenceIntegrationSqliteTests
{
    [Fact]
    public async Task Reference_refresh_survives_sqlite_restart_with_registration_disabled()
    {
        var path = Path.Combine(Path.GetTempPath(), $"wgt-reference-{Guid.NewGuid():N}.db");
        try
        {
            await using (var first = new Database(path))
            {
                await first.Runner.ApplyAsync();
                var adapter = new ReferenceIntegrationAdapter();
                var refresh = new RefreshPublicationsUseCase(
                    new StaticIntegrationAdapterCatalog([adapter]),
                    new SqliteServiceIntegrationStore(first.Factory),
                    new SqliteIntegrationPublicationStore(first.Factory));
                Assert.Equal(IntegrationRefreshStatus.Refreshed, (await refresh.RefreshAsync()).Single().Status);
            }

            await using (var reopened = new Database(path))
            {
                await reopened.Runner.ApplyAsync();
                var integration = await new SqliteServiceIntegrationStore(reopened.Factory).LoadAsync(ReferenceIntegrationAdapter.StableServiceIdentity);
                var publication = await new SqliteIntegrationPublicationStore(reopened.Factory).LoadAsync(ReferenceIntegrationAdapter.StableServiceIdentity);
                Assert.NotNull(integration);
                Assert.Equal(Enablement.Disabled, integration!.GlobalEnablement);
                Assert.NotNull(publication);
                Assert.Equal(4, publication!.Capabilities.Count);
                Assert.Equal(["reference.available", "reference.unsupported", "reference.unavailable", "reference.version-mismatch"], publication.Capabilities.Select(x => x.Id.Value));
            }
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    private sealed class Database(string path) : IAsyncDisposable
    {
        public SqliteConnectionFactory Factory { get; } = new($"Data Source={path};Pooling=False");
        public MigrationRunner Runner { get; } = new(new SqliteConnectionFactory($"Data Source={path};Pooling=False"));
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
