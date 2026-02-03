# Mesen-Embedded

Mesen Emulator modified for embedded ROM games. Supports **macOS** and **Windows** only.

Based on [Mesen](https://github.com/SourMesen/Mesen2) multi-system emulator.

## Supported Systems
- NES/Famicom
- SNES/Super Famicom
- Game Boy / Game Boy Color
- Game Boy Advance
- PC Engine / TurboGrafx-16
- Master System / Game Gear
- WonderSwan

## Supported Controllers
- Keyboard
- XInput (Xbox controllers)
- PlayStation controllers (via GameController framework on macOS)

## Building

### macOS
```bash
# Prerequisites
brew install sdl2
# Install .NET 8 SDK from https://dotnet.microsoft.com/download

# Build
make
```

### Windows
Open `Mesen.sln` in Visual Studio 2022 and build as Release/x64.

## Embedding ROMs

### Method 1: Using EmbeddedRomHelper (Recommended)

In your app startup code:

```csharp
using Mesen.Utilities;

// Register ROM from byte array
byte[] myRomData = /* your ROM data */;
EmbeddedRomHelper.RegisterRom("My Game", myRomData);

// Or register from embedded resource
EmbeddedRomHelper.RegisterRomFromResource("My Game", "MyNamespace.MyGame.nes");

// Or register from file in EmbeddedRoms folder
EmbeddedRomHelper.RegisterRomFromFile("My Game", "mygame.nes");

// Later, load the ROM
EmbeddedRomHelper.LoadEmbeddedRom("My Game");

// Or for single-game builds, just load the first registered ROM
EmbeddedRomHelper.LoadDefaultRom();
```

### Method 2: Direct API Call

```csharp
using Mesen.Interop;

byte[] romData = File.ReadAllBytes("game.nes");
EmuApi.LoadRomFromMemory(romData, (uint)romData.Length, "game.nes");
```

### Method 3: Using Embedded Resources

1. Add ROM file to your project
2. Set Build Action to "Embedded Resource"
3. Load at startup:

```csharp
var assembly = Assembly.GetExecutingAssembly();
using var stream = assembly.GetManifestResourceStream("YourNamespace.game.nes");
using var ms = new MemoryStream();
stream.CopyTo(ms);
byte[] romData = ms.ToArray();

EmuApi.LoadRomFromMemory(romData, (uint)romData.Length, "game.nes");
```

## Project Structure

```
Mesen-Embedded/
├── Core/           # C++ emulation core (NES, SNES, GB, GBA, SMS, PCE, WS)
├── Utilities/      # C++ utilities (VirtualFile, compression)
├── InteropDLL/     # C++ interop bridge
├── Sdl/            # SDL2 rendering/audio (macOS)
├── MacOS/          # macOS platform code (GameController, keyboard)
├── Windows/        # Windows platform code (DirectX, XInput)
├── SevenZip/       # Archive support
├── Lua/            # Scripting engine
├── UI/             # C# Avalonia UI
├── EmbeddedRoms/   # Place ROM files here for embedding
└── makefile        # macOS build
```

## Key Files for Customization

- `UI/Utilities/EmbeddedRomHelper.cs` - Helper for embedded ROMs
- `UI/Interop/EmuApi.cs` - API bindings (includes `LoadRomFromMemory`)
- `InteropDLL/EmuApiWrapper.cpp` - C++ API implementation
- `Utilities/VirtualFile.h` - Supports loading ROMs from memory buffer

## License

GPL v3 (inherited from Mesen)
