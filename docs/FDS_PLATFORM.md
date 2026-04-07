# FDS: Fast Drawing Streamer Protocol

FDS is a __browser-less remote UI platform__ designed to deliver high-fidelity, interactive user interfaces over low-bandwidth connections. By replacing the heavy Chromium/Blink stack with a distributed __WASM-native architecture__, FDS achieves near-native performance with zero network-induced rendering jitter.

## What is FDS?

1.  __Server__: Pushes a lightweight UI Logic Module (WASM/Binary) to the client.
2.  __Client__: Downloads the module in chunks and executes it locally against a native Skia engine.
3.  __Reflow__: Window resizing, scrolling, and hover effects happen __at the edge__ (locally on the client), resulting in 0ms interaction latency.

## Key Features

-   Zero-Chromium: No V8, no Blink, no WebView overhead.
-   WASM-Native: UI logic is written once and executed everywhere at near-native speeds.
-   Chunked Streaming: Modules are delivered in segments with real-time compilation feedback.
-   Skia-First: Pixel-perfect rendering across all platforms.

## Documentation Index

-   [Developing Apps](file:///d:/fds/docs/DEVELOPING_APPS.md): How to build your first FDS application.
-   [Architecture](file:///d:/fds/docs/ARCHITECTURE.md): Deep dive into the WASM-Chunked Streaming protocol.
-   [API Reference](file:///d:/fds/docs/API.md): Skia-based rendering hooks and interaction handlers.

## Getting Started

1.  **Launch Streamer**: `dotnet run --project d:\fds\streamer\StreamerServer.csproj`
2.  **Launch Client**: `dotnet run --project d:\fds\fds-client\FdsClient.csproj`
