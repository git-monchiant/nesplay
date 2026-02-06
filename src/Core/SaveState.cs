using System.Text.Json;

public class SaveState {
    // CPU State
    public byte CPU_A { get; set; }
    public byte CPU_X { get; set; }
    public byte CPU_Y { get; set; }
    public ushort CPU_PC { get; set; }
    public ushort CPU_SP { get; set; }
    public byte CPU_Status { get; set; }

    // PPU State
    public byte[] PPU_VRAM { get; set; } = new byte[2048];
    public byte[] PPU_PaletteRAM { get; set; } = new byte[32];
    public byte[] PPU_OAM { get; set; } = new byte[256];
    public byte PPU_CTRL { get; set; }
    public byte PPU_MASK { get; set; }
    public byte PPU_STATUS { get; set; }
    public byte PPU_OAMADDR { get; set; }
    public byte PPU_ScrollX { get; set; }
    public byte PPU_ScrollY { get; set; }
    public ushort PPU_Addr { get; set; }
    public byte PPU_FineX { get; set; }
    public bool PPU_ScrollLatch { get; set; }
    public bool PPU_AddrLatch { get; set; }
    public ushort PPU_V { get; set; }
    public ushort PPU_T { get; set; }
    public int PPU_ScanlineCycle { get; set; }
    public int PPU_Scanline { get; set; }
    public byte PPU_DataBuffer { get; set; }

    // Bus RAM
    public byte[] RAM { get; set; } = new byte[2048];

    // Cartridge PRG RAM (for games that use it)
    public byte[]? PRG_RAM { get; set; }

    private static string ExeDir => Path.GetDirectoryName(Environment.ProcessPath) ?? AppContext.BaseDirectory;
    private static string SavePath => Path.Combine(ExeDir, "savestate.json");

    public static void Save(Bus bus) {
        var state = new SaveState();

        // Save CPU
        state.CPU_A = bus.cpu.A;
        state.CPU_X = bus.cpu.X;
        state.CPU_Y = bus.cpu.Y;
        state.CPU_PC = bus.cpu.PC;
        state.CPU_SP = bus.cpu.SP;
        state.CPU_Status = bus.cpu.status;

        // Save RAM
        Array.Copy(bus.ram, state.RAM, 2048);

        // Save PPU (using reflection to access private fields)
        var ppuType = bus.ppu.GetType();
        state.PPU_VRAM = (byte[])GetPrivateField(bus.ppu, "vram")!;
        state.PPU_PaletteRAM = (byte[])GetPrivateField(bus.ppu, "paletteRAM")!;
        state.PPU_OAM = (byte[])GetPrivateField(bus.ppu, "oam")!;
        state.PPU_CTRL = (byte)GetPrivateField(bus.ppu, "PPUCTRL")!;
        state.PPU_MASK = (byte)GetPrivateField(bus.ppu, "PPUMASK")!;
        state.PPU_STATUS = (byte)GetPrivateField(bus.ppu, "PPUSTATUS")!;
        state.PPU_OAMADDR = (byte)GetPrivateField(bus.ppu, "OAMADDR")!;
        state.PPU_ScrollX = (byte)GetPrivateField(bus.ppu, "PPUSCROLLX")!;
        state.PPU_ScrollY = (byte)GetPrivateField(bus.ppu, "PPUSCROLLY")!;
        state.PPU_Addr = (ushort)GetPrivateField(bus.ppu, "PPUADDR")!;
        state.PPU_FineX = (byte)GetPrivateField(bus.ppu, "fineX")!;
        state.PPU_ScrollLatch = (bool)GetPrivateField(bus.ppu, "scrollLatch")!;
        state.PPU_AddrLatch = (bool)GetPrivateField(bus.ppu, "addrLatch")!;
        state.PPU_V = (ushort)GetPrivateField(bus.ppu, "v")!;
        state.PPU_T = (ushort)GetPrivateField(bus.ppu, "t")!;
        state.PPU_ScanlineCycle = (int)GetPrivateField(bus.ppu, "scanlineCycle")!;
        state.PPU_Scanline = (int)GetPrivateField(bus.ppu, "scanline")!;
        state.PPU_DataBuffer = (byte)GetPrivateField(bus.ppu, "ppuDataBuffer")!;

        // Save to file
        var options = new JsonSerializerOptions { WriteIndented = true };
        string json = JsonSerializer.Serialize(state, options);
        File.WriteAllText(SavePath, json);
        Console.WriteLine("State saved!");
        Notification.Show("SAVED");
    }

    public static void Load(Bus bus) {
        if (!File.Exists(SavePath)) {
            Console.WriteLine("No save state found!");
            Notification.Show("NO SAVE FOUND");
            return;
        }

        try {
            string json = File.ReadAllText(SavePath);
            var state = JsonSerializer.Deserialize<SaveState>(json);
            if (state == null) {
                Console.WriteLine("Failed to deserialize save state!");
                return;
            }

            // Restore CPU
            bus.cpu.A = state.CPU_A;
            bus.cpu.X = state.CPU_X;
            bus.cpu.Y = state.CPU_Y;
            bus.cpu.PC = state.CPU_PC;
            bus.cpu.SP = state.CPU_SP;
            bus.cpu.status = state.CPU_Status;

            // Restore RAM
            Array.Copy(state.RAM, bus.ram, 2048);

            // Restore PPU arrays (copy content, not replace reference)
            var vram = (byte[]?)GetPrivateField(bus.ppu, "vram");
            var paletteRAM = (byte[]?)GetPrivateField(bus.ppu, "paletteRAM");
            var oam = (byte[]?)GetPrivateField(bus.ppu, "oam");

            if (vram != null && state.PPU_VRAM != null)
                Array.Copy(state.PPU_VRAM, vram, Math.Min(state.PPU_VRAM.Length, vram.Length));
            if (paletteRAM != null && state.PPU_PaletteRAM != null)
                Array.Copy(state.PPU_PaletteRAM, paletteRAM, Math.Min(state.PPU_PaletteRAM.Length, paletteRAM.Length));
            if (oam != null && state.PPU_OAM != null)
                Array.Copy(state.PPU_OAM, oam, Math.Min(state.PPU_OAM.Length, oam.Length));

            // Restore PPU registers
            SetPrivateField(bus.ppu, "PPUCTRL", state.PPU_CTRL);
            SetPrivateField(bus.ppu, "PPUMASK", state.PPU_MASK);
            SetPrivateField(bus.ppu, "PPUSTATUS", state.PPU_STATUS);
            SetPrivateField(bus.ppu, "OAMADDR", state.PPU_OAMADDR);
            SetPrivateField(bus.ppu, "PPUSCROLLX", state.PPU_ScrollX);
            SetPrivateField(bus.ppu, "PPUSCROLLY", state.PPU_ScrollY);
            SetPrivateField(bus.ppu, "PPUADDR", state.PPU_Addr);
            SetPrivateField(bus.ppu, "fineX", state.PPU_FineX);
            SetPrivateField(bus.ppu, "scrollLatch", state.PPU_ScrollLatch);
            SetPrivateField(bus.ppu, "addrLatch", state.PPU_AddrLatch);
            SetPrivateField(bus.ppu, "v", state.PPU_V);
            SetPrivateField(bus.ppu, "t", state.PPU_T);
            SetPrivateField(bus.ppu, "scanlineCycle", state.PPU_ScanlineCycle);
            SetPrivateField(bus.ppu, "scanline", state.PPU_Scanline);
            SetPrivateField(bus.ppu, "ppuDataBuffer", state.PPU_DataBuffer);

            Console.WriteLine("State loaded!");
            Notification.Show("LOADED");
        } catch (Exception ex) {
            Console.WriteLine($"Error loading state: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            Notification.Show("LOAD FAILED");
        }
    }

    private static object? GetPrivateField(object obj, string fieldName) {
        var field = obj.GetType().GetField(fieldName,
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance);
        return field?.GetValue(obj);
    }

    private static void SetPrivateField(object obj, string fieldName, object? value) {
        var field = obj.GetType().GetField(fieldName,
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance);
        field?.SetValue(obj, value);
    }
}
