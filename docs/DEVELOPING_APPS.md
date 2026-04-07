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

Your module should export static methods in the __FdsLogic__ namespace:

  namespace FdsLogic;

  public static class DocumentationRenderer
  {
      // [Required] Drawing loop called at 60FPS
      public static float Render(SKCanvas canvas, float width, float height, float scrollOffset)
      {
          canvas.Clear(SKColors.Black);
          // Drawing logic here...
          return 2000; // Return total content height for scroll clamping
      }

      // [Optional] Local hit-testing called on click/touch
      public static void HandleClick(float x, float y, float width, float height, float scrollOffset)
      {
          // Interaction logic (e.g. navigation or state updates)
      }
  }


2. Interactivity & Routing
___________________________

FDS supports __Local-Edge Interactivity__. By implementing `HandleClick`, you can respond to user input with 0ms delay. You can also implement internal routing by maintaining a `_currentPage` state and switching rendering paths inside the `Render` method.


3. Compile & Test
___________________________

To prepare your app and launch all services:

  1. Build Logic: `dotnet build -c Release`
  2. Launch: `python run.py`


4. Best Practices
___________________________

  - Responsive-First: Check __width__ to switch between Mobile (<600) and Desktop layouts.
  - Unified Context: Create a shared `LayoutContext` to keep `Render` and `HandleClick` in sync.
  - Content Height: Always return the accurate total length of your content from `Render` to enable smooth scrolling.
  - Embedded Assets: Use `EmbeddedResource` for fonts and images; they will be streamed in the logic bundle.
