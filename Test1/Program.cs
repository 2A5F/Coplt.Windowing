using System.Runtime.Versioning;
using Coplt.Windowing;
using Coplt.Windowing.Win32;

namespace Test1;

class Program
{
    [STAThread]
    [SupportedOSPlatform("windows10.0")]
    static void Main(string[] args)
    {
        var ws = new Win32WindowSystem();
        Task.Run(async () =>
        {
            var win = await ws.CreateWindow(new("Hello World!", 960, 540)
            {
                MainWindow = true,
                Blur = WindowBlur.MicaAlt,
                Centered = true,
            });
            Console.WriteLine(win);
            win.On<WindowSizeChangeEvent>(ev => Console.WriteLine(ev));
            await Task.Delay(1000);
            win.Close();
        });
        ws.MessageLoop();
    }
}
