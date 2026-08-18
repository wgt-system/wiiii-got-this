using System.Net;
using System.Net.Sockets;
using System.Text;
using WiiiiGotThis.Desktop;

namespace WiiiiGotThis.Integration.Tests;

public sealed class ProviderProductRuntimeTests
{
    [Fact]
    public async Task Vocation_reuses_an_already_running_loopback_product_without_repository_startup()
    {
        await using var server = new LoopbackHttpServer();
        using var runtime = new VocationDesktopProductRuntime();

        var first = await runtime.EnsureReadyAsync(server.ProductUri);
        var second = await runtime.EnsureReadyAsync(server.ProductUri);

        Assert.True(first.IsReady, first.FailureMessage);
        Assert.True(second.IsReady, second.FailureMessage);
    }

    private sealed class LoopbackHttpServer : IAsyncDisposable
    {
        private static readonly byte[] Response = Encoding.ASCII.GetBytes(
            "HTTP/1.1 200 OK\r\nContent-Length: 2\r\nConnection: close\r\n\r\nOK");

        private readonly TcpListener listener = new(IPAddress.Loopback, 0);
        private readonly CancellationTokenSource cancellation = new();
        private readonly Task serverLoop;

        public LoopbackHttpServer()
        {
            listener.Start();
            var endpoint = (IPEndPoint)listener.LocalEndpoint;
            ProductUri = new Uri($"http://127.0.0.1:{endpoint.Port}/", UriKind.Absolute);
            serverLoop = ServeAsync(cancellation.Token);
        }

        public Uri ProductUri { get; }

        public async ValueTask DisposeAsync()
        {
            await cancellation.CancelAsync();
            listener.Stop();
            try
            {
                await serverLoop;
            }
            catch (OperationCanceledException)
            {
            }
            cancellation.Dispose();
        }

        private async Task ServeAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                using var client = await listener.AcceptTcpClientAsync(cancellationToken);
                await using var stream = client.GetStream();
                var request = new byte[2048];
                _ = await stream.ReadAsync(request, cancellationToken);
                await stream.WriteAsync(Response, cancellationToken);
            }
        }
    }
}
