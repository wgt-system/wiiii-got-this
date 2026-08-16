using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Avalonia.Platform;
using Microsoft.Web.WebView2.Core.Raw;

namespace WiiiiGotThis.Presentation;

internal static class WindowsOrientationWebViewHost
{
    internal const string VirtualHostName = "orientation-map.invalid";
    internal static readonly Uri EmbedUri = new($"https://{VirtualHostName}/embed.html");

    [SupportedOSPlatform("windows")]
    internal static bool TryConfigure(IPlatformHandle? platformHandle, string embedPath, out string? error)
    {
        if (platformHandle is not IWindowsWebView2PlatformHandle webView2Handle || webView2Handle.CoreWebView2 == IntPtr.Zero)
        {
            error = "Orientation requires the WebView2 host on Windows.";
            return false;
        }

        string bundleDirectory;
        try
        {
            bundleDirectory = Path.GetDirectoryName(Path.GetFullPath(embedPath))
                ?? throw new InvalidOperationException("The Orientation bundle directory could not be resolved.");
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException or InvalidOperationException)
        {
            error = "Orientation map host path is invalid.";
            return false;
        }

        try
        {
            var rawWebView = Marshal.GetObjectForIUnknown(webView2Handle.CoreWebView2);
            if (rawWebView is not ICoreWebView2_3 webView3)
            {
                error = "The installed WebView2 runtime does not support local host mapping.";
                return false;
            }

            webView3.SetVirtualHostNameToFolderMapping(
                VirtualHostName,
                bundleDirectory,
                COREWEBVIEW2_HOST_RESOURCE_ACCESS_KIND.COREWEBVIEW2_HOST_RESOURCE_ACCESS_KIND_DENY);
            error = null;
            return true;
        }
        catch (COMException)
        {
            error = "Orientation map host mapping could not be configured.";
            return false;
        }
    }
}
