# FDS: Fast Drawing Streamer
---

FDS is a high-performance, browser-less UI streaming platform designed to deliver application logic rather than pre-rendered pixels. By executing WASM-native logic modules at the network edge and layering them with high-frequency vector overlays, FDS achieves native-grade performance with zero perceived interaction latency.

## Documentation Index

Detailed technical specifications and architectural guides are available in the docs directory:

* [Core Engine](docs/CORE_ENGINE.md): Technical breakdown of the rendering and optimization suit.
* [Protocol Specification](docs/PROTOCOL_V3.md): Binary packet formats and transport layering.
* [Session Management](docs/SESSION_MANAGEMENT.md): Multi-threaded concurrent client handling.
* [Hybrid Streaming](docs/HYBRID_STREAMING.md): Guide to layered WASM and UDP transport.
* [Developing Applications](docs/DEVELOPING_APPS.md): Methodology for building FDS-compatible UI modules.

## Architectural Highlights

FDS V3.1 introduces a refined concurrent architecture optimized for modern high-bandwidth, low-latency applications.

* **Multi-threaded Session Manager**: The server maintains independent state contexts for each connected client, spawning dedicated parallel tasks for high-frequency vector distribution.
* **125 FPS Vector Protocol**: A recalibrated 8ms refresh gate delivers buttery-smooth animations, utilizing 8KB MTU-aware UDP packets for maximum network reliability.
* **Compiled Delegate Execution**: Bypasses reflection-based method invocation via direct RenderFunc delegates, reducing server-side UI logic execution to under 1.0ms.
* **Content-Aware Hashing**: Implements a high-speed XOR-fold hashing algorithm to eliminate redundant frame transmissions, significantly reducing network overhead during static UI states.
* **Kinetic Interaction Engine**: Features 0ms local interaction feedback coupled with server-side state synchronization and smooth velocity-based scroll interpolation.

## Getting Started

FDS requires the .NET 10 SDK and Python 3.x for its orchestration layer.

### 1. Build the Platform
Execute a project-wide build of the logic, client, and streamer components:
```bash
dotnet build
```

### 2. Launch Services
Start the FDS orchestration script to initialize the streamer, client, and documentation site:
```bash
python run.py
```

### 3. Protocol Integration
Import the included registry configuration to enable fds:// protocol handling across the operating system:
```bash
reg import install_fds_protocol.reg
```

## Project Components

* /streamer: High-concurrency TCP/UDP engine for logic distribution and vector streaming.
* /fds-client: Avalonia-hosted Skia runtime designed for WASM execution and remote replay.
* /fds-logic: C# source for portable UI modules designed for the FDS hybrid protocol.
* /docs: Comprehensive technical documentation and specifications.

---
Development facilitated by the Antigravity IDE.