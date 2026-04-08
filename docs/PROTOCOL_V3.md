# Protocol Specification V3.1
---

FDS utilizes a dual-transport model to deliver reliable application logic and high-frequency visual overlays. This document specifies the communication protocol between the FDS Streamer and FDS Client.

## 1. Logic Transport (TCP:5000)

The logic transport layer is used for initial module distribution.

- **Payload**: .NET 10 DLL (WASM-compatible C# logic).
- **Format**: 
  - [Module Length (4 bytes, Int32)]: Total size of the DLL.
  - [Binary Data]: The raw DLL bytes.
- **Handling**: The client reads the length, buffers the incoming data, and loads the assembly into a local AppDomain/Context for execution.

## 2. Vector Streaming (UDP:5005)

High-frequency visual updates are delivered via a fragmented UDP stream.

- **Payload**: Serialized Skia `SKPicture` data.
- **Fragmentation**: Frames exceeding MTU are split into fixed-size chunks to ensure network stability.

### Packet Binary Format (Header: 16 Bytes)

| Offset | Type | Field | Description |
| :--- | :--- | :--- | :--- |
| 0 | Int32 | FrameID | Unique identifier for the current frame. |
| 4 | Int32 | TotalChunks | Number of packets required to reassemble the frame. |
| 8 | Int32 | ChunkIndex | Zero-based index of the current packet. |
| 12 | Int32 | Length | Length of the binary data payload in this packet. |
| 16 | Byte[] | Data | Serialized Skia command data. |

### Distribution Settings
- **Chunk Size**: 8,192 bytes (8KB). Consolidated to reduce interrupt overhead while remaining efficient for network reassembly.
- **Refresh Gate**: 8ms (125 FPS).

## 3. Control Plane (TCP:5001)

Bidirectional control messages for window state and user interaction.

- **Message Format**: [Type (Int32)] [Value1 (Float)] [Value2 (Float)]

| Type | Name | Value 1 | Value 2 | Description |
| :--- | :--- | :--- | :--- | :--- |
| 0 | Click | X (Normalized) | Y (Normalized) | Coordinate-based interaction routing. |
| 1 | Resize | Width | Height | Host window dimension synchronization. |
| 2 | Scroll | Delta | - | Vertical displacement update. |

Note: Clicking and scrolling execute locally on the client (0ms feedback) before being synchronized with the server's session state.
