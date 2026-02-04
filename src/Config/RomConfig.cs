public class RomConfig {
    // ROM identification
    public string RomName { get; set; } = "Game";
    public string WindowTitle { get; set; } = "NES";

    // Cheat settings
    public bool CheatEnabled { get; set; } = false;
    public int CheatAddress1 { get; set; } = 0;
    public int CheatAddress2 { get; set; } = 0;
    public byte CheatValue { get; set; } = 0;
    public int CheatDelayFrames { get; set; } = 180;  // Wait frames before applying

    // Predefined ROM configs
    public static RomConfig Contra => new RomConfig {
        RomName = "Contra",
        WindowTitle = "Contra",
        CheatEnabled = true,
        CheatAddress1 = 0x0032,  // Player 1 lives
        CheatAddress2 = 0x0033,  // Player 2 lives
        CheatValue = 29,         // 30 lives (0-indexed)
        CheatDelayFrames = 180
    };

    public static RomConfig SuperMarioBros => new RomConfig {
        RomName = "SuperMarioBros",
        WindowTitle = "Super Mario Bros",
        CheatEnabled = true,
        CheatAddress1 = 0x075A,  // Lives
        CheatAddress2 = 0,
        CheatValue = 99,
        CheatDelayFrames = 120
    };

    public static RomConfig Megaman2 => new RomConfig {
        RomName = "Megaman2",
        WindowTitle = "Mega Man 2",
        CheatEnabled = false,
        CheatAddress1 = 0,
        CheatAddress2 = 0,
        CheatValue = 0,
        CheatDelayFrames = 0
    };

    public static RomConfig Default => new RomConfig {
        RomName = "Game",
        WindowTitle = "NES",
        CheatEnabled = false,
        CheatAddress1 = 0,
        CheatAddress2 = 0,
        CheatValue = 0,
        CheatDelayFrames = 0
    };

    // ============================================
    // CHANGE THIS LINE FOR DIFFERENT ROM BUILDS
    // ============================================
    public static RomConfig Current => Contra;
    // Options: Contra, SuperMarioBros, Megaman2, Default
}
