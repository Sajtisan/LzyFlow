# LzyFlow — V1 Technical Design

**Revision:** 6<br>
**Status:** Product V1 architecture finalized; BSc research profile and classifier evaluation contract added<br>
**Platform:** Linux-only for V1  
**Purpose:** Canonical V1 technical specification and implementation handoff

## 1. Purpose and product goal

LzyFlow is a local file-organization service that automatically keeps one or more configurable directories organized. The default use case is the user's Downloads directory, resolved through XDG user directories where possible, but arbitrary watched directories are supported through configuration.

The V1 product goal is:

> Automatically keep a configurable directory organized while preferring no filesystem action over an uncertain or potentially destructive action.

V1 must be a genuinely usable Linux desktop utility rather than a technical prototype. It is also designed so its core and application layers can later be embedded in Lzy-Brain or another .NET host.

The Product V1 architecture is finalized. Exact DTOs, model selection, the concrete confidence-calibration algorithm, migration details, Linux library choices, the systemd unit, and installer mechanics remain implementation decisions. Sections 23-31 define the bounded BSc research profile and the normative evaluation and verification contracts that constrain those choices.

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
- Configurable provisional thresholds, initially `0.75` and `0.80`, calibrated and frozen through the process in Section 26 before final evaluation or release defaults are claimed.
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
- Ranked classifier candidates in user-facing Product V1 APIs. The offline evaluation runner may retain rankings and experiment provenance required by Sections 25-29.
- A general-purpose vector database or persistent note-embedding subsystem. A bounded local embedding model may be used by the semantic classifier without introducing a vector database.
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

The research and provenance extension of this contract is defined in Section 25. Product DTOs may expose a smaller projection, but application decisions retain classifier identity, version, score, confidence, and reason codes.

### 11.2 Stages and thresholds

**Lightweight classification** may use extension, filename, source URL or domain, and referrer URL. The default acceptance threshold is `0.75`. Lower confidence escalates to semantic classification.

**Semantic classification** may use richer metadata, limited content extraction, and a small local model or classifier. The default threshold is `0.80`. Lower confidence becomes `NeedsReview`.

A stricter automatic new-folder threshold near `0.90` is proposed because changing directory structure is riskier than selecting an existing directory. The exact value is not finalized and is absent from the TOML baseline.

Confidence must be calibrated during implementation rather than trusted solely because a model reports it.

Thresholds are policy parameters rather than inherent model truth. Their calibration, margin requirement, and final selection follow Section 26. Final evaluation must not tune thresholds on the held-out test split.

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

  classifier_key text
  classifier_version text
  preprocessing_version text
  input_fingerprint text
  raw_score real
  runner_up_raw_score real
  reason_codes text
  created_at datetime [not null]

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
- Classifier, preprocessing, score, fingerprint, and reason-code fields preserve the minimum decision provenance required for reproducible evaluation and debugging. They never store extracted document text.
- `file_operations` stores historical source and destination snapshots and may record failures.
- Undo is another validated reverse move rather than a parent-operation graph.
- The schema intentionally has no `review_items`, `folder_revisions`, `config_revisions`, or general event table.
- `operation_status` has only `completed` and `failed`. Download waiting and review state belong to `files.status` and `classifications.status`; they never create an operation row by themselves.

### 17.3 Migration implementation audit

Before the first migration is frozen, review:

- partial uniqueness for active directory paths;
- foreign-key deletion behavior, especially `ON DELETE SET NULL` after directory purge;
- recovery and debugging timestamps;
- migration defaults and nullability for classification-provenance fields;
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
minimum_candidate_margin = 0.05
threshold_profile = "default-v1"

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
- `minimum_candidate_margin` is between `0.0` and `1.0`;
- `threshold_profile` is non-empty;
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
- concrete lightweight and semantic classifier implementations, subject to the interface and evaluation contract in Sections 25-27;
- concrete calibration algorithm, subject to the data-separation and reporting rules in Section 26;
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
7. Add the offline evaluation runner, freeze the dataset splits, calibrate the decision policy, and produce reproducible comparison and error-analysis reports.
8. Publish self-contained `linux-x64` binaries and test install, service startup, journal diagnostics, and uninstall on a clean user account.

## 22. Final V1 design principle

> LzyFlow should prefer leaving a file untouched over taking an autonomous action whose correctness or safety it cannot establish with sufficient confidence.

---

## 23. Product V1 and BSc research profile

The Product V1 scope in Section 3 remains the long-term first release. The BSc implementation is a deliberately smaller executable profile of that architecture.

The research profile exists to answer one technical question in sufficient depth:

> How can a local file organizer trade automation coverage for a lower risk of incorrect filesystem actions by combining deterministic rules, semantic classification, confidence-based rejection, and human review?

The BSc profile is not a separate architecture. It uses the same core rules, filesystem boundaries, state model, and application operations as Product V1. Features omitted from the profile remain Product V1 work rather than deleted requirements.

### 23.1 Required BSc profile

The required profile includes:

- Linux-only execution;
- one configured watched directory;
- a fixed set of categories and existing destination directories;
- file-event deduplication and stability detection;
- deterministic extension and metadata rules;
- one lightweight local semantic classifier;
- confidence-based acceptance or rejection;
- manual review with the source file left in place;
- safe synchronous moves with no implicit overwrite;
- operation history and validated undo;
- SQLite persistence for durable state;
- startup recovery for interrupted file moves;
- one thin interaction surface, initially the CLI;
- an offline evaluation runner using the same classification application service;
- automated unit, integration, recovery, and safety tests.

### 23.2 Optional stretch profile

The following work is implemented only after every required acceptance criterion in Section 31 passes:

- multiple watched directories;
- interactive TUI;
- configuration hot reload;
- Linux origin metadata beyond filename and extension;
- native notifications;
- `systemd --user` installation;
- self-contained packaging;
- proposed new-folder classifications.

### 23.3 Excluded from the BSc profile

The following Product V1 or post-V1 capabilities are outside the thesis implementation:

- GUI, Windows, macOS, and mobile clients;
- MCP integration;
- remote or LAN APIs;
- generative language models;
- automatic summarization or metadata generation;
- dynamically generated taxonomies;
- semantic duplicate merging;
- adaptive learning from user corrections;
- automatic cleanup and deletion;
- Lzy-Brain knowledge-base, task, calendar, and study modules.

### 23.4 Hosting profile

The research implementation may run in either of these forms:

1. **Standalone Product V1 host:** `lzyflowd` owns state and the CLI calls the localhost API.
2. **Research host:** a foreground executable embeds `LzyFlow.Application` directly and exposes the required CLI operations without REST.

The research host is allowed because it follows the embedded-mode boundary in Section 4.2. It must not duplicate business rules or bypass filesystem authorization. The choice of host does not affect classifier evaluation because both hosts call the same application service.

REST authentication, service installation, and distribution must not block completion of the research profile.

---

## 24. Research questions and hypotheses

### 24.1 Research questions

**RQ1 - Classification quality**

Does local semantic classification improve destination prediction over deterministic extension, filename, and metadata rules for a fixed directory taxonomy?

**RQ2 - Selective automation**

How does changing the automatic-acceptance threshold affect classification risk, automation coverage, and the number of files requiring review?

**RQ3 - Operational safety**

Can the system preserve its filesystem safety invariants during filename collisions, unavailable destinations, duplicate watcher events, interrupted moves, and process restarts?

### 24.2 Testable hypotheses

**H1**

The hybrid classifier achieves a higher macro-averaged F1 score than the deterministic rule baseline on the held-out test set.

**H2**

Rejecting low-confidence predictions reduces the error rate among automatically accepted decisions, at the cost of lower automation coverage.

**H3**

The safety and recovery test suite produces zero implicit overwrites, zero unauthorized writes, and zero automatic deletions in ambiguous recovery states.

### 24.3 Interpretation boundary

Embedding similarity is deterministic for a fixed runtime, model, input, and preprocessing pipeline. It is not proof that a semantic decision is correct.

The design must not describe a cosine-similarity score as a probability or as a percentage of correctness. A score becomes a usable confidence value only after calibration and validation on representative data.

The thesis evaluates the implemented models and dataset. It does not claim universal classification performance for arbitrary users, languages, file types, or taxonomies.

---

## 25. Classification and decision contract

Section 11 defines the Product V1 classification shape. The research profile extends it with provenance and an explicit decision policy.

### 25.1 Input contract

```text
ClassificationContext
    FileName
    Extension
    SourceUrl?                optional
    ReferrerUrl?              optional
    BoundedExtractedText?     optional
    CategoryKey
    CandidateDirectories[]
    PreprocessingVersion
```

The classifier receives only normalized data. It has no filesystem mutation capability.

`CandidateDirectories` contains stable directory IDs, display names, and optional descriptions. Every candidate must be active and located beneath the deterministic destination root selected by configuration.

### 25.2 Output contract

```text
ClassificationPrediction
    Target                    category_root | existing_folder | new_folder
    CandidateDirectoryId?     required for existing_folder
    ProposedFolderName?       required for new_folder
    RawScore
    CalibratedConfidence?     absent when no calibrator exists
    RunnerUpRawScore?
    ClassifierKey
    ClassifierVersion
    PreprocessingVersion
    InputFingerprint
    ReasonCodes[]
```

`RawScore` is the native output used to rank candidates. It is not necessarily a probability.

`CalibratedConfidence`, when available, estimates the likelihood that the selected prediction is correct under the calibration distribution. The calibration method and dataset version are part of `ClassifierVersion`.

`InputFingerprint` is a cryptographic digest of the normalized classifier input used to detect accidental experiment drift. It must not contain source document text and must not be presented as anonymization, because equal or low-entropy inputs may still be inferable.

`ReasonCodes` are bounded machine-readable values such as:

```text
extension_rule
source_domain_rule
filename_token_match
semantic_top_candidate
below_acceptance_threshold
insufficient_candidate_margin
semantic_classifier_unavailable
unsupported_content_type
content_extraction_failed
```

Reason codes support diagnostics and evaluation. They are not natural-language explanations generated by a model.

### 25.3 Decision policy

The decision policy is owned by the application layer, not by a classifier implementation.

The BSc profile applies this order:

1. A matching deterministic rule establishes the category and destination root.
2. If `classify = false`, the policy selects the category root after normal safety validation.
3. If `classify = true`, the lightweight semantic classifier ranks only active candidates beneath that root.
4. An existing-directory prediction may be accepted only when:
   - the calibrated confidence or approved raw threshold is met;
   - the difference between the best and second-best candidates meets the configured minimum margin;
   - the target still exists and remains authorized;
   - the destination filename is available.
5. Otherwise, the prediction becomes `needs_review` and the source remains untouched.
6. New-folder proposals always require review in the BSc profile.
7. If the semantic classifier is unavailable, only an already-complete deterministic decision may move automatically.

The policy must record the exact fixed prediction presented for review. Approval does not rerun classification.

### 25.4 Bounded content extraction

Content extraction is optional evidence and must remain bounded.

For each supported type, configuration or constants define:

- maximum input file size;
- maximum extracted character or token count;
- extraction timeout;
- supported encodings;
- extractor key and version;
- behavior for encrypted, malformed, or unsupported files.

The required research profile supports plain text and Markdown. PDF extraction is optional. OCR, archive recursion, office-document macros, and executable inspection are excluded.

Extraction failure never prevents deterministic routing. It adds a reason code and causes semantic-required work to fall back to review.

### 25.5 Classifier variants

The evaluation compares these variants through the same interface:

| Key | Variant | Evidence | Purpose |
| --- | --- | --- | --- |
| `b0_root` | Category-root baseline | Extension only | Lower reference point |
| `b1_rules` | Deterministic rules | Extension, filename, optional origin | Non-semantic product baseline |
| `b2_semantic` | Semantic classifier | Local document and candidate embeddings | Isolate semantic contribution |
| `b3_hybrid` | Hybrid selective classifier | Rules, embeddings, threshold, margin | Proposed operating policy |

Model selection is an implementation decision, but every evaluated model must:

- run locally after installation;
- have a recorded model identifier and immutable version;
- have a license compatible with the intended project distribution;
- fit within the declared resource budget;
- produce repeatable embeddings for fixed inputs within the supported runtime;
- be evaluated before becoming the default.

---

## 26. Confidence calibration and threshold selection

The fixed `0.75` and `0.80` values in Section 11 are configuration defaults, not evidence that a prediction is correct.

### 26.1 Data split

The dataset is divided before threshold selection:

```text
development data
    -> implementation and qualitative inspection

calibration data
    -> threshold, margin, and optional score calibration

held-out test data
    -> final reported results only
```

Near-duplicate files, renamed copies, and document versions must remain in the same split to reduce leakage.

### 26.2 Calibration procedure

The initial BSc implementation may use empirical threshold calibration without training a separate model:

1. Run the frozen classifier on the calibration split.
2. Sort predictions by raw score.
3. Measure selective risk and coverage at each candidate threshold.
4. Select the lowest threshold that satisfies the predeclared maximum accepted risk or minimum automatic precision.
5. Select a minimum top-one versus top-two margin using the same calibration split.
6. Freeze the policy before evaluating the test split.

If the calibration dataset is sufficiently large, a monotonic calibration method may be compared. The uncalibrated and calibrated results must both be reported. A calibration method must never be fitted on the final test set.

### 26.3 Risk-coverage definitions

For a threshold `t`:

```text
accepted(t) = predictions whose score and margin satisfy policy at t

coverage(t) = |accepted(t)| / |all predictions|

selective_risk(t) = incorrect accepted predictions / |accepted(t)|

automatic_precision(t) = 1 - selective_risk(t)
```

An empty accepted set has undefined selective risk and must not be reported as perfect performance.

The primary result is the risk-coverage curve rather than a single uncontextualized accuracy value.

### 26.4 Configuration update

The classification configuration should distinguish score, confidence, and margin:

```toml
[classification]
lightweight_threshold = 0.75
semantic_threshold = 0.80
minimum_candidate_margin = 0.05
threshold_profile = "default-v1"
```

The exact numeric values remain provisional until calibration. A configuration file generated from an experiment must record the associated dataset, classifier, preprocessing, and threshold-profile versions in the evaluation report.

---

## 27. Evaluation dataset and offline runner

### 27.1 Dataset purpose

The evaluation dataset measures destination prediction. It does not need to reproduce a user's entire personal Downloads directory.

The target BSc dataset contains:

- approximately 300 to 500 items;
- 5 to 8 documented destination categories;
- a mixture of easy, ambiguous, and out-of-scope items;
- enough examples per category to report category-level errors;
- only public, synthetic, or explicitly anonymized content.

The final size and category distribution are frozen before the final experiment.

### 27.2 Gold-label manifest

The repository may contain metadata and scripts, but it must not contain private source documents.

```json
{
  "item_id": "doc-0001",
  "relative_path": "documents/doc-0001.txt",
  "gold_category": "university",
  "gold_destination": "course-materials",
  "language": "hu",
  "content_type": "text/plain",
  "ambiguous": false,
  "split": "test"
}
```

The manifest schema and category definitions are versioned. If source files cannot be redistributed, the manifest stores only identifiers and checksums, and the reproduction guide explains how an authorized evaluator reconstructs the corpus.

Ambiguous examples remain in the dataset but are marked. Results are reported both with and without them.

### 27.3 Labeling protocol

Before labeling begins, every category must have:

- a stable key;
- a short natural-language definition;
- positive examples;
- explicit boundary cases;
- rules for ties and genuinely ambiguous items.

A second person should independently label a small stratified subset when practical. Disagreements are documented rather than silently overwritten. The goal is to detect unclear category definitions, not to claim a large-scale annotation study.

### 27.4 Offline evaluation runner

The evaluation runner is a non-mutating adapter over the same classification application service used by the product.

```text
manifest + read-only corpus
    -> bounded preprocessing
    -> classifier variant
    -> decision policy
    -> prediction record
    -> metrics and error report
```

The runner must not:

- start filesystem watchers;
- move, rename, create, or delete corpus files;
- write to the Product V1 state database;
- use a separate implementation of classification rules.

Suggested command shape:

```text
lzyflow evaluate \
    --manifest ./evaluation/corpus-v1.jsonl \
    --classifier b3_hybrid \
    --config ./evaluation/config-v1.toml \
    --output ./evaluation/results/run-id/
```

Each run writes:

```text
run.json                  environment and version metadata
predictions.jsonl         one immutable record per item
metrics.json              machine-readable aggregate results
confusion-matrix.csv      category-level confusion matrix
errors.csv                false positives, false negatives, and rejected items
report.md                 human-readable summary
```

### 27.5 Metrics

The evaluation reports at least:

- accuracy;
- macro-averaged precision, recall, and F1;
- per-category precision, recall, and F1;
- confusion matrix;
- automatic precision;
- coverage;
- selective risk;
- review rate;
- risk-coverage curve;
- median and 95th-percentile classification latency;
- peak resident memory during the run;
- model size on disk.

The primary metrics are automatic precision and coverage for the hybrid selective classifier. Macro-F1 is used to compare classification quality independently of the reject policy.

### 27.6 Error analysis

Errors are assigned to one primary category:

```text
taxonomy ambiguity
insufficient filename evidence
content extraction failure
embedding semantic mismatch
closely competing destinations
threshold calibration error
language mismatch
unsupported file type
incorrect gold label
implementation defect
```

The final report includes representative examples from every material error category, with private content removed.

---

## 28. Safety invariants and verification

The existing safety model is converted into explicit invariants that must hold in unit and integration tests.

### 28.1 Filesystem invariants

**S1 - No implicit overwrite**

LzyFlow never replaces an existing destination entry during move, approval, resolution, or undo.

**S2 - Managed-root confinement**

Every mutation uses canonical filesystem paths and remains within active configured roots. Lexical prefix checks alone are insufficient.

**S3 - Uncertainty causes no mutation**

A low-confidence, low-margin, invalid, or unavailable classification leaves the source file untouched.

**S4 - New child confinement**

When creating a child path, LzyFlow authorizes the canonical existing parent and appends exactly one validated path segment.

**S5 - Ambiguous recovery is non-destructive**

If both expected source and destination exist after an interrupted operation, LzyFlow deletes neither and requires review.

**S6 - Database failure disables autonomous mutation**

If durable state cannot be read or written safely, automatic filesystem operations stop.

**S7 - Undo is a new validated operation**

Undo receives the same path, existence, authorization, and collision validation as a forward move.

**S8 - Review approval is stable**

Approval executes the stored decision. It does not silently rerun classification or change destination.

**S9 - Event handling is idempotent**

Repeated filesystem notifications for the same stable file do not create concurrent logical ingestion flows.

**S10 - No cleanup without provenance**

Cleanup may affect only files that durable history proves were placed by LzyFlow under an enabled cleanup policy.

### 28.2 Required fault scenarios

The integration suite uses isolated temporary directories and covers:

| Scenario | Expected result |
| --- | --- |
| Destination filename already exists | `needs_review`; source unchanged |
| Destination root disappears before move | operation refused; root not recreated |
| Symlink resolves outside managed root | authorization failure |
| Duplicate watcher events | one logical file record and one classification flow |
| Source changes after classification | stored decision invalidated or reviewed before mutation |
| Process stops before filesystem move | recovery restores a pre-move processing state |
| Process stops after move but before state commit | recovery repairs durable state from filesystem reality |
| Both source and destination exist | `needs_review`; neither deleted |
| Neither source nor destination exists | operation marked failed and surfaced |
| SQLite becomes unavailable | daemon degraded; automatic mutation disabled |
| Undo destination is occupied | undo refused without overwrite |

The tests must assert both database state and filesystem state after every scenario.

### 28.3 Concurrency rule

Filesystem events may be observed concurrently, but a single logical file cannot execute two state transitions or mutations concurrently.

The implementation must define one ownership mechanism, such as:

- a bounded channel with one mutation consumer;
- per-file keyed locking around application operations;
- an equivalent serialized application command queue.

The chosen mechanism must be covered by a burst test that emits repeated create and change events for the same files.

---

## 29. Reproducibility and decision provenance

### 29.1 Reproducible run metadata

Every evaluation run records:

- source commit SHA;
- dirty-worktree flag;
- operating system and architecture;Oh come on, no one will look at t
- .NET runtime version;
- classifier key and immutable version;
- model file checksum;
- preprocessing and extractor versions;
- dataset manifest checksum;
- configuration checksum;
- threshold profile;
- random seed when applicable;
- start and finish timestamps;
- warm-up policy and number of measured repetitions.

### 29.2 Production classification provenance

To support debugging and later comparison, the existing `classifications` table should add:

```dbml
classifier_key text
classifier_version text
preprocessing_version text
input_fingerprint text
raw_score real
runner_up_raw_score real
reason_codes text
created_at datetime
```

`confidence` remains the calibrated or policy-consumed confidence field already present in the schema.

`reason_codes` stores a bounded serialized list. It must not contain extracted user text.

This change does not add a sixth Product V1 table. It extends the existing classification history with the minimum provenance needed to explain why two runs may differ.

The first migration is not frozen until these fields and their privacy implications are reviewed.

### 29.3 Logging boundary

Normal logs may include:

- stable IDs;
- lifecycle transitions;
- reason codes;
- classifier and preprocessing versions;
- elapsed time;
- normalized error categories.

Normal logs must not include:

- bearer tokens;
- document contents;
- extracted text;
- full private URLs with sensitive query parameters;
- model prompts;
- raw filesystem data unrelated to a managed operation.

Debug logging that includes paths is opt-in and documented.

---

## 30. Test strategy and quality gates

### 30.1 Test layers

**Core unit tests**

- lifecycle transition validation;
- destination policy;
- threshold and margin policy;
- category and candidate constraints;
- new-folder label validation;
- collision and undo rules;
- metric calculations.

**Application tests**

- deterministic and semantic classification orchestration;
- fixed review decisions;
- degraded classifier behavior;
- configuration validation;
- state and history updates.

**Infrastructure integration tests**

- SQLite migrations and foreign keys;
- canonical-path authorization;
- temporary-directory file moves;
- stability detection;
- watcher-event deduplication;
- restart and crash-recovery scenarios.

**Evaluation regression tests**

- fixed small public fixture corpus;
- expected prediction record schema;
- deterministic metric computation;
- failure when model, manifest, or configuration metadata is missing;
- comparison report generation.

### 30.2 Continuous integration gates

Every merge to the main branch should require:

```text
dotnet format --verify-no-changes
dotnet build
dotnet test
```

The exact coverage target is set after the first vertical slice. Coverage is a diagnostic rather than a substitute for invariant and fault-scenario tests.

Slow model benchmarks and distribution tests may run separately from the fast pull-request suite, but their latest successful results must be recorded before a tagged thesis release.

### 30.3 Benchmark validity

Performance measurements must:

- separate cold-start and warm inference;
- exclude model download time;
- run multiple repetitions;
- report machine hardware and power mode;
- avoid comparing different preprocessing inputs as if they were the same classifier workload;
- retain raw measurements alongside aggregates.

No fixed latency promise is part of the design until measurements exist.

---

## 31. Definition of done for the BSc profile

The research profile is complete only when all required items below are true.

### 31.1 Functional completion

- [ ] A stable file can traverse detection, classification, review or approval, move, history, and undo.
- [ ] Low-confidence or low-margin predictions leave the source untouched.
- [ ] Approval executes a fixed stored decision.
- [ ] Restart recovery handles every filesystem reality in Section 15.1.
- [ ] The required CLI operations are documented and usable.

### 31.2 Safety completion

- [ ] Every invariant in Section 28.1 has at least one automated test.
- [ ] Every fault scenario in Section 28.2 has an integration test.
- [ ] No test produces an implicit overwrite, unauthorized write, or automatic ambiguous-state deletion.
- [ ] Database unavailability disables autonomous mutation.

### 31.3 Research completion

- [ ] Dataset and category definitions are versioned.
- [ ] Development, calibration, and test splits are frozen before final evaluation.
- [ ] All four classifier variants run through the same evaluation interface.
- [ ] Threshold and margin are selected only from calibration data.
- [ ] Final test results include macro-F1, automatic precision, coverage, selective risk, and a confusion matrix.
- [ ] The report contains a categorized error analysis.
- [ ] Runtime, memory, model size, and environment metadata are reported.
- [ ] Another checkout can reproduce the metric files using documented commands and legally available inputs.

### 31.4 Documentation completion

- [ ] `README.md` distinguishes Product V1 from the BSc research profile.
- [ ] Configuration examples match the implemented profile.
- [ ] Safety and privacy limitations are documented.
- [ ] Model and dataset licenses are recorded.
- [ ] Deferred features are not presented as implemented.

## 32. References

1. Reimers, N., and Gurevych, I. (2019). *Sentence-BERT: Sentence Embeddings using Siamese BERT-Networks*. EMNLP-IJCNLP. <https://aclanthology.org/D19-1410/>
2. Geifman, Y., and El-Yaniv, R. (2017). *Selective Classification for Deep Neural Networks*. NeurIPS 30. <https://papers.neurips.cc/paper/2017/hash/4a8423d5e91fda00bb7e46540e2b0cf1-Abstract.html>
3. Orosz, G., Szabó, G., Berkecz, P., Szántó, Z., and Farkas, R. (2023). *Advancing Hungarian Text Processing with HuSpaCy: Efficient and Accurate NLP Pipelines*. <https://arxiv.org/abs/2308.12635>
4. Szántó, Z., Sliz-Nagy, A., Nagy T., I., Csuma-Kovács, Á., Vincze, V., and Farkas, R. (2018). *Relevance Segmentation of Long Documents*. <https://real.mtak.hu/86146/>
