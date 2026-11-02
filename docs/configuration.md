# Configuration

LzyFlow uses TOML for user intent. This document follows technical-design **Revision 8** and the requirements review. It describes intended behavior; new fields and setup flows are not implemented by this documentation change.

The default location is:

```text
~/.config/lzyflow/config.toml
```

Setup resolves Downloads through XDG where possible rather than assuming the literal path `~/Downloads`.

## Initial setup

Both TUI and CLI setup are required. Interactive setup asks for supported user-facing choices. The CLI also supports fully noninteractive setup and operation so an integrating application can supply those choices without prompts.

Setup collects watched directories, categories, destinations, routing rules, one global mode, per-destination existing-file choices, and applicable supported notification/API/policy preferences.

Suggested values may be offered. Examples must not silently decide mode or adopt files. Asking about configuration does not mean asking users to select model dimensions, database internals, or unfinished features.

Exact command grammar, output schemas, and exit codes remain open. Noninteractive setup must report missing required choices without blocking for terminal input.

## Organization mode

| Value | Meaning |
| --- | --- |
| `static` | Use the category root and suitable existing folders; no classifier-driven creation. |
| `dynamic` | Also create useful nested groups and subdivide them adaptively when confident. |

The initial mode is global and selected during setup. No automatic mode default has been agreed. Per-destination overrides are a later optional feature.

Dynamic mode includes confident automatic creation. It does not require a blanket `new_folder_policy = "review"` switch. Uncertain decisions require review; optional rename/merge suggestions always require approval.

## Temporary downloads and grace period

A download receives **60 seconds after completion** before organization. Initial detection of an incomplete file does not start that period. Completion/stability detection and grace expiry are separate eligibility checks.

The user can move temporary files into:

```text
<XDG Downloads>/to-be-deleted
```

This folder is excluded from organization. Pending work rechecks location and eligibility so a manual move there cancels organization. No automatic deletion is implied, including when other cleanup policies exist.

The agreed duration is one minute. Whether it is configurable, how setup creates/adopts this folder, behavior when a file moves back out, and corresponding behavior for other watches remain open. This revision does not invent keys for those unresolved choices.

## Destinations and pre-existing files

Each destination gets a separate setup choice: include its pre-existing files, or manage only files LzyFlow places or the user explicitly includes.

Configuring a path alone does not adopt existing contents. A shared destination used by multiple rules must have one unambiguous effective adoption choice.

Fine-grained exclusions, later changes, and initial scanning remain open. Adoption permits organization within the selected scope; it does not grant automatic cleanup or unrestricted rename/merge authority.

## Proposed configuration representation

The following is a coherent **illustrative schema**, not a frozen parser API. Existing general/classification/rule fields are retained where possible. `destinations` records illustrate how independent adoption choices could be stored while rules still name destination paths.

### General

```toml
[general]
poll_interval_seconds = 5
stable_checks_required = 3
stalled_after_seconds = 600
```

These earlier parameters control stability observation. They do not replace the separate post-completion grace period. Poll interval must be positive; required checks must be at least one. Stalling never authorizes a move.

### Classification

```toml
[classification]
mode = "dynamic"
lightweight_threshold = 0.75
semantic_threshold = 0.80
minimum_candidate_margin = 0.05
threshold_profile = "example-unvalidated"
```

`dynamic` is an example user's explicit choice, not a default. Numeric acceptance values are inherited provisional examples. A bounded score is not automatically a probability. `threshold_profile` identifies a versioned compatible policy; the example profile does not certify autonomous correctness.

Creation needs evidence of a useful coherent group, recognizable naming, a suitable parent, and acceptable uncertainty. Exact group/split criteria, limits, and action thresholds remain open. There is no universal minimum-file-count gate.

The margin applies in the profile's declared score space. A bounded margin must not be applied directly to arbitrary unbounded logits. See [AI Research Plan](ai-research.md).

### Per-destination choices

```toml
[[destinations]]
key = "documents-root"
path = "~/Documents"
include_existing_files = true

[[destinations]]
key = "pictures-root"
path = "~/Pictures"
include_existing_files = false
```

The booleans illustrate independent user answers. Neither is a default. Field names, identity handling, and migration are implementation proposals; the separate choices are required behavior.

### Categories, watches, and rules

Categories have stable keys and mutable labels. Watches have unique IDs. Rule IDs are unique within a watch; each rule selects a category and authorized destination root.

`classify = false` selects deterministic root routing. `classify = true` permits semantic refinement consistent with the global mode. Whether this flag also exempts those files from later grouping must be defined explicitly; do not silently overload it into another consent mechanism.

Ambiguous extension mappings within a watch are rejected. These examples use extension rules. Broader content-aware category routing may require an explicit contract extension rather than undocumented precedence.

### API, notifications, and MCP

The standalone API remains localhost-only at `127.0.0.1:5274`. Notifications can be enabled/disabled and initially use immediate delivery. MCP remains optional.

## Complete illustrative example

```toml
# LzyFlow configuration — explicit example user choices

[general]
poll_interval_seconds = 5
stable_checks_required = 3
stalled_after_seconds = 600

[classification]
mode = "dynamic"
lightweight_threshold = 0.75
semantic_threshold = 0.80
minimum_candidate_margin = 0.05
threshold_profile = "example-unvalidated"

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
key = "images"
label = "Images"

[[destinations]]
key = "documents-root"
path = "~/Documents"
include_existing_files = true

[[destinations]]
key = "pictures-root"
path = "~/Pictures"
include_existing_files = false

[[watches]]
id = "downloads"
path = "~/Downloads"
enabled = true

    [[watches.rules]]
    id = "documents"
    extensions = [".pdf", ".docx", ".txt", ".md"]
    category = "documents"
    destination = "~/Documents"
    classify = true
    cleanup = { mode = "never" }

    [[watches.rules]]
    id = "images"
    extensions = [".png", ".jpg", ".jpeg", ".webp"]
    category = "images"
    destination = "~/Pictures"
    classify = true
    cleanup = { mode = "never" }
```

Examples do not select a mandatory document extractor or visual model. File-type support and evidence sources must be declared by the implementation.

## Cleanup

The earlier V1 plan includes explicit policies `never` and `on_startup`; `after_days` remains deferred. Cleanup affects only files placement history proves LzyFlow placed under an enabled cleanup policy. Adopting existing files is insufficient deletion provenance.

`Downloads/to-be-deleted` remains manually managed regardless of its name. It must not inherit cleanup authority from an enclosing watch or destination. Initial examples use `never`.

## Validation and hot reload

Validate a complete candidate before applying it:

- explicit valid global mode;
- explicit per-destination adoption choices, with no conflicting choices for one effective root;
- unique category keys, watch IDs, and scoped rule IDs;
- consistent category/destination references;
- valid unambiguous extension mappings;
- valid authorized destination roots;
- thresholds and margins compatible with their declared policy;
- known cleanup policies;
- localhost-only API binding;
- temporary-folder exclusion before mutation.

Invalid candidates are rejected entirely and preserve the last-known-good configuration. Pending actions are revalidated against current mode, scope, and authorization.

CLI/TUI changes use shared validation. In standalone deployment they go through the daemon. Setup while the daemon is stopped needs explicit ownership and must not race an active writer; bootstrap ownership remains an implementation decision.

Database/token locations follow XDG runtime conventions. Logs initially go to journald.

## Open choices

Grouping/split criteria, numeric limits, grace configurability, temporary-folder return behavior, fine-grained exclusions, post-setup adoption changes, override syntax, CLI grammar/output, model choice, and the final schema remain open. They must not be silently filled in from examples.
