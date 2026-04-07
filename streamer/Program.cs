using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

class Program
{
    private static float _currentW = 800;
    private static float _currentH = 480;
    private static float _scrollOffset = 0;
    private static float _maxScroll = 1000;

    public static async Task Main(string[] args)
    {
        Console.WriteLine("=== FDS STREAMER V3 (WASM NATIVE MODE) ===");
        
        // Start input listener (port 5001) in background
        _ = Task.Run(() => ListenForInputEvents());

        // Module/Drawing stream (port 5000)
        var listener = new TcpListener(IPAddress.Any, 5000);
        listener.Start();
        Console.WriteLine("StreamerServer: Listening on port 5000...");
        Console.WriteLine("StreamerServer: Input listener on port 5001...");

        while (true)
        {
            try
            {
                using var client = await listener.AcceptTcpClientAsync();
                Console.WriteLine("StreamerServer: Client connected.");
                using var stream = client.GetStream();

                // 1. Load the Binary Module (Native WASM-equivalent)
                var dllPath = @"d:\fds\fds-logic\bin\Release\net10.0\fds-logic.dll";
                if (!File.Exists(dllPath)) {
                    Console.WriteLine($"Streamer: Error - Logic module binary not found at {dllPath}");
                    continue;
                }
                var moduleData = File.ReadAllBytes(dllPath);

                // 2. Send Module via Chunked Streaming Protocol
                Console.WriteLine($"Streamer: Streaming UI Logic Module ({moduleData.Length} bytes) in 4KB chunks...");
                
                // Header: [Total Length] (4 bytes)
                await stream.WriteAsync(BitConverter.GetBytes(moduleData.Length), 0, 4);
                
                int chunkSize = 4096;
                int offset = 0;
                while (offset < moduleData.Length)
                {
                    int toSend = Math.Min(chunkSize, moduleData.Length - offset);
                    await stream.WriteAsync(moduleData, offset, toSend);
                    offset += toSend;
                    
                    // Small delay to make the streaming/compilation progress visible
                    await Task.Delay(1); 
                }
                await stream.FlushAsync();

                Console.WriteLine("Streamer: Module stream complete. Transitioning to state-only mode.");

                while (client.Connected)
                {
                    // State synchronization can happen here if needed.
                    await Task.Delay(100); 
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Streamer: Client error: {ex.Message}");
            }
        }
    }

    private static async Task ListenForInputEvents()
    {
        var listener = new TcpListener(IPAddress.Any, 5001);
        listener.Start();
        while (true)
        {
            try
            {
                using var client = await listener.AcceptTcpClientAsync();
                Console.WriteLine("StreamerServer: Input client connected.");
                using var stream = client.GetStream();
                var reader = new BinaryReader(stream);

                while (client.Connected)
                {
                    int type = reader.ReadInt32(); // 0: Click, 1: Resize, 2: Scroll
                    float v1 = reader.ReadSingle();
                    float v2 = reader.ReadSingle();

                    if (type == 0) // Click (Normalized)
                    {
                        // Logic moved to local module, but server can still track global state
                        Console.WriteLine($"Click: {v1:F2}, {v2:F2}");
                    }
                    else if (type == 1) // Resize
                    {
                        lock (typeof(Program)) { _currentW = v1; _currentH = v2; }
                    }
                    else if (type == 2) // Scroll
                    {
                        float delta = v1;
                        lock (typeof(Program))
                        {
                            _scrollOffset -= delta * 30.0f;
                            if (_scrollOffset < 0) _scrollOffset = 0;
                            if (_scrollOffset > _maxScroll) _scrollOffset = _maxScroll;
                            Console.WriteLine($"Server: Scroll Delta {delta:F2}, New Offset {(int)_scrollOffset}");
                        }
                    }
                }
            }
            catch { /* restart listener */ }
        }
    }
}
