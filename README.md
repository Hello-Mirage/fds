FDS: Fast Drawing Streamer
___________________________

FDS is a browser-less, WASM-native remote UI platform that delivers UI logic instead of pixels. By executing drawing logic at the client edge, FDS provides a native experience even on low-bandwidth connections.


Documentation
___________________________

Detailed guides and technical specifications are in the docs/ folder:

  - docs/FDS_PLATFORM.md     What is FDS?
  - docs/DEVELOPING_APPS.md  Guide to building Skia-native UI modules.
  - docs/ARCHITECTURE.md     The WASM-Chunked Streaming protocol.


Quick Start
___________________________

Ensure you have the .NET 10 SDK installed.

  1. Clone and Build:
     dotnet build

  2. Start Streamer (Logic Provider):
     dotnet run --project streamer/StreamerServer.csproj

  3. Start Client (Logic Host):
     dotnet run --project fds-client/FdsClient.csproj


Project Structure
___________________________

  streamer/      The TCP server that distributes the UI logic module.
  fds-client/    The Avalonia client that hosts the local Skia engine.
  fds-logic/     A sample application module compiled for remote distribution.
  docs/          Technical documentation and developer guides.
