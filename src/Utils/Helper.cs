using System.Reflection;

public class Helper {
    // Get actual exe location (works with single-file publish)
    public static string ExeDirectory => Path.GetDirectoryName(Environment.ProcessPath) ?? AppContext.BaseDirectory;

    public static int scale;
    public static string romPath = "";
    public static bool debug = false;
    public static bool debugs0h = false;
    public static bool fpsEnable;
    public static bool raylibLog = false;
    public static int mode = 1;
    public static string jsonPath = "";
    public static int flagArraySize = -1;
    public static bool insertingRom = false;
    public static bool showMenuBar = false;  // Hide menu for embedded build
    public static Version version = new Version(1, 0, 0);
    public static byte[]? embeddedRom = null;

    static Helper() {
        // Load settings from config
        scale = Config.Instance.Scale;
        fpsEnable = Config.Instance.ShowFps;

        // Extract embedded resources (images) on first run
        ExtractEmbeddedResources();

        // Try to load embedded ROM from resources
        try {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "Nesplay.roms.game.nes";
            using (var stream = assembly.GetManifestResourceStream(resourceName)) {
                if (stream != null) {
                    embeddedRom = new byte[stream.Length];
                    stream.Read(embeddedRom, 0, embeddedRom.Length);
                    insertingRom = true;
                    Console.WriteLine("Loaded embedded ROM");
                }
            }
        } catch (Exception ex) {
            Console.WriteLine($"No embedded ROM: {ex.Message}");
        }
    }

    private static void ExtractEmbeddedResources() {
        var assembly = Assembly.GetExecutingAssembly();
        var resDir = Path.Combine(ExeDirectory, "res");

        // Create res folder if it doesn't exist
        if (!Directory.Exists(resDir)) {
            Directory.CreateDirectory(resDir);
        }

        // List of resources to extract
        var resources = new Dictionary<string, string> {
            { "Nesplay.res.Background.png", "Background.png" },
            { "Nesplay.res.Logo.png", "Logo.png" }
        };

        foreach (var res in resources) {
            var filePath = Path.Combine(resDir, res.Value);
            // Always overwrite to ensure latest icons
            try {
                using (var stream = assembly.GetManifestResourceStream(res.Key)) {
                    if (stream != null) {
                        using (var fileStream = File.Create(filePath)) {
                            stream.CopyTo(fileStream);
                        }
                    }
                }
            } catch (Exception ex) {
                Console.WriteLine($"Failed to extract {res.Value}: {ex.Message}");
            }
        }
    }

    public static void Flags(string[] args) {
        flagArraySize = args.Length;
        if (args.Length >= 1) {
            for (int i = 0; i < args.Length; i++) {
                if (args[i] == "--nes") {
                    if (i + 1 < args.Length) {
                        romPath = args[i + 1];
                        if (!File.Exists(romPath)) {
                            Console.WriteLine("ROM path \"" + romPath + "\" is invalid");
                            Environment.Exit(1);
                        }
                        insertingRom = true;
                        mode = 1;
                        i += 1;
                    } else {
                        Console.WriteLine("No ROM passed in");
                        Console.WriteLine("Usage: --nes <string:rom>");
                        //Environment.Exit(1);
                    }
                }
                if (args[i] == "--json") {
                    if (i + 1 < args.Length) {
                        jsonPath = args[i + 1];
                        i += 1;
                    }

                    mode = 2;
                }
                if (args[i] == "-s" || args[i] == "--scale") {
                    if (i + 1 < args.Length && int.TryParse(args[i + 1], out int parsedScale)) {
                        scale = parsedScale;
                        i += 1;
                    } else {
                        Console.WriteLine("No scale integer passed in");
                        Console.WriteLine("Usage: -s <int:scale>, --scale <int:scale>");
                        Environment.Exit(1);
                    }
                }
                if (args[i] == "-f" || args[i] == "--fps") {
                    fpsEnable = true;
                }
                if (args[i] == "-rl" || args[i] == "-raylib-log") {
                    raylibLog = true;
                }
                if (args[i] == "-d" || args[i] == "--debug") {
                    debug = true;
                    Console.WriteLine("Press [SPACE] to toggle Sprite0 Hit Check");
                }
                if (args[i] == "-v" || args[i] == "--version") {
                    Console.WriteLine(version);
                    Console.WriteLine("Made by Bot Randomness :)");
                    ASCII_NES();
                    Environment.Exit(1);
                }
                if (args[i] == "-a" || args[i] == "--about") {
                    Console.WriteLine("Made by Bot Randomness :)");
                    Console.WriteLine(version);
                    ASCII_NES();
                    Environment.Exit(1);
                }
                if (args[i] == "-h" || args[i] == "--help") {
                    Console.WriteLine("NES Help:");
                    Console.WriteLine("--nes <string:rom>: Start up the emulator with given ROM passed in. Consider as mode 1");
                    Console.WriteLine("--json <string:json>: Runs the JSON Test. The tests must be in \"test\\v1\". Consider as mode 2");
                    Console.WriteLine("-s <int>, --scale <int>: Scale window size by factor (2 is default)");
                    Console.WriteLine("-f, --fps: Enables FPS counter (off is default)");
                    Console.WriteLine("-rl, --raylib-log: Enables Raylib logs (off is default)");
                    Console.WriteLine("-d, --debug: Enables debug mode (off is default)");
                    Console.WriteLine("-v, --version: Shows version number");
                    Console.WriteLine("-a, --about: Show about screen");
                    Console.WriteLine("-h, --help: Show help screen (What you are seeing now)");
                    Console.WriteLine("Controls: (A)=X, (B)=Z, D-Pad=ArrowKeys, [START]=ENTER, [SELECT]=SHIFT");
                    Console.WriteLine("In debug mode: Press [SPACE] to toggle Sprite0 Hit Check. Try this out if a game freezes");
                    Console.WriteLine("ROMs must have a iNES header!");
                    Console.WriteLine("In GUI, look at Help -> Manual");
                    Environment.Exit(1);
                }
            }

            if (mode == 0) {
                Console.WriteLine("Error: No mode passed in");
                Console.WriteLine("Mode: --nes <string:rom> or --json <string:json>");
                Console.WriteLine("Use -h or --help to bring up help options.");
                Environment.Exit(1);
            }
        } else {
            Console.WriteLine("To get started, use -h or --help to bring up help options.");
            Console.WriteLine("Or use the GUI. \"Help -> Manual\"");
            //Environment.Exit(1);
        }
        Console.WriteLine();
    }

    public static void ASCII_NES() {
        Console.WriteLine(" ____________________________ ");
        Console.WriteLine("│ │  NES               │---│ │");
        Console.WriteLine("│ │____________________│___│ │");
        Console.WriteLine("│____________________________│");
        Console.WriteLine("|                     1  2   |");
        Console.WriteLine(" \\ ■ [ ] [ ]          ▒  ▒  / ");
        Console.WriteLine("  ∙------------------------∙  ");
    }
}