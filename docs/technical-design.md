# LzyFlow — V1 Technical Design

**Revision:** 8<br>
**Status:** Reviewed product behavior recorded; model selection, detailed grouping policy, and final thesis scope remain open<br>
**Reviewed:** 2026-11-02<br>
**Platform:** Linux-only for V1  
**Purpose:** Canonical V1 technical specification and implementation handoff

## 1. Purpose and product goal

LzyFlow is a local file organizer for configurable directories, with Downloads as the initial primary use case. It aims to organize files with little user effort while keeping uncertainty, user-selected exclusions, and filesystem authorization explicit.

The core behavior is:

1. Detect download completion using the stability/completion mechanism.
2. Allow one minute before organization.
3. Exclude files the user moves into `Downloads/to-be-deleted`.
4. Place eligible files in a suitable existing destination or an appropriate broad category root when confident.
5. In dynamic mode, adaptively group managed files into useful named subfolders, including after initial placement.
6. Send uncertain decisions to review; preserve the current file until a valid action is available.

Revision 8 records the requirements review. It supersedes the previous blanket new-folder approval rule, direct-child-only restriction, automatic static default, optional-only TUI scope, and mandatory neural-ranker selection. This document specifies intended behavior; it does not claim those features are implemented.

Architecture and earlier safety foundations are retained. Detailed grouping criteria, limits, complete configuration/migration contracts, AI architecture, and final thesis deliverables remain explicitly open. See [AI Research Plan](ai-research.md).

## 2. Core principles

1. **Safety before automation.** If LzyFlow cannot establish that an action is safe, it leaves the source file untouched.
2. **One standalone authority.** `lzyflowd` owns runtime state, SQLite, configuration changes, filesystem mutations, and notifications.
3. **Deterministic filesystem boundaries.** Configuration selects the category and destination root. Classification may refine placement only inside that boundary.
4. **Persist only durable value.** SQLite stores state required after restart, directory identity, decisions, history, and undo information. Transient filesystem facts are derived when needed.
5. **Configuration is user intent.** TOML defines watches, rules, thresholds, destinations, cleanup, and feature toggles. SQLite does not duplicate the configuration.
6. **Shared application logic.** REST, CLI, TUI, a future MCP adapter, embedded hosts, and tests use the same application operations.
7. **Linux V1 with real seams.** Linux integration may be direct. Interfaces are introduced only for genuine platform boundaries.

## 3. Product requirements and scope boundaries

### 3.1 Agreed Product V1 behavior

- Linux execution with the existing Core/Application/Infrastructure/Daemon/CLI boundaries.
- Configurable watches, categories, and authorized destination roots.
- Download stability/completion detection followed by a 60-second grace period.
- `Downloads/to-be-deleted` excluded from organization and automatic deletion.
- Initial setup through both TUI and CLI, including fully noninteractive CLI setup and operation.
- Setup asks for supported user-facing choices rather than silently choosing organization mode or adopting files.
- Per-destination choice to include or exclude pre-existing files from management.
- One global static/dynamic mode initially.
- Static mode uses existing destinations; dynamic mode adds useful nested creation and adaptive regrouping.
- Broad category-root placement when confident and no specific folder fits.
- Confident authorized placement/creation can execute automatically; uncertainty goes to review.
- Recognizable document-derived folder names and avoidance of unnecessary fragmentation.
- SQLite history, review, safe moves, recovery, no implicit overwrite, and validated undo.
- TOML validation and last-known-good reload behavior, XDG paths, localhost REST, and Linux integration from the existing V1 plan.

### 3.2 Optional features and later work

- Folder rename/merge suggestions showing proposed changes and affected files; execution always requires approval.
- Toggleable per-destination mode overrides after the initial global mode implementation.
- Windows/macOS, GUI, package-manager distribution, and broader integrations.
- MCP adapter; it is not required for the first usable release.
- Learned personalization, distillation, and other research extensions only if selected later.

### 3.3 Decisions not frozen by this revision

No model, mandatory training contribution, experiment roster, dataset size, group/split threshold, minimum file count, depth/rate limit, or final thesis subset is selected. Exact naming transformations, multiple-missing-parent creation, adoption exclusions, grace configurability, and temporary-folder return behavior remain open.

The product needs limits against fragmentation, but their types and values must be chosen explicitly. Do not treat assistant recommendations or example TOML as confirmed settings. Automatic deletion of temporary downloads and fully autonomous existing-folder renaming/merging are not part of the agreed behavior.

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

## 5. CLI, TUI, and initial setup

Both interfaces are required. TUI setup guides the user through supported settings. CLI setup provides the same choices and also supports fully noninteractive operation for a larger integrating project.

Setup covers mode, watches, categories, roots, routing, existing-file adoption separately per destination, and applicable supported notification/API/policy settings. It can offer suggestions, but it must obtain the user's choices. It does not expose internal model dimensions or unfinished features as mandatory setup questions.

The following capability map is illustrative, not frozen command grammar:

```text
lzyflow
lzyflow setup ...
lzyflow status ...
lzyflow review ...
lzyflow approve <decision-id> ...
lzyflow resolve <decision-id> ...
lzyflow ignore <file-id> ...
lzyflow history ...
lzyflow undo <operation-id> ...
lzyflow watch list|add|remove|enable|disable ...
lzyflow category list|add|rename|remove ...
lzyflow rule list|add|edit|remove ...
lzyflow config show|path|validate ...
lzyflow reconcile ...
lzyflow service install|start|stop|restart|status|uninstall ...
```

Fully noninteractive commands must accept sufficient explicit input and report missing choices without opening prompts or a TUI. Structured results and stable exit codes are recommended; their exact schemas remain open.

Shared application services own validation and behavior. In standalone operation, the daemon remains the authority and thin clients use REST. Bootstrap setup with the daemon stopped needs a defined ownership mechanism so configuration writes do not race. Embedded hosts reuse application services directly.

Optional rename/merge commands and review DTOs need their own execution/reversal design before shipping. They are not implied by the illustrative file-operation grammar above.

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

## 8. Security and management authority

The standalone API binds only to `127.0.0.1`. A per-user bearer token at the XDG data location uses restrictive permissions such as `0600`; clients send `Authorization: Bearer <token>`.

Authentication and filesystem authority are independent. Every mutation must satisfy configured managed-root authorization, current mode, file-management scope, temporary-folder exclusion, current file stability, and applicable approval rules.

Configuring a destination does not adopt all its contents. Initial setup separately asks whether pre-existing files in that destination may be managed. A broad adoption choice does not authorize deletion or bypass explicit rename/merge approval.

Authorization uses canonical/real paths, not lexical prefixes. Resolve and authorize the existing parent of each newly created child, then append one validated label. The authorized parent may be an existing nested folder beneath the selected root; this is not a direct-child-only product restriction. Arbitrary absolute parent paths from model output are forbidden.

`Downloads/to-be-deleted` is excluded before classification, regrouping, cleanup, and pending mutation. Manual relocation there invalidates organization eligibility. Recheck immediately before mutation rather than trusting an earlier watcher event or prediction.

The same boundaries apply to REST, CLI, TUI, internal grouping, embedded hosts, and future adapters. See [Security](security.md) for details.

## 9. File lifecycle and organization workflows

The earlier lifecycle vocabulary is retained as a baseline:

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

### 9.1 Ingestion and grace eligibility

Observation starts stability/completion detection, not the grace timer. After completion is established, a separate 60-second grace period must elapse before automatic organization. Incomplete or stalled files remain untouched.

A manual move into `Downloads/to-be-deleted` cancels pending organization. No dedicated temporary flag UI or extra Pending directory is required. Temporary files must not be re-ingested from that subtree.

The precise persistence/state representation of completion time and grace eligibility is not finalized. Do not invent an additional enum value or reset grace on every event without a design decision. Restart behavior must preserve the exclusion and prevent premature mutation; exact timer recovery semantics need an implementation audit.

### 9.2 Initial placement

After grace expiry and a fresh eligibility check:

1. Establish the configured category and authorized root.
2. If a specific existing folder fits confidently, use it.
3. If a category root is a confident appropriate fallback, place the file there.
4. If the action is uncertain or unavailable, retain the current location and review it.

A file already placed at a root can later participate in adaptive grouping. The initial move does not terminate all future organization.

### 9.3 Adaptive regrouping

Dynamic mode considers eligible managed files at category roots and within managed folders. It may establish broad groups first and later useful nested subgroups. Existing-file participation follows that destination's explicit adoption choice.

A group awaiting useful additional evidence is not necessarily an ingestion failure or a review item. A genuinely uncertain proposed placement, creation, or name requires review. Exact scheduling and this distinction need the selected grouping policy.

Regrouping can revisit a completed file for classification and a new validated move. That is an explicit extension to the earlier one-shot lifecycle. A proposed mapping is `completed -> classifying -> approved|needs_review -> moving -> completed`, while undo remains a validated reverse move. Final transition implementation must account for group membership and partial execution before being treated as frozen.

### 9.4 Review and execution

Approval executes the fixed stored decision, never silently reruns the model. It revalidates current mode, management scope, file version, temporary exclusion, parent identity, authorization, and conflicts. Invalidated decisions return to review.

New-folder creation does not require blanket approval when confident and permitted in dynamic mode. Optional rename/merge suggestions require approval even when confident. Their group/folder-level review representation must be defined explicitly rather than forced into a file-only DTO.

All automatic and approved actions use the same execution rules. Failed/ambiguous recovery must not delete data or silently choose another destination.

## 10. Download stability and temporary files

Earlier stability parameters are provisional operational defaults:

```text
poll interval = 5 seconds
stable checks required = 3
stalled after = 600 seconds
```

Observe at least size and last-write time, and recognize temporary download extensions such as `.crdownload` and `.part`. An unchanged file is evidence, not proof of completion. Origin/downloader signals may supplement stability where available. Resumed writing invalidates completion evidence and organization eligibility.

The agreed grace period is **one minute after the download finishes**. It is separate from stability observations and must not be consumed while downloading. `stalled` never means eligible to move.

The user marks a download temporary by moving it into `<Downloads>/to-be-deleted`. Files and pending work there are excluded from organization. The utility does not automatically delete them.

Whether the minute is configurable, who creates the folder during setup, behavior when a file moves back out, and the policy for other watches are open. Stability counters can be transient, but implementation must determine what completion/grace information needs durability for restart correctness.

## 11. Routing, adaptive grouping, and decision policy

### 11.1 Static and dynamic behavior

| Mode | Existing destinations | New folders | Later grouping |
| --- | --- | --- | --- |
| Static | Configured root and suitable active folders | No classifier-proposed creation | Placement into existing destinations only |
| Dynamic | Same | Confident useful creation can execute automatically | Eligible files may be regrouped into meaningful nested folders |

Setup asks the user for one global mode. Per-destination overrides are a later toggleable feature. Static does not require a fixed-output neural classifier; discovered existing folders can still become candidates.

### 11.2 Broad roots and useful groups

Documents, Pictures, Projects, and Videos illustrate configurable roots. When a narrower destination is unavailable but the category is clear, files can move to that root after the download grace period. They need not remain in Downloads awaiting a group.

Dynamic mode can later form related groups there or subdivide an established course/project collection. Reuse suitable existing folders. Grouping must improve navigation rather than reward one folder per file.

There is no universal minimum file count. The evidence required for a useful group, single-file exceptions, subdivision criteria, activity-aware waiting, limits, and their values remain open. A quiet-period strategy is a proposal, not an agreed timer. Do not confuse that open grouping timing with the confirmed 60-second download grace period.

### 11.3 Names, parents, and uncertainty

Names must retain recognizable meaningful information from the documents, and for a group from their shared evidence. Exact substring rules, abbreviation/language policy, and extractive versus generative naming are unresolved.

A new-folder action names a known authorized parent and a validated child label. Parents can be nested beneath the configured root. Whether one plan may create multiple missing parent levels is open.

Poor scores for existing folders alone do not justify creation. Insufficient evidence or ambiguous grouping can explain those scores. A useful proposed group, suitable name/parent, acceptable uncertainty, and absence of unnecessary overlap are needed before creation.

### 11.4 Progressive evidence and action confidence

Lightweight evidence may include extension, filename, and available origin metadata. A semantic stage may use bounded text or another selected local model. Earlier thresholds `0.75` and `0.80` remain provisional examples; no creation threshold or trained model is selected.

Raw similarity, logits, and model-reported confidence are not interchangeable. Evaluate the action policy for the actual model, candidate distribution, and task. Group creation/splitting and file placement may need different acceptance criteria. If an action cannot be justified confidently, send it to review.

### 11.5 Optional restructuring

Rename/merge suggestions are optional. Show proposed changes and affected files, and require approval. Automatic file placement/new subgroup creation does not grant permission for autonomous renaming or merging of existing folders. Folder deletion, collision handling, identity changes, and reversal semantics need a separate design before implementing this extension.

## 12. Download-origin metadata

V1 uses `IDownloadOriginReader` to normalize Linux metadata such as:

```text
user.xdg.origin.url
user.xdg.referrer.url
```

The resulting `source_url` and `referrer_url` are persisted because metadata may be stripped or lost after movement. Availability depends on the browser or downloader, filesystem, copying behavior, removable or network storage, and metadata stripping.

Missing or unreadable metadata is not fatal. Classification continues with filename and extension. Windows `Zone.Identifier` and macOS `kMDItemWhereFroms` are known future seams but have no V1 implementations.

## 13. Categories, destinations, setup scope, and cleanup

Categories have stable keys and mutable labels; watches and rules retain stable identities. Rules establish category and authorized root, with unambiguous extension mappings. The rule model must be extended explicitly if content-aware broad-category routing needs additional semantics.

Each destination independently records the user's setup choice on pre-existing-file management. Configuring a watch/root is not consent to adopt every existing file. Files LzyFlow places can participate in later dynamic grouping. Fine-grained exclusions, adoption rescans, and post-setup changes remain open.

`classify = false` retains deterministic root routing; `classify = true` permits semantic placement under the selected mode. Whether this flag also opts out of later grouping is unresolved and must not be inferred from its name.

The earlier V1 cleanup plan supports explicit `never` and `on_startup` policies; `after_days` is deferred. Cleanup is limited to files placement history proves LzyFlow placed under a matching policy. Adoption alone is not deletion provenance. `Downloads/to-be-deleted` is manually managed and excluded from automatic deletion even if an enclosing watch has cleanup settings.

## 14. Directory identity soft deletion and reconciliation

Directories are first-class database entities. A file's current full path is:

```text
directories.path + files.file_name
```

Directory identity avoids rewriting every associated file row after a path change. With stored full directory paths, a nested rename also requires reconciling affected descendant directory paths and containment. Historical `file_operations.source_path` and `destination_path` values remain snapshots.

Normal event-driven behavior:

- known directory renamed -> update `directories.path`;
- new directory under a managed destination -> register it and make it an active classifier candidate;
- known directory deleted -> set `is_deleted = true` and `deleted_at = now`;
- directory moved outside a managed destination -> stop treating it as an active managed candidate.

Classifier queries use only active authorized directories. File membership is additionally filtered by adoption provenance, temporary exclusion, and current management scope; discovering a path does not itself adopt every file beneath it.

Soft-deleted directories remain for approximately 30 days and may then be purged. The retention period may become configurable later. A later directory created at the same path normally receives a new identity.

Active directory paths must be unique, preferably through a partial SQLite unique index where `is_deleted = false`. Permanent purge may use `ON DELETE SET NULL` for historical `classifications.suggested_directory_id`; this is finalized in the migration audit.

Reconciliation runs at daemon startup, after relevant configuration reloads, and through an explicit manual command if exposed. Periodic full reconciliation is deferred. If a rename happened while the daemon was offline and identity cannot be established reliably, V1 treats the old directory as deleted and the discovered directory as new.

Observed user-driven renames are reconciled with identity and scope checks. Optional daemon-driven rename/merge suggestions require approval and a separately designed persistence/reversal protocol; no folder-undo guarantee or exemption is selected here. File-move undo remains part of the baseline.

## 15. Filesystem operations, failure, and recovery

Application execution validates current source state, file-management scope, temporary exclusion, mode, canonical authorization, parent identity, filename availability, and applicable approval before mutation. It records durable history and the outcome. SQLite and filesystem changes do not form one atomic transaction.

Per-file moves remain synchronous for normal local operations. Group creation/regrouping needs an explicit plan containing its fixed membership and destinations before execution. Exact plan persistence and group-level review schemas are open. Do not claim a series of file moves is atomic.

Required behavior:

- Uncertainty leaves the current file untouched pending review.
- A temporary-folder relocation cancels pending organization rather than causing a retry into a new destination.
- A filename collision never overwrites; surface it for review.
- A disappeared root/mount is not blindly recreated.
- A stale target or changed file invalidates the stored action.
- Semantic model failure degrades semantic-required work to review/failure while the host stays alive where possible.
- Database failure disables autonomous mutation.
- Concurrent proposals for one folder do not create duplicate destinations or competing moves.
- If a proposed folder appears before execution, revalidate identity and the fixed intent; do not silently treat it as an interchangeable target.
- If creation succeeds but moves fail, preserve files and reconcile the created directory. Do not automatically delete a folder that may contain user data.

### 15.1 Interrupted move recovery

| Observed state | Recovery |
| --- | --- |
| Source absent, destination present | Verify the recorded operation and reconcile likely success; do not guess from an unrelated path alone. |
| Source present, destination absent | Reconcile the incomplete operation without premature retry. |
| Both present | Review; delete neither. |
| Neither present | Surface failed/missing state. |

A group may contain completed, pending, and failed member moves after interruption. Record/reconcile those outcomes individually and expose partial completion. Undo of a file move is another validated move; it does not implicitly delete newly created directories. Optional folder rename/merge execution and reversal require a separate audited design.

## 16. Notifications

Notifications run inside `lzyflowd` through `INotificationService` and a native Linux implementation. An in-process event reacts to state changes such as entering `NeedsReview`; no extra process watches a Pending directory.

V1 supports enable or disable and immediate delivery. Batching is deferred. A separate user-session notifier is reconsidered only if a future deployment becomes a system-level daemon outside the logged-in session.

## 17. SQLite state, baseline schema, and required audit

The earlier persistence design has five tables: `categories`, `directories`, `files`, `classifications`, and `file_operations`. Current location uses directory identity; operation paths are historical snapshots.

### 17.1 Existing design baseline

The following DBML preserves the earlier baseline for reference. It is **not a finalized migration for the expanded requirements**.

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

### 17.2 Semantics retained

Categories store stable identity; TOML stores labels/configuration. Directories support identity, path changes, active uniqueness, and soft deletion. Classifications retain predictions and provenance without document text. Operations record validated filesystem attempts and their outcomes; waiting or review alone is not a move operation.

### 17.3 Revision 8 migration audit

Before freezing migrations, explicitly design durable representation for:

- file-management provenance and per-destination adoption, without duplicating TOML choices unnecessarily;
- completion/grace eligibility and restart handling;
- a new child under a selected existing nested parent, which cannot be reconstructed from the label alone;
- fixed group membership, grouping decisions, and partial multi-file execution;
- required review snapshots and model/policy provenance;
- optional rename/merge plans and reversal only if that feature is implemented;
- foreign-key behavior, active-path uniqueness, directory purge, and source/destination reconciliation.

The existing file-only suggestion fields are insufficient to declare this work finished. No arbitrary new tables or final column names are selected here. Align DBML, application contracts, and migrations together; do not preserve an "exactly five tables" claim at the expense of required durable behavior.

## 18. Configuration and setup

TOML stores user intent. Both TUI and fully noninteractive CLI setup must supply an explicit global organization mode and a separate existing-file choice per destination. Later per-destination mode overrides are optional, distinct from adoption scope.

The [Configuration](configuration.md) document supplies the complete illustrative example and proposed destination records. Its syntax is a proposal, not a frozen parser contract. No silent static default, blanket new-folder approval switch, or universal minimum group count is introduced.

The agreed one-minute post-completion grace period and temporary-folder exclusion apply regardless of whether their eventual settings use constants or configuration. Configurability is still open.

A complete candidate is parsed and validated before atomic replacement. Invalid updates preserve the last-known-good configuration. Validate IDs, references, destination choices, extension ambiguity, policy compatibility, allowed cleanup, localhost binding, and temporary exclusion. Pending actions use current authorization/mode/scope at execution.

Standalone configuration changes use daemon-owned application operations. Initial setup when the daemon is absent needs explicit bootstrap ownership. Database/token locations follow XDG; logs initially use journald.

## 19. Planned REST baseline

These are the earlier planned file/configuration routes, not a claim that the runtime already implements them. Setup/adoption, group decisions, partial plans, and optional rename/merge reviews need explicit contract extensions before implementation.

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

## 20. Open design and implementation choices

Resolve these without treating examples as approved answers:

- Group coherence, navigational usefulness, subdivision evidence, activity/timing strategy, and single-file exceptions.
- Limits against over-creation, their values, and configurability.
- Parent selection and whether a plan can create multiple missing hierarchy levels.
- Naming transformations, language, extraction, and any generative assistance.
- Fine-grained destination exclusions, adoption scanning, and later adoption changes.
- Grace configurability, temporary-folder provisioning/return behavior, and other-watch equivalents.
- Interaction between rule classification flags and later grouping eligibility.
- Concrete local model, training obligation, dataset size, and final experiment set.
- Final TOML/DTO/database contracts and lifecycle/plan persistence.
- Exact noninteractive commands, output/error formats, bootstrap ownership, and TUI layout.
- Optional rename/merge priority, eligibility, collision handling, and reversal.
- Final assessed thesis subset and numeric targets.

Existing runtime implementation choices such as Linux libraries, unit files, installer mechanics, and calibration algorithms remain subject to the established architecture and measured behavior.

## 21. Suggested implementation sequence

This is guidance, not a newly agreed milestone contract:

1. Complete foundation work: paths, configuration, tests, token, persistence, and ownership.
2. Implement completion detection, one-minute grace, temporary exclusion, and safe deterministic placement.
3. Add shared TUI/CLI setup, destination adoption choices, global mode, and noninteractive control.
4. Complete review, safe moves, history, undo, and recovery.
5. Establish the shared local decision contract and a small controlled comparison before selecting an AI architecture.
6. Implement existing-folder placement and dynamic group creation/reuse within agreed authority.
7. Extend to adaptive subdivision, nested parents, regrouping, and partial-plan recovery.
8. Evaluate quality, uncertainty, fragmentation, naming, and resource costs in Docker.
9. Add optional rename/merge suggestions or mode overrides only as separately scoped work.

Do not turn a suggested implementation order into a claim that the unselected neural architecture is mandatory.

## 22. Final V1 design principle

> LzyFlow should prefer leaving a file untouched over taking an autonomous action whose correctness or safety it cannot establish with sufficient confidence.

---

## 23. Product V1 and BSc research profile

The reviewed behavior defines product intent. The final subset assessed in the BSc thesis is not frozen by this revision.

### 23.1 Agreed product requirements

Static/dynamic organization, confident automatic creation, adaptive nested grouping, recognizable names, category-root fallback, temporary-folder exclusion, the one-minute grace, per-destination adoption, both setup interfaces, and noninteractive CLI integration are requirements of the reviewed product. They are not optional merely because an earlier draft placed them in a stretch list.

### 23.2 Thesis scope still to select

Select an achievable executable slice and describe its relationship to those product requirements. Decide the supported content types, model/training contribution, dataset, experiment roster, runtime budget, interaction coverage, and release gates explicitly.

No custom NN, frozen encoder, mandatory fine-tuning, fixed 300-500-item dataset, or required list of every model family is imposed. A comparative study or a task-adapted decision model remains possible.

### 23.3 Optional extensions

Reviewable rename/merge suggestions and later per-destination mode overrides are optional. Neither becomes a thesis completion gate without a separate scope decision. Additional platforms, GUI, MCP, personalization, and advanced training techniques remain unselected later work.

### 23.4 Hosting

Standalone runtime uses the daemon and REST. A research/embedded host may call shared application services directly. It must preserve the same policy and filesystem boundaries. Selecting a research host does not remove the required product CLI/TUI setup capabilities or imply they are implemented.

## 24. Candidate research questions

These guide scope selection; they are not claims of proven improvement:

- Which local approach best places files into existing destinations under uncertainty?
- How can dynamic grouping improve navigation without unnecessary folders or unstable hierarchy changes?
- Does a learned action/group scorer improve over generic similarity and a manually designed grouping policy?
- Can a lightweight Jev-style question-conditioned model make useful bounded organizational judgments?
- What trade-offs arise among automatic precision, coverage, review, latency, memory, and model size?
- Do grace, exclusion, adoption, collision, recovery, and optional approval boundaries hold under faults?

The distinction between semantic similarity and filing preference matters. Related subjects may belong to different courses/projects, while differently worded documents may belong together. Hypotheses, measurable targets, and the selected comparison must be declared before final testing, without assuming the most complex model wins.

## 25. Shared decision contract

Exact DTOs remain open. Implementations must share normalized evidence, action semantics, and provenance so results can be compared and safely executed.

### 25.1 Inputs

A decision context can contain filename/type, optional origin evidence, bounded extracted text, category/root identity, global mode, eligible files, active existing folders with their known parent relationships, and optional candidate groups/names. All evidence is normalized and scope-filtered before reaching a model.

A group-level context additionally needs fixed candidate membership and current parent/hierarchy context. It must not expose unmanaged files or temporary downloads merely because they are physically nearby.

### 25.2 Intent and outcomes

The existing placement targets remain useful: `category_root`, `existing_folder`, and `new_folder`. A nested new-folder intent additionally identifies its known authorized parent. Reject/review is a policy outcome, not another arbitrary filesystem path.

Adaptive organization also needs to distinguish keeping the current broad placement, waiting for group evidence, creating a subgroup, and proposing optional rename/merge changes. Their final action/plan schema is not yet frozen.

Record model/version, preprocessing, input fingerprint, raw scores, calibrated/policy confidence where meaningful, and bounded reason codes. Include relevant candidate/group snapshots and policy identity in reproducibility records. Document text does not belong in normal logs or provenance fields.

### 25.3 Execution policy

1. Apply completion/grace, exclusion, management-scope, and root constraints.
2. Select a suitable existing destination or confident category-root fallback.
3. In dynamic mode, evaluate useful group/subgroup creation, recognizable naming, parent suitability, and fragmentation.
4. Execute confident permitted placement/creation automatically.
5. Store uncertain actionable decisions for review. Ordinary accumulation does not require inventing an uncertain action.
6. Require explicit approval for every optional rename/merge suggestion.
7. Revalidate the fixed action immediately before execution; a model cannot override authority.

Acceptance policy must be compatible with the actual score space and task. Placement and group creation need separately evaluated behavior, but no specific new threshold/profile switch is selected by this revision.

### 25.4 Evidence extraction and file types

Extraction needs declared type support, size/text limits, encoding handling, timeout, and failure behavior. Text-containing documents are the primary semantic use case. Specific PDF/office/OCR or visual-content support is not selected merely by listing Documents/Pictures/Videos categories.

Where safe deterministic category routing is possible it can continue without content extraction. Otherwise unavailable/failed extraction contributes uncertainty and review. No classifier executes document macros or receives arbitrary filesystem mutation access.

### 25.5 Candidate implementations

Rules, classical models, embeddings/prototypes, clustering, compact scorers, fine-tuned encoders, and local Jev-style decision models are candidates described in [AI Research Plan](ai-research.md). Existing baseline keys may be retained for continuity, but this revision does not mandate all variants or a neural default. Candidate count and folder hierarchy must not be confused with a fixed model output-label set.

## 26. Calibration and action-policy selection

Earlier `0.75` and `0.80` values are provisional examples, not probabilities of correctness or evidence that an action is safe. Evaluate the selected model, score normalization, margin handling, and task distribution.

If models are trained, keep related documents/versions together when splitting training, development, calibration, and held-out test data. Generate pairs/triplets, prototypes, synthetic examples, and teacher labels only from permitted partitions. Pair expansion does not create independent documents.

For an untrained model, still separate development/policy selection from final evaluation. Tune acceptance and limits only on the allowed development/calibration data; freeze them before testing. Group/topic-held-out evaluation is needed if claiming unseen-topic generalization.

Evaluate existing placement, creation, subdivision, and uncertainty separately. Include ambiguous and insufficient-evidence cases; a softmax over candidates is not a none-fits detector. A technically valid action is not necessarily a useful organizational action.

For threshold t:

```text
coverage(t) = accepted decisions / all eligible decisions
selective_risk(t) = incorrect accepted decisions / accepted decisions
automatic_precision(t) = 1 - selective_risk(t)
```

An empty accepted set has undefined risk. Define action-specific correctness and denominators, especially for multi-file plans. Review assistance does not count as autonomous success. Optional rename/merge approval remains mandatory regardless of confidence.

## 27. Dataset and reproducible evaluation

### 27.1 Dataset and labels

Dataset size, language distribution, content types, and final taxonomy are not selected. Use legally available public/synthetic or appropriately permitted content; do not commit private source documents.

Labels may need more than one correct destination: useful groups, acceptable recognizable names, valid parent choices, appropriate broad-root retention, creation/subdivision opportunities, and review cases. A learned group utility model needs organizational judgments beyond file-to-folder labels.

Version item/group IDs, source relationships, snapshots, splits, sequence order, and annotation policy. Define ambiguity and acceptable aliases. Evaluator gold information must not leak into model context or proposal generation.

### 27.2 Static and sequential scenarios

Compare existing-destination routing, absent destinations, broad-root accumulation, and growth from coarse groups into useful nested subgroups. Include same-subject/different-course cases and folder-fragmentation failures.

Replay only current and permitted past evidence. Give each run the same initial directory state. New groups become later destinations; errors may affect future decisions. Report closed-loop runs separately from oracle-corrected or reviewer-assisted runs.

Model-only comparisons can hold candidate groups/names fixed. Full-pipeline comparisons include group generation, naming, timing, and acceptance. Unsupported cases remain visible rather than silently disappearing from a model's denominator.

### 27.3 Runner and filesystem tests

A non-mutating runner uses the same decision/application policies with a simulated directory registry. A separate disposable filesystem harness tests actual grace/exclusion, creation, regrouping, adoption, review, and recovery. Neither uses real personal directories or production state.

Exact commands, schemas, and selected scenarios remain to be implemented; example commands in earlier drafts are not executable feature claims.

### 27.4 Suggested report contents

For the selected experiment, report placement quality, automatic precision/coverage/risk, review rate, useful/missed/unnecessary creation, fragmentation, name/parent acceptability, later reuse, and hierarchy stability. Include cold/warm latency, extraction/grouping costs, peak memory, total deployed model size, and training cost where applicable.

Define denominators, action versus per-file metrics, undefined cases, uncertainty intervals where appropriate, and error categories. The final thesis metric roster and numeric targets remain a scope decision; no unmeasured performance promise is made.

## 28. Safety invariants and verification

The implementation must preserve:

1. No implicit overwrite during placement, grouping, approval, resolution, or undo.
2. Canonical managed-root confinement, including nested parents and symlink indirection.
3. No autonomous mutation for an uncertain or unauthorized action.
4. No organization of files in `Downloads/to-be-deleted` and no automatic deletion there.
5. No automatic organization before completion plus the 60-second grace.
6. No adoption of pre-existing files without that destination's user choice.
7. No classifier-proposed creation in static mode.
8. No autonomous existing-folder rename/merge, including high-confidence suggestions.
9. Fixed review approval with fresh execution-time eligibility checks.
10. Idempotent event handling and serialized ownership of each file/action.
11. Database failure disables autonomous mutation.
12. Ambiguous recovery preserves data; multi-file plans expose partial completion.
13. Cleanup requires explicit policy and placement provenance; adoption is not deletion provenance.

Fault scenarios include collisions, vanished mounts, nested symlink escape, repeated watcher events, changes after classification, temporary relocation during grace or pending work, mode/adoption changes, concurrent creation, interrupted grace/creation/group moves, database failure, undo conflicts, and optional unapproved restructure suggestions.

Tests assert filesystem and durable state together. Precise group-plan and grace recovery schemas must be designed rather than inferred from passing file-move tests.

## 29. Reproducibility and provenance

Record source commit and dirty state, runtime/hardware, model/preprocessing/extractor identities, policy/configuration checksum, corpus/split checksums, candidate/group snapshots, initial hierarchy, replay order, randomness, warm-up, repetitions, Docker image/dependency versions, and resource limits. Record teacher/training provenance only when used.

Production history needs enough immutable action context to explain placement and regrouping, including the selected nested parent, management authority, model/policy version, and affected files. The baseline schema audit must address this explicitly. Fingerprints detect drift; they do not anonymize private content.

Normal logs must not contain bearer tokens, document text, model prompts, private URL queries, or unrelated filesystem data. Debug path logging is opt-in. Optional restructuring previews intentionally show affected paths to the authorized user; that is separate from indiscriminate logging.

## 30. Test strategy and development gates

Core tests cover domain/action rules, mode and approval boundaries, naming validation, and metrics. Application tests cover setup choices, noninteractive operation, grace/exclusion, adoption, decisions, grouping plans, fixed review, and degraded models. Infrastructure tests use isolated filesystem/SQLite state for real-path authorization, moves, directory identity, and recovery.

Evaluation tests check schema/metric determinism, missing provenance, split leakage, replay order, state reset, future-information exclusion, and reviewer accounting. No selected model accuracy is implied by architecture-boundary tests.

The established repository baseline is restore, format verification, Release build, and tests. Slow model experiments and Docker filesystem scenarios run separately from fast checks as needed. See [Development](development.md).

Performance comparisons separate cold start, warm inference, extraction, group/naming work, and total latency. Control resources and inputs, retain raw measurements, and record relevant cache state. Exact acceptance targets and the final thesis test roster remain to be selected.

## 31. Completion and unresolved scope

### 31.1 Product behavior checklist

- [ ] Stable/completed downloads receive one minute before organization.
- [ ] Moving into `Downloads/to-be-deleted` prevents pending organization; deletion stays manual.
- [ ] Confident existing-folder/category-root placement works; uncertainty goes to review.
- [ ] Dynamic mode creates useful recognizable nested groups and can regroup eligible files later.
- [ ] Static mode creates no classifier-proposed folders.
- [ ] The selected grouping policy avoids unnecessary fragmentation without a universal count gate.
- [ ] Setup obtains mode and separate per-destination adoption choices through TUI and CLI.
- [ ] CLI setup and operation can run fully noninteractively.
- [ ] History, safe moves, review, undo, and recovery preserve the stated invariants.
- [ ] New durable behavior is represented by reviewed contracts and migrations.

### 31.2 Optional extensions

If implemented, rename/merge suggestions show affected files and require approval. Per-destination mode overrides remain a later toggleable feature. Neither is a mandatory thesis gate by default.

### 31.3 Thesis completion still to define

Before claiming thesis completion, select and document its assessed product slice, AI contribution, dataset/splits, experiment roster, metrics/targets, resource budget, and reproduction procedure. Do not substitute the earlier unapproved mandatory neural architecture or model catalogue.

### 31.4 Documentation integrity

Examples must match each other and distinguish proposed syntax from implementation. Retain open choices rather than silently choosing defaults. Describe actual implemented support at release time; this specification alone is not evidence of runtime completion.

## 32. References

1. Reimers, N., and Gurevych, I. (2019). *Sentence-BERT: Sentence Embeddings using Siamese BERT-Networks*. EMNLP-IJCNLP. <https://aclanthology.org/D19-1410/>
2. Geifman, Y., and El-Yaniv, R. (2017). *Selective Classification for Deep Neural Networks*. NeurIPS 30. <https://papers.neurips.cc/paper/2017/hash/4a8423d5e91fda00bb7e46540e2b0cf1-Abstract.html>
3. Orosz, G., Szabó, G., Berkecz, P., Szántó, Z., and Farkas, R. (2023). *Advancing Hungarian Text Processing with HuSpaCy: Efficient and Accurate NLP Pipelines*. <https://arxiv.org/abs/2308.12635>
4. Szántó, Z., Sliz-Nagy, A., Nagy T., I., Csuma-Kovács, Á., Vincze, V., and Farkas, R. (2018). *Relevance Segmentation of Long Documents*. <https://real.mtak.hu/86146/>
