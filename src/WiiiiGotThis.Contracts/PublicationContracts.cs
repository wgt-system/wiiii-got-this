using WiiiiGotThis.Domain;

namespace WiiiiGotThis.Contracts;

public sealed record CapabilityPublication(
    CapabilityId Id,
    string Title,
    Version ContractVersion,
    Availability Availability);

public sealed record ServicePublication(
    ServiceId ServiceId,
    string DisplayName,
    IReadOnlyList<CapabilityPublication> Capabilities,
    DateTimeOffset PublishedAtUtc);

public interface IIntegrationAdapter
{
    ServiceId ServiceId { get; }
    ValueTask<ServicePublication> GetPublicationAsync(CancellationToken cancellationToken = default);
}
