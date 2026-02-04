using Raylib_cs;

public class NES {
    Cartridge cartridge;
    public Bus bus;  // Made public for save state access
    int frameCount = 0;
    bool livesSet = false;

    // Save/Load button state (prevent repeat triggers)
    private bool savePressed = false;
    private bool loadPressed = false;

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

    public void SetRenderOffset(int x, int y) {
        bus.ppu.textureX = x;
        bus.ppu.textureY = y;
    }

    public void Run() {
        int cycles = 0;

        bus.input.UpdateController();

        // Save/Load state handling
        HandleSaveLoad();

        // Apply cheat based on RomConfig
        var romCfg = RomConfig.Current;
        frameCount++;
        if (!livesSet && romCfg.CheatEnabled && frameCount > romCfg.CheatDelayFrames) {
            if (romCfg.CheatAddress1 > 0) {
                bus.ram[romCfg.CheatAddress1] = romCfg.CheatValue;
            }
            if (romCfg.CheatAddress2 > 0) {
                bus.ram[romCfg.CheatAddress2] = romCfg.CheatValue;
            }
            livesSet = true;
            Console.WriteLine($"Cheat applied for {romCfg.RomName}");
        }

        while (cycles < 29828) {
            int used = bus.cpu.ExecuteInstruction();
            cycles += used;
            bus.ppu.Step(used * 3);
        }

        bus.ppu.DrawFrame(Helper.scale);
    }

    private void HandleSaveLoad() {
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

        // Save state (with debounce)
        if ((saveKey || saveButton) && !savePressed) {
            savePressed = true;
            SaveState.Save(bus);
        } else if (!saveKey && !saveButton) {
            savePressed = false;
        }

        // Load state (with debounce)
        if ((loadKey || loadButton) && !loadPressed) {
            loadPressed = true;
            SaveState.Load(bus);
        } else if (!loadKey && !loadButton) {
            loadPressed = false;
        }
    }
}