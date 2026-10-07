# LzyFlow

**LzyFlow** is a local-first Linux file organizer that automatically keeps configurable directories clean, while preferring no action over an unsafe or uncertain filesystem change.

> **Status:** Product V1 architecture is defined. The bounded BSc research profile is in active development.

## Product V1 features

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

## BSc research profile

The repository distinguishes the complete Product V1 architecture from the smaller BSc research profile currently used for implementation and evaluation.

The research profile focuses on:

- deterministic and local semantic document classification;
- confidence-based rejection and manual review;
- risk-coverage calibration;
- safe filesystem operations and recovery;
- reproducible offline evaluation and error analysis.

Product features such as multiple clients, complete service distribution, notifications, and broader platform integration remain on the V1 roadmap but do not block the research profile.

See [V1 Technical Design](docs/technical-design.md#23-product-v1-and-bsc-research-profile) for the normative scope and completion criteria.

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

LzyFlow requires the **.NET 10 SDK**.

From the repository root:

```bash
dotnet restore LzyFlow.slnx
dotnet build LzyFlow.slnx
dotnet test LzyFlow.slnx
```

Before opening a pull request, verify formatting as well:

```bash
dotnet format LzyFlow.slnx --verify-no-changes
```

GitHub Actions runs formatting verification, a Release build, and the complete test suite for pull requests and pushes to `main`.

See [Development](docs/development.md) for the test structure, isolation rules, temporary filesystem and SQLite fixtures, and CI workflow.

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
- [Configuration](docs/configuration.md)
- [Security](docs/security.md)
- [Development](docs/development.md)

The technical design is the canonical reference for Product V1 architecture, lifecycle, persistence, security, recovery, API boundaries, the BSc research profile, and its evaluation contract.

## Roadmap

**BSc research profile**

- one watched directory and fixed destination taxonomy
- deterministic and local semantic classification
- confidence-based rejection and manual review
- safe moves, recovery, history, and undo
- offline evaluation runner and error analysis
- automated safety and recovery tests

**Product V1**

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

Licensed under the [Apache License 2.0](LICENSE).
