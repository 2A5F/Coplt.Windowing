# Coplt.Windowing

A simple window creation library

- Currently only supports Windows

## Example

```csharp
using Coplt.Windowing;
using Coplt.Windowing.Win32;

class Program
{
    [STAThread]
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
            
            win.On<WindowSizeChangeEvent>(ev => Console.WriteLine(ev));
            
            await Task.Delay(10000);
            win.Close();
        });
        ws.MessageLoop();
    }
}
```