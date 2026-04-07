import subprocess
import sys
import time
import signal
import os

ROOT = os.path.dirname(os.path.abspath(__file__))

STREAMER = os.path.join(ROOT, "streamer", "StreamerServer.csproj")
CLIENT   = os.path.join(ROOT, "fds-client", "FdsClient.csproj")
SITE     = os.path.join(ROOT, "fds-site", "fds-site.csproj")
LOGIC    = os.path.join(ROOT, "fds-logic", "fds-logic.csproj")

procs = []
reported_exits = set()

def cleanup(*_):
    print("\n[FDS] Shutting down...")
    
    # Kill common .NET processes related to this repo first
    try:
        if os.name == 'nt':
            subprocess.run(["taskkill", "/F", "/IM", "StreamerServer.exe", "/T"], capture_output=True)
            subprocess.run(["taskkill", "/F", "/IM", "FdsClient.exe", "/T"], capture_output=True)
            subprocess.run(["taskkill", "/F", "/IM", "fds-site.exe", "/T"], capture_output=True)
    except:
        pass

    for name, p in procs:
        try:
            p.terminate()
            print(f"  [{name}] terminated")
        except Exception:
            pass
    sys.exit(0)

signal.signal(signal.SIGINT, cleanup)
signal.signal(signal.SIGTERM, cleanup)

def run(name, proj, delay=0):
    if delay:
        time.sleep(delay)
    print(f"[FDS] Starting {name}...")
    p = subprocess.Popen(
        ["dotnet", "run", "--project", proj],
        cwd=ROOT,
        stdout=sys.stdout,
        stderr=sys.stderr,
    )
    procs.append((name, p))
    return p

if __name__ == "__main__":
    print("=" * 50)
    print("  FDS - Fast Drawing Streamer")
    print("  Starting all services...")
    print("=" * 50)
    print()

    # 1. Build logic module first
    print("[FDS] Building logic module...")
    subprocess.run(
        ["dotnet", "build", LOGIC, "-c", "Release"],
        cwd=ROOT,
    )
    print()

    # 2. Start streamer first
    run("Streamer", STREAMER)
    time.sleep(3)

    # 3. Start client
    run("Client", CLIENT)

    # 4. Start site
    run("Site", SITE)

    print()
    print("[FDS] All services running. Press Ctrl+C to stop.")
    print()

    try:
        while True:
            for name, p in procs:
                if name in reported_exits:
                    continue
                ret = p.poll()
                if ret is not None:
                    print(f"[FDS] {name} exited with code {ret}")
                    reported_exits.add(name)
            time.sleep(1)
    except KeyboardInterrupt:
        cleanup()
