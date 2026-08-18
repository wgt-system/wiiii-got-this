using System.Diagnostics;
using WiiiiGotThis.Presentation;

namespace WiiiiGotThis.Desktop;

public sealed class VocationDesktopProductRuntime : IVocationProductRuntime, IDisposable
{
    private static readonly Uri DefaultProductUri = new("http://127.0.0.1:8765/");
    private readonly HttpClient httpClient = new() { Timeout = TimeSpan.FromSeconds(2) };
    private readonly SemaphoreSlim gate = new(1, 1);
    private Process? ownedProcess;
    private bool disposed;

    public async Task<ProductRuntimeReadiness> EnsureReadyAsync(Uri productUri, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        ArgumentNullException.ThrowIfNull(productUri);

        await gate.WaitAsync(cancellationToken);
        try
        {
            if (await DesktopProviderRuntimeSupport.IsSuccessfulHttpResponseAsync(httpClient, productUri, cancellationToken))
                return ProductRuntimeReadiness.Ready;

            if (!DesktopProviderRuntimeSupport.IsSameEndpoint(productUri, DefaultProductUri))
            {
                return ProductRuntimeReadiness.Unavailable(
                    $"The configured Vocation endpoint {productUri} is not running. Automatic startup is limited to the default local Vocation endpoint.");
            }

            if (ownedProcess is { HasExited: true })
            {
                ownedProcess.Dispose();
                ownedProcess = null;
            }

            var root = DesktopProviderRuntimeSupport.ResolveRepositoryRoot("WGT_VOCATION_ROOT", "vocation", "pyproject.toml");
            if (root is null)
            {
                return ProductRuntimeReadiness.Unavailable(
                    "Vocation is not running and its repository could not be located. Set WGT_VOCATION_ROOT or keep the Vocation repository beside Wiiii Got This.");
            }

            var python = Path.Combine(root, ".venv", "Scripts", "python.exe");
            if (!File.Exists(python))
            {
                return ProductRuntimeReadiness.Unavailable(
                    "Vocation was found, but its Python environment is missing. Run `uv sync --locked --extra dev` in the Vocation repository.");
            }

            var frontend = await EnsureVocationFrontendAsync(root, cancellationToken);
            if (!frontend.IsReady)
                return frontend;

            ownedProcess ??= DesktopProviderRuntimeSupport.StartProcess(
                python,
                root,
                "-m",
                "vocation",
                "--no-browser");

            var ready = await DesktopProviderRuntimeSupport.WaitForSuccessfulHttpResponseAsync(
                httpClient,
                productUri,
                ownedProcess,
                TimeSpan.FromSeconds(30),
                cancellationToken);
            return ready
                ? ProductRuntimeReadiness.Ready
                : ProductRuntimeReadiness.Unavailable(
                    "WGT started Vocation, but the provider did not expose its local product surface. Check the Vocation runtime prerequisites and retry.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return ProductRuntimeReadiness.Unavailable($"Vocation could not be started: {ex.Message}");
        }
        finally
        {
            gate.Release();
        }
    }

    public void Dispose()
    {
        if (disposed)
            return;
        disposed = true;
        DesktopProviderRuntimeSupport.StopOwnedProcess(ownedProcess);
        ownedProcess?.Dispose();
        httpClient.Dispose();
        gate.Dispose();
    }

    private static async Task<ProductRuntimeReadiness> EnsureVocationFrontendAsync(
        string root,
        CancellationToken cancellationToken)
    {
        var index = Path.Combine(root, "frontend", "dist", "index.html");
        if (File.Exists(index))
            return ProductRuntimeReadiness.Ready;

        if (!Directory.Exists(Path.Combine(root, "frontend", "node_modules")))
        {
            return ProductRuntimeReadiness.Unavailable(
                "Vocation's web dependencies are missing. Run `pnpm --dir frontend install --frozen-lockfile` in the Vocation repository.");
        }

        var build = DesktopProviderRuntimeSupport.StartProcess(
            "cmd.exe",
            root,
            "/d",
            "/s",
            "/c",
            "pnpm --dir frontend build");
        using (build)
        {
            await build.WaitForExitAsync(cancellationToken);
            if (build.ExitCode != 0 || !File.Exists(index))
            {
                return ProductRuntimeReadiness.Unavailable(
                    "WGT found Vocation but could not build its provider-owned web surface. Run `pnpm --dir frontend build` in Vocation and retry.");
            }
        }

        return ProductRuntimeReadiness.Ready;
    }
}

public sealed class OrientationDesktopProductRuntime : IOrientationProductRuntime, IDisposable
{
    private static readonly Uri DefaultProductUri = new("http://127.0.0.1:5173/app.html");
    private static readonly Uri BackendProbeUri = new("http://127.0.0.1:8080/");
    private readonly HttpClient httpClient = new() { Timeout = TimeSpan.FromSeconds(2) };
    private readonly SemaphoreSlim gate = new(1, 1);
    private Process? ownedMapProcess;
    private Process? ownedBackendProcess;
    private bool disposed;

    public async Task<ProductRuntimeReadiness> EnsureReadyAsync(Uri productUri, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        ArgumentNullException.ThrowIfNull(productUri);

        await gate.WaitAsync(cancellationToken);
        try
        {
            var mapReady = await DesktopProviderRuntimeSupport.IsSuccessfulHttpResponseAsync(httpClient, productUri, cancellationToken);
            var isDefaultEndpoint = DesktopProviderRuntimeSupport.IsSameEndpoint(productUri, DefaultProductUri);
            if (!isDefaultEndpoint)
            {
                return mapReady
                    ? ProductRuntimeReadiness.Ready
                    : ProductRuntimeReadiness.Unavailable(
                        $"The configured Orientation endpoint {productUri} is not running. WGT will not infer or start additional backend processes for a custom provider endpoint.");
            }

            var backendReady = await DesktopProviderRuntimeSupport.IsHttpServerRespondingAsync(httpClient, BackendProbeUri, cancellationToken);
            if (mapReady && backendReady)
                return ProductRuntimeReadiness.Ready;

            var root = DesktopProviderRuntimeSupport.ResolveRepositoryRoot("WGT_ORIENTATION_ROOT", "orientation", Path.Combine("backend", "pom.xml"));
            if (root is null)
            {
                return ProductRuntimeReadiness.Unavailable(
                    "Orientation is not running and its repository could not be located. Set WGT_ORIENTATION_ROOT or keep the Orientation repository beside Wiiii Got This.");
            }

            if (!Directory.Exists(Path.Combine(root, "map", "node_modules")))
            {
                return ProductRuntimeReadiness.Unavailable(
                    "Orientation was found, but its map dependencies are missing. Install the provider's map dependencies and retry.");
            }

            if (!backendReady)
            {
                if (ownedBackendProcess is { HasExited: true })
                {
                    ownedBackendProcess.Dispose();
                    ownedBackendProcess = null;
                }
                ownedBackendProcess ??= DesktopProviderRuntimeSupport.StartProcess(
                    "powershell.exe",
                    root,
                    "-NoLogo",
                    "-NoProfile",
                    "-ExecutionPolicy",
                    "Bypass",
                    "-File",
                    Path.Combine(root, "scripts", "dev.ps1"),
                    "-Target",
                    "backend");
            }

            if (!mapReady)
            {
                if (ownedMapProcess is { HasExited: true })
                {
                    ownedMapProcess.Dispose();
                    ownedMapProcess = null;
                }
                ownedMapProcess ??= DesktopProviderRuntimeSupport.StartProcess(
                    "cmd.exe",
                    Path.Combine(root, "map"),
                    "/d",
                    "/s",
                    "/c",
                    "npm run dev -- --host 127.0.0.1 --port 5173 --strictPort");
            }

            mapReady = await DesktopProviderRuntimeSupport.WaitForSuccessfulHttpResponseAsync(
                httpClient,
                productUri,
                ownedMapProcess,
                TimeSpan.FromSeconds(35),
                cancellationToken);
            backendReady = await DesktopProviderRuntimeSupport.WaitForHttpServerAsync(
                httpClient,
                BackendProbeUri,
                ownedBackendProcess,
                TimeSpan.FromSeconds(45),
                cancellationToken);

            if (!mapReady)
            {
                return ProductRuntimeReadiness.Unavailable(
                    "WGT started Orientation, but its standalone map surface did not become reachable. Check Node/npm and the Orientation map runtime, then retry.");
            }
            if (!backendReady)
            {
                return ProductRuntimeReadiness.Unavailable(
                    "Orientation's map surface started, but its local Java backend did not become reachable. Check Java/Maven and the Orientation backend prerequisites, then retry.");
            }

            return ProductRuntimeReadiness.Ready;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return ProductRuntimeReadiness.Unavailable($"Orientation could not be started: {ex.Message}");
        }
        finally
        {
            gate.Release();
        }
    }

    public void Dispose()
    {
        if (disposed)
            return;
        disposed = true;
        DesktopProviderRuntimeSupport.StopOwnedProcess(ownedMapProcess);
        DesktopProviderRuntimeSupport.StopOwnedProcess(ownedBackendProcess);
        ownedMapProcess?.Dispose();
        ownedBackendProcess?.Dispose();
        httpClient.Dispose();
        gate.Dispose();
    }
}

internal static class DesktopProviderRuntimeSupport
{
    public static string? ResolveRepositoryRoot(
        string environmentVariable,
        string repositoryDirectoryName,
        string requiredRelativePath)
    {
        var configured = Environment.GetEnvironmentVariable(environmentVariable);
        if (!string.IsNullOrWhiteSpace(configured))
        {
            var candidate = Path.GetFullPath(configured.Trim());
            if (HasRequiredPath(candidate, requiredRelativePath))
                return candidate;
        }

        foreach (var start in new[] { Environment.CurrentDirectory, AppContext.BaseDirectory })
        {
            var directory = new DirectoryInfo(Path.GetFullPath(start));
            for (var depth = 0; directory is not null && depth < 12; depth++, directory = directory.Parent)
            {
                if (string.Equals(directory.Name, repositoryDirectoryName, StringComparison.OrdinalIgnoreCase)
                    && HasRequiredPath(directory.FullName, requiredRelativePath))
                {
                    return directory.FullName;
                }

                var sibling = Path.Combine(directory.FullName, repositoryDirectoryName);
                if (HasRequiredPath(sibling, requiredRelativePath))
                    return sibling;
            }
        }

        return null;
    }

    public static bool IsSameEndpoint(Uri left, Uri right) =>
        string.Equals(left.Scheme, right.Scheme, StringComparison.OrdinalIgnoreCase)
        && left.Port == right.Port
        && left.IsLoopback
        && right.IsLoopback;

    public static Process StartProcess(string fileName, string workingDirectory, params string[] arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        foreach (var argument in arguments)
            startInfo.ArgumentList.Add(argument);

        return Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Failed to start {fileName}.");
    }

    public static async Task<bool> IsSuccessfulHttpResponseAsync(
        HttpClient client,
        Uri uri,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException)
        {
            return false;
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return false;
        }
    }

    public static async Task<bool> IsHttpServerRespondingAsync(
        HttpClient client,
        Uri uri,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            return true;
        }
        catch (HttpRequestException)
        {
            return false;
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return false;
        }
    }

    public static Task<bool> WaitForSuccessfulHttpResponseAsync(
        HttpClient client,
        Uri uri,
        Process? ownedProcess,
        TimeSpan timeout,
        CancellationToken cancellationToken) =>
        WaitForEndpointAsync(client, uri, ownedProcess, timeout, requireSuccessStatus: true, cancellationToken);

    public static Task<bool> WaitForHttpServerAsync(
        HttpClient client,
        Uri uri,
        Process? ownedProcess,
        TimeSpan timeout,
        CancellationToken cancellationToken) =>
        WaitForEndpointAsync(client, uri, ownedProcess, timeout, requireSuccessStatus: false, cancellationToken);

    public static void StopOwnedProcess(Process? process)
    {
        if (process is null)
            return;
        try
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException)
        {
        }
        catch (System.ComponentModel.Win32Exception)
        {
        }
    }

    private static bool HasRequiredPath(string root, string relativePath) =>
        Directory.Exists(root)
        && (File.Exists(Path.Combine(root, relativePath)) || Directory.Exists(Path.Combine(root, relativePath)));

    private static async Task<bool> WaitForEndpointAsync(
        HttpClient client,
        Uri uri,
        Process? ownedProcess,
        TimeSpan timeout,
        bool requireSuccessStatus,
        CancellationToken cancellationToken)
    {
        var deadline = DateTimeOffset.UtcNow + timeout;
        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (ownedProcess is { HasExited: true })
                return false;

            var ready = requireSuccessStatus
                ? await IsSuccessfulHttpResponseAsync(client, uri, cancellationToken)
                : await IsHttpServerRespondingAsync(client, uri, cancellationToken);
            if (ready)
                return true;

            await Task.Delay(TimeSpan.FromMilliseconds(300), cancellationToken);
        }

        return false;
    }
}
