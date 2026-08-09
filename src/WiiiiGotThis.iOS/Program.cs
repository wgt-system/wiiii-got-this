#if WGT_IOS
using UIKit;
#endif

namespace WiiiiGotThis.iOS;

internal static class Program
{
    public static void Main(string[] args)
    {
#if WGT_IOS
        UIApplication.Main(args, null, typeof(AppDelegate));
#endif
    }
}
