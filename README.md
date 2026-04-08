# FDS (Fast Drawing Streamer)
________________________________________________________________________________

FDS is a browser-less, WASM-native remote UI platform that delivers **UI logic instead of pixels**. By streaming drawing commands and executing them at the client edge, FDS provides a pixel-perfect native experience with zero interaction latency.

________________________________________________________________________________

### Documentation

Detailed guides and technical specifications are located in the `docs/` folder:

*   **[FDS Platform](docs/FDS_PLATFORM.md)**: Introduction to the FDS ecosystem.
*   **[Developing Apps](docs/DEVELOPING_APPS.md)**: Guide to building Skia-native UI modules.
*   **[Hybrid Streaming](docs/HYBRID_STREAMING.md)**: Technical guide to layered WASM & UDP transport.
*   **[Core Architecture](docs/ARCHITECTURE.md)**: The WASM-Chunked Streaming protocol.

________________________________________________________________________________

### Quick Start

Ensure you have the **.NET 10 SDK** and **Python 3.x** installed.

1.  **Build the Project**:
    ```bash
    dotnet build
    ```

2.  **Launch All Services**:
    ```bash
    python run.py
    ```

3.  **Enable Protocol Handling**:
    Import `install_fds_protocol.reg` to enable `fds://` browser-to-native application launching.

________________________________________________________________________________

### Core Features

*   **WASM-Native Logic**: UI logic is streamed as binary chunks and executed at native speed.
*   **Concurrent Hybrid Transport**: Simultaneously layer reliable local WASM (TCP) with high-frequency remote Vector (UDP) overlays.
*   **Zero-Latency Input**: Hit-testing occurs at the local edge for 0ms interaction delay.
*   **Skia-Native Routing**: State-based navigation internal to the logic bundle.
*   **Browser-to-Native**: Launch seamless native UI sessions directly from any website via `fds://`.

________________________________________________________________________________

### Project Structure

*   **/streamer**: The TCP/UDP server that distributes logic and streams vectors.
*   **/fds-client**: The Avalonia host that executes modules and replays remote streams.
*   **/fds-logic**: Example interactive UI module compiled for remote distribution.
*   **/docs**: Technical documentation and developer guides.

________________________________________________________________________________

### License

This project is licensed under the **MIT License** — see the [LICENSE](LICENSE) file for details.

________________________________________________________________________________

*This project was developed and refined using the Antigravity IDE.*