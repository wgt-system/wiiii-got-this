using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Avalonia.Platform;

namespace WiiiiGotThis.Presentation;

internal static class WindowsOrientationWebViewHost
{
    internal const string VirtualHostName = "orientation-map.invalid";
    internal static readonly Uri EmbedUri = new($"https://{VirtualHostName}/embed.html");

    private const int HostResourceAccessKindDeny = 0;
    private const int SetVirtualHostNameToFolderMappingSlot = 71;
    private static readonly Guid CoreWebView2_3InterfaceId = new("A0D6DF20-3B92-416D-AA0C-437A9C727857");

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

        var interfaceId = CoreWebView2_3InterfaceId;
        var queryResult = Marshal.QueryInterface(webView2Handle.CoreWebView2, ref interfaceId, out var webView3);
        if (queryResult < 0 || webView3 == IntPtr.Zero)
        {
            error = "The installed WebView2 runtime does not support local host mapping.";
            return false;
        }

        try
        {
            var vtable = Marshal.ReadIntPtr(webView3);
            var method = Marshal.ReadIntPtr(vtable, SetVirtualHostNameToFolderMappingSlot * IntPtr.Size);
            var setMapping = Marshal.GetDelegateForFunctionPointer<SetVirtualHostNameToFolderMappingDelegate>(method);
            var result = setMapping(webView3, VirtualHostName, bundleDirectory, HostResourceAccessKindDeny);
            if (result < 0)
            {
                error = $"Orientation map host mapping failed with HRESULT 0x{result:X8}.";
                return false;
            }

            error = null;
            return true;
        }
        catch (Exception exception) when (exception is ArgumentException or MarshalDirectiveException)
        {
            error = "Orientation map host mapping could not be invoked.";
            return false;
        }
        finally
        {
            Marshal.Release(webView3);
        }
    }

    [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode)]
    private delegate int SetVirtualHostNameToFolderMappingDelegate(
        IntPtr @this,
        [MarshalAs(UnmanagedType.LPWStr)] string hostName,
        [MarshalAs(UnmanagedType.LPWStr)] string folderPath,
        int accessKind);
}
