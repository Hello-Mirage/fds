using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using SkiaSharp;

class Program
{
    private static float _currentW = 800;
    private static float _currentH = 480;
    private static float _scrollOffset = 0;
    private static float _maxScroll = 1000;

    // Hybrid State
    private static MethodInfo? _remoteRenderMethod;
    private static MethodInfo? _remoteClickHandler;
    private static readonly UdpClient _udpSender = new UdpClient();
    private static IPEndPoint _clientEndpoint = new IPEndPoint(IPAddress.Loopback, 5005);

    public static async Task Main(string[] args)
    {
        Console.WriteLine("=== FDS HYBRID STREAMER V3 (VECTOR + WASM) ===");
        
        // 1. Initial Load of Logic Module (Server-Side Execution)
        LoadLogicModule();

        // 2. Start UDP Vector Stream Loop (60 FPS)
        _ = Task.Run(() => VectorStreamLoop());

        // 3. Start input listener (port 5001) in background
        _ = Task.Run(() => ListenForInputEvents());

        // Module/WASM stream (port 5000)
        var listener = new TcpListener(IPAddress.Any, 5000);
        listener.Start();
        Console.WriteLine("StreamerServer: Logic (TCP) on port 5000...");
        Console.WriteLine("StreamerServer: Input (TCP) on port 5001...");
        Console.WriteLine("StreamerServer: Vector (UDP) on port 5005...");

        while (true)
        {
            try
            {
                using var client = await listener.AcceptTcpClientAsync();
                Console.WriteLine("StreamerServer: Client connected.");
                using var stream = client.GetStream();

                // 1. Send Module via Chunked Streaming Protocol
                var dllPath = @"d:\fds\fds-logic\bin\Release\net10.0\fds-logic.dll";
                if (File.Exists(dllPath))
                {
                    var moduleData = File.ReadAllBytes(dllPath);
                    Console.WriteLine($"Streamer: Streaming WASM Logic Module ({moduleData.Length} bytes)...");
                    
                    await stream.WriteAsync(BitConverter.GetBytes(moduleData.Length), 0, 4);
                    int offset = 0;
                    while (offset < moduleData.Length)
                    {
                        int toSend = Math.Min(65536, moduleData.Length - offset);
                        await stream.WriteAsync(moduleData, offset, toSend);
                        offset += toSend;
                        await Task.Delay(1); 
                    }
                    await stream.FlushAsync();
                    Console.WriteLine("Streamer: WASM module stream complete.");
                }

                while (client.Connected) { await Task.Delay(100); }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Streamer: Client error: {ex.Message}");
            }
        }
    }

    private static void LoadLogicModule()
    {
        try {
            var dllPath = @"d:\fds\fds-logic\bin\Release\net10.0\fds-logic.dll";
            if (!File.Exists(dllPath)) return;
            var assembly = Assembly.LoadFrom(dllPath);
            var type = assembly.GetType("FdsLogic.DocumentationRenderer");
            _remoteRenderMethod = type?.GetMethod("Render");
            _remoteClickHandler = type?.GetMethod("HandleClick");
            
            // Set IsServer = true for the remote logic instance
            var isServerProp = type?.GetProperty("IsServer");
            isServerProp?.SetValue(null, true);

            Console.WriteLine("Streamer: Logic module loaded for server-side execution.");
        } catch (Exception e) {
            Console.WriteLine($"Streamer: Module load error: {e.Message}");
        }
    }

    private static async Task VectorStreamLoop()
    {
        var recorder = new SKPictureRecorder();
        while (true)
        {
            if (_remoteRenderMethod != null)
            {
                try {
                    float w, h, scroll;
                    lock (typeof(Program)) { w = _currentW; h = _currentH; scroll = _scrollOffset; }

                    // Record drawing commands
                    using (var canvas = recorder.BeginRecording(new SKRect(0, 0, w, h)))
                    {
                        var result = _remoteRenderMethod.Invoke(null, new object[] { canvas, w, h, scroll });
                        if (result is float maxS) { _maxScroll = maxS; }
                    }

                    using var picture = recorder.EndRecording();
                    using var data = picture.Serialize();
                    
                    if (data != null)
                    {
                        byte[] bytes = data.ToArray();
                        
                        // --- V3 Vector Packetizer ---
                        int frameId = (int)(DateTime.Now.Ticks % 1000000);
                        int chunkSize = 32768; // 32KB chunks for stable UDP delivery
                        int totalChunks = (int)Math.Ceiling(bytes.Length / (double)chunkSize);

                        for (int i = 0; i < totalChunks; i++)
                        {
                            int offset = i * chunkSize;
                            int length = Math.Min(chunkSize, bytes.Length - offset);
                            
                            // Packet Header: [FrameId(4)] [Total(4)] [Index(4)] [Length(4)]
                            byte[] packet = new byte[16 + length];
                            BitConverter.GetBytes(frameId).CopyTo(packet, 0);
                            BitConverter.GetBytes(totalChunks).CopyTo(packet, 4);
                            BitConverter.GetBytes(i).CopyTo(packet, 8);
                            BitConverter.GetBytes(length).CopyTo(packet, 12);
                            Buffer.BlockCopy(bytes, offset, packet, 16, length);

                            await _udpSender.SendAsync(packet, packet.Length, _clientEndpoint);
                            if (totalChunks > 1) await Task.Delay(1); // Prevent UDP buffer overflow
                        }
                    }
                }
                catch (Exception e) { Console.WriteLine("UDP Stream Error: " + e.Message); }
            }

            await Task.Delay(16); // ~60 FPS
        }
    }

    private static async Task ListenForInputEvents()
    {
        var listener = new TcpListener(IPAddress.Any, 5001);
        listener.Start();
        while (true)
        {
            try {
                using var client = await listener.AcceptTcpClientAsync();
                var reader = new BinaryReader(client.GetStream());
                while (client.Connected)
                {
                    int type = reader.ReadInt32();
                    float v1 = reader.ReadSingle();
                    float v2 = reader.ReadSingle();

                    if (type == 0) // Click
                    {
                        float px, py, cw, ch, co;
                        lock (typeof(Program)) { px = v1 * _currentW; py = v2 * _currentH; cw = _currentW; ch = _currentH; co = _scrollOffset; }
                        _remoteClickHandler?.Invoke(null, new object[] { px, py, cw, ch, co });
                    }
                    else if (type == 1) // Resize
                    {
                        lock (typeof(Program)) { _currentW = v1; _currentH = v2; }
                    }
                    else if (type == 2) // Scroll
                    {
                        lock (typeof(Program)) {
                            _scrollOffset -= v1 * 30.0f;
                            _scrollOffset = Math.Clamp(_scrollOffset, 0, _maxScroll);
                        }
                    }
                }
            } catch { }
        }
    }
}
