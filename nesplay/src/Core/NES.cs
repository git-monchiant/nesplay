using Raylib_cs;

public class NES {
    Cartridge cartridge;
    public Bus bus;  // Made public for save state access
    int frameCount = 0;
    bool livesSet = false;

    // Save/Load button state (prevent repeat triggers)
    private bool savePressed = false;
    private bool loadPressed = false;
    private int saveCooldown = 0;
    private int loadCooldown = 0;
    private const int COOLDOWN_FRAMES = 30; // 0.5 second cooldown

    public NES() {
        if (Helper.embeddedRom != null) {
            cartridge = new Cartridge(Helper.embeddedRom);
        } else {
            cartridge = new Cartridge(Helper.romPath);
        }
        bus = new Bus(cartridge);

        bus.cpu.Reset();

        Console.WriteLine("NES");
    }

    private float renderScale = 3;

    public void SetRenderOffset(int x, int y) {
        bus.ppu.textureX = x;
        bus.ppu.textureY = y;
    }

    public void RunScaled(float scale) {
        renderScale = scale;
        Run();
    }

    public void DrawLastFrame(float scale) {
        bus.ppu.DrawFrame(scale);
    }

    public void Run() {
        try {
            int cycles = 0;

            bus.input.UpdateController();

            // Save/Load state handling
            HandleSaveLoad();

            // Apply cheat based on RomConfig
            // Wait until game initializes lives (value > 0) then set cheat value
            var romCfg = Config.Instance.Rom;
            frameCount++;
            if (!livesSet && romCfg.CheatEnabled && frameCount > romCfg.CheatDelayFrames) {
                // Check if lives have been initialized (> 0) and not already at cheat value
                bool addr1Ready = romCfg.CheatAddress1 == 0 ||
                                 (bus.ram[romCfg.CheatAddress1] > 0 && bus.ram[romCfg.CheatAddress1] < romCfg.CheatValue);
                bool addr2Ready = romCfg.CheatAddress2 == 0 ||
                                 (bus.ram[romCfg.CheatAddress2] > 0 && bus.ram[romCfg.CheatAddress2] < romCfg.CheatValue);

                if (addr1Ready || addr2Ready) {
                    if (romCfg.CheatAddress1 > 0) {
                        bus.ram[romCfg.CheatAddress1] = romCfg.CheatValue;
                    }
                    if (romCfg.CheatAddress2 > 0) {
                        bus.ram[romCfg.CheatAddress2] = romCfg.CheatValue;
                    }
                    livesSet = true;
                    Console.WriteLine($"Cheat applied for {romCfg.RomName}");
                }
            }

            while (cycles < 29828) {
                int used = bus.cpu.ExecuteInstruction();
                cycles += used;
                bus.ppu.Step(used * 3);
                bus.apu.Step(used);
            }

            bus.apu.OutputSamples();
            bus.ppu.DrawFrame(renderScale);
        } catch (Exception ex) {
            LogError("Run", ex);
            throw; // Re-throw to crash with log
        }
    }

    private void HandleSaveLoad() {
        // Decrease cooldowns
        if (saveCooldown > 0) saveCooldown--;
        if (loadCooldown > 0) loadCooldown--;

        // Keyboard: 1 = Save, 2 = Load
        bool saveKey = Raylib.IsKeyDown(KeyboardKey.One);
        bool loadKey = Raylib.IsKeyDown(KeyboardKey.Two);

        // Gamepad: L1/L2 = Save, R1/R2 = Load
        bool saveButton = false;
        bool loadButton = false;

        if (Raylib.IsGamepadAvailable(0)) {
            // L1 (LeftTrigger1) or L2 (LeftTrigger2) = Save
            saveButton = Raylib.IsGamepadButtonDown(0, GamepadButton.LeftTrigger1) ||
                        Raylib.IsGamepadButtonDown(0, GamepadButton.LeftTrigger2);
            // R1 (RightTrigger1) or R2 (RightTrigger2) = Load
            loadButton = Raylib.IsGamepadButtonDown(0, GamepadButton.RightTrigger1) ||
                        Raylib.IsGamepadButtonDown(0, GamepadButton.RightTrigger2);
        }

        // Save state (with debounce + cooldown)
        if ((saveKey || saveButton) && !savePressed && saveCooldown == 0) {
            savePressed = true;
            saveCooldown = COOLDOWN_FRAMES;
            try {
                SaveState.Save(bus);
            } catch (Exception ex) {
                LogError("Save", ex);
            }
        } else if (!saveKey && !saveButton) {
            savePressed = false;
        }

        // Load state (with debounce + cooldown)
        if ((loadKey || loadButton) && !loadPressed && loadCooldown == 0) {
            loadPressed = true;
            loadCooldown = COOLDOWN_FRAMES;
            try {
                SaveState.Load(bus);
            } catch (Exception ex) {
                LogError("Load", ex);
            }
        } else if (!loadKey && !loadButton) {
            loadPressed = false;
        }
    }

    private void LogError(string action, Exception ex) {
        string logPath = Path.Combine(Path.GetDirectoryName(Environment.ProcessPath) ?? ".", "error.log");
        string log = $"[{DateTime.Now}] {action} Error: {ex.Message}\n{ex.StackTrace}\n\n";
        File.AppendAllText(logPath, log);
        Console.WriteLine($"{action} Error: {ex.Message}");
        Notification.Show($"{action} ERROR");
    }
}