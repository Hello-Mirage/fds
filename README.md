FDS: Fast Drawing Streamer
___________________________

FDS is a browser-less, WASM-native remote UI platform that delivers UI logic instead of pixels. By executing drawing logic at the client edge, FDS provides a native experience even on low-bandwidth connections.



___________________________

Documentation


Detailed guides and technical specifications are in the docs/ folder:

  - docs/FDS_PLATFORM.md     What is FDS?
  - docs/DEVELOPING_APPS.md  Guide to building Skia-native UI modules.
  - docs/ARCHITECTURE.md     The WASM-Chunked Streaming protocol.

___________________________

Quick Start


Ensure you have the .NET 10 SDK installed.

  1. Clone and Build:
     dotnet build

  2. One-Click Launch (All Services):
     python run.py

  3. Custom Protocol Integration:
     Import install_fds_protocol.reg to enable fds:// browser links.

___________________________

Core Features


  - WASM-Native Logic: UI logic is streamed as binary chunks and executed at native speed.
  - Zero-Latency Input: Interaction hit-tests occur locally on the client (0ms delay).
  - Skia-Native Routing: Multi-page navigation (Home, Docs, QuickStart) managed within the logic bundle.
  - Custom Protocol (fds://): Launch native UI sessions directly from any browser or website.


___________________________

Project Structure


  streamer/      The TCP server that distributes the UI logic module.
  fds-client/    The Avalonia client that hosts the local Skia engine.
  fds-logic/     A sample application module compiled for remote distribution.
  docs/          Technical documentation and developer guides.
