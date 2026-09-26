# LzyFlow — V1 Technical Design

**Revision:** 5  
**Status:** V1 design finalized and frozen; ready for implementation  
**Platform:** Linux-only for V1  
**Purpose:** Canonical V1 technical specification and implementation handoff

## 1. Purpose and product goal

LzyFlow is a local file-organization service that automatically keeps one or more configurable directories organized. The default use case is the user's Downloads directory, resolved through XDG user directories where possible, but arbitrary watched directories are supported through configuration.

The V1 product goal is:

> Automatically keep a configurable directory organized while preferring no filesystem action over an uncertain or potentially destructive action.

V1 must be a genuinely usable Linux desktop utility rather than a technical prototype. It is also designed so its core and application layers can later be embedded in Lzy-Brain or another .NET host.

The V1 architecture is finalized. Exact DTOs, model selection, confidence calibration, migration details, Linux library choices, the systemd unit, and installer mechanics are implementation decisions rather than unresolved product architecture.

## 2. Core principles

1. **Safety before automation.** If LzyFlow cannot establish that an action is safe, it leaves the source file untouched.
2. **One standalone authority.** `lzyflowd` owns runtime state, SQLite, configuration changes, filesystem mutations, and notifications.
3. **Deterministic filesystem boundaries.** Configuration selects the category and destination root. Classification may refine placement only inside that boundary.
4. **Persist only durable value.** SQLite stores state required after restart, directory identity, decisions, history, and undo information. Transient filesystem facts are derived when needed.
5. **Configuration is user intent.** TOML defines watches, rules, thresholds, destinations, cleanup, and feature toggles. SQLite does not duplicate the configuration.
6. **Shared application logic.** REST, CLI, TUI, a future MCP adapter, embedded hosts, and tests use the same application operations.
7. **Linux V1 with real seams.** Linux integration may be direct. Interfaces are introduced only for genuine platform boundaries.

## 3. V1 scope freeze

### 3.1 Included in V1

- Linux-only implementation.
- User-scoped `lzyflowd` service managed through `systemd --user`.
- XDG-compatible configuration, data, token, and cache locations.
- SQLite persistence and TOML configuration.
- Atomic configuration hot reload with validation and last-known-good fallback.
- Startup recovery and filesystem reconciliation.
- `FileSystemWatcher` for normal event-driven observation.
- Configurable watched directories and per-watch rules.
- Download stability monitoring.
- Deterministic routing.
- Lightweight and semantic classification.
- Configurable thresholds of `0.75` and `0.80`.
- Category-root, existing-directory, and proposed new-folder results.
- Manual review state with the source file left in place.
- Safe synchronous file moves and undo history.
- No implicit overwrite.
- Normalized directory identity and directory soft deletion.
- Filename, derived extension, source URL, referrer URL, and Linux origin metadata when available.
- Limited content extraction only when required by semantic classification.
- A normal CLI and an interactive TUI using the same application operations.
- A versioned localhost REST API.
- Per-user bearer-token authentication and managed-root filesystem authorization.
- Daemon-integrated native Linux notifications.
- Self-contained `linux-x64` distribution, single-file where practical, with a simple install and uninstall workflow.

### 3.2 Deferred beyond V1

- Windows and macOS implementations.
- GUI.
- AUR and other package-manager distribution.
- `linux-arm64` unless a concrete need appears.
- Remote or LAN REST access.
- A required MCP adapter. MCP compatibility remains an architectural direction but does not block the first usable release.
- Microservices, Redis, RabbitMQ, distributed workers, or background-job infrastructure for normal local moves.
- SHA-256 duplicate detection and general-purpose deduplication.
- Ranked classifier candidates and extensive model experimentation metadata.
- Embeddings or a vector database solely for file organization.
- Adaptive learning from corrections.
- Automatic numbered filename collision resolution.
- Aggressive cleanup.
- Arbitrary filesystem access.
- Complex audit or event sourcing.
- Periodic full reconciliation unless real-world testing proves it necessary.
- Notification batching and `after_days` cleanup.

## 4. Architecture

### 4.1 Standalone mode

```text
CLI / TUI
    |
    | localhost REST API
    v
lzyflowd
    |
    +-- file watching
    +-- stability detection
    +-- deterministic routing
    +-- classification pipeline
    +-- filesystem operations
    +-- notifications
    +-- SQLite
    +-- configuration
```

The CLI and TUI never edit SQLite or mutate managed filesystem state directly. They request operations through `lzyflowd`.

### 4.2 Embedded mode

```text
Lzy-Brain or another .NET host
    |
    v
LzyFlow application and core layers
```

An embedded host calls the application layer directly. It does not need the daemon, REST loopback, CLI, or TUI. The same application behavior and safety rules are reused.

### 4.3 Conceptual solution boundaries

The names below are illustrative rather than frozen:

- `LzyFlow.Core`: domain concepts and rules.
- `LzyFlow.Application`: use cases and workflows.
- `LzyFlow.Infrastructure`: SQLite, filesystem integration, classifiers, Linux metadata, notifications, and service integration.
- `LzyFlow.Daemon`: standalone runtime and REST host.
- `LzyFlow.Cli`: CLI and TUI presentation.

V1 may use ordinary .NET filesystem operations and `FileSystemWatcher` directly. Do not wrap `File.Move`, `Directory.CreateDirectory`, `File.Exists`, or similar calls merely to appear cross-platform. Genuine seams include `IDownloadOriginReader`, `INotificationService`, XDG or platform path resolution, and service installation or runtime integration.

## 5. CLI and TUI

Running `lzyflow` is expected to open the TUI. The TUI covers Dashboard, Review, History, Watched directories, Categories, Rules, Settings, and Service. It exposes no unique business logic.

The current CLI capability map is:

```text
lzyflow
lzyflow status [--json]
lzyflow review [--json]
lzyflow approve <file-id>
lzyflow resolve <file-id> ...
lzyflow ignore <file-id>
lzyflow history [--json]
lzyflow undo <operation-id>
lzyflow watch list|add|remove|enable|disable ...
lzyflow category list|add|rename|remove ...
lzyflow rule list|add|edit|remove ...
lzyflow config show|path|validate
lzyflow reconcile
lzyflow service install|start|stop|restart|status|uninstall
```

The capability set is settled. Exact parser grammar, flags, prompts, JSON fields, exit codes, key bindings, and screen layouts are implementation details. Guided prompts may complement explicit flags.

## 6. REST and MCP boundary

The V1 API is versioned and localhost-only:

```text
http://127.0.0.1:5274/api/v1
```

REST is the primary standalone control interface for the CLI, TUI, a future GUI, and other local integrations. It exposes application actions and state, not database rows.

MCP remains optional. A future MCP adapter calls the same application services as REST and must not duplicate business logic or make loopback HTTP requests when hosted in the same process. An agent receives bounded LzyFlow capabilities rather than arbitrary filesystem access.

## 7. Linux runtime and distribution

LzyFlow runs as the current user and does not require root privileges for normal operation.

```text
~/.config/lzyflow/config.toml
~/.local/share/lzyflow/lzyflow.db
~/.local/share/lzyflow/api.token
~/.cache/lzyflow/                  optional cache or temporary data
~/.local/bin/lzyflow
~/.local/bin/lzyflowd
```

The daemon runs as a `systemd --user` service. Logs initially go to the systemd journal:

```bash
journalctl --user -u lzyflow
```

Development may use `dotnet run`. The first release is self-contained for `linux-x64`, with single-file executables where practical. The installer places binaries in `~/.local/bin`, creates initial XDG directories and configuration, and installs the systemd user unit.

Conceptual startup order:

1. Load and validate configuration.
2. Open SQLite. If unavailable or unusable, disable autonomous filesystem mutations.
3. Recover interrupted states and operations.
4. Reconcile directories and managed roots.
5. Start filesystem watchers.
6. Start the localhost REST API.
7. Enter normal operation.

## 8. Security model

The API listens only on `127.0.0.1`. Binding to `0.0.0.0` or another externally reachable address is outside V1.

A per-user token is stored at:

```text
~/.local/share/lzyflow/api.token
```

with permissions such as `0600`. Clients send:

```http
Authorization: Bearer <token>
```

Two independent boundaries apply:

```text
Who may control LzyFlow?
    -> local bearer token

What may LzyFlow modify?
    -> configured managed roots and destinations
```

Authentication never grants arbitrary filesystem authority. Every source and destination path is normalized and checked against the active configured roots before mutation. The same rule applies to REST, CLI, TUI, and any future MCP adapter.

Authorization must be based on the canonical/real filesystem path, not only on lexical path prefixes. Symlinks and other path indirections must not allow a source or destination that appears to be inside a managed root to resolve outside it. For an existing path, LzyFlow resolves the real path before authorization. For a new child path, it resolves and authorizes the existing parent directory first, then appends only a validated single path segment. If canonical resolution is ambiguous or cannot be established safely, the operation is rejected.

## 9. File lifecycle

Current file states:

```text
detected
waiting_for_stability
stalled
ready
classifying
needs_review
approved
moving
completed
failed
ignored
```

### 9.1 Canonical V1 state transitions

Only the following transitions are valid in V1. Application code must not invent additional transitions without an explicit design update.

| From | Allowed next state(s) | Typical reason |
| --- | --- | --- |
| `detected` | `waiting_for_stability`, `ignored`, `failed` | Begin observation, ignore by rule, or fail before observation can continue. |
| `waiting_for_stability` | `ready`, `stalled`, `ignored`, `failed` | File becomes stable, stops progressing, is intentionally ignored, or becomes unavailable/unreadable. |
| `stalled` | `waiting_for_stability`, `ignored`, `failed` | Writing resumes, user/rule ignores the file, or monitoring can no longer continue. |
| `ready` | `classifying`, `approved`, `ignored`, `failed` | Start classification, accept a deterministic rule with no semantic classification, ignore, or fail before a destination is finalized. |
| `classifying` | `approved`, `needs_review`, `failed` | Classification succeeds confidently, remains uncertain, or fails. |
| `needs_review` | `approved`, `ignored`, `failed` | User approves/resolves the stored decision, ignores the file, or the file becomes unusable before resolution. |
| `approved` | `moving`, `needs_review`, `failed` | Start the validated move, return to review if the stored destination is no longer safe/valid, or fail validation. |
| `moving` | `completed`, `needs_review`, `failed` | Move completes, recovery finds an ambiguous state, or the mutation fails. |
| `completed` | `moving` | A validated undo or another explicit reverse move begins. |
| `failed` | _none automatically_ | Recovery or retry requires an explicit application action defined during implementation. |
| `ignored` | _none automatically_ | The file remains untouched unless a future explicit user action reintroduces it. |

Recovery may move `classifying` back to `ready` after restart because no classifier invocation survives process termination. `waiting_for_stability` resumes observation in place. These recovery transitions are part of the V1 recovery model rather than normal forward processing.

Normal flow:

1. Detect a file in an enabled watch and create or update its runtime record.
2. Wait for stability.
3. Use the matching per-watch rule to establish category and destination root.
4. If `classify = false`, prepare the deterministic destination-root move.
5. If `classify = true`, run lightweight classification and escalate only when needed.
6. Accept a sufficiently confident bounded result, or persist the suggestion and set `NeedsReview` while leaving the file in place.
7. Immediately before mutation, validate source existence, authorization, destination existence and active state, and filename availability.
8. Perform the move, update current state, and record historical source and destination snapshots.

## 10. File stability detection

Defaults:

```text
poll interval = 5 seconds
stable checks required = 3
stalled after = 600 seconds
```

Each check considers at least file size and last-write time. Known temporary download extensions such as `.crdownload` and `.part` remain unprocessed. A browser rename to the final name is useful evidence, but the final file still passes stability checks.

A single unchanged interval does not prove completion. After three consecutive unchanged checks the file becomes a stable candidate. Ten minutes without useful progress sets `stalled`, but never authorizes a move. If writing resumes, the file returns to `waiting_for_stability`.

Stability observations are transient runtime data and are not persisted in SQLite.

## 11. Progressive classification

### 11.1 Contract

```text
ClassificationContext
    FileName
    Extension
    SourceUrl
    ReferrerUrl
    CategoryKey
    ExistingFolders

Folder candidate
    directory ID
    display or folder name

ClassificationResult
    target = category_root | existing_folder | new_folder
    existing directory ID or proposed folder name
    confidence
```

The classifier receives normalized context and no arbitrary filesystem access. It returns semantic intent, never a path. V1 returns only the single best result plus confidence; ranked alternatives are deferred.

`category_root` is a valid result because the classifier must not be forced to create unnecessary subfolders.

### 11.2 Stages and thresholds

**Lightweight classification** may use extension, filename, source URL or domain, and referrer URL. The default acceptance threshold is `0.75`. Lower confidence escalates to semantic classification.

**Semantic classification** may use richer metadata, limited content extraction, and a small local model or classifier. The default threshold is `0.80`. Lower confidence becomes `NeedsReview`.

A stricter automatic new-folder threshold near `0.90` is proposed because changing directory structure is riskier than selecting an existing directory. The exact value is not finalized and is absent from the TOML baseline.

Confidence must be calibrated during implementation rather than trusted solely because a model reports it.

### 11.3 New-folder validation

A classifier proposes only a label. Deterministic validation rejects:

- empty names;
- absolute paths;
- `.` and `..`;
- path separators;
- traversal attempts;
- any name that cannot be safely created under the configured destination root.

### 11.4 Review behavior

Low confidence never moves the file to a Pending directory. The file remains at its current path. SQLite stores the fixed suggestion and confidence so `approve` executes the stored decision rather than rerunning classification.

If the stored target directory was deleted, approval becomes invalid and requires review. `resolve` applies the user's replacement decision while preserving the original classifier suggestion as history. `ignore` leaves the file in place and sets its file status to `ignored`.

## 12. Download-origin metadata

V1 uses `IDownloadOriginReader` to normalize Linux metadata such as:

```text
user.xdg.origin.url
user.xdg.referrer.url
```

The resulting `source_url` and `referrer_url` are persisted because metadata may be stripped or lost after movement. Availability depends on the browser or downloader, filesystem, copying behavior, removable or network storage, and metadata stripping.

Missing or unreadable metadata is not fatal. Classification continues with filename and extension. Windows `Zone.Identifier` and macOS `kMDItemWhereFroms` are known future seams but have no V1 implementations.

## 13. Categories watches rules destinations and cleanup

- A category has a stable `key` and a mutable presentation `label`.
- A watch has a stable `id`, path, enabled state, and its own rules.
- A rule has a stable `id` within its watch.
- Each rule maps extensions to a category and destination.
- Category identity and destination path are separate concepts.
- `classify = false` moves to the deterministic destination root.
- `classify = true` allows semantic placement beneath the destination root.
- Ambiguous duplicate extension mappings within one watch are rejected instead of introducing rule priorities.
- Cleanup is conceptually a destination policy and is stored on the rule.
- V1 examples use `never` and `on_startup`.
- Cleanup may delete only files LzyFlow can identify through its history as files it placed there. It never empties a directory blindly.
- `after_days` is deferred.

## 14. Directory identity soft deletion and reconciliation

Directories are first-class database entities. A file's current full path is:

```text
directories.path + files.file_name
```

Renaming a directory changes one `directories.path` value instead of every associated file row. Historical `file_operations.source_path` and `destination_path` values remain text snapshots.

Normal event-driven behavior:

- known directory renamed -> update `directories.path`;
- new directory under a managed destination -> register it and make it an active classifier candidate;
- known directory deleted -> set `is_deleted = true` and `deleted_at = now`;
- directory moved outside a managed destination -> stop treating it as an active managed candidate.

Classifier queries use only `is_deleted = false` directories.

Soft-deleted directories remain for approximately 30 days and may then be purged. The retention period may become configurable later. A later directory created at the same path normally receives a new identity.

Active directory paths must be unique, preferably through a partial SQLite unique index where `is_deleted = false`. Permanent purge may use `ON DELETE SET NULL` for historical `classifications.suggested_directory_id`; this is finalized in the migration audit.

Reconciliation runs at daemon startup, after relevant configuration reloads, and through an explicit manual command if exposed. Periodic full reconciliation is deferred. If a rename happened while the daemon was offline and identity cannot be established reliably, V1 treats the old directory as deleted and the discovered directory as new.

If a daemon-driven directory rename is exposed, the filesystem change occurs before the database path update. Folder-rename undo is not required for V1; file-move undo remains required.

## 15. Filesystem operations failure and recovery

`approve`, `resolve`, `ignore`, and `undo` normally execute synchronously:

```text
REST request
    -> read persistent state
    -> validate current filesystem and authorization
    -> perform mutation
    -> update SQLite and history
    -> return result
```

`file_operations` is audit and history, not a queue. A downloading file may have `files.status = waiting_for_stability` without any operation row. A row is written only for an attempted filesystem mutation, and its final status is either `completed` or `failed`.

Required failure behavior:

- Low confidence -> `NeedsReview`, source untouched.
- Classifier exception -> `failed`, source untouched.
- Semantic classifier unavailable -> daemon remains alive; lightweight classification continues where possible; semantic-required work becomes review or degraded.
- Destination filename exists -> `NeedsReview`; no overwrite and no automatic `file (1).pdf` naming.
- Destination root or mount is unavailable -> do not recreate it blindly; source remains untouched.
- Target directory deleted after classification -> stored decision is invalid; do not silently reclassify during approval.
- SQLite unavailable or unusable -> daemon is unhealthy or degraded and autonomous filesystem mutations are disabled.

### 15.1 Crash recovery

Conceptual move sequence:

```text
1. validate operation
2. files.status = moving
3. perform filesystem move
4. update current file and directory state
5. record operation outcome
6. files.status = completed
```

Startup recovery for an interrupted move:

| Filesystem reality | Recovery |
| --- | --- |
| Source missing, destination exists | Treat as likely success and repair SQLite state and history. |
| Source exists, destination missing | Treat as not completed and restore a suitable processing state. |
| Both exist | Ambiguous; set `NeedsReview` and delete neither. |
| Neither exists | Mark failed and surface the missing expected state. |

`waiting_for_stability` resumes monitoring. `classifying` returns to a suitable pre-classification state because no classifier invocation survives restart.

## 16. Notifications

Notifications run inside `lzyflowd` through `INotificationService` and a native Linux implementation. An in-process event reacts to state changes such as entering `NeedsReview`; no extra process watches a Pending directory.

V1 supports enable or disable and immediate delivery. Batching is deferred. A separate user-session notifier is reconsidered only if a future deployment becomes a system-level daemon outside the logged-in session.

## 17. SQLite state and history

SQLite stores only data required after restart or for directory identity, classification history, debugging, recovery, and undo. The V1 schema has exactly five tables:

```text
categories
directories
files
classifications
file_operations
```

Current-state paths are normalized through `files.directory_id`. Historical operation paths remain stored strings.

### 17.1 Final V1 DBML

```dbml
Enum file_status {
  detected
  waiting_for_stability
  stalled
  ready
  classifying
  needs_review
  approved
  moving
  completed
  failed
  ignored
}

Enum classification_stage {
  lightweight
  semantic
}

Enum classification_status {
  accepted
  escalated
  needs_review
}

Enum classification_target {
  category_root
  existing_folder
  new_folder
}

Enum operation_status {
  completed
  failed
}

Table categories {
  id integer [pk, increment]
  key text [not null, unique]
}

Table directories {
  id integer [pk, increment]

  category_id integer
  path text [not null]

  is_deleted boolean [not null, default: false]
  deleted_at datetime

  indexes {
    category_id
    is_deleted
  }
}

Table files {
  id integer [pk, increment]

  file_name text [not null]
  directory_id integer [not null]

  status file_status [not null]

  source_url text
  referrer_url text

  detected_at datetime [not null]
  updated_at datetime [not null]

  indexes {
    directory_id
    status
  }
}

Table classifications {
  id integer [pk, increment]

  file_id integer [not null]
  category_id integer

  stage classification_stage [not null]
  status classification_status [not null]

  target classification_target

  suggested_directory_id integer
  suggested_folder_name text

  confidence real

  indexes {
    file_id
  }
}

Table file_operations {
  id integer [pk, increment]

  file_id integer [not null]

  source_path text [not null]
  destination_path text [not null]

  status operation_status [not null]
  error_message text

  created_at datetime [not null]
  finished_at datetime

  indexes {
    file_id
    status
  }
}

Ref: directories.category_id > categories.id

Ref: files.directory_id > directories.id

Ref: classifications.file_id > files.id
Ref: classifications.category_id > categories.id
Ref: classifications.suggested_directory_id > directories.id

Ref: file_operations.file_id > files.id
```

### 17.2 Schema semantics

- `categories` stores only normalized stable category identity. Labels and settings remain in TOML.
- `directories.category_id` is nullable so a known source directory such as Downloads need not be a semantic destination.
- `directories.is_deleted` and `deleted_at` implement soft deletion.
- `files.file_name` plus `files.directory_id` defines current location.
- `source_url` and `referrer_url` preserve optional origin evidence.
- One file may have multiple `classifications` rows, for example lightweight `escalated` followed by semantic `accepted` or `needs_review`.
- `suggested_directory_id` references an existing candidate. `suggested_folder_name` stores a proposed new label.
- `file_operations` stores historical source and destination snapshots and may record failures.
- Undo is another validated reverse move rather than a parent-operation graph.
- The schema intentionally has no `review_items`, `folder_revisions`, `config_revisions`, or general event table.
- `operation_status` has only `completed` and `failed`. Download waiting and review state belong to `files.status` and `classifications.status`; they never create an operation row by themselves.

### 17.3 Migration implementation audit

Before the first migration is frozen, review:

- partial uniqueness for active directory paths;
- foreign-key deletion behavior, especially `ON DELETE SET NULL` after directory purge;
- recovery and debugging timestamps;
- indexes;
- consistency of state transitions with crash recovery.

Any schema change requires an explicit design update rather than undocumented drift.

## 18. Configuration

TOML is the source of user intent. Stable category, watch, and rule identifiers allow labels and paths to change without breaking references.

The Downloads path below is an editable example. Initial Linux configuration should write the XDG-resolved Downloads path when available.

### 18.1 Current TOML baseline

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

The proposed new-folder threshold is intentionally absent because its exact value is not finalized.

### 18.2 Validation and hot reload

The daemon parses and validates the entire candidate before replacing the active configuration. If any part fails, it rejects the candidate, reports the error, and continues with the last-known-good configuration. No valid subset of an invalid file is applied.

Validation includes:

- every rule category exists;
- watch IDs are unique;
- rule IDs are unique within a watch;
- extensions are valid and not duplicated within one watch;
- destinations are valid;
- thresholds are between `0.0` and `1.0`;
- `stable_checks_required >= 1`;
- `poll_interval_seconds > 0`;
- cleanup modes and their parameters are valid;
- V1 API binding remains localhost-only.

Manual edits are detected by file watching. CLI changes use:

```text
CLI -> REST API -> daemon validation -> config write -> active configuration
```

Database and token paths are absent from TOML because XDG locations are part of the runtime model. Logs go to journald.

## 19. Current REST surface

| Method and route | Purpose |
| --- | --- |
| `GET /status` | Return health and current counts. |
| `GET /files?status={status}` | Query files by lifecycle state. |
| `GET /files/{id}` | Inspect one file and its classifications. |
| `POST /files/{id}/approve` | Execute the stored suggestion without reclassification. |
| `POST /files/{id}/resolve` | Apply the user's replacement destination decision. |
| `POST /files/{id}/ignore` | Leave the file in place and mark it ignored. |
| `GET /operations` | List operation history, with filters such as `status` and `fileId`. |
| `GET /operations/{id}` | Inspect one operation. |
| `POST /operations/{id}/undo` | Validate and attempt the reverse move. |
| `GET, POST /watches` | List or create watches through validated configuration. |
| `PATCH, DELETE /watches/{id}` | Update or remove a watch. |
| `GET, POST /categories` | List or create categories. |
| `PATCH, DELETE /categories/{key}` | Update or remove a category by stable key. |
| `GET, POST /watches/{watchId}/rules` | List or create rules for a watch. |
| `PATCH, DELETE /watches/{watchId}/rules/{ruleId}` | Update or remove a rule. |
| `GET /config` | Return effective configuration. |
| `POST /config/validate` | Validate a candidate atomically. |

An explicit reload endpoint is unnecessary because manual TOML edits already trigger validated hot reload.

`approve`, `resolve`, `ignore`, and `undo` are synchronous for normal local operations. `approve` executes the fixed stored decision. `resolve` records the user's replacement. `ignore` performs no filesystem mutation. `undo` reverses `destination_path` to `source_path` only after validation and never overwrites a conflict.

Proposed status semantics include `200`, `201`, `204`, `400`, `401`, `403`, `404`, `409`, `422`, and `500`. Exact DTOs, pagination, response envelopes, validation bodies, and final status mapping are implementation decisions.

## 20. Implementation decisions deferred to development

The following choices do not reopen the V1 architecture:

- exact REST DTOs, pagination, validation bodies, and concurrency or idempotency behavior where required;
- exact CLI flags, prompts, JSON output, exit codes, and TUI key bindings;
- concrete lightweight and semantic classifiers;
- confidence derivation and calibration;
- exact new-folder threshold near `0.90`;
- xattr and native notification libraries;
- XDG resolver details;
- systemd unit contents;
- install and uninstall script mechanics;
- final SQLite indexes, timestamps, and foreign-key actions;
- future MCP tools, transport, and permissions.

## 21. Recommended implementation sequence

1. Create the .NET solution boundaries and shared application contracts without unused platform implementations.
2. Implement XDG paths, TOML parsing and hot reload, bearer-token creation, and SQLite migrations.
3. Implement daemon lifecycle, `FileSystemWatcher`, stability states, directory registration, and reconciliation.
4. Implement deterministic routing, classifier interfaces, review state, safe path resolution, and synchronous moves.
5. Add crash recovery, collision handling, unavailable-destination behavior, database-failure safeguards, and native notifications.
6. Expose the REST API and build the CLI and TUI as thin clients.
7. Publish self-contained `linux-x64` binaries and test install, service startup, journal diagnostics, and uninstall on a clean user account.

## 22. Final V1 design principle

> LzyFlow should prefer leaving a file untouched over taking an autonomous action whose correctness or safety it cannot establish with sufficient confidence.
