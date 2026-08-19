namespace WiiiiGotThis.Integration.Tests;

public sealed class ProviderRuntimeStartupContractTests
{
    [Fact]
    public void Vocation_uses_provider_health_and_can_prepare_a_missing_dev_environment()
    {
        var source = RuntimeSource();

        Assert.Contains("http://127.0.0.1:8765/api/health", source, StringComparison.Ordinal);
        Assert.Contains("Preparing Vocation Python environment", source, StringComparison.Ordinal);
        Assert.Contains("Preparing Vocation interface", source, StringComparison.Ordinal);
        Assert.Contains("Waiting for Vocation health", source, StringComparison.Ordinal);
        Assert.Contains("uv", source, StringComparison.Ordinal);
        Assert.Contains("sync", source, StringComparison.Ordinal);
        Assert.Contains("--locked", source, StringComparison.Ordinal);
        Assert.Contains("pnpm", source, StringComparison.Ordinal);
        Assert.Contains("--frozen-lockfile", source, StringComparison.Ordinal);
        Assert.Contains("frontend", source, StringComparison.Ordinal);
        Assert.Contains("dist", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Orientation_uses_actuator_health_and_waits_for_map_and_backend_concurrently()
    {
        var source = RuntimeSource();

        Assert.Contains("http://127.0.0.1:8080/actuator/health", source, StringComparison.Ordinal);
        Assert.Contains("Waiting for Orientation map + backend", source, StringComparison.Ordinal);
        Assert.Contains("Task.WhenAll(mapTask, backendTask)", source, StringComparison.Ordinal);
        Assert.Contains("TimeSpan.FromSeconds(35)", source, StringComparison.Ordinal);
        Assert.Contains("TimeSpan.FromSeconds(45)", source, StringComparison.Ordinal);
        Assert.Contains("npm", source, StringComparison.Ordinal);
        Assert.Contains("ci", source, StringComparison.Ordinal);
        Assert.DoesNotContain("WaitForHttpServerAsync", source, StringComparison.Ordinal);
        Assert.DoesNotContain("BackendProbeUri = new(\"http://127.0.0.1:8080/\")", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Node_package_shims_are_started_through_cmd_on_windows()
    {
        var source = RuntimeSource();

        Assert.Contains("OperatingSystem.IsWindows()", source, StringComparison.Ordinal);
        Assert.Contains("string.Equals(command, \"npm\"", source, StringComparison.Ordinal);
        Assert.Contains("string.Equals(command, \"pnpm\"", source, StringComparison.Ordinal);
        Assert.Contains("StartProcess(\"cmd.exe\"", source, StringComparison.Ordinal);
    }

    private static string RuntimeSource()
    {
        var root = FindRepositoryRoot();
        return File.ReadAllText(Path.Combine(root, "src", "WiiiiGotThis.Desktop", "DesktopProviderRuntimes.cs"));
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "WiiiiGotThis.sln")))
                return directory.FullName;
            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the Wiiii Got This repository root.");
    }
}
