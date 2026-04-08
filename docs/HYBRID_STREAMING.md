# Hybrid Streaming in FDS (V3)
___________________________

FDS V3 supports a **Hybrid Architecture** that allows developers to choose the best transport method for their specific use case. By writing a single Skia-native `Render` method, your application can be delivered as either a distributed logic module or a high-frequency vector stream.

## 1. Logic Streaming (WASM/TCP)
**Mode**: Internal Logic Execution (Client-Side)
**Transport**: TCP (Port 5000)

In this mode, the Streamer pushes the entire binary logic module (`.dll`) to the client. The client compiles the module locally and executes the `Render` loop natively at 60 FPS.

- **Best For**: High-performance desktops, high-latency/jitter connections, and offline-capable sessions.
- **Latency**: 0ms interaction latency (hit-tests are local).
- **Overhead**: Occurs on the Client (CPU/WASM execution).

---

## 2. Vector Streaming (UDP)
**Mode**: Remote Logic Execution (Server-Side)
**Transport**: UDP (Port 5005)

In this mode, the Streamer executes the logic module server-side. It records the drawing commands into a Skia Vector frame (`SKPicture`) and streams the serialized vectors to the client via UDP. The client simply "replays" the commands on its canvas.

- **Best For**: Thin clients (Mobiles, IoT), low-power devices, and ultra-high-performance server-side rendering.
- **Latency**: Network-dependent (Server-to-Client RTT).
- **Overhead**: Occurs on the Server. Client overhead is near-zero.

---

## 3. Writing Hybrid-Ready Code
___________________________

To ensure your code works in both modes, follow the **Unified Render Signature**:

```csharp
namespace FdsLogic;

public static class MyRenderer
{
    // This method is called by BOTH the Client (WASM mode) and the Server (Vector mode)
    public static float Render(SKCanvas canvas, float width, float height, float scrollOffset)
    {
        canvas.Clear(SKColors.Black);
        
        // Use standard SkiaSharp commands
        using var paint = new SKPaint { Color = SKColors.Cyan, TextSize = 32 };
        canvas.DrawText("Hybrid FDS", 50, 100, paint);

        return 2000; // Always return the total content height for scroll support
    }
}
```

### Constraints for Hybrid Code:
1. **Stateless Drawing**: Ensure your `Render` method doesn't rely on local filesystem or hardware that only exists on one side.
2. **Deterministic Layout**: Since the server and client might have different DPIs, always use the provided `width` and `height` for all relative positioning.
3. **Asset Handling**: Embedded resources (fonts, icons) should be bundled within the DLL so both the server and client can access them during their respective execution loops.

---

## 4. Concurrent Layering
___________________________

FDS V3 supports simultaneous transport. The client composites two independent drawing layers into a single frame:

1.  **Lower Layer (WASM)**: Reliable UI, layout, and interaction logic.
2.  **Upper Layer (Vector)**: High-frequency remote overlays or "live feed" animations.

### Handling Remote Overlays:
Use the `IsServer` property to detect if your code is executing on the Streamer (for UDP broadcast) or on the Client (for local WASM):

```csharp
public static bool IsServer { get; set; }

public static float Render(SKCanvas canvas, float w, float h, float s)
{
    // Draw base UI (Client & Server)
    DrawLayout(canvas);

    // Draw remote-only overlay (Server only)
    if (IsServer)
    {
        DrawRemoteIndicator(canvas);
    }
}
```

The FDS Client automatically reassembles incoming UDP packets on Port 5005 and overlays them on top of the local WASM render in real-time.
