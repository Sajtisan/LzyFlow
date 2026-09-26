# Configuration

LzyFlow uses **TOML** as the source of user configuration.

The default configuration file is:

```text
~/.config/lzyflow/config.toml
```

Initial Linux setup should resolve the user's XDG Downloads directory when possible instead of assuming `~/Downloads`.

## Principles

- Configuration expresses user intent.
- Stable category, watch, and rule identifiers should not change when labels or paths change.
- Configuration updates are validated atomically.
- Invalid changes are rejected entirely.
- The daemon continues using the last-known-good configuration after a failed reload.
- SQLite does not duplicate configuration that belongs in TOML.

## Reference

### General

```toml
[general]
poll_interval_seconds = 5
stable_checks_required = 3
stalled_after_seconds = 600
```

| Option | Description |
| --- | --- |
| `poll_interval_seconds` | Delay between file-stability checks. Must be greater than `0`. |
| `stable_checks_required` | Consecutive unchanged checks required before a file may be considered stable. Must be at least `1`. |
| `stalled_after_seconds` | Time without useful progress before a file is marked stalled. Stalling never authorizes a move. |

### Classification

```toml
[classification]
lightweight_threshold = 0.75
semantic_threshold = 0.80
```

| Option | Description |
| --- | --- |
| `lightweight_threshold` | Minimum confidence required to accept a lightweight classification result. |
| `semantic_threshold` | Minimum confidence required to accept a semantic classification result. |

Both values must be between `0.0` and `1.0`.

A stricter threshold for automatic new-folder creation is planned, but its exact V1 value is intentionally not part of the configuration contract yet.

### Notifications

```toml
[notifications]
enabled = true
mode = "immediate"
```

V1 supports immediate native Linux notifications.

Notifications are intended for states that require attention, such as `NeedsReview`.

### Local API

```toml
[api]
enabled = true
host = "127.0.0.1"
port = 5274
```

The V1 REST API is local-only.

`host` must remain bound to `127.0.0.1`. Remote or LAN exposure is outside V1.

### MCP

```toml
[mcp]
enabled = false
```

MCP is an architectural extension point and does not block V1. The adapter is disabled by default and is not required for the first usable release.

## Categories

Categories use a stable machine-readable key and a mutable display label.

```toml
[[categories]]
key = "documents"
label = "Documents"
```

The key is the stable identity referenced by rules.

Changing a label must not require changing the key.

## Watches

Each watched directory has a stable ID.

```toml
[[watches]]
id = "downloads"
path = "~/Downloads"
enabled = true
```

| Option | Description |
| --- | --- |
| `id` | Stable watch identifier. |
| `path` | Directory observed by LzyFlow. |
| `enabled` | Whether the watch is active. |

Watch IDs must be unique.

## Rules

Rules belong to a watch.

```toml
[[watches.rules]]
id = "documents"
extensions = [".pdf", ".docx", ".txt"]
category = "documents"
destination = "~/Documents"
classify = true
cleanup = { mode = "never" }
```

| Option | Description |
| --- | --- |
| `id` | Stable rule identifier within the watch. |
| `extensions` | Extensions matched by the rule. |
| `category` | Stable category key. |
| `destination` | Allowed destination root. |
| `classify` | Whether semantic placement beneath the destination root is allowed. |
| `cleanup` | Cleanup policy associated with the destination. |

When `classify = false`, routing is deterministic and the file is placed at the configured destination root.

When `classify = true`, classification may choose the category root, an existing active directory beneath it, or a proposed new folder. The classifier never receives authority to choose an arbitrary filesystem path.

Within a single watch, duplicate or ambiguous extension mappings are rejected instead of introducing rule priorities.

## Cleanup

V1 supports:

```toml
cleanup = { mode = "never" }
```

and:

```toml
cleanup = { mode = "on_startup" }
```

Cleanup may remove only files LzyFlow can identify through its own history as files it placed there.

LzyFlow must never blindly empty a configured directory.

`after_days` cleanup is deferred beyond V1.

## Complete Example

```toml
# LzyFlow configuration

[general]
poll_interval_seconds = 5
stable_checks_required = 3
stalled_after_seconds = 600

[classification]
lightweight_threshold = 0.75
semantic_threshold = 0.80

[notifications]
enabled = true
mode = "immediate"

[api]
enabled = true
host = "127.0.0.1"
port = 5274

[mcp]
enabled = false

[[categories]]
key = "documents"
label = "Documents"

[[categories]]
key = "installers"
label = "Installers"

[[categories]]
key = "images"
label = "Images"

[[watches]]
id = "downloads"
path = "~/Downloads"
enabled = true

    [[watches.rules]]
    id = "documents"
    extensions = [".pdf", ".docx", ".txt"]
    category = "documents"
    destination = "~/Documents"
    classify = true
    cleanup = { mode = "never" }

    [[watches.rules]]
    id = "installers"
    extensions = [".exe", ".msi"]
    category = "installers"
    destination = "~/LzyFlow/Temp/Installers"
    classify = false
    cleanup = { mode = "on_startup" }

    [[watches.rules]]
    id = "images"
    extensions = [".png", ".jpg", ".jpeg", ".webp"]
    category = "images"
    destination = "~/Pictures"
    classify = true
    cleanup = { mode = "never" }
```

## Validation

A candidate configuration is accepted only if the complete file validates.

Validation includes:

- every referenced category exists;
- watch IDs are unique;
- rule IDs are unique within their watch;
- extensions are valid;
- extensions are not ambiguously duplicated within one watch;
- destinations are valid;
- thresholds are between `0.0` and `1.0`;
- `stable_checks_required >= 1`;
- `poll_interval_seconds > 0`;
- cleanup modes and parameters are valid;
- the V1 API remains localhost-only.

No valid subset of an invalid configuration is applied.

## Hot Reload

Manual edits to `config.toml` are watched by the daemon.

The reload flow is:

```text
file changed
    -> parse complete candidate
    -> validate complete candidate
    -> apply atomically if valid
    -> otherwise keep last-known-good configuration
```

CLI configuration changes follow the daemon boundary:

```text
CLI -> REST API -> daemon validation -> config write -> active configuration
```

Database, token, and log locations are runtime concerns and are intentionally absent from the TOML configuration.
