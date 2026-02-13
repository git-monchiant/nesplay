// Backward compatibility - redirects to Config.Instance.Rom
public class RomConfig {
    public static RomSettings Current => Config.Instance.Rom;
}
