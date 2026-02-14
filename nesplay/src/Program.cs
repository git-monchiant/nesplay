using System.Diagnostics;
using System.Runtime.InteropServices;

public class Program {
    [DllImport("user32.dll")]
    static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")]
    static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    [DllImport("user32.dll")]
    static extern bool IsIconic(IntPtr hWnd);

    public static void Main(string[] args) {
        // Single instance - prevent multiple windows
        using var mutex = new Mutex(true, $"Nesplay_{Helper.ExeName}", out bool isNew);
        if (!isNew) {
            // Activate existing window
            var current = Process.GetCurrentProcess();
            foreach (var proc in Process.GetProcessesByName(current.ProcessName)) {
                if (proc.Id != current.Id && proc.MainWindowHandle != IntPtr.Zero) {
                    if (IsIconic(proc.MainWindowHandle))
                        ShowWindow(proc.MainWindowHandle, 9); // SW_RESTORE
                    SetForegroundWindow(proc.MainWindowHandle);
                    break;
                }
            }
            return;
        }

        Console.WriteLine("NET-NES");

        Helper.Flags(args);

        if (Helper.mode == 1) {
           GUI gui = new GUI();

           gui.Run();
        } else if (Helper.mode == 2) {
            TestRunner testRunner = new TestRunner();

            testRunner.Run(Helper.jsonPath);
        }
    }
}