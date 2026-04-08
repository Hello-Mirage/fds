using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using SkiaSharp;

class ClientSession
{
    public IPEndPoint? VectorEndpoint { get; set; }
    public float W { get; set; } = 800;
    public float H { get; set; } = 400;
    public float Scroll { get; set; } = 0;
    public float MaxScroll { get; set; } = 1000;
    public long LastHash { get; set; } = 0;
    public bool IsActive { get; set; } = true;
}

class Program
{
    // Hybrid Logic (Shared)
    private delegate float RenderFunc(SKCanvas canvas, float w, float h, float s);
    private delegate void ClickFunc(float x, float y, float w, float h, float s);
    private static RenderFunc? _fastRender;
    private static ClickFunc? _fastClick;

    private static readonly ConcurrentDictionary<string, ClientSession> _sessions = new();
    private static readonly UdpClient _udpSender = new UdpClient();

    public static async Task Main(string[] args)
    {
        Console.WriteLine("=== FDS MULTI-THREADED STREAMER V3.1 ===");
        
        LoadLogicModule();

        // Input Listener (Shared port, routes via IP)
        _ = Task.Run(() => ListenForInputEvents());

        var listener = new TcpListener(IPAddress.Any, 5000);
        listener.Start();
        Console.WriteLine("StreamerServer: Logic (TCP) on port 5000...");
        Console.WriteLine("StreamerServer: Dynamic Session Routing Enabled.");

        while (true)
        {
            try
            {
                var tcpClient = await listener.AcceptTcpClientAsync();
                var clientInfo = tcpClient.Client.RemoteEndPoint?.ToString() ?? "unknown";
                Console.WriteLine($"StreamerServer: New Client Connection [{clientInfo}]");

                // Spawn session handler
                _ = Task.Run(() => HandleClientSession(tcpClient));
            }
            catch (Exception ex) { Console.WriteLine($"Connection Error: {ex.Message}"); }
        }
    }

    private static async Task HandleClientSession(TcpClient tcpClient)
    {
        string clientId = tcpClient.Client.RemoteEndPoint?.ToString()?.Split(':')[0] ?? "unknown";
        var session = new ClientSession { 
            VectorEndpoint = new IPEndPoint(((IPEndPoint)tcpClient.Client.RemoteEndPoint!).Address, 5005) 
        };
        _sessions[clientId] = session;

        try {
            using (tcpClient)
            using (var stream = tcpClient.GetStream())
            {
                // 1. Stream WASM Module
                var dllPath = @"d:\fds\fds-logic\bin\Release\net10.0\fds-logic.dll";
                if (File.Exists(dllPath))
                {
                    var moduleData = File.ReadAllBytes(dllPath);
                    await stream.WriteAsync(BitConverter.GetBytes(moduleData.Length), 0, 4);
                    await stream.WriteAsync(moduleData, 0, moduleData.Length);
                    await stream.FlushAsync();
                }

                // 2. Start Dedicated Vector Thread for this session
                _ = Task.Run(() => VectorStreamLoop(session));

                // Maintain TCP connection
                while (tcpClient.Connected && session.IsActive) { await Task.Delay(500); }
            }
        }
        catch { }
        finally { session.IsActive = false; _sessions.TryRemove(clientId, out _); }
    }

    private static void LoadLogicModule()
    {
        try {
            var dllPath = @"d:\fds\fds-logic\bin\Release\net10.0\fds-logic.dll";
            if (!File.Exists(dllPath)) return;
            var assembly = Assembly.LoadFrom(dllPath);
            var type = assembly.GetType("FdsLogic.DocumentationRenderer");
            _fastRender = (RenderFunc?)Delegate.CreateDelegate(typeof(RenderFunc), type!.GetMethod("Render")!);
            _fastClick = (ClickFunc?)Delegate.CreateDelegate(typeof(ClickFunc), type!.GetMethod("HandleClick")!);
            type.GetProperty("IsServer")?.SetValue(null, true);
            Console.WriteLine("Streamer: Global Logic Engine initialized.");
        } catch (Exception e) { Console.WriteLine("Module Load Fail: " + e.Message); }
    }

    private static async Task VectorStreamLoop(ClientSession session)
    {
        var recorder = new SKPictureRecorder();
        while (session.IsActive)
        {
            if (_fastRender != null && session.VectorEndpoint != null)
            {
                try {
                    float w, h, scroll;
                    lock (session) { w = session.W; h = session.H; scroll = session.Scroll; }

                    using (var canvas = recorder.BeginRecording(new SKRect(0, 0, w, h)))
                    {
                        session.MaxScroll = _fastRender(canvas, w, h, scroll);
                    }

                    using var picture = recorder.EndRecording();
                    using var data = picture.Serialize();
                    
                    if (data != null)
                    {
                        long currentHash = GetFastHash(data);
                        if (currentHash == session.LastHash) { await Task.Delay(16); continue; }
                        session.LastHash = currentHash;

                        byte[] bytes = data.ToArray();
                        int frameId = (int)(DateTime.Now.Ticks % 1000000);
                        int chunkSize = 30000;
                        int totalChunks = (int)Math.Ceiling(bytes.Length / (double)chunkSize);

                        for (int i = 0; i < totalChunks; i++)
                        {
                            int offset = i * chunkSize;
                            int length = Math.Min(chunkSize, bytes.Length - offset);
                            byte[] packet = new byte[16 + length];
                            BitConverter.GetBytes(frameId).CopyTo(packet, 0);
                            BitConverter.GetBytes(totalChunks).CopyTo(packet, 4);
                            BitConverter.GetBytes(i).CopyTo(packet, 8);
                            BitConverter.GetBytes(length).CopyTo(packet, 12);
                            Buffer.BlockCopy(bytes, offset, packet, 16, length);

                            await _udpSender.SendAsync(packet, packet.Length, session.VectorEndpoint);
                            if (totalChunks > 1) await Task.Delay(1);
                        }
                    }
                }
                catch { }
            }
            await Task.Delay(16);
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
                string clientId = ((IPEndPoint)client.Client.RemoteEndPoint!).Address.ToString();
                
                var reader = new BinaryReader(client.GetStream());
                while (client.Connected)
                {
                    int type = reader.ReadInt32();
                    float v1 = reader.ReadSingle();
                    float v2 = reader.ReadSingle();

                    if (_sessions.TryGetValue(clientId, out var session))
                    {
                        if (type == 0) // Click
                        {
                            float px, py, cw, ch, co;
                            lock (session) { px = v1 * session.W; py = v2 * session.H; cw = session.W; ch = session.H; co = session.Scroll; }
                            _fastClick?.Invoke(px, py, cw, ch, co);
                        }
                        else if (type == 1) { lock (session) { session.W = v1; session.H = v2; } }
                        else if (type == 2) { lock (session) { session.Scroll = Math.Clamp(session.Scroll - v1 * 30.0f, 0, session.MaxScroll); } }
                    }
                }
            } catch { await Task.Delay(100); }
        }
    }

    private static unsafe long GetFastHash(SKData data)
    {
        ReadOnlySpan<long> span = new ReadOnlySpan<long>(data.Data.ToPointer(), (int)data.Size / 8);
        long hash = 0;
        int len = Math.Min(span.Length, 512);
        for (int i = 0; i < len; i++) hash ^= span[i];
        return hash;
    }
}
