# Developing Apps for FDS

FDS applications are written in __Skia-Native logic__ and compiled into portable binary modules. The host (FdsClient) executes this logic locally, providing 0ms interaction latency.

## 1. Setup the Logic Module

Your application logic must be a separate project (like `fds-logic`). 

### Create Project
```powershell
dotnet new classlib -n MyFdsApp
cd MyFdsApp
dotnet add package SkiaSharp --version 2.88.9
```

### Protocol Signature
Your module must export a static __Render__ method in the __FdsLogic__ namespace:

```
namespace FdsLogic;

public static class DocumentationRenderer
{
    public static void Render(SKCanvas canvas, float width, float height, float scrollOffset)
    {
        // 1. Clear the canvas
        canvas.Clear(SKColors.Black);

        // 2. Handle Responsive Layout
        bool isMobile = width < 580;

        // 3. Draw using SkiaSharp API
        using var paint = new SKPaint { Color = SKColors.Cyan, TextSize = 32 };
        canvas.DrawText("Hello FDS!", 50, 100, paint);
    }
}
```

## 2. Compile for Distribution

To prepare your app for the Streamer:
```powershell
dotnet build -c Release
```
This produces `MyFdsApp.dll` (your UI logic binary).

## 3. Register with the Streamer

In `Streamer/Program.cs`, point the module loader to your binary:

```csharp
var dllPath = @"path/to/MyFdsApp.dll";
var moduleData = File.ReadAllBytes(dllPath);
```

## 4. Best Practices

- __Responsive-First__: Always check __width__ and __height__ inside the __Render__ method to dynamically adjust layout.
- __Stateless Rendering__: The __Render__ method should focus on drawing. Keep session state on the server.
- __Embedded Assets__: Large images or fonts should be included as __EmbeddedResource__ in your logic project. FDS will stream them as part of the binary module.
