# Development

## Requirements

LzyFlow currently targets:

- Linux
- .NET 10 SDK

## Restore, build, and test

From the repository root:

```bash
dotnet restore LzyFlow.slnx
dotnet format LzyFlow.slnx --verify-no-changes --no-restore
dotnet build LzyFlow.slnx --configuration Release --no-restore
dotnet test LzyFlow.slnx --configuration Release --no-build --no-restore
```

During normal development, the shorter commands are also sufficient:

```bash
dotnet build LzyFlow.slnx
dotnet test LzyFlow.slnx
```

To automatically apply formatting fixes:

```bash
dotnet format LzyFlow.slnx
```

## Test structure

The test suite is separated by architectural responsibility.

```text
tests/
├── LzyFlow.Core.Tests/
├── LzyFlow.Application.Tests/
├── LzyFlow.Infrastructure.Tests/
└── LzyFlow.IntegrationTests/
```

### Core tests

`LzyFlow.Core.Tests` contains tests for pure domain behavior and architectural boundaries.

Core tests must not depend on:

- Application
- Infrastructure
- Daemon
- CLI
- real filesystem state
- SQLite
- external services

### Application tests

`LzyFlow.Application.Tests` contains tests for application-layer behavior, contracts, orchestration, and architectural boundaries.

Application tests may depend on Core, but must remain independent from Infrastructure, Daemon, and CLI implementations.

### Infrastructure tests

`LzyFlow.Infrastructure.Tests` tests concrete infrastructure implementations.

Infrastructure tests must isolate external state. They must not read or modify the developer's real LzyFlow configuration, data directories, cache directories, or home-directory contents.

The Linux XDG path-resolution tests use an isolated environment abstraction rather than changing process-wide environment variables or accessing the developer's actual XDG configuration.

### Integration tests

`LzyFlow.IntegrationTests` may compose multiple layers and use real local infrastructure where necessary.

Integration tests may use:

- isolated temporary filesystem directories;
- temporary SQLite databases;
- composed Application and Infrastructure components;
- later, daemon-level integration where appropriate.

Integration tests must never depend on the developer's real:

```text
~/.config/lzyflow
~/.local/share/lzyflow
~/.cache/lzyflow
```

Temporary resources must be isolated per test and cleaned up after execution.

## Temporary filesystem fixture

Integration tests use isolated directories beneath the operating system's temporary directory.

Each fixture receives its own unique directory and must reject paths that escape that directory.

This allows filesystem behavior to be tested without modifying the user's actual files.

## Temporary SQLite fixture

SQLite integration tests use a database created inside an isolated temporary directory.

Tests must never use the real LzyFlow database:

```text
~/.local/share/lzyflow/lzyflow.db
```

The temporary database fixture can later be reused by migration, repository, recovery, and persistence tests.

## Fast tests and slower evaluation

The normal test suite is intended to remain fast enough for pull requests and routine local development.

The following workflows remain separate from the normal `dotnet test` baseline:

- containerized end-to-end testing;
- semantic-classifier evaluation datasets;
- model benchmarks;
- long-running research experiments;
- distribution and installation testing.

These may run in separate workflows as the project develops.

## Continuous integration

GitHub Actions runs the baseline CI workflow for pull requests and pushes to `main`.

The current pipeline performs:

```text
restore
    ↓
format verification
    ↓
Release build
    ↓
tests
```

The commands are equivalent to:

```bash
dotnet restore LzyFlow.slnx
dotnet format LzyFlow.slnx --verify-no-changes --no-restore
dotnet build LzyFlow.slnx --configuration Release --no-restore
dotnet test LzyFlow.slnx --configuration Release --no-build --no-restore
```

A formatting failure, build failure, or test failure causes the workflow to fail.

## Running individual test projects

A single test project can be run directly when working on one layer.

Examples:

```bash
dotnet test tests/LzyFlow.Core.Tests/LzyFlow.Core.Tests.csproj
dotnet test tests/LzyFlow.Application.Tests/LzyFlow.Application.Tests.csproj
dotnet test tests/LzyFlow.Infrastructure.Tests/LzyFlow.Infrastructure.Tests.csproj
dotnet test tests/LzyFlow.IntegrationTests/LzyFlow.IntegrationTests.csproj
```

Before opening a pull request, run the complete solution test suite.