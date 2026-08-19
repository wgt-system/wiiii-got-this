using System.Diagnostics;
using WiiiiGotThis.Presentation;

namespace WiiiiGotThis.Desktop;

public sealed class VocationDesktopProductRuntime : IVocationProductRuntime, IProductRuntimeStatusSource, IDisposable
{
    private static readonly Uri DefaultProductUri = new("http://127.0.0.1:8765/");
    private static readonly Uri DefaultHealthUri = new("http://127.0.0.1:8765/api/health");
    private readonly HttpClient httpClient = new() { Timeout = TimeSpan.FromSeconds(2) };
    private readonly SemaphoreSlim gate = new(1, 1);
    private Process? ownedProcess;
    private bool disposed;

    public event Action<string>? StageChanged;

    public async Task<ProductRuntimeReadiness> EnsureReadyAsync(Uri productUri, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        ArgumentNullException.ThrowIfNull(productUri);

        await gate.WaitAsync(cancellationToken);
        try
        {
            ReportStage("Checking Vocation runtime…");
            var isDefaultEndpoint = DesktopProviderRuntimeSupport.IsSameEndpoint(productUri, DefaultProductUri);
            if (!isDefaultEndpoint)
            {
                var customReady = await DesktopProviderRuntimeSupport.IsSuccessfulHttpResponseAsync(httpClient, productUri, cancellationToken);
                if (customReady)
                    ReportStage("Vocation endpoint ready.");
                return customReady
                    ? ProductRuntimeReadiness.Ready
                    : ProductRuntimeReadiness.Unavailable(
                        $"The configured Vocation endpoint {productUri} is not running. WGT will not start provider processes for a custom endpoint.");
            }

            var healthReady = await DesktopProviderRuntimeSupport.IsSuccessfulHttpResponseAsync(httpClient, DefaultHealthUri, cancellationToken);
            var productReady = healthReady
                && await DesktopProviderRuntimeSupport.IsSuccessfulHttpResponseAsync(httpClient, productUri, cancellationToken);
            if (productReady)
            {
                ReportStage("Vocation ready.");
                return ProductRuntimeReadiness.Ready;
            }

            if (ownedProcess is { HasExited: true })
            {
                ownedProcess.Dispose();
                ownedProcess = null;
            }

            ReportStage("Locating Vocation checkout…");
            var root = DesktopProviderRuntimeSupport.ResolveRepositoryRoot("WGT_VOCATION_ROOT", "vocation", "pyproject.toml");
            if (root is null)
            {
                return ProductRuntimeReadiness.Unavailable(
                    "Vocation is not running and its repository could not be located. Set WGT_VOCATION_ROOT or keep the Vocation repository beside Wiiii Got This.");
            }

            ReportStage("Preparing Vocation Python environment…");
            var environment = await EnsureVocationEnvironmentAsync(root, cancellationToken);
            if (!environment.IsReady)
                return environment.Readiness;

            ReportStage("Preparing Vocation interface…");
            var frontend = await EnsureVocationFrontendAsync(root, cancellationToken);
            if (!frontend.IsReady)
                return frontend;

            ReportStage("Starting Vocation provider…");
            ownedProcess ??= DesktopProviderRuntimeSupport.StartProcess(
                environment.PythonPath!,
                root,
                "-m",
                "vocation",
                "--no-browser",
                "--host",
                "127.0.0.1",
                "--port",
                "8765");

            ReportStage("Waiting for Vocation health…");
            healthReady = await DesktopProviderRuntimeSupport.WaitForSuccessfulHttpResponseAsync(
                httpClient,
                DefaultHealthUri,
                ownedProcess,
                TimeSpan.FromSeconds(35),
                cancellationToken);
            if (!healthReady)
            {
                return ProductRuntimeReadiness.Unavailable(
                    "WGT started Vocation, but `/api/health` did not become ready within 35 seconds. Check the Vocation Python/migration output and retry.");
            }

            ReportStage("Checking Vocation product surface…");
            productReady = await DesktopProviderRuntimeSupport.WaitForSuccessfulHttpResponseAsync(
                httpClient,
                productUri,
                ownedProcess,
                TimeSpan.FromSeconds(8),
                cancellationToken);
            if (productReady)
            {
                ReportStage("Vocation ready.");
                return ProductRuntimeReadiness.Ready;
            }

            return ProductRuntimeReadiness.Unavailable(
                "Vocation's backend is healthy, but its provider-owned web surface is not being served. Rebuild `frontend/dist` and retry.");
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

    private void ReportStage(string stage) => StageChanged?.Invoke(stage);

    private static async Task<VocationEnvironmentResult> EnsureVocationEnvironmentAsync(
        string root,
        CancellationToken cancellationToken)
    {
        var python = Path.Combine(root, ".venv", "Scripts", "python.exe");
        if (File.Exists(python))
            return VocationEnvironmentResult.Ready(python);

        var uvAvailable = await DesktopProviderRuntimeSupport.CommandSucceedsAsync(
            root,
            TimeSpan.FromSeconds(8),
            "uv",
            ["--version"],
            cancellationToken);
        if (!uvAvailable)
        {
            return VocationEnvironmentResult.Unavailable(
                "Vocation was found, but `.venv` is missing and `uv` is not available. Install uv or run `uv sync --locked --extra dev` in Vocation.");
        }

        var synced = await DesktopProviderRuntimeSupport.RunCommandAsync(
            root,
            TimeSpan.FromMinutes(3),
            "uv",
            ["sync", "--locked", "--extra", "dev"],
            cancellationToken);
        if (!synced || !File.Exists(python))
        {
            return VocationEnvironmentResult.Unavailable(
                "WGT found Vocation and uv, but could not prepare its locked Python environment. Run `uv sync --locked --extra dev` in Vocation and inspect the provider error.");
        }

        return VocationEnvironmentResult.Ready(python);
    }

    private static async Task<ProductRuntimeReadiness> EnsureVocationFrontendAsync(
        string root,
        CancellationToken cancellationToken)
    {
        var frontendRoot = Path.Combine(root, "frontend");
        var index = Path.Combine(frontendRoot, "dist", "index.html");
        if (File.Exists(index))
            return ProductRuntimeReadiness.Ready;

        var pnpmAvailable = await DesktopProviderRuntimeSupport.CommandSucceedsAsync(
            root,
            TimeSpan.FromSeconds(8),
            "pnpm",
            ["--version"],
            cancellationToken);
        if (!pnpmAvailable)
        {
            return ProductRuntimeReadiness.Unavailable(
                "Vocation's web surface is not built and `pnpm` is unavailable. Install pnpm, then run `pnpm --dir frontend install --frozen-lockfile` and `pnpm --dir frontend build`.");
        }

        if (!Directory.Exists(Path.Combine(frontendRoot, "node_modules")))
        {
            var installed = await DesktopProviderRuntimeSupport.RunCommandAsync(
                root,
                TimeSpan.FromMinutes(3),
                "pnpm",
                ["--dir", "frontend", "install", "--frozen-lockfile"],
                cancellationToken);
            if (!installed)
            {
                return ProductRuntimeReadiness.Unavailable(
                    "WGT found Vocation but could not install its locked provider web dependencies. Run `pnpm --dir frontend install --frozen-lockfile` in Vocation and retry.");
            }
        }

        var built = await DesktopProviderRuntimeSupport.RunCommandAsync(
            root,
            TimeSpan.FromMinutes(2),
            "pnpm",
            ["--dir", "frontend", "build"],
            cancellationToken);
        if (!built || !File.Exists(index))
        {
            return ProductRuntimeReadiness.Unavailable(
                "WGT found Vocation but could not build its provider-owned web surface. Run `pnpm --dir frontend build` in Vocation and inspect the provider build error.");
        }

        return ProductRuntimeReadiness.Ready;
    }

    private sealed record VocationEnvironmentResult(bool IsReady, string? PythonPath, ProductRuntimeReadiness Readiness)
    {
        public static VocationEnvironmentResult Ready(string pythonPath) =>
            new(true, pythonPath, ProductRuntimeReadiness.Ready);

        public static VocationEnvironmentResult Unavailable(string message) =>
            new(false, null, ProductRuntimeReadiness.Unavailable(message));
    }
}

public sealed class OrientationDesktopProductRuntime : IOrientationProductRuntime, IProductRuntimeStatusSource, IDisposable
{
    private static readonly Uri DefaultProductUri = new("http://127.0.0.1:5173/app.html");
    private static readonly Uri BackendHealthUri = new("http://127.0.0.1:8080/actuator/health");
    private readonly HttpClient httpClient = new() { Timeout = TimeSpan.FromSeconds(2) };
    private readonly SemaphoreSlim gate = new(1, 1);
    private Process? ownedMapProcess;
    private Process? ownedBackendProcess;
    private bool disposed;

    public event Action<string>? StageChanged;

    public async Task<ProductRuntimeReadiness> EnsureReadyAsync(Uri productUri, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        ArgumentNullException.ThrowIfNull(productUri);

        await gate.WaitAsync(cancellationToken);
        try
        {
            ReportStage("Checking Orientation runtime…");
            var mapReady = await DesktopProviderRuntimeSupport.IsSuccessfulHttpResponseAsync(httpClient, productUri, cancellationToken);
            var isDefaultEndpoint = DesktopProviderRuntimeSupport.IsSameEndpoint(productUri, DefaultProductUri);
            if (!isDefaultEndpoint)
            {
                if (mapReady)
                    ReportStage("Orientation endpoint ready.");
                return mapReady
                    ? ProductRuntimeReadiness.Ready
                    : ProductRuntimeReadiness.Unavailable(
                        $"The configured Orientation endpoint {productUri} is not running. WGT will not infer or start additional backend processes for a custom provider endpoint.");
            }

            var backendReady = await DesktopProviderRuntimeSupport.IsSuccessfulHttpResponseAsync(httpClient, BackendHealthUri, cancellationToken);
            if (mapReady && backendReady)
            {
                ReportStage("Orientation ready.");
                return ProductRuntimeReadiness.Ready;
            }

            ReportStage("Locating Orientation checkout…");
            var root = DesktopProviderRuntimeSupport.ResolveRepositoryRoot(
                "WGT_ORIENTATION_ROOT",
                "orientation",
                Path.Combine("backend", "pom.xml"));
            if (root is null)
            {
                return ProductRuntimeReadiness.Unavailable(
                    "Orientation is not running and its repository could not be located. Set WGT_ORIENTATION_ROOT or keep Orientation beside Wiiii Got This.");
            }

            ReportStage("Preparing Orientation map dependencies…");
            var mapReadyToStart = await EnsureOrientationMapDependenciesAsync(root, cancellationToken);
            if (!mapReadyToStart.IsReady)
                return mapReadyToStart;

            if (!backendReady)
            {
                ReportStage("Starting Orientation Java backend…");
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
                ReportStage("Starting Orientation map surface…");
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

            ReportStage("Waiting for Orientation map + backend…");
            var mapTask = mapReady
                ? Task.FromResult(true)
                : DesktopProviderRuntimeSupport.WaitForSuccessfulHttpResponseAsync(
                    httpClient,
                    productUri,
                    ownedMapProcess,
                    TimeSpan.FromSeconds(35),
                    cancellationToken);
            var backendTask = backendReady
                ? Task.FromResult(true)
                : DesktopProviderRuntimeSupport.WaitForSuccessfulHttpResponseAsync(
                    httpClient,
                    BackendHealthUri,
                    ownedBackendProcess,
                    TimeSpan.FromSeconds(45),
                    cancellationToken);

            await Task.WhenAll(mapTask, backendTask);
            mapReady = await mapTask;
            backendReady = await backendTask;

            if (!mapReady && !backendReady)
            {
                return ProductRuntimeReadiness.Unavailable(
                    "Orientation's map and Java backend both failed to become ready within the bounded startup window. Check Node/npm plus Java/Maven in the Orientation checkout and retry.");
            }
            if (!mapReady)
            {
                return ProductRuntimeReadiness.Unavailable(
                    "Orientation's Java backend is healthy, but its browser map did not become reachable within 35 seconds. Check the Orientation map/Vite process and retry.");
            }
            if (!backendReady)
            {
                return ProductRuntimeReadiness.Unavailable(
                    "Orientation's browser map is ready, but `/actuator/health` did not become healthy within 45 seconds. Check Java 25/Maven and the Orientation backend output, then retry.");
            }

            ReportStage("Orientation ready.");
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

    private void ReportStage(string stage) => StageChanged?.Invoke(stage);

    private static async Task<ProductRuntimeReadiness> EnsureOrientationMapDependenciesAsync(
        string root,
        CancellationToken cancellationToken)
    {
        var mapRoot = Path.Combine(root, "map");
        if (Directory.Exists(Path.Combine(mapRoot, "node_modules")))
            return ProductRuntimeReadiness.Ready;

        var npmAvailable = await DesktopProviderRuntimeSupport.CommandSucceedsAsync(
            mapRoot,
            TimeSpan.FromSeconds(8),
            "npm",
            ["--version"],
            cancellationToken);
        if (!npmAvailable)
        {
            return ProductRuntimeReadiness.Unavailable(
                "Orientation was found, but its map dependencies are missing and npm is unavailable. Install Node/npm and run `npm ci` in `orientation/map`.");
        }

        var installed = await DesktopProviderRuntimeSupport.RunCommandAsync(
            mapRoot,
            TimeSpan.FromMinutes(3),
            "npm",
            ["ci"],
            cancellationToken);
        return installed
            ? ProductRuntimeReadiness.Ready
            : ProductRuntimeReadiness.Unavailable(
                "WGT found Orientation but could not install its locked map dependencies. Run `npm ci` in `orientation/map` and inspect the provider error.");
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

    public static Task<bool> CommandSucceedsAsync(
        string workingDirectory,
        TimeSpan timeout,
        string command,
        string[] arguments,
        CancellationToken cancellationToken) =>
        RunCommandAsync(workingDirectory, timeout, command, arguments, cancellationToken);

    public static async Task<bool> RunCommandAsync(
        string workingDirectory,
        TimeSpan timeout,
        string command,
        string[] arguments,
        CancellationToken cancellationToken)
    {
        Process? process = null;
        try
        {
            process = StartCommandProcess(command, workingDirectory, arguments);
            using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutSource.CancelAfter(timeout);
            try
            {
                await process.WaitForExitAsync(timeoutSource.Token);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                StopOwnedProcess(process);
                return false;
            }
            return process.ExitCode == 0;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return false;
        }
        finally
        {
            process?.Dispose();
        }
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

    public static Task<bool> WaitForSuccessfulHttpResponseAsync(
        HttpClient client,
        Uri uri,
        Process? ownedProcess,
        TimeSpan timeout,
        CancellationToken cancellationToken) =>
        WaitForEndpointAsync(client, uri, ownedProcess, timeout, cancellationToken);

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

    private static Process StartCommandProcess(string command, string workingDirectory, string[] arguments)
    {
        if (OperatingSystem.IsWindows()
            && (string.Equals(command, "npm", StringComparison.OrdinalIgnoreCase)
                || string.Equals(command, "pnpm", StringComparison.OrdinalIgnoreCase)))
        {
            var commandLine = string.Join(' ', new[] { command }.Concat(arguments));
            return StartProcess("cmd.exe", workingDirectory, "/d", "/s", "/c", commandLine);
        }

        return StartProcess(command, workingDirectory, arguments);
    }

    private static bool HasRequiredPath(string root, string relativePath) =>
        Directory.Exists(root)
        && (File.Exists(Path.Combine(root, relativePath)) || Directory.Exists(Path.Combine(root, relativePath)));

    private static async Task<bool> WaitForEndpointAsync(
        HttpClient client,
        Uri uri,
        Process? ownedProcess,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var deadline = DateTimeOffset.UtcNow + timeout;
        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (ownedProcess is { HasExited: true })
                return false;

            if (await IsSuccessfulHttpResponseAsync(client, uri, cancellationToken))
                return true;

            await Task.Delay(TimeSpan.FromMilliseconds(300), cancellationToken);
        }

        return false;
    }
}
