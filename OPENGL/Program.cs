using GLCC2D;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

namespace OPENGL;

internal static class Program
{
    static void Main()
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
