public class NES {
    Cartridge cartridge;
    Bus bus;
    int frameCount = 0;
    bool livesSet = false;

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

        // Apply 30 lives cheat for Contra - only once at start
        frameCount++;
        if (!livesSet && frameCount > 180) {  // Wait for game to initialize (~3 seconds)
            // Contra (USA) - Player 1 lives at $0032, Player 2 at $0033
            // Lives are 0-indexed (value 29 = 30 lives displayed)
            if (bus.ram[0x0032] > 0 && bus.ram[0x0032] < 29) {
                bus.ram[0x0032] = 29;  // 30 lives (0-indexed)
                bus.ram[0x0033] = 29;
                livesSet = true;
            }
        }

        while (cycles < 29828) {
            int used = bus.cpu.ExecuteInstruction();
            cycles += used;
            bus.ppu.Step(used * 3);
        }

        bus.ppu.DrawFrame(Helper.scale);
    }
}