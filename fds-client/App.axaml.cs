using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace FdsClient;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            string? url = desktop.Args?.Length > 0 ? desktop.Args[0] : null;
            desktop.MainWindow = new MainWindow(url);
        }

        base.OnFrameworkInitializationCompleted();
    }
}