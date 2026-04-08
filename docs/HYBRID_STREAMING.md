# Hybrid Streaming Architecture
---

FDS V3.1 utilizes a tiered transport architecture that separates persistent application logic from high-frequency visual updates. This document outlines the technical interplay between the WASM-based Logic Layer and the UDP-based Vector Layer.

## 1. Persistent Logic Layer (TCP:5000)

The Logic Layer delivers the application's executable code. 

- **Mechanism**: The streamer transmits the compiled logic module (`.dll`) to the client upon connection.
- **Execution**: The client executes this binary within a native Skia-hosted runtime. 
- **Role**: This layer handles primary layout, interaction logic (hit-testing), and local state management. It provides a reliable "base" that operates at 60 FPS regardless of network jitter.

## 2. Dynamic Vector Layer (UDP:5005)

The Vector Layer provides high-frequency visual overlays and "live" UI properties streamed from the server.

- **Mechanism**: The server executes the logic module server-side, records the output as an `SKPicture` (Vector commands), and streams these commands to the client.
- **Transport**: Individual frames are fragmented into MTU-aware **8,192-byte chunks** for resilient delivery over UDP.
- **Tick Rate**: The stream is calibrated to an **8ms refresh gate (125 FPS)**, providing higher fidelity than the local 60 FPS logic loop for cinematic UI properties (glows, pulsing gradients, live telemetry).

## 3. Layered Composition

The FDS Client implements a multi-layer composition engine to merge these streams into a seamless UI.

### WASM Background Layer (Local)
The client executes the `Render` entry point locally. This ensures that buttons respond to hover and click states with 0ms delay, as the logic is running on the host machine.

### Vector Overlay Layer (Remote)
Incoming UDP vector packets are reassembled in a specialized background thread. Once a complete `Frame` is reassembled, it is "played back" on top of the WASM layer.

## 4. Optimization Strategies

To maintain performance across both layers, FDS implements several critical optimizations:

- **XOR-Fold Frame Suppression**: Within the Vector Layer, frames that have not changed are detected via an XOR-fold hash of the `SKPicture` buffer. If no change is detected, the server skips the UDP broadcast for that tick.
- **Compiled Delegates**: Both the local client runtime and the server-side vector recorder use compiled delegates for logic execution, bypassing the latency over reflection-based invocation.
- **Delta Vectorization**: Only the drawing commands are sent, not pixel data. This maintains a massive bandwidth advantage over traditional VNC or bitmap-based remote desktop protocols.
