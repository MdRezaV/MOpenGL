using MGLCC;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

namespace MOpenGL;

internal class Program
{
    static void Main(string[] args)
    {
        using (var window = new MGLWindow(new GameWindowSettings()
        {
            UpdateFrequency = 60,
        },
        new NativeWindowSettings()
        {
            CurrentMonitor = Monitors.GetMonitors()[1].Handle,
            WindowState = WindowState.Fullscreen,
        }))
        {
            window.Run();
        }
    }
}
