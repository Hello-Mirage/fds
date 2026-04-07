FDS Protocol
___________________________

FDS (Fast Drawing Streamer) is a browser-less remote UI platform designed to deliver high-fidelity, interactive user interfaces over low-bandwidth connections. By replacing the heavy Chromium/Blink stack with a distributed WASM-native architecture, FDS achieves near-native performance with zero network-induced rendering jitter.


What is FDS?
___________________________

Traditional remote desktop (RDP/VNC) streams pixels, which are heavy and latent. Modern web apps stream HTML/JS, which requires a resource-intensive browser engine.

FDS streams __logic__ and __drawing commands__.

  1. Server: Pushes a lightweight UI Logic Module (WASM/Binary) to the client.
  2. Client: Downloads the module in chunks and executes it locally against a native Skia engine.
  3. Reflow: Window resizing, scrolling, and hover effects happen at the edge (locally on the client), resulting in 0ms interaction latency.


Key Features
___________________________

  - Zero-Chromium: No V8, no Blink, no WebView overhead.
  - WASM-Native: UI logic is written once and executed everywhere at near-native speeds.
  - Chunked Streaming: Modules are delivered in segments with real-time compilation feedback.
  - Skia-First: Pixel-perfect rendering across all platforms.


Documentation Index
___________________________

  - docs/DEVELOPING_APPS.md   How to build your first FDS application.
  - docs/ARCHITECTURE.md      Deep dive into the WASM-Chunked Streaming protocol.


Getting Started
___________________________

  1. Launch Streamer:
     dotnet run --project streamer/StreamerServer.csproj

  2. Launch Client:
     dotnet run --project fds-client/FdsClient.csproj
