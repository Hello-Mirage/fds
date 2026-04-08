# Core Performance Engine
---

The FDS engine is optimized for high-frequency rendering and low-latency interaction. This document details the specific technical implementations that enable V3.1 to achieve sub-millisecond logic execution and buttery-smooth distribution.

## 1. Compiled Delegate Invocation

Traditional reflection-based method invocation (`MethodInfo.Invoke`) introduces significant overhead during high-frequency loops. To eliminate this bottleneck, FDS utilizes compiled delegates for both the `Render` and `HandleClick` entry points.

- **Implementation**: The logic DLL is loaded via `Assembly.LoadFrom`, and entry points are bound to `RenderFunc` and `ClickFunc` delegates using `Delegate.CreateDelegate`.
- **Impact**: This provides near-native execution speed, allowing the server to process complex UI logic for multiple concurrent sessions in under 1.0ms per frame.

## 2. XOR-Fold Content Hashing

Network efficiency is achieved by suppressing redundant frame transmissions during static UI states.

- **Mechanism**: The streamer captures the serialized `SKPicture` data and performs a high-speed XOR-fold operation over the binary buffer.
- **Delta Suppression**: If the computed hash matches the previous frame's hash, the UDP broadcast for that session is skipped.
- **Efficiency**: In static states, network throughput drops to near-zero, preserving bandwidth for dynamic animations and high-frequency overlays.

## 3. Kinetic Scroll Interpolation

FDS implements a client-side interpolation system to provide an inertia-like, premium scrolling experience without increasing server load.

- **Lerp System**: The client maintains a `_targetScroll` and a current `_scrollOffset`. 
- **Velocity**: Every render tick, the current offset is linearly interpolated towards the target using a `0.15` velocity factor.
- **Fluidity**: This creates a smooth kinetic movement that masks network latency and individual packet arrival times.

## 4. 125 FPS Calibrated Tick Rate

The FDS vector stream handles a calibrated 8ms refresh gate.

- **Tick Rate**: The engine targets 125 frames per second. This is twice the standard refresh rate of video equipment, ensuring that even fast-moving animations (glow pulses, gradients) appear perfectly fluid.
- **Stability**: Unlike a 1ms "packet storm" approach, the 8ms gate provides a stable interval that respects OS timer resolutions while delivering native-grade responsiveness.
