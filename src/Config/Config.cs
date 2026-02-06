using System.Text.Json;
using Raylib_cs;

public class KeyboardConfig {
    public int A { get; set; } = (int)KeyboardKey.X;
    public int B { get; set; } = (int)KeyboardKey.Z;
    public int TurboA { get; set; } = (int)KeyboardKey.C;
    public int TurboB { get; set; } = (int)KeyboardKey.V;
    public int Select { get; set; } = (int)KeyboardKey.RightShift;
    public int Start { get; set; } = (int)KeyboardKey.Enter;
    public int Up { get; set; } = (int)KeyboardKey.Up;
    public int Down { get; set; } = (int)KeyboardKey.Down;
    public int Left { get; set; } = (int)KeyboardKey.Left;
    public int Right { get; set; } = (int)KeyboardKey.Right;
}

public class XInputConfig {
    // Xbox controller layout: A(bottom), B(right), X(left), Y(top)
    public int A { get; set; } = (int)GamepadButton.RightFaceLeft;       // X button = shoot
    public int B { get; set; } = (int)GamepadButton.RightFaceDown;       // A button = jump
    public int TurboA { get; set; } = (int)GamepadButton.RightFaceUp;    // Y button = turbo shoot
    public int TurboB { get; set; } = (int)GamepadButton.RightFaceRight; // B button = turbo jump
    public int Select { get; set; } = (int)GamepadButton.MiddleLeft;     // Back
    public int Start { get; set; } = (int)GamepadButton.MiddleRight;     // Start
}

public class PSInputConfig {
    // PlayStation controller layout: Cross(bottom), Circle(right), Square(left), Triangle(top)
    public int A { get; set; } = (int)GamepadButton.RightFaceLeft;       // Square = shoot
    public int B { get; set; } = (int)GamepadButton.RightFaceDown;       // Cross = jump
    public int TurboA { get; set; } = (int)GamepadButton.RightFaceUp;    // Triangle = turbo shoot
    public int TurboB { get; set; } = (int)GamepadButton.RightFaceRight; // Circle = turbo jump
    public int Select { get; set; } = (int)GamepadButton.MiddleLeft;     // Share
    public int Start { get; set; } = (int)GamepadButton.MiddleRight;     // Options
}

public class Config {
    private static string ExeDir => Path.GetDirectoryName(Environment.ProcessPath) ?? AppContext.BaseDirectory;
    private static string ConfigPath => Path.Combine(ExeDir, "config.json");
    private static Config? _instance;

    public static Config Instance {
        get {
            if (_instance == null) {
                _instance = Load();
            }
            return _instance;
        }
    }

    // Display settings
    public int Scale { get; set; } = 3;
    public bool ShowFps { get; set; } = false;

    // Input configs
    public KeyboardConfig Keyboard { get; set; } = new KeyboardConfig();
    public XInputConfig XInput { get; set; } = new XInputConfig();
    public PSInputConfig PSInput { get; set; } = new PSInputConfig();

    // Analog stick deadzone
    public float AnalogDeadzone { get; set; } = 0.7f;

    // Turbo speed (frames per toggle)
    public int TurboSpeed { get; set; } = 3;

    public static Config Load() {
        try {
            if (File.Exists(ConfigPath)) {
                string json = File.ReadAllText(ConfigPath);
                var config = JsonSerializer.Deserialize<Config>(json);
                if (config != null) {
                    Console.WriteLine("Config loaded from " + ConfigPath);
                    return config;
                }
            }
        } catch (Exception ex) {
            Console.WriteLine("Error loading config: " + ex.Message);
        }

        // Return default config and save it
        var defaultConfig = new Config();
        defaultConfig.Save();
        return defaultConfig;
    }

    public void Save() {
        try {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(this, options);
            File.WriteAllText(ConfigPath, json);
            Console.WriteLine("Config saved to " + ConfigPath);
        } catch (Exception ex) {
            Console.WriteLine("Error saving config: " + ex.Message);
        }
    }
}
