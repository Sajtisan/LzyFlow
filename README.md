# LzyFlow

**LzyFlow** is a local-first Linux file organizer that routes downloads into appropriate destinations and adapts the folder structure as related files accumulate.

> **Status:** This README describes reviewed Product V1 requirements. These are implementation targets, not a claim that the complete utility is available. The AI architecture and final thesis experiment set remain open.

## Organization behavior

After a download finishes, LzyFlow gives the user **one minute** before organizing it. During that minute, the user can move a temporary file into **`Downloads/to-be-deleted`**. Files there stay excluded from organization. The user deletes them when no longer needed; the folder name does not enable automatic deletion.

Other eligible files are handled as follows:

- Use a suitable specific existing folder when available.
- Otherwise, use an appropriate configured category root, such as Documents, Pictures, Projects, or Videos, when that placement is confident.
- Leave uncertain decisions for review at the file's current location.

Those category names are examples, not a hard-coded taxonomy.

## Static and dynamic modes

| Mode | Behavior |
| --- | --- |
| Static | Use configured roots and suitable existing folders. Do not create classifier-proposed folders. |
| Dynamic | Also identify useful groups, create meaningfully named nested folders, and move eligible files into them. |

Grouping is **adaptive**: a course or project folder can later gain useful subfolders as the collection develops. Files can be regrouped after their initial placement into a category root or another managed folder.

There is no universal minimum file count that automatically justifies creation. Folder creation must improve navigation and avoid unnecessary fragmentation. The exact grouping algorithm, action criteria, and limits remain open.

Names retain recognizable information from the documents. Nested creation is supported; creation is not restricted to direct children of a category root.

Confident, authorized placement and creation can execute automatically. Uncertain grouping, placement, or naming goes to review. **Optional rename/merge suggestions always require approval** and show the proposed changes and affected files.

Initially, one global setting selects static or dynamic mode. Per-destination mode overrides are a later optional feature.

## Initial setup and integration

Both **TUI setup and CLI setup are required**. Setup collects supported user-facing choices, including mode, watches, categories, destinations, and whether existing files may be managed.

The existing-file choice is made **separately for each destination**. Configuring a path alone does not silently adopt its contents.

The CLI must support **fully noninteractive setup and operation** for integration with a larger project. Exact command flags and output formats remain implementation decisions. Both interfaces use shared application behavior and validation.

## Product V1 scope

- Linux daemon and configurable watched directories
- Stability detection and a post-completion grace period
- Explicit exclusion of temporary downloads
- Deterministic routing and lightweight/local semantic decisions
- Static placement and adaptive dynamic organization
- Uncertainty review, fixed approvals, history, and validated undo
- Canonical-path authorization and no implicit overwrite
- Per-destination adoption of existing files
- SQLite, XDG paths, and atomically validated TOML
- TUI and interactive/noninteractive CLI setup and control
- Localhost REST API and native Linux notifications

Optional extensions include approval-based folder rename/merge suggestions and later per-destination mode overrides. Their detailed implementation and release priority are not fixed.

## Architecture

In standalone deployment, `lzyflowd` owns runtime state, configuration, SQLite, and filesystem mutations. CLI/TUI are thin clients over shared application operations, using the localhost API.

An embedded host may call Core/Application services directly. Models return bounded semantic intent; application code validates and executes actions.

| Project | Responsibility |
| --- | --- |
| Core | Domain concepts and rules |
| Application | Setup, organization, review, history, and other use cases |
| Infrastructure | Filesystem, SQLite, models, metadata, notifications, and Linux integration |
| Daemon | Standalone runtime and REST host |
| CLI/TUI | User interaction and integration commands |

## AI and thesis direction

Candidates include embeddings and clustering, classical learned scorers, compact neural scorers, and lightweight Jev-style decision models.

**No architecture, fixed dataset size, trained model, or mandatory model catalogue has been selected.** Training a custom neural component remains a scope decision. A frozen encoder plus MLP is a candidate, not a requirement.

Docker comparisons are planned. Research must distinguish placement accuracy from useful grouping, naming, unnecessary creation, fragmentation, uncertainty, and resource use.

The agreed product requirements and final assessed thesis deliverables are separate. The thesis subset, experiment roster, targets, and model choice must be stated when settled.

## Platform and installation

V1 targets **Linux**, using .NET, XDG, SQLite, TOML, and `systemd --user`. Windows/macOS remain later work.

Planned user-scoped layout:

```text
~/.local/bin/lzyflow
~/.local/bin/lzyflowd
~/.config/lzyflow/config.toml
~/.local/share/lzyflow/lzyflow.db
~/.local/share/lzyflow/api.token
```

Self-contained `linux-x64` releases are planned. This documentation does not implement installation or setup.

## Development

The repository targets the **.NET 10 SDK**. From its root:

```bash
dotnet restore LzyFlow.slnx
dotnet build LzyFlow.slnx
dotnet test LzyFlow.slnx
```

Before opening a pull request:

```bash
dotnet format LzyFlow.slnx --verify-no-changes
```

The established CI baseline verifies formatting, builds Release, and runs tests. No application code or models are added by this documentation revision.

Daemon logs are intended to be available through:

```bash
journalctl --user -u lzyflow
```

## Configuration

TOML stores user intent. Candidate updates are validated as a whole and applied atomically. Invalid updates preserve the last-known-good configuration.

Setup obtains an explicit global mode and per-destination existing-file choices. Complete examples illustrate choices, not silent defaults or guaranteed current parser support.

## Documentation

- [V1 Technical Design](docs/technical-design.md)
- [Configuration](docs/configuration.md)
- [Security](docs/security.md)
- [Development](docs/development.md)
- [AI Research Plan](docs/ai-research.md)

The technical design distinguishes agreed behavior, proposed implementation details, optional extensions, and unresolved decisions.

## License

Licensed under the [Apache License 2.0](LICENSE).
