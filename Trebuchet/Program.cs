using Avalonia;
using System;
using System.Diagnostics;
using System.Threading;
using Avalonia.ReactiveUI;
using Projektanker.Icons.Avalonia;
using Projektanker.Icons.Avalonia.MaterialDesign;
using TrebuchetLib;

namespace Trebuchet;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        bool createdNew = true;
        using (var mutex = new Mutex(true, @"TotTrebuchet", out createdNew))
        {
            if(createdNew)
                BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
            else
            {
                var currentProcess = Process.GetCurrentProcess();
                foreach (var process in Process.GetProcessesByName(currentProcess.ProcessName))
                {
                    if (process.Id != currentProcess.Id)
                    {
                        Tools.FocusWindow(process.MainWindowHandle);
                        break;
                    }
                }
            }
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
    {
        IconProvider.Current
            .Register<MaterialDesignIconProvider>();
        
        return AppBuilder.Configure<App>()
            .UseReactiveUI()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
    }
}