FDS: Distributed Logic Architecture
___________________________

FDS (Fast Drawing Streamer) is a distributed rendering engine that separates application logic from the physical display hardware.


1. The Logic Provider (Streamer)
___________________________

The Streamer is the single source of truth. It acts as a Module Distributor.

  - State Sync: It maintains the authoritative state.
  - Chunked Delivery: It serves the UI logic binary in 4KB chunks with a simulated pacing delay.


2. The Logic Host (FdsClient)
___________________________

The Client is a high-performance rendering host. It performs Native Skia Execution at the edge.

  - WASM/Binary Host: The client receives the logic module and hooks it into the local frame loop.
  - Zero-Latency Interactions: All high-frequency interactions (scrolling) happen instantaneously.


3. WASM-Chunked Streaming Protocol
___________________________

FDS uses a custom binary protocol optimized for incremental delivery:

  - Header: 4-byte total length prefix.
  - Segments: N-number of 4KB data packets.
  - Incremental Load: The client provides real-time progress feedback as each segment is buffered.
  - Local Skia Context: Once the full module is resolved, the client passes its native SKCanvas context to the remote logic, ensuring pixel-perfect results with native performance.


4. Why Binary Logic over Pixel Streaming?
___________________________

  Feature              Pixel Streaming (RDP/VNC)       FDS Logic Streaming
  ___________          ___________________________     ___________________________
  Bandwidth            High (MBs of video)             Ultra-Low (KBs of binary)
  Latency              Network RTT for every frame     0ms for local interactions
  Fidelity             Compressed/Blurry video         Pixel-Perfect Skia rendering
  Reflow               Fixed resolution                Truly Responsive native reflow
