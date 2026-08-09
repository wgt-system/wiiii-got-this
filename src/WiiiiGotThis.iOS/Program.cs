namespace WiiiiGotThis.iOS;

// The iOS composition root is intentionally present at bootstrap time.
// UIApplication/NSObject startup wiring is added with the first Mac-hosted iOS smoke slice.
internal static class Program
{
    public static void Main() { }
}
