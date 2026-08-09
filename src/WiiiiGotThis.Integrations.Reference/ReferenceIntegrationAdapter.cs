using WiiiiGotThis.Contracts;
using WiiiiGotThis.Domain;

namespace WiiiiGotThis.Integrations.Reference;

public sealed class ReferenceIntegrationAdapter : IIntegrationAdapter
{
    public static readonly ServiceIdentity StableServiceIdentity = new("reference-service");
    public ServiceIdentity ServiceId => StableServiceIdentity;

    public ValueTask<ServicePublication> GetPublicationAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(new ServicePublication(
            StableServiceIdentity,
            "Reference Integration",
            [
                new(new CapabilityIdentity("reference.available"), "Reference capability", new Version(1, 0)),
                new(new CapabilityIdentity("reference.unsupported"), "Unsupported reference capability", new Version(99, 0)),
                new(new CapabilityIdentity("reference.unreachable"), "Unavailable reference capability", new Version(1, 0))
            ],
            DateTimeOffset.UtcNow));
    }
}
