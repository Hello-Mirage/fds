using System;
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
using SkiaSharp;

namespace FdsClient;

public class SkiaRendererControl : Control
{
    private SKPicture? _currentPicture;
    private readonly object _syncRoot = new();
    private CancellationTokenSource? _cts;
    private TcpClient? _inputClient;
    private NetworkStream? _inputStream;
    private readonly SemaphoreSlim _streamLock = new(1, 1);
    private readonly System.Collections.Concurrent.ConcurrentQueue<SKPicture> _disposalQueue = new();
    private float _lastSyncedW = -1;
    private float _lastSyncedH = -1;

    public SkiaRendererControl()
    {
        _cts = new CancellationTokenSource();
        Task.Run(() => ListenForDrawingCommands(_cts.Token));
        Task.Run(() => ConnectInputChannel(_cts.Token));
        this.PointerPressed += OnPointerPressed;
        
        // Ensure we can receive wheel events
        this.Focusable = true;
        this.Background = Brushes.Transparent;
        this.AttachedToVisualTree += (s, e) => this.Focus();

        // Ensure size sync on any layout update
        this.LayoutUpdated += (s, e) => SyncSizeToServer();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == BoundsProperty)
        {
            SyncSizeToServer();
        }
    }

    private void SyncSizeToServer()
    {
        var stream = _inputStream;
        if (stream == null) return;
        
        // Wait for valid bounds
        if (Bounds.Width <= 0 || Bounds.Height <= 0) return;

        float w = (float)Bounds.Width;
        float h = (float)Bounds.Height;

        // Throttling: Only sync if size actually changed logically to avoid flooding
        if (Math.Abs(w - _lastSyncedW) < 0.5f && Math.Abs(h - _lastSyncedH) < 0.5f) return;

        _lastSyncedW = w;
        _lastSyncedH = h;

        Task.Run(async () =>
        {
            await _streamLock.WaitAsync();
            try
            {
                Console.WriteLine($"Client: Syncing size {w:F0}x{h:F0} to server...");
                var buf = new byte[12];
                BitConverter.GetBytes(1).CopyTo(buf, 0); // Type 1: Resize
                BitConverter.GetBytes(w).CopyTo(buf, 4);
                BitConverter.GetBytes(h).CopyTo(buf, 8);
                await stream.WriteAsync(buf, 0, 12);
                await stream.FlushAsync();
                Console.WriteLine($"Client: Size {w:F0}x{h:F0} synced OK.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Client: Size sync failed: {ex.Message}");
                // Reset sync state to retry later
                _lastSyncedW = -1;
            }
            finally { _streamLock.Release(); }
        });
    }

    private async Task ConnectInputChannel(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                _inputClient = new TcpClient();
                await _inputClient.ConnectAsync("127.0.0.1", 5001, ct);
                _inputStream = _inputClient.GetStream();
                
                // Force sync on connect
                _lastSyncedW = -1;
                Dispatcher.UIThread.Post(() => SyncSizeToServer());

                // Keep alive until cancelled
                await Task.Delay(Timeout.Infinite, ct);
            }
            catch
            {
                _inputStream = null;
                await Task.Delay(1000, ct);
            }
        }
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var pos = e.GetPosition(this);
        if (Bounds.Width <= 0 || Bounds.Height <= 0) return;

        // Normalize to 0-1 range
        float nx = (float)(pos.X / Bounds.Width);
        float ny = (float)(pos.Y / Bounds.Height);

        var stream = _inputStream;
        if (stream == null) return;

        Task.Run(async () =>
        {
            await _streamLock.WaitAsync();
            try
            {
                var buf = new byte[12];
                BitConverter.GetBytes(0).CopyTo(buf, 0); // Type 0: Click
                BitConverter.GetBytes(nx).CopyTo(buf, 4);
                BitConverter.GetBytes(ny).CopyTo(buf, 8);
                await stream.WriteAsync(buf, 0, 12);
                await stream.FlushAsync();
            }
            catch { /* ignore */ }
            finally { _streamLock.Release(); }
        });
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        float dy = (float)e.Delta.Y;
        Console.WriteLine($"Client: Scroll Delta captured: {dy}");
        var stream = _inputStream;
        if (stream == null) return;

        Task.Run(async () =>
        {
            await _streamLock.WaitAsync();
            try
            {
                var buf = new byte[12];
                BitConverter.GetBytes(2).CopyTo(buf, 0); // Type 2: Scroll
                BitConverter.GetBytes(dy).CopyTo(buf, 4);
                BitConverter.GetBytes(0f).CopyTo(buf, 8); // Unused V2
                await stream.WriteAsync(buf, 0, 12);
                await stream.FlushAsync();
            }
            catch { /* ignore */ }
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
                await client.ConnectAsync("127.0.0.1", 5000);
                using var stream = client.GetStream();

                byte[] lengthBuffer = new byte[4];

                while (client.Connected && !ct.IsCancellationRequested)
                {
                    // 1. Read length
                    int bytesRead = 0;
                    while (bytesRead < 4)
                    {
                        int n = await stream.ReadAsync(lengthBuffer, bytesRead, 4 - bytesRead, ct);
                        if (n == 0) throw new IOException("Stream closed");
                        bytesRead += n;
                    }

                    int dataLength = BitConverter.ToInt32(lengthBuffer, 0);
                    if (dataLength <= 0 || dataLength > 10 * 1024 * 1024) throw new IOException("Invalid data length");

                    // 2. Read SKPicture data
                    byte[] data = new byte[dataLength];
                    bytesRead = 0;
                    while (bytesRead < dataLength)
                    {
                        int n = await stream.ReadAsync(data, bytesRead, dataLength - bytesRead, ct);
                        if (n == 0) throw new IOException("Stream closed");
                        bytesRead += n;
                    }

                    // 3. Deserialize
                    using var ms = new MemoryStream(data);
                    var newPicture = SKPicture.Deserialize(ms);

                    if (newPicture == null) continue;

                    // 4. Update and Invalidate
                    Dispatcher.UIThread.Post(() =>
                    {
                        lock (_syncRoot)
                        {
                            // Retire previous picture to disposal queue
                            if (_currentPicture != null)
                            {
                                _disposalQueue.Enqueue(_currentPicture);
                            }
                            _currentPicture = newPicture;
                        }
                        
                        // Age out old pictures
                        while (_disposalQueue.Count > 20)
                        {
                            if (_disposalQueue.TryDequeue(out var old))
                            {
                                try { old.Dispose(); } catch { /* ignore */ }
                            }
                        }
                        
                        InvalidateVisual();
                    });
                }
            }
            catch (Exception)
            {
                // Wait before reconnecting
                await Task.Delay(1000, ct);
            }
        }
    }

    public override void Render(DrawingContext context)
    {
        lock (_syncRoot)
        {
            context.Custom(new SkiaDrawOperation(new Rect(0, 0, Bounds.Width, Bounds.Height), _currentPicture));
        }
    }

    private class SkiaDrawOperation : ICustomDrawOperation
    {
        private readonly SKPicture? _picture;

        public SkiaDrawOperation(Rect bounds, SKPicture? picture)
        {
            Bounds = bounds;
            _picture = picture;
        }

        public Rect Bounds { get; }

        public void Dispose()
        {
        }

        public bool Equals(ICustomDrawOperation? other) => false;
        public bool HitTest(Point p) => false;

        public void Render(ImmediateDrawingContext context)
        {
            var leaseFeature = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();
            if (leaseFeature == null) return;

            using var lease = leaseFeature.Lease();
            var canvas = lease.SkCanvas;
            canvas.Clear(SKColors.Transparent);

            if (_picture != null)
            {
                // We draw 1:1 because the server is now generating the picture at our exact size
                canvas.DrawPicture(_picture);
            }
            else
            {
                using var paint = new SKPaint
                {
                    Color = SKColors.Gray,
                    TextSize = 24,
                    IsAntialias = true
                };
                canvas.DrawText("Waiting for StreamerServer...", 20, 40, paint);
            }
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _cts?.Cancel();
        _currentPicture?.Dispose();
        _inputStream?.Dispose();
        _inputClient?.Dispose();
        base.OnDetachedFromVisualTree(e);
    }
}
