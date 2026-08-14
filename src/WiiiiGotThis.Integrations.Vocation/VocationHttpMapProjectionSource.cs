using WiiiiGotThis.Application;

namespace WiiiiGotThis.Integrations.Vocation;

public sealed class VocationHttpMapProjectionSource : IVocationMapProjectionSource
{
    public static readonly Uri DefaultBaseUri = new("http://127.0.0.1:8765/");
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);
    private const string PublicationPath = "published/v1/map-projection";
    private readonly HttpClient httpClient;
    private readonly Uri baseUri;

    public VocationHttpMapProjectionSource(HttpClient httpClient, Uri? baseUri = null, TimeSpan? timeout = null)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        this.httpClient = httpClient;
        this.baseUri = NormalizeBaseUri(baseUri ?? DefaultBaseUri);
        var effectiveTimeout = timeout ?? DefaultTimeout;
        if (effectiveTimeout <= TimeSpan.Zero || effectiveTimeout == Timeout.InfiniteTimeSpan)
            throw new ArgumentOutOfRangeException(nameof(timeout), "The Vocation HTTP timeout must be finite and positive.");
        httpClient.Timeout = effectiveTimeout;
    }

    public Uri BaseUri => baseUri;

    public async ValueTask<VocationMapProjection> GetAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            using var response = await httpClient.GetAsync(new Uri(baseUri, PublicationPath), HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (!response.IsSuccessStatusCode)
                throw Failure(VocationMapProjectionSourceFailureKind.Unavailable, "The Vocation map publication endpoint returned a non-success status.");

            var payload = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            return VocationMapProjectionContractReader.Read(payload);
        }
        catch (VocationPublishedContractValidationException exception) when (!cancellationToken.IsCancellationRequested)
        {
            var kind = exception.Kind switch
            {
                VocationContractFailureKind.UnsupportedContractVersion => VocationMapProjectionSourceFailureKind.IncompatibleContract,
                VocationContractFailureKind.MalformedContract or VocationContractFailureKind.UnexpectedCapability => VocationMapProjectionSourceFailureKind.InvalidContract,
                _ => VocationMapProjectionSourceFailureKind.InvalidContract
            };
            throw Failure(kind, "The Vocation map publication contract was not accepted.", exception.UnsupportedVersion);
        }
        catch (HttpRequestException) when (!cancellationToken.IsCancellationRequested)
        {
            throw Failure(VocationMapProjectionSourceFailureKind.Unavailable, "The Vocation map publication endpoint was unavailable.");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw Failure(VocationMapProjectionSourceFailureKind.Unavailable, "The Vocation map publication request timed out.");
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw Failure(VocationMapProjectionSourceFailureKind.Unavailable, "The Vocation map publication endpoint could not be read.");
        }
    }

    private static Uri NormalizeBaseUri(Uri uri)
    {
        if (!uri.IsAbsoluteUri) throw new ArgumentException("The Vocation base URI must be absolute.", nameof(uri));
        return new Uri(uri.AbsoluteUri.EndsWith('/') ? uri.AbsoluteUri : uri.AbsoluteUri + "/", UriKind.Absolute);
    }

    private static VocationMapProjectionSourceException Failure(
        VocationMapProjectionSourceFailureKind kind,
        string message,
        string? observedContractVersion = null) => new(kind, message, observedContractVersion);
}
