using Raylib_cs;

public static class Notification {
    private static string message = "";
    private static int framesRemaining = 0;
    private const int DISPLAY_FRAMES = 120;  // 2 seconds at 60fps

    public static void Show(string text) {
        message = text;
        framesRemaining = DISPLAY_FRAMES;
    }

    public static void Draw(int x, int y) {
        if (framesRemaining > 0) {
            // Calculate alpha for fade out effect
            byte alpha = (byte)(255 * Math.Min(1.0f, framesRemaining / 30.0f));

            // Draw shadow
            Raylib.DrawText(message, x + 2, y + 2, 24, new Color((byte)0, (byte)0, (byte)0, alpha));
            // Draw text (yellow)
            Raylib.DrawText(message, x, y, 24, new Color((byte)255, (byte)255, (byte)0, alpha));

            framesRemaining--;
        }
    }

    public static bool IsShowing() => framesRemaining > 0;
}
