using System;
using Avalonia.Controls;

namespace FdsClient;

public partial class MainWindow : Window
{
    public MainWindow() : this(null) { }

    public MainWindow(string? fdsUrl)
    {
        InitializeComponent();
        
        string host = "127.0.0.1";
        int port = 5000;

        if (!string.IsNullOrEmpty(fdsUrl) && fdsUrl.StartsWith("fds://"))
        {
            try {
                var uri = new Uri(fdsUrl);
                host = uri.Host;
                if (uri.Port != -1) port = uri.Port;
            } catch { }
        }

        RendererControl.ConnectionHost = host;
        RendererControl.ConnectionPort = port;
        RendererControl.StartConnection();
    }
}