using WiiiiGotThis.Domain;

namespace WiiiiGotThis.Contracts;

public sealed record CapabilityPublication(
    CapabilityIdentity Id,
    string Title,
    Version ContractVersion);

public sealed record ServicePublication(
    ServiceIdentity ServiceId,
    string DisplayName,
    IReadOnlyList<CapabilityPublication> Capabilities,
    DateTimeOffset PublishedAtUtc);

public interface IIntegrationAdapter
{
    ServiceIdentity ServiceId { get; }
    ValueTask<ServicePublication> GetPublicationAsync(CancellationToken cancellationToken = default);
}
