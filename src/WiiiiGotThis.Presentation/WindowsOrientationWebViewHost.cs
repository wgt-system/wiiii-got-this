using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Avalonia.Platform;

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

            webView3.SetVirtualHostNameToFolderMapping(VirtualHostName, bundleDirectory, HostResourceAccessKindDeny);
            error = null;
            return true;
        }
        catch (COMException)
        {
            error = "Orientation map host mapping could not be configured.";
            return false;
        }
    }

    // COREWEBVIEW2_HOST_RESOURCE_ACCESS_KIND_DENY. The mapped host may load its own
    // files, while unrelated origins are not granted access to the local bundle.
    private const int HostResourceAccessKindDeny = 0;

    // This narrow COM declaration mirrors the WebView2 ABI used by
    // Avalonia.Controls.WebView 12.0.1. Only ICoreWebView2_3's virtual-host mapping
    // method is invoked; inherited members are declared solely to preserve vtable layout.
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    private struct EventRegistrationToken
    {
        public long Value;
    }

    [ComImport]
    [Guid("76ECEACB-0462-4D94-AC83-423A6793775E")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface ICoreWebView2
    {
        IntPtr GetSettings();
        [return: MarshalAs(UnmanagedType.LPWStr)] string GetSource();
        void Navigate([MarshalAs(UnmanagedType.LPWStr)] string uri);
        void NavigateToString([MarshalAs(UnmanagedType.LPWStr)] string htmlContent);
        void add_NavigationStarting(IntPtr eventHandler, out EventRegistrationToken token);
        void remove_NavigationStarting(EventRegistrationToken token);
        void add_ContentLoading(IntPtr eventHandler, out EventRegistrationToken token);
        void remove_ContentLoading(EventRegistrationToken token);
        void add_SourceChanged(IntPtr eventHandler, out EventRegistrationToken token);
        void remove_SourceChanged(EventRegistrationToken token);
        void add_HistoryChanged(IntPtr eventHandler, out EventRegistrationToken token);
        void remove_HistoryChanged(EventRegistrationToken token);
        void add_NavigationCompleted(IntPtr eventHandler, out EventRegistrationToken token);
        void remove_NavigationCompleted(EventRegistrationToken token);
        void add_FrameNavigationStarting(IntPtr eventHandler, out EventRegistrationToken token);
        void remove_FrameNavigationStarting(EventRegistrationToken token);
        void add_FrameNavigationCompleted(IntPtr eventHandler, out EventRegistrationToken token);
        void remove_FrameNavigationCompleted(EventRegistrationToken token);
        void add_ScriptDialogOpening(IntPtr eventHandler, out EventRegistrationToken token);
        void remove_ScriptDialogOpening(EventRegistrationToken token);
        void add_PermissionRequested(IntPtr eventHandler, out EventRegistrationToken token);
        void remove_PermissionRequested(EventRegistrationToken token);
        void add_ProcessFailed(IntPtr eventHandler, out EventRegistrationToken token);
        void remove_ProcessFailed(EventRegistrationToken token);
        void AddScriptToExecuteOnDocumentCreated([MarshalAs(UnmanagedType.LPWStr)] string javaScript, IntPtr handler);
        void RemoveScriptToExecuteOnDocumentCreated([MarshalAs(UnmanagedType.LPWStr)] string id);
        void ExecuteScript([MarshalAs(UnmanagedType.LPWStr)] string javaScript, IntPtr handler);
        void CapturePreview(int imageFormat, IntPtr imageStream, IntPtr handler);
        void Reload();
        void PostWebMessageAsJson([MarshalAs(UnmanagedType.LPWStr)] string webMessageAsJson);
        void PostWebMessageAsString([MarshalAs(UnmanagedType.LPWStr)] string webMessageAsString);
        void add_WebMessageReceived(IntPtr handler, out EventRegistrationToken token);
        void remove_WebMessageReceived(EventRegistrationToken token);
        void CallDevToolsProtocolMethod([MarshalAs(UnmanagedType.LPWStr)] string methodName, [MarshalAs(UnmanagedType.LPWStr)] string parametersAsJson, IntPtr handler);
        uint GetBrowserProcessId();
        int GetCanGoBack();
        int GetCanGoForward();
        void GoBack();
        void GoForward();
        IntPtr GetDevToolsProtocolEventReceiver([MarshalAs(UnmanagedType.LPWStr)] string eventName);
        void Stop();
        void add_NewWindowRequested(IntPtr eventHandler, out EventRegistrationToken token);
        void remove_NewWindowRequested(EventRegistrationToken token);
        void add_DocumentTitleChanged(IntPtr eventHandler, out EventRegistrationToken token);
        void remove_DocumentTitleChanged(EventRegistrationToken token);
        [return: MarshalAs(UnmanagedType.LPWStr)] string GetDocumentTitle();
        void AddHostObjectToScript([MarshalAs(UnmanagedType.LPWStr)] string name, IntPtr @object);
        void RemoveHostObjectFromScript([MarshalAs(UnmanagedType.LPWStr)] string name);
        void OpenDevToolsWindow();
        void add_ContainsFullScreenElementChanged(IntPtr eventHandler, out EventRegistrationToken token);
        void remove_ContainsFullScreenElementChanged(EventRegistrationToken token);
        int GetContainsFullScreenElement();
        void add_WebResourceRequested(IntPtr eventHandler, out EventRegistrationToken token);
        void remove_WebResourceRequested(EventRegistrationToken token);
        [PreserveSig] int AddWebResourceRequestedFilter([MarshalAs(UnmanagedType.LPWStr)] string uri, int resourceContext);
        [PreserveSig] int RemoveWebResourceRequestedFilter([MarshalAs(UnmanagedType.LPWStr)] string uri, int resourceContext);
        void add_WindowCloseRequested(IntPtr eventHandler, out EventRegistrationToken token);
        void remove_WindowCloseRequested(EventRegistrationToken token);
    }

    [ComImport]
    [Guid("9E8F0CF8-E670-4B5E-B2BC-73E061E3184C")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface ICoreWebView2_2 : ICoreWebView2
    {
        void add_WebResourceResponseReceived(IntPtr eventHandler, out EventRegistrationToken token);
        void remove_WebResourceResponseReceived(EventRegistrationToken token);
        void NavigateWithWebResourceRequest(IntPtr request);
        void add_DOMContentLoaded(IntPtr eventHandler, out EventRegistrationToken token);
        void remove_DOMContentLoaded(EventRegistrationToken token);
        IntPtr GetCookieManager();
        IntPtr Environment();
    }

    [ComImport]
    [Guid("A0D6DF20-3B92-416D-AA0C-437A9C727857")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface ICoreWebView2_3 : ICoreWebView2_2
    {
        void TrySuspend(IntPtr handler);
        void Resume();
        int get_IsSuspended();
        void SetVirtualHostNameToFolderMapping(
            [MarshalAs(UnmanagedType.LPWStr)] string hostName,
            [MarshalAs(UnmanagedType.LPWStr)] string folderPath,
            int accessKind);
        void ClearVirtualHostNameToFolderMapping([MarshalAs(UnmanagedType.LPWStr)] string hostName);
    }
}
