# Session Management and Concurrency
---

The FDS Streamer V3.1 is built for high-concurrency, multi-threaded operation. It maintains independent state contexts for each connected client, allowing a single server instance to host multiple users with isolated UI states.

## 1. ClientSession Architecture

Each connection is encapsulated in a `ClientSession` object. This container isolates the dynamic state of a user's remote session.

- **Window State**: Stores the individual Width (`W`) and Height (`H`) of the client's host window.
- **Scroll Memory**: Maintains the current `Scroll` offset and `MaxScroll` boundaries unique to that user's view.
- **UDP Routing**: Stores the resolved `VectorEndpoint` (IP/Port) for the parallel high-frequency stream.
- **Hashing Context**: Maintains the `LastHash` of the previous frame sent to this specific client to enable per-session delta suppression.

## 2. Parallel Rendering Tasks

The Streamer utilizes asynchronous task-based concurrency to prevent cross-session interference.

- **Session Spawning**: Upon a new TCP connection on Port 5000, the Streamer initializes a `ClientSession` and spawns a dedicated `VectorStreamLoop` on a background thread.
- **Isolation**: Drawing commands for Session A are recorded and streamed independently from Session B. This ensures that a complex UI update for one user does not "stutter" the stream for others.
- **Resource Cleanup**: When a TCP connection is severed, the Streamer automatically terminates the associated render task and purges the session from the ConcurrentDictionary.

## 3. High-Concurrency Input Routing

The Control Plane (Port 5001) is designed for shared-port input routing.

- **Dynamic Addressing**: The `ListenForInputEvents` task accepts connections from any client.
- **IP-Based Routing**: It identifies the transmitting client via their `RemoteEndPoint` and retrieves the corresponding `ClientSession`.
- **Atomic Updates**: Interaction updates (clicks, resizes) are pushed into the session state using thread-safe locking mechanisms, ensuring the parallel `VectorStreamLoop` reads a consistent state for the next render tick.

## 4. Scalability Considerations

- **Global Logic Singleton**: The WASM logic (e.g., `DocumentationRenderer`) is loaded once as a global assembly. Multiple sessions share the same executable logic but maintain independent state variables, significantly reducing memory overhead per client.
- **Thread Pool Utilization**: FDS leverages the .NET 10 thread pool for efficient task scheduling, allowing the system to scale linearly with the number of CPU cores available on the host server.
