using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using Avalonia.Threading;
using Avalonia.Interactivity;
using SkiaSharp;

namespace FdsClient;

public class SkiaRendererControl : Control
{
    private CancellationTokenSource? _cts;
    private TcpClient? _inputClient;
    private NetworkStream? _inputStream;
    private readonly SemaphoreSlim _streamLock = new(1, 1);
    
    private float _lastSyncedW = -1;
    private float _lastSyncedH = -1;
    private float _scrollOffset = 0; // Current scroll (interpolated)
    private float _targetScroll = 0; // Target scroll (intended)
    private float _maxScroll = 0;
    private float _streamProgress = 0;

    private delegate float RenderFunc(SKCanvas canvas, float w, float h, float s);
    private delegate void ClickFunc(float x, float y, float w, float h, float s);
    private RenderFunc? _fastRender;
    private ClickFunc? _fastClick;

    private SKPicture? _latestVectorFrame;
    private readonly object _frameLock = new();
    
    // Packet reassembly cache
    private readonly Dictionary<int, byte[][]> _frameChunks = new();
    private readonly byte[] _reassemblySharedBuffer = new byte[1024 * 1024];

    public string ConnectionHost { get; set; } = "127.0.0.1";
    public int ConnectionPort { get; set; } = 5000;

    public SkiaRendererControl()
    {
        _cts = new CancellationTokenSource();
    }

    public void StartConnection()
    {
        if (_cts != null && !_cts.IsCancellationRequested) _cts.Cancel();
        _cts = new CancellationTokenSource();

        Task.Run(() => ListenForDrawingCommands(_cts.Token));
        Task.Run(() => ConnectInputChannel(_cts.Token));
        Task.Run(() => ListenForVectorCommands(_cts.Token));
        
        this.PointerPressed += OnPointerPressed;
        this.AddHandler(InputElement.PointerWheelChangedEvent, (s, e) => OnPointerWheelChangedInternal(e), RoutingStrategies.Tunnel | RoutingStrategies.Bubble, true);

        this.Focusable = true;
        this.IsHitTestVisible = true;
        this.AttachedToVisualTree += (s, e) => this.Focus();
        this.LayoutUpdated += (s, e) => SyncSizeToServer();
    }

    private void SyncSizeToServer()
    {
        var stream = _inputStream;
        if (stream == null || Bounds.Width <= 0 || Bounds.Height <= 0) return;

        float w = (float)Bounds.Width;
        float h = (float)Bounds.Height;

        if (Math.Abs(w - _lastSyncedW) < 0.5f && Math.Abs(h - _lastSyncedH) < 0.5f) return;

        _lastSyncedW = w;
        _lastSyncedH = h;

        Task.Run(async () =>
        {
            await _streamLock.WaitAsync();
            try
            {
                var buf = new byte[12];
                BitConverter.GetBytes(1).CopyTo(buf, 0); // Type 1: Resize
                BitConverter.GetBytes(w).CopyTo(buf, 4);
                BitConverter.GetBytes(h).CopyTo(buf, 8);
                await stream.WriteAsync(buf, 0, 12);
                await stream.FlushAsync();
            }
            catch { _lastSyncedW = -1; }
            finally { _streamLock.Release(); }
        });
    }

    private async Task ListenForDrawingCommands(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                using var client = new TcpClient();
                await client.ConnectAsync(ConnectionHost, ConnectionPort);
                using var stream = client.GetStream();

                byte[] lengthBuffer = new byte[4];
                int bytesRead = 0;
                while (bytesRead < 4)
                {
                    int n = await stream.ReadAsync(lengthBuffer, bytesRead, 4 - bytesRead, ct);
                    if (n == 0) throw new IOException("Stream closed");
                    bytesRead += n;
                }
                int moduleLength = BitConverter.ToInt32(lengthBuffer, 0);

                byte[] moduleData = new byte[moduleLength];
                bytesRead = 0;
                while (bytesRead < moduleLength)
                {
                    int toRead = Math.Min(4096, moduleLength - bytesRead);
                    int n = await stream.ReadAsync(moduleData, bytesRead, toRead, ct);
                    if (n == 0) throw new IOException("Stream closed");
                    bytesRead += n;
                    
                    _streamProgress = (float)bytesRead / moduleLength;
                    Dispatcher.UIThread.Post(() => InvalidateVisual()); 
                }

                var assembly = System.Reflection.Assembly.Load(moduleData);
                var type = assembly.GetType("FdsLogic.DocumentationRenderer");
                if (type != null)
                {
                    var renderM = type.GetMethod("Render");
                    var clickM = type.GetMethod("HandleClick");
                    if (renderM != null) _fastRender = (RenderFunc)Delegate.CreateDelegate(typeof(RenderFunc), renderM);
                    if (clickM != null) _fastClick = (ClickFunc)Delegate.CreateDelegate(typeof(ClickFunc), clickM);
                    _streamProgress = 1.0f;
                }

                while (client.Connected && !ct.IsCancellationRequested)
                {
                    Dispatcher.UIThread.Post(() => InvalidateVisual());
                    await Task.Delay(16, ct);
                }
            }
            catch { await Task.Delay(1000, ct); }
        }
    }

    public override void Render(DrawingContext context)
    {
        context.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, Bounds.Width, Bounds.Height));

        if (_streamProgress < 1.0f && _latestVectorFrame == null)
        {
            context.Custom(new LoadingDrawOperation(new Rect(0, 0, Bounds.Width, Bounds.Height), _streamProgress));
        }
        else
        {
            context.Custom(new SkiaDrawOperation(new Rect(0, 0, Bounds.Width, Bounds.Height), _fastRender, _latestVectorFrame, _scrollOffset, this));
        }
    }

    private class LoadingDrawOperation : ICustomDrawOperation
    {
        private readonly float _progress;
        public LoadingDrawOperation(Rect bounds, float progress) { Bounds = bounds; _progress = progress; }
        public Rect Bounds { get; }
        public void Dispose() { }
        public bool Equals(ICustomDrawOperation? other) => false;
        public bool HitTest(Point p) => false;
        public void Render(ImmediateDrawingContext context)
        {
            var leaseFeature = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();
            if (leaseFeature == null) return;
            using var lease = leaseFeature.Lease();
            var canvas = lease.SkCanvas;

            canvas.Clear(new SKColor(15, 15, 15));
            using var paint = new SKPaint { Color = SKColors.Cyan, TextSize = 24, IsAntialias = true, Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold) };
            float cx = (float)Bounds.Width / 2;
            float cy = (float)Bounds.Height / 2;
            canvas.DrawText("STREAMING UI LOGIC...", cx - 120, cy - 40, paint);
            
            float barW = (float)Bounds.Width * 0.6f;
            var barRect = new SKRect(cx - barW/2, cy, cx + barW/2, cy + 20);
            paint.Style = SKPaintStyle.Stroke;
            paint.Color = new SKColor(255, 255, 255, 40);
            canvas.DrawRoundRect(barRect, 10, 10, paint);
            
            paint.Style = SKPaintStyle.Fill;
            paint.Color = SKColors.Cyan;
            canvas.DrawRoundRect(new SKRect(barRect.Left, barRect.Top, barRect.Left + barW * _progress, barRect.Bottom), 10, 10, paint);

            paint.TextSize = 14; paint.Color = SKColors.White;
            canvas.DrawText($"{(_progress * 100):F1}% COMPLETE", cx - 40, cy + 50, paint);
        }
    }

    private class SkiaDrawOperation : ICustomDrawOperation
    {
        private readonly RenderFunc? _method;
        private readonly SKPicture? _picture;
        private readonly float _scroll;
        private readonly SkiaRendererControl _owner;
        public SkiaDrawOperation(Rect bounds, RenderFunc? method, SKPicture? picture, float scroll, SkiaRendererControl owner) 
        { 
            Bounds = bounds; 
            _method = method; 
            _picture = picture;
            _scroll = scroll; 
            _owner = owner; 
        }
        public Rect Bounds { get; }
        public void Dispose() { }
        public bool Equals(ICustomDrawOperation? other) => false;
        public bool HitTest(Point p) => false;
        public void Render(ImmediateDrawingContext context)
        {
            var lease = context.TryGetFeature<ISkiaSharpApiLeaseFeature>()?.Lease();
            if (lease == null) return;
            using (lease)
            {
                var canvas = lease.SkCanvas;
                
                // --- SMOOTH SCROLL INTERPOLATION ---
                if (Math.Abs(_owner._targetScroll - _owner._scrollOffset) > 0.1f)
                {
                    _owner._scrollOffset += (_owner._targetScroll - _owner._scrollOffset) * 0.15f;
                    Dispatcher.UIThread.Post(() => _owner.InvalidateVisual(), DispatcherPriority.Render);
                }

                // --- LAYER 1: Local WASM (Reliable UI) ---
                if (_owner._fastRender != null)
                {
                    float maxS = _owner._fastRender(canvas, (float)Bounds.Width, (float)Bounds.Height, _owner._scrollOffset);
                    _owner._maxScroll = maxS;
                }

                // --- LAYER 2: Remote UDP Vectors (Dynamic Overlays) ---
                lock (_owner._frameLock)
                {
                    if (_owner._latestVectorFrame != null)
                    {
                        canvas.DrawPicture(_owner._latestVectorFrame);
                    }
                }
            }
        }
    }

    private async Task ListenForVectorCommands(CancellationToken ct)
    {
        using var udp = new UdpClient(5005);
        while (!ct.IsCancellationRequested)
        {
            try {
                var result = await udp.ReceiveAsync(ct);
                var packet = result.Buffer;
                if (packet.Length < 16) continue;

                int frameId = BitConverter.ToInt32(packet, 0);
                int totalChunks = BitConverter.ToInt32(packet, 4);
                int chunkIndex = BitConverter.ToInt32(packet, 8);
                int length = BitConverter.ToInt32(packet, 12);
                
                if (!_frameChunks.ContainsKey(frameId)) 
                    _frameChunks[frameId] = new byte[totalChunks][];

                var chunks = _frameChunks[frameId];
                chunks[chunkIndex] = new byte[length];
                Buffer.BlockCopy(packet, 16, chunks[chunkIndex], 0, length);

                // Check if frame is complete
                bool complete = true;
                int totalSize = 0;
                for (int i = 0; i < totalChunks; i++) {
                    if (chunks[i] == null) { complete = false; break; }
                    totalSize += chunks[i].Length;
                }

                if (complete)
                {
                    int offset = 0;
                    for (int i = 0; i < totalChunks; i++) {
                        Buffer.BlockCopy(chunks[i], 0, _reassemblySharedBuffer, offset, chunks[i].Length);
                        offset += chunks[i].Length;
                    }
                    
                    // Copy to fixed size for Skia
                    byte[] frameData = new byte[offset];
                    Buffer.BlockCopy(_reassemblySharedBuffer, 0, frameData, 0, offset);

                    Task.Run(() => {
                        try {
                            using var skData = SKData.CreateCopy(frameData);
                            var picture = SKPicture.Deserialize(skData);
                            
                            if (picture != null)
                            {
                                lock (_frameLock)
                                {
                                    var oldFrame = _latestVectorFrame;
                                    _latestVectorFrame = picture;
                                    Dispatcher.UIThread.Post(() => oldFrame?.Dispose());
                                }
                                Dispatcher.UIThread.Post(() => InvalidateVisual());
                            }
                        } catch { }
                    });

                    // Keep cache clean
                    _frameChunks.Remove(frameId);
                    if (_frameChunks.Count > 5) _frameChunks.Clear();
                }
            } catch { }
        }
    }

    private async Task ConnectInputChannel(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                _inputClient = new TcpClient();
                await _inputClient.ConnectAsync(ConnectionHost, ConnectionPort + 1, ct);
                _inputStream = _inputClient.GetStream();
                _lastSyncedW = -1;
                Dispatcher.UIThread.Post(() => SyncSizeToServer());
                await Task.Delay(Timeout.Infinite, ct);
            }
            catch { _inputStream = null; await Task.Delay(1000, ct); }
        }
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var point = e.GetCurrentPoint(this);
        float x = (float)point.Position.X;
        float y = (float)point.Position.Y;
        if (Bounds.Width <= 0) return;

        // Update local logic immediately for 0ms feedback
        _fastClick?.Invoke(x, y + _scrollOffset, (float)Bounds.Width, (float)Bounds.Height, _scrollOffset);
        InvalidateVisual();

        var stream = _inputStream;
        if (stream != null)
        {
            Task.Run(async () =>
            {
                await _streamLock.WaitAsync();
                try
                {
                    var buf = new byte[12];
                    BitConverter.GetBytes(0).CopyTo(buf, 0); // Type 0: Click
                    BitConverter.GetBytes(x / (float)Bounds.Width).CopyTo(buf, 4);
                    BitConverter.GetBytes(y / (float)Bounds.Height).CopyTo(buf, 8);
                    await stream.WriteAsync(buf, 0, 12);
                    await stream.FlushAsync();
                }
                catch { } finally { _streamLock.Release(); }
            });
        }
    }

    private void OnPointerWheelChangedInternal(PointerWheelEventArgs e)
    {
        _targetScroll -= (float)e.Delta.Y * 60.0f;
        _targetScroll = Math.Clamp(_targetScroll, 0, _maxScroll);
        
        var stream = _inputStream;
        if (stream != null)
        {
            Task.Run(async () =>
            {
                await _streamLock.WaitAsync();
                try
                {
                    var buf = new byte[12];
                    BitConverter.GetBytes(2).CopyTo(buf, 0); // Type 2: Scroll
                    BitConverter.GetBytes((float)e.Delta.Y).CopyTo(buf, 4);
                    BitConverter.GetBytes(0f).CopyTo(buf, 8);
                    await stream.WriteAsync(buf, 0, 12);
                    await stream.FlushAsync();
                }
                catch { } finally { _streamLock.Release(); }
            });
        }
        InvalidateVisual();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _cts?.Cancel();
        _inputStream?.Dispose();
        _inputClient?.Dispose();
        base.OnDetachedFromVisualTree(e);
    }
}
