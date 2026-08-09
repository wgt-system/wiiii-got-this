using WiiiiGotThis.Contracts;
using WiiiiGotThis.Domain;

namespace WiiiiGotThis.Integrations.Reference;

public sealed class ReferenceIntegrationAdapter : IIntegrationAdapter
{
    public static readonly ServiceId StableServiceId = new(Guid.Parse("9ef0dc12-4d12-4e2f-9b52-3db4dfc3db01"));
    public ServiceId ServiceId => StableServiceId;

    public ValueTask<ServicePublication> GetPublicationAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(new ServicePublication(
            StableServiceId,
            "Reference Integration",
            [
                new(new CapabilityId("reference.available"), "Reference capability", new Version(1, 0), new(AvailabilityReason.Available)),
                new(new CapabilityId("reference.unsupported"), "Unsupported reference capability", new Version(99, 0), new(AvailabilityReason.UnsupportedContext)),
                new(new CapabilityId("reference.unreachable"), "Unavailable reference capability", new Version(1, 0), new(AvailabilityReason.ProviderUnreachable))
            ],
            DateTimeOffset.UtcNow));
    }
}
