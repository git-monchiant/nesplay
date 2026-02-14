using Raylib_cs;
using System.Runtime.InteropServices;

public class Input {
    private byte controllerState = 0;
    private byte controllerShift = 0;
    private int turboCounter = 0;
    private string? detectedGamepadName = null;
    private bool isPlayStationController = false;
    private bool gameStarted = false;
    private bool isPaused = false;
    private bool prevStartState = false;
    private bool prevXButtonState = false;

    private unsafe void DetectGamepadType() {
        if (Raylib.IsGamepadAvailable(0)) {
            sbyte* namePtr = Raylib.GetGamepadName(0);
            string name = Marshal.PtrToStringAnsi((IntPtr)namePtr) ?? "Unknown";
            if (name != detectedGamepadName) {
                detectedGamepadName = name;
                // Detect PlayStation controllers
                string nameLower = name.ToLowerInvariant();
                isPlayStationController = nameLower.Contains("playstation") ||
                                         nameLower.Contains("sony") ||
                                         nameLower.Contains("dualshock") ||
                                         nameLower.Contains("dualsense") ||
                                         nameLower.Contains("ps4") ||
                                         nameLower.Contains("ps5");
                Console.WriteLine($"Gamepad detected: {name} (using {(isPlayStationController ? "PSInput" : "XInput")} config)");
            }
        } else {
            detectedGamepadName = null;
        }
    }

    public void UpdateController() {
        var cfg = Config.Instance;
        var kb = cfg.Keyboard;

        controllerState = 0;
        turboCounter++;
        bool turboActive = (turboCounter / cfg.TurboSpeed) % 2 == 0;

        // Keyboard input (configurable)
        if (Raylib.IsKeyDown((KeyboardKey)kb.A)) controllerState |= 1 << 0;
        if (Raylib.IsKeyDown((KeyboardKey)kb.B)) controllerState |= 1 << 1;
        if (Raylib.IsKeyDown((KeyboardKey)kb.TurboA) && turboActive) controllerState |= 1 << 0;
        if (Raylib.IsKeyDown((KeyboardKey)kb.TurboB) && turboActive) controllerState |= 1 << 1;
        if (Raylib.IsKeyDown((KeyboardKey)kb.Select) || Raylib.IsKeyDown(KeyboardKey.LeftShift)) controllerState |= 1 << 2;
        if (Raylib.IsKeyDown((KeyboardKey)kb.Start)) controllerState |= 1 << 3;
        if (Raylib.IsKeyDown((KeyboardKey)kb.Up)) controllerState |= 1 << 4;
        if (Raylib.IsKeyDown((KeyboardKey)kb.Down)) controllerState |= 1 << 5;
        if (Raylib.IsKeyDown((KeyboardKey)kb.Left)) controllerState |= 1 << 6;
        if (Raylib.IsKeyDown((KeyboardKey)kb.Right)) controllerState |= 1 << 7;


        // Gamepad input (auto-detect Xbox vs PlayStation)
        DetectGamepadType();
        if (Raylib.IsGamepadAvailable(0)) {
            // Use appropriate config based on detected controller type
            var pad = isPlayStationController ? (object)cfg.PSInput : cfg.XInput;
            int padA = isPlayStationController ? cfg.PSInput.A : cfg.XInput.A;
            int padB = isPlayStationController ? cfg.PSInput.B : cfg.XInput.B;
            int padTurboA = isPlayStationController ? cfg.PSInput.TurboA : cfg.XInput.TurboA;
            int padTurboB = isPlayStationController ? cfg.PSInput.TurboB : cfg.XInput.TurboB;
            int padSelect = isPlayStationController ? cfg.PSInput.Select : cfg.XInput.Select;
            int padStart = isPlayStationController ? cfg.PSInput.Start : cfg.XInput.Start;

            if (Raylib.IsGamepadButtonDown(0, (GamepadButton)padA)) controllerState |= 1 << 0;
            if (Raylib.IsGamepadButtonDown(0, (GamepadButton)padB)) controllerState |= 1 << 1;
            if (Raylib.IsGamepadButtonDown(0, (GamepadButton)padTurboA) && turboActive) controllerState |= 1 << 0;
            if (Raylib.IsGamepadButtonDown(0, (GamepadButton)padTurboB) && turboActive) controllerState |= 1 << 1;
            if (Raylib.IsGamepadButtonDown(0, (GamepadButton)padSelect)) controllerState |= 1 << 2;
            if (Raylib.IsGamepadButtonDown(0, (GamepadButton)padStart)) controllerState |= 1 << 3;
            // D-Pad (fixed - not configurable)
            if (Raylib.IsGamepadButtonDown(0, GamepadButton.LeftFaceUp)) controllerState |= 1 << 4;
            if (Raylib.IsGamepadButtonDown(0, GamepadButton.LeftFaceDown)) controllerState |= 1 << 5;
            if (Raylib.IsGamepadButtonDown(0, GamepadButton.LeftFaceLeft)) controllerState |= 1 << 6;
            if (Raylib.IsGamepadButtonDown(0, GamepadButton.LeftFaceRight)) controllerState |= 1 << 7;

            // Left analog stick (as alternative D-Pad)
            float axisX = Raylib.GetGamepadAxisMovement(0, GamepadAxis.LeftX);
            float axisY = Raylib.GetGamepadAxisMovement(0, GamepadAxis.LeftY);
            if (axisY < -cfg.AnalogDeadzone) controllerState |= 1 << 4;
            if (axisY > cfg.AnalogDeadzone) controllerState |= 1 << 5;
            if (axisX < -cfg.AnalogDeadzone) controllerState |= 1 << 6;
            if (axisX > cfg.AnalogDeadzone) controllerState |= 1 << 7;

            // D-pad Up/Down also triggers Select (works always, for menu navigation)
            if (Raylib.IsGamepadButtonDown(0, GamepadButton.LeftFaceUp) ||
                Raylib.IsGamepadButtonDown(0, GamepadButton.LeftFaceDown))
                controllerState |= 1 << 2; // Select

            // Bottom face button (PS Cross / Xbox A) = Start when not playing
            if (cfg.Rom.EnableStartShortcut) {
                bool xDown = Raylib.IsGamepadButtonDown(0, GamepadButton.RightFaceDown);
                bool xJustPressed = xDown && !prevXButtonState;
                prevXButtonState = xDown;

                if ((!gameStarted || isPaused) && xJustPressed)
                    controllerState |= 1 << 3; // Start

                // Track pause state from Start rising edge
                bool startNow = (controllerState & (1 << 3)) != 0;
                if (startNow && !prevStartState) {
                    if (!gameStarted) gameStarted = true;
                    else isPaused = !isPaused;
                }
                prevStartState = startNow;
            }
        }
    }

    public void Write4016(byte value) {
        if ((value & 1) != 0) {
            controllerShift = controllerState;
        }
    }

    public byte Read4016() {
        byte result = (byte)(controllerShift & 1);
        controllerShift >>= 1;
        return result;
    }
}
