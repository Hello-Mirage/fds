using System.Net;
using System.Net.Sockets;
using SkiaSharp;

class Program
{
    private static float _currentW = 800;
    private static float _currentH = 480;
    private static float _lastRenderW = -1;
    private static float _lastRenderH = -1;
    private static float _scrollOffset = 0;

    static async Task Main(string[] args)
    {
        // Start input listener (port 5001) in background
        _ = Task.Run(() => ListenForInputEvents());

        // Drawing stream (port 5000)
        var listener = new TcpListener(IPAddress.Any, 5000);
        listener.Start();
        Console.WriteLine("StreamerServer: Listening on port 5000...");

        while (true)
        {
            try
            {
                using var client = await listener.AcceptTcpClientAsync();
                Console.WriteLine("StreamerServer: Client connected.");
                using var stream = client.GetStream();

                while (client.Connected)
                {
                    float w, h, scroll;
                    lock (typeof(Program)) 
                    { 
                        w = _currentW; 
                        h = _currentH; 
                        scroll = _scrollOffset;
                    }

                    // Log only when size changes to avoid log spam
                    if (Math.Abs(w - _lastRenderW) > 1 || Math.Abs(h - _lastRenderH) > 1) {
                        Console.WriteLine($"Streamer: Rendering at {w:F0}x{h:F0}");
                        _lastRenderW = w; _lastRenderH = h;
                    }

                    // 1. Record the drawing at current dynamic size
                    using var recorder = new SKPictureRecorder();
                    using var canvas = recorder.BeginRecording(SKRect.Create(0, 0, w, h));
                    
                    // Render the documentation site via pure C# Skia logic
                    Streamer.DocumentationRenderer.Render(canvas, w, h, scroll);

                    using var picture = recorder.EndRecording();

                    // 2. Serialize
                    using var ms = new MemoryStream();
                    picture.Serialize(ms);
                    var data = ms.ToArray();

                    // 3. Send: [Length prefix] + [Data]
                    var lengthPrefix = BitConverter.GetBytes(data.Length);
                    await stream.WriteAsync(lengthPrefix, 0, 4);
                    await stream.WriteAsync(data, 0, data.Length);
                    await stream.FlushAsync();

                    // 4. Stream delay (~60 FPS)
                    await Task.Delay(16);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"StreamerServer: Client disconnected ({ex.Message})");
            }
        }
    }

    static async Task ListenForInputEvents()
    {
        var inputListener = new TcpListener(IPAddress.Any, 5001);
        inputListener.Start();
        Console.WriteLine("StreamerServer: Input listener on port 5001...");

        while (true)
        {
            var client = await inputListener.AcceptTcpClientAsync();
            Console.WriteLine("StreamerServer: Input client connected.");
            _ = Task.Run(() => HandleInputClient(client));
        }
    }

    static async Task HandleInputClient(TcpClient client)
    {
        using var _ = client;
        using var stream = client.GetStream();
        var buf = new byte[12]; // Type(4) + V1(4) + V2(4)

        try
        {
            while (client.Connected)
            {
                int read = 0;
                while (read < 12)
                {
                    int n = await stream.ReadAsync(buf, read, 12 - read);
                    if (n == 0) return;
                    read += n;
                }

                int type = BitConverter.ToInt32(buf, 0);
                float v1 = BitConverter.ToSingle(buf, 4);
                float v2 = BitConverter.ToSingle(buf, 8);

                if (type == 0) // Click event
                {
                    float w, h, scroll;
                    lock (typeof(Program)) 
                    { 
                        w = _currentW; 
                        h = _currentH; 
                        scroll = _scrollOffset; 
                    }
                    float cx = v1 * w;
                    float cy = v2 * h;
                    Console.WriteLine($"Input: click at ({cx:F0}, {cy:F0}) with scroll {scroll}");
                    Streamer.DocumentationRenderer.HandleClick(cx, cy, scroll);
                }
                else if (type == 1) // Resize event
                {
                    lock (typeof(Program))
                    {
                        _currentW = v1;
                        _currentH = v2;
                    }
                    Console.WriteLine($"Resize: new resolution {v1:F0}x{v2:F0}");
                }
                else if (type == 2) // Scroll event
                {
                    lock (typeof(Program))
                    {
                        // Multiple by 20 to make scrolling feel more natural
                        _scrollOffset -= v1 * 30.0f;
                        if (_scrollOffset < 0) _scrollOffset = 0;
                        if (_scrollOffset > 3000) _scrollOffset = 3000;
                        Console.WriteLine($"Server: Scroll Delta {v1:F2}, New Offset {_scrollOffset:F0}");
                    }
                }
                else
                {
                    Console.WriteLine($"Unknown packet type received: {type}");
                }
            }
        }
        catch
        {
            Console.WriteLine("StreamerServer: Input client disconnected.");
        }
    }
}
