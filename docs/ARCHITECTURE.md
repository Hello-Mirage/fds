# FDS: Distributed Logic Architecture

FDS (Fast Drawing Streamer) is a __distributed rendering engine__ that separates application logic from the physical display hardware.

## 1. The Logic Provider (Streamer)
The __Streamer__ is the 'Single Source of Truth'. It acts as a __Module Distributor__.

- __State Sync__: It maintains the authoritative state.
- __Chunked Delivery__: It serves the UI logic binary in 4KB chunks with a simulated pacing delay.

## 2. The Logic Host (FdsClient)
The __Client__ is a high-performance rendering host. It performs __Native Skia Execution__ at the edge.

- __WASM/Binary Host__: The client receives the logic module and hooks it into the local frame loop.
- __Zero-Latency Interactions__: All high-frequency interactions (scrolling) happen instantaneously.

## 3. WASM-Chunked Streaming Protocol

FDS uses a custom binary protocol optimized for incremental delivery:

-   **Header**: 4-byte total length prefix.
-   **Segments**: N-number of 4KB data packets.
-   **Incremental Load**: The client provides real-time '% COMPLETE' feedback as each segment is verified and buffered.
-   **Local Skia Context**: Once the full module is resolved, the client passes its native `SKCanvas` context to the remote logic, ensuring pixel-perfect results with native performance.

## 4. Why Binary Logic over Pixel Streaming?

| Feature | Pixel Streaming (RDP/VNC) | FDS Logic Streaming |
| :--- | :--- | :--- |
| **Bandwidth** | High (MBs of video) | Ultra-Low (KBs of binary) |
| **Latency** | Network RTT for every frame | **0ms** for local interactions |
| **Fidelity** | Compressed/Blurry video | **Pixel-Perfect** Skia rendering |
| **Reflow** | Fixed resolution | **Truly Responsive** native reflow |
