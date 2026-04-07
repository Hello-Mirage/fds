Developing Apps for FDS
___________________________

FDS applications are written in Skia-Native logic and compiled into portable binary modules. The host (FdsClient) executes this logic locally, providing 0ms interaction latency.


1. Setup the Logic Module
___________________________

Your application logic must be a separate project (like fds-logic).

Create Project:

  dotnet new classlib -n MyFdsApp
  cd MyFdsApp
  dotnet add package SkiaSharp --version 2.88.9


Protocol Signature
___________________________

Your module must export a static __Render__ method in the __FdsLogic__ namespace:

  namespace FdsLogic;

  public static class DocumentationRenderer
  {
      public static void Render(SKCanvas canvas, float width, float height, float scrollOffset)
      {
          canvas.Clear(SKColors.Black);

          bool isMobile = width < 580;

          using var paint = new SKPaint { Color = SKColors.Cyan, TextSize = 32 };
          canvas.DrawText("Hello FDS!", 50, 100, paint);
      }
  }


2. Compile for Distribution
___________________________

To prepare your app for the Streamer:

  dotnet build -c Release

This produces MyFdsApp.dll (your UI logic binary).


3. Register with the Streamer
___________________________

In Streamer/Program.cs, point the module loader to your binary:

  var dllPath = @"path/to/MyFdsApp.dll";
  var moduleData = File.ReadAllBytes(dllPath);


4. Best Practices
___________________________

  - Responsive-First: Always check __width__ and __height__ inside the Render method to dynamically adjust layout.
  - Stateless Rendering: The Render method should focus on drawing. Keep session state on the server.
  - Embedded Assets: Large images or fonts should be included as EmbeddedResource in your logic project. FDS will stream them as part of the binary module.
