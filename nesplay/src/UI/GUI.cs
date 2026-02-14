#pragma warning disable

using Raylib_cs;
using rlImGui_cs;
using ImGuiNET;

public class GUI {
    private NES nes;

    private FileDialog fileDialog;
    private string selectedFilePath = "";

    private bool showScaleWindow = false;
    private bool showAboutWindow = false;
    private bool showManualWindow = false;

    private const int NES_WIDTH = 256;
    private const int NES_HEIGHT = 240;

    Image icon;
    Texture2D backgroundTexture;
    Texture2D logoTexture;

    public GUI() {
        if (!Helper.raylibLog) Raylib.SetTraceLogLevel(TraceLogLevel.None);

        // Fullscreen
        Raylib.SetConfigFlags(ConfigFlags.FullscreenMode);
        int monW = Raylib.GetMonitorWidth(0);
        int monH = Raylib.GetMonitorHeight(0);
        Raylib.InitWindow(monW, monH, Config.Instance.Rom.WindowTitle);
        Raylib.InitAudioDevice();
        Raylib.SetTargetFPS(60);

        rlImGui.Setup(true);

        ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.DockingEnable;

        //Unsafe access for no imgui.ini files
        var io = ImGui.GetIO();
        unsafe {
            IntPtr ioPtr = (IntPtr)io.NativePtr;
            ImGuiIO* imguiIO = (ImGuiIO*)ioPtr.ToPointer();
            imguiIO->IniFilename = null;
        }

        fileDialog = new FileDialog(Directory.GetCurrentDirectory());

        // Load images directly from embedded resources (no file extraction)
        icon = LoadImageFromResource("Nesplay.res.Logo.png");
        Raylib.SetWindowIcon(icon);
        logoTexture = Raylib.LoadTextureFromImage(icon);

        var bgImage = LoadImageFromResource("Nesplay.res.Background.png");
        backgroundTexture = Raylib.LoadTextureFromImage(bgImage);
        Raylib.UnloadImage(bgImage);
    }

    // Calculate scale and offset to fit NES output in current window
    private (float scale, int offsetX, int offsetY) CalculateLayout() {
        int winW = Raylib.GetScreenWidth();
        int winH = Raylib.GetScreenHeight();
        float scaleX = winW / (float)NES_WIDTH;
        float scaleY = winH / (float)NES_HEIGHT;
        float scale = Math.Min(scaleX, scaleY);
        int offsetX = (int)((winW - NES_WIDTH * scale) / 2);
        int offsetY = (int)((winH - NES_HEIGHT * scale) / 2);
        return (scale, offsetX, offsetY);
    }

    private int loadingFrames = 0;
    private const int LOADING_DURATION = 90; // 1.5 seconds at 60fps

    public void Run() {
        while (!Raylib.WindowShouldClose()) {
            var (scale, offsetX, offsetY) = CalculateLayout();

            Raylib.BeginDrawing();
            rlImGui.Begin();

            Raylib.ClearBackground(Color.Black);

            // Menu bar hidden for embedded ROM build
            // MenuBar();

            if (loadingFrames < LOADING_DURATION && (Helper.embeddedRom != null || Helper.romPath.Length != 0)) {
                // Loading screen - show logo centered
                loadingFrames++;
                int winW = Raylib.GetScreenWidth();
                int winH2 = Raylib.GetScreenHeight();
                float logoScale = Math.Min((float)winW / logoTexture.Width, (float)winH2 / logoTexture.Height) * 0.3f;
                int logoW = (int)(logoTexture.Width * logoScale);
                int logoH = (int)(logoTexture.Height * logoScale);
                Raylib.DrawTextureEx(logoTexture, new System.Numerics.Vector2((winW - logoW) / 2, (winH2 - logoH) / 2), 0, logoScale, Color.White);
            } else if ((Helper.romPath.Length != 0 || Helper.embeddedRom != null) && Helper.insertingRom == false) {
                nes.SetRenderOffset(offsetX, offsetY);
                nes.RunScaled(scale);
            } else if (Helper.insertingRom == true) {
                nes = new NES();
                Helper.insertingRom = false;
            } else {
                Raylib.ClearBackground(Color.DarkGray);
                Raylib.DrawTextureEx(backgroundTexture, new System.Numerics.Vector2(offsetX, offsetY), 0, scale * 0.5f, Color.White);
            }

            // Update title bar with FPS
            Raylib.SetWindowTitle($"{Config.Instance.Rom.WindowTitle} - {Raylib.GetFPS()} fps");

            if (Helper.fpsEnable) Raylib.DrawFPS(0, 0);

            // Draw save/load notification
            int winH = Raylib.GetScreenHeight();
            Notification.Draw(10, winH - 40);

            rlImGui.End();
            Raylib.EndDrawing();
        }

        if (nes != null) nes.bus.apu.Close();
        Raylib.CloseAudioDevice();
        Raylib.CloseWindow();
    }

    private static Image LoadImageFromResource(string resourceName) {
        byte[]? data = Helper.LoadEmbeddedResource(resourceName);
        if (data != null) {
            unsafe {
                byte[] fileType = System.Text.Encoding.ASCII.GetBytes(".png\0");
                fixed (byte* ptr = data)
                fixed (byte* ftPtr = fileType) {
                    return Raylib.LoadImageFromMemory((sbyte*)ftPtr, ptr, data.Length);
                }
            }
        }
        return new Image();
    }

    public void MenuBar() {
        if (Helper.showMenuBar) {
            if (ImGui.BeginMainMenuBar()) {
                ImGui.Text("NET-NES");

                ImGui.Separator();
                    
                if (ImGui.BeginMenu("File")) {
                    if (ImGui.MenuItem("Open ROM")) {
                        fileDialog.Open();
                        Helper.showMenuBar = false;
                    }
                    ImGui.EndMenu();
                }
                if (ImGui.BeginMenu("Config")) {
                    if (ImGui.MenuItem("Window Size")) {
                        showScaleWindow = true;
                    }
                    ImGui.EndMenu();
                }
                if (ImGui.BeginMenu("Debug")) {
                    if (ImGui.MenuItem("FPS Enable", null, Helper.fpsEnable)) {
                        Helper.fpsEnable = !Helper.fpsEnable;
                    }
                    if (ImGui.MenuItem("Sprite0 Hit Disable", null, Helper.debugs0h)) {
                        Helper.debugs0h = !Helper.debugs0h;
                        Console.WriteLine("Sprite0 Hit Check Disable: " + Helper.debugs0h);
                    }
                    ImGui.EndMenu();
                }
                if (ImGui.BeginMenu("Help")) {
                    if (ImGui.MenuItem("Manual")) {
                        showManualWindow = true;
                    }
                    if (ImGui.MenuItem("About")) {
                        showAboutWindow = true;
                    }
                    ImGui.EndMenu();
                }
            }
        } else {
            Helper.showMenuBar = ImGui.GetMousePos().Y <= 20.0f && ImGui.GetMousePos().Y != 0;
        }

        ImGui.SetNextWindowPos(new System.Numerics.Vector2(0, 0), ImGuiCond.Appearing);
        ImGui.SetNextWindowSize(new System.Numerics.Vector2(256 * Helper.scale, 240 * Helper.scale), ImGuiCond.Appearing);
        if (fileDialog.Show(ref selectedFilePath)) {
            Helper.romPath = selectedFilePath;
            Helper.insertingRom = true;
        }

        ScaleWindow();
        ManualWindow();
        AboutWindow();
    }

    public void ScaleWindow() {
        if (showScaleWindow) {
            ImGui.SetNextWindowPos(new System.Numerics.Vector2(0, 20), ImGuiCond.Appearing);
            ImGui.SetNextWindowSize(new System.Numerics.Vector2(125 * 2, 50 * 2), ImGuiCond.Appearing);
            if (ImGui.Begin("Window Size Config", ref showScaleWindow)) {
                ImGui.SliderInt("Scale", ref Helper.scale, 1, 10);

                ImGui.Spacing();

                if (ImGui.Button("Close")) {
                    showScaleWindow = false;
                }
            }
            ImGui.End();
        }
    }

    public void AboutWindow() {
        if (showAboutWindow) {
            ImGui.SetNextWindowPos(new System.Numerics.Vector2(0, 20), ImGuiCond.Appearing);
            ImGui.SetNextWindowSize(new System.Numerics.Vector2(125 * 2, 120 * 2), ImGuiCond.Appearing);
            if (ImGui.Begin("About", ref showAboutWindow)) {
                ImGui.Text("NET-NES");
                ImGui.Text(Helper.version.ToString());
                ImGui.Text("Made by Bot Randomness :)");

                ImGui.Text(" ____________________________ ");
                ImGui.Text("| |  NES               |---| |");
                ImGui.Text("| |____________________|___| |");
                ImGui.Text("|____________________________|");
                ImGui.Text("|                     1  2   |");
                ImGui.Text(" \\ O [ ] [ ]          D  D  / ");
                ImGui.Text("  *------------------------*  ");

                if (ImGui.Button("Close")) {
                    showAboutWindow = false;
                }
            }
            ImGui.End();
        }
    }

    public void ManualWindow() {
        if (showManualWindow) {
            ImGui.SetNextWindowPos(new System.Numerics.Vector2(0, 20), ImGuiCond.Appearing);
            ImGui.SetNextWindowSize(new System.Numerics.Vector2(125 * 2, 120 * 2), ImGuiCond.Appearing);
            if (ImGui.Begin("Manual", ref showManualWindow)) {
                ImGui.Text("Controls: (A)=X, (B)=Z, \nD-Pad=ArrowKeys, [START]=ENTER, \n[SELECT]=SHIFT");
                ImGui.Spacing();
                ImGui.Text("Press [SPACE] to toggle the Menu \nBar.");
                ImGui.Spacing();
                ImGui.Text("In Debug: Toggle Sprite0 Hit Check. \nTry this out if a game freezes");
                ImGui.Spacing();
                ImGui.Text("Only Catridge Mapper ID Supported: \n0, 1, 2, 4");

                if (ImGui.Button("Close")) {
                    showManualWindow = false;
                }
            }
            ImGui.End();
        }
    }

    class FileDialog {
        /*
        *   File Dialog for ImGui.NET
        *   This is a simple file dialog, it's flexible and expandable
        *   This can also easily be ported to other languages for other bindings or the original Dear ImGui
        *
        *   Basic Usage:
        *   FileDialog fileDialog = new FileDialog();
        *
        *   string selectedFilePath = "";
        *
        *   fileDialog.Open(); //Trigger the file dialog to open
        *
        *   if (fileDialog.Show(ref selectedFilePath)) {
        *       //Do something with string path
        *   }
        *   
        *   Made by Bot Randomness
        */

        private string currentDirectory;
        private string selectedFilePath = "";
        private bool isOpen = false;

        private bool canCancel = true;

        public FileDialog(string startDirectory, bool canCancel = true) {
            currentDirectory = Directory.Exists(startDirectory) ? startDirectory : Directory.GetCurrentDirectory();
            this.canCancel = canCancel;
        }

        public bool Show(ref string resultFilePath) {
            if (!isOpen) return false;

            bool fileSelected = false;

            if (ImGui.Begin("File Dialog", ImGuiWindowFlags.HorizontalScrollbar)) {
                ImGui.Text("Select a file:");

                string[] directories = Directory.GetDirectories(currentDirectory);
                string[] files = Directory.GetFiles(currentDirectory, "*.*");

                ImGui.InputText("File Path", ref selectedFilePath, 260);

                if (ImGui.Button("Select File")) {
                    if (File.Exists(selectedFilePath)) {
                        resultFilePath = selectedFilePath;
                        fileSelected = true;
                        isOpen = false;
                    }
                }

                ImGui.SameLine();

                if (canCancel) {
                    if (ImGui.Button("Cancel")) {
                        isOpen = false;
                    }
                }

                if (Path.GetPathRoot(currentDirectory) != currentDirectory) {
                    if (ImGui.Button("Back")) {
                        currentDirectory = Directory.GetParent(currentDirectory)?.FullName ?? currentDirectory;
                    }
                }

                foreach (var dir in directories) {
                    if (ImGui.Selectable("[DIR] " + Path.GetFileName(dir))) {
                        currentDirectory = dir;
                    }
                }

                foreach (var file in files) {
                    if (ImGui.Selectable(Path.GetFileName(file))) {
                        selectedFilePath = file;
                    }
                }

                if (directories.Length == 0 && files.Length == 0) {
                    ImGui.Text("No files or folders found.");
                }
            }
            ImGui.End();

            return fileSelected;
        }

        public void Open() {
            isOpen = true;
        }
    }
}

#pragma warning restore