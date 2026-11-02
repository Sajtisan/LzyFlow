# Security

LzyFlow runs as the current Linux user and can move files inside configured management boundaries. The reviewed Product V1 principle is:

> If an action is uncertain, unauthorized, or cannot be validated safely, leave the affected file unchanged and surface the decision for review or failure.

This document describes required behavior, not evidence that all safeguards are implemented.

## Runtime and API

Normal operation needs no root privileges. The standalone daemon owns runtime state, SQLite, configuration, and filesystem mutations. CLI/TUI use shared application operations; embedded hosts inherit the same boundaries.

The REST API binds only to `127.0.0.1`, normally port `5274`. Remote/LAN access is outside V1. A user-scoped token at the XDG data location, normally `~/.local/share/lzyflow/api.token`, uses restrictive permissions such as `0600`. Clients send:

```http
Authorization: Bearer <token>
```

Authentication answers who may control LzyFlow. It does not authorize arbitrary filesystem access. The fully noninteractive CLI must not bypass the checks used by TUI/REST actions.

## Roots and management scope

Every source and destination must belong to the configured roots valid for the operation. Destination configuration and existing-file adoption are separate choices.

Initial setup asks whether pre-existing contents may be managed **separately for each destination**. Until that choice is obtained, do not silently reorganize existing contents. Within an excluded destination, files placed by LzyFlow or explicitly included can still be eligible under the selected policy; unrelated pre-existing files cannot join a grouping plan merely because they are nearby.

A choice to include existing files authorizes organization within that scope. It does not grant automatic deletion, arbitrary root changes, or autonomous folder renaming/merging. Fine-grained exclusions and later adoption changes remain to be designed.

## Completion, grace, and temporary downloads

Downloads must finish and receive **60 seconds** before automatic organization. Initial detection is not completion. Stability/completion evidence and grace expiry are independent requirements; resumed writing invalidates readiness.

The user can move a file into:

```text
<XDG Downloads>/to-be-deleted
```

This subtree is excluded from organization, including recursive observation, content-based grouping, pending moves, and cleanup. Its name is not permission to delete files automatically. The user controls deletion.

Recheck current location and eligibility immediately before mutation. A previously queued action must not follow a manually relocated file into this folder or re-ingest it there. Exact watcher cancellation, file identity, restart timing, and behavior when files move back out require implementation design.

No blanket notification approval or per-file temporary flag UI is required: relocation during grace is the agreed interaction.

## Canonical path validation and nesting

Lexical prefix checks are insufficient. Resolve real paths and prevent symlink/path-indirection escape from an authorized root.

For a new child:

1. Identify the existing parent through known directory identity.
2. Resolve and authorize its real path beneath the selected destination root.
3. Validate the new folder label as a single path segment.
4. Recheck identity, conflicts, and relevant current policy before creation.

The parent may be an existing nested folder. Single-segment validation applies to each child name; it does not restrict organization to direct children of the root. Whether one plan can create multiple missing parent levels is unresolved and must not be inferred as arbitrary path authority.

Reject empty labels, absolute paths, `.`/`..`, path separators, traversal attempts, and names unsafe for the intended filesystem operation. A model returns semantic intent and a parent reference, never an executable arbitrary path.

Canonical checks must be combined with execution-time checks appropriate to the supported platform. Do not claim an earlier string validation eliminates filesystem races. Reject operations when safe identity/containment cannot be established.

## Models, names, and grouping

Models receive normalized, bounded evidence and eligible candidates. They have no direct mutation capability. Document contents are evidence, not instructions to change scope or policy.

A name should preserve recognizable document/group information. A plausible label, high similarity, or schema-valid output does not prove that creating a folder is useful. Avoid overlapping destinations and unnecessary fragmentation; exact criteria and numeric limits remain open.

Static mode allows existing destinations only. Dynamic mode permits confident useful nested creation and regrouping. A mode change is rechecked before pending execution.

Group plans must have fixed eligible membership and explicit destinations. A model cannot expand the operation to unmanaged files, temporary downloads, or unrelated paths. Group proposal, acceptance, and execution are distinct responsibilities even if one model assists several of them.

## Review and optional restructuring

Uncertain placement, creation, grouping, or naming requires review. A confidently appropriate broad-root placement can remain while further grouping evidence accumulates; lack of a subgroup alone does not force an unsafe action.

Approval executes the fixed stored intent and revalidates current source state, mode, adoption scope, parent identity, authorization, and conflicts. It must not silently rerun classification or redirect a file to a different destination.

**Optional rename/merge suggestions always require explicit approval**, even when confident. Show the proposed changes and affected files. Fully autonomous renaming/merging is not part of the agreed behavior.

Folder-level execution, identity, collision handling, and undo/reversal semantics must be designed before that optional feature ships. Merging folders must not silently imply deduplicating or deleting file contents. Removal of merged-away directories has not been separately specified.

## No implicit overwrite

Never overwrite an existing destination entry during placement, regrouping, approval, resolution, or undo. Filename conflicts require review; automatic numbered collision renaming is not part of the established V1 baseline.

If a proposed directory appears before execution, revalidate its identity and the fixed intent rather than silently treating it as interchangeable. Serialize competing creation plans so related files do not race to create duplicate destinations.

## Missing roots and durable state

If a destination root or mount disappears, do not blindly recreate it. Leave the source unchanged and surface the condition.

If SQLite cannot reliably store/read required state, disable autonomous mutation. Initial setup while the daemon is stopped needs explicit bootstrap ownership so CLI/TUI cannot race another configuration writer.

The expanded requirements need reviewed persistence for adoption provenance, completion/grace state, nested-parent intent, fixed group membership, and partial operations. The old file-only schema is not proof that those requirements are covered.

## Failure, recovery, and undo

A filesystem mutation and SQLite update cannot form one atomic transaction. Persist enough action intent to reconcile interrupted execution.

| Observed source/destination state | Required behavior |
| --- | --- |
| Source absent, destination present | Verify against operation evidence and reconcile likely success. |
| Source present, destination absent | Reconcile incomplete work without premature retry. |
| Both present | Require review; delete neither. |
| Neither present | Surface failed/missing state. |

A group of moves may partially complete. Preserve member outcomes and expose partial completion; do not describe it as an atomic batch. If folder creation succeeds but a move fails, reconcile the created folder without automatically deleting potentially useful user data.

Undo is a fresh validated reverse move. It must not overwrite a conflict, escape roots, bypass temporary exclusion, or implicitly delete a newly created directory. Optional folder rename/merge reversal needs a separate design.

## Cleanup

The earlier V1 cleanup plan is limited to explicit policies and files durable placement history proves LzyFlow placed under those policies. Merely adopting pre-existing files is insufficient deletion provenance. Never blindly empty a destination.

`Downloads/to-be-deleted` stays manually managed and excluded from automatic deletion, including through an enclosing watch's cleanup policy. The reviewed temporary-folder behavior does not authorize a scheduler or retention deadline.

## Privacy and research

Normal logs and classification provenance must not contain bearer tokens, document text, prompts, sensitive URL query strings, or unrelated filesystem data. Debug path logging is opt-in. Authorized review previews may show affected paths as part of a specific proposed operation.

Local inference is the intended model deployment direction. No hosted Jev call, teacher labeling, or other external processing is authorized merely by listing it as a research alternative. Runtime evidence scope and research data permissions must remain explicit.

## Verification and non-goals

Test grace expiry, temporary relocation during queued work, adoption exclusions, nested symlink escape, collisions, mode changes, concurrent creation, partial grouping failure, interrupted moves, database failure, fixed review, undo, and unapproved optional restructuring.

The design does not claim malware scanning, privilege separation against a compromised user account, OS sandboxing, or remote administration. Docker evaluation isolation does not replace product authorization.
