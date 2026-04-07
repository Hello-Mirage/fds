# FDS: Fast Drawing Streamer

FDS is a __browser-less, WASM-native remote UI platform__ that delivers UI logic instead of pixels. By executing drawing logic at the client edge, FDS provides a native experience even on low-bandwidth connections.

---

## Documentation

Detailed guides and technical specifications are available in the [docs/](file:///d:/fds/docs/FDS_PLATFORM.md) folder:

- [Platform Overview](file:///d:/fds/docs/FDS_PLATFORM.md): What is FDS?
- [Developing Apps](file:///d:/fds/docs/DEVELOPING_APPS.md): Guide to building Skia-native UI modules.
- [Architecture Deep-Dive](file:///d:/fds/docs/ARCHITECTURE.md): The WASM-Chunked Streaming protocol.

---

## 🚀 Quick Start

Ensure you have the .NET 10 SDK installed.

1.  **Clone & Build**:
    ```powershell
    dotnet build
    ```
2.  **Start Streamer** (Logic Provider):
    ```powershell
    dotnet run --project streamer/StreamerServer.csproj
    ```
3.  **Start Client** (Logic Host):
    ```powershell
    dotnet run --project fds-client/FdsClient.csproj
    ```

---

## 🛠 Project Structure

-   **`streamer/`**: The TCP server handles session persistence and distributes the UI logic module.
-   **`fds-client/`**: The Avalonia-based client hosts the local Skia engine and the binary logic runtime.
-   **`fds-logic/`**: A sample application module (Document Renderer) compiled for remote distribution.

---

## 📜 License
Licensed under our custom 'FDS Open Protocol' agreement. See [docs/ARCHITECTURE.md](file:///d:/fds/docs/ARCHITECTURE.md) for technical specifications.
