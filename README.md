# LzyFlow

**LzyFlow** is a local-first Linux file organizer that automatically keeps configurable directories clean, while preferring no action over an unsafe or uncertain filesystem change.

> **Status:** V1 design is finalized. Linux-only implementation is in active development.

## Features

- Watch one or more configurable directories
- Route files using deterministic rules
- Refine placement with lightweight and semantic classification
- Detect incomplete or stalled downloads before acting
- Keep uncertain files in place for manual review
- Prevent implicit overwrites and unsafe path escapes
- Review, approve, resolve, ignore, and undo through CLI/TUI
- Store state and history locally in SQLite
- Configure behavior through hot-reloaded TOML
- Expose a localhost-only REST API for local clients
- Send native Linux notifications when attention is required

## Architecture

```text
lzyflow (CLI / TUI)
        |
        | localhost REST
        v
     lzyflowd
        |
        +-- file watching
        +-- stability detection
        +-- routing & classification
        +-- filesystem operations
        +-- notifications
        +-- SQLite
        +-- configuration
```

`lzyflowd` is the standalone authority for runtime state and filesystem mutations.  
The CLI and TUI are thin clients over the same application operations.

## Platform

V1 targets **Linux** only.

Runtime integration is designed around:

- .NET
- `systemd --user`
- XDG directories
- SQLite
- TOML configuration
- Linux download-origin metadata
- native desktop notifications

Windows and macOS support are intentionally deferred until after V1.

## Installation

V1 releases are planned as self-contained `linux-x64` binaries with a simple install/uninstall workflow.

The installed layout will follow normal user-scoped Linux conventions:

```text
~/.local/bin/lzyflow
~/.local/bin/lzyflowd
~/.config/lzyflow/config.toml
~/.local/share/lzyflow/lzyflow.db
```

Release installation instructions will be added with the first usable build.

## Development

Once the solution is bootstrapped:

```bash
dotnet restore
dotnet build
```

Development builds may be run with standard `dotnet run` workflows from the relevant project.

Daemon logs are intended to be available through:

```bash
journalctl --user -u lzyflow
```

## Configuration

LzyFlow uses TOML as the source of user configuration.

The V1 configuration model includes:

- watched directories
- per-watch rules
- category mappings
- destination roots
- classification thresholds
- notification settings
- local API settings
- cleanup policy

Configuration is validated atomically. Invalid changes are rejected and the last-known-good configuration remains active.

## Documentation

- [V1 Technical Design](docs/technical-design.md)

The technical design is the canonical reference for V1 architecture, lifecycle, persistence, security, recovery, API boundaries, and implementation constraints.

## Roadmap

**V1**

- Linux daemon
- CLI and TUI
- REST control API
- safe automatic organization
- classification pipeline
- review workflow
- undo/history
- native notifications

**Post-V1**

- Windows and macOS support
- GUI
- package-manager distribution
- MCP adapter
- broader platform integrations

## License

A license has not yet been selected.
