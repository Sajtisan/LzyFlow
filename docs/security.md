# Security

LzyFlow is a local file-management service with authority to move user files. V1 therefore follows a conservative security model:

> If LzyFlow cannot establish that an operation is authorized and safe, it does not perform the filesystem mutation.

## Execution Model

LzyFlow V1 runs as the current Linux user.

- No root privileges are required for normal operation.
- `lzyflowd` runs as a `systemd --user` service.
- Filesystem access is limited by both the operating-system permissions of that user and LzyFlow's own managed-root rules.

Running as the user does **not** mean every path accessible to that user is authorized for LzyFlow.

## Local API

The V1 REST API listens only on:

```text
127.0.0.1
```

The default endpoint is:

```text
http://127.0.0.1:5274/api/v1
```

Binding to `0.0.0.0`, LAN interfaces, or externally reachable addresses is outside V1.

Remote authentication, TLS termination, and remote administration are therefore intentionally out of scope.

## Authentication

A per-user bearer token is stored at:

```text
~/.local/share/lzyflow/api.token
```

The token should use restrictive permissions such as:

```text
0600
```

Clients authenticate with:

```http
Authorization: Bearer <token>
```

CLI and TUI clients use the same local API boundary as other standalone clients.

Authentication answers:

```text
Who may control LzyFlow?
```

It does **not** grant unrestricted filesystem authority.

## Filesystem Authorization

Filesystem authorization answers:

```text
What may LzyFlow modify?
```

Every source and destination must remain within the configured watched roots and destination roots that are valid for the requested operation.

Configuration establishes those boundaries. A successful bearer-token check does not override them.

The same rule applies to:

- REST requests;
- CLI operations;
- TUI operations;
- internal automation;
- any future MCP adapter.

## Canonical Path Validation

Authorization must use the real filesystem target, not only the path string provided by a caller.

A lexical prefix check is insufficient.

For example, a path that appears to be inside a managed root must not be allowed to escape through a symlink.

Conceptually:

```text
~/Documents/Managed/link -> ~/.ssh
```

must not give LzyFlow permission to write into `~/.ssh`.

For an existing path, LzyFlow resolves the canonical or real path before authorization.

For a new child path, LzyFlow:

1. resolves the existing parent directory;
2. verifies that the resolved parent is inside the permitted managed root;
3. validates the new child as a single safe path segment;
4. only then creates or uses that child path.

If real-path resolution is ambiguous or cannot be established safely, the operation is rejected.

## Classifier Isolation

Classifiers return semantic placement intent, never arbitrary filesystem paths.

Allowed result types are:

```text
category_root
existing_folder
new_folder
```

An existing folder is referenced through a known directory identity.

A new-folder result contains only a proposed folder name. Deterministic application logic validates that name and constructs the resulting path beneath the configured destination root.

Unsafe names are rejected, including:

- absolute paths;
- `.` or `..`;
- path separators;
- traversal attempts;
- empty names;
- names that cannot be safely created within the configured root.

## No Implicit Overwrite

LzyFlow never silently overwrites an existing user file.

If the destination filename already exists:

```text
operation -> NeedsReview
```

The source remains untouched.

V1 also does not automatically rename the incoming file to values such as:

```text
file (1).pdf
```

without an explicit user decision.

## Uncertainty and Failure

Low confidence is not permission to act.

When classification is uncertain:

```text
source file remains in place
status -> NeedsReview
```

A stalled download also remains untouched.

Missing origin metadata is treated as missing evidence rather than a reason to bypass safety checks.

If a stored classification target disappears before approval, LzyFlow does not silently choose another destination.

## Database Failure

SQLite stores persistent state required for:

- file lifecycle state;
- directory identity;
- classification history;
- operation history;
- undo and recovery.

If the database cannot be opened or is considered unusable:

```text
autonomous filesystem mutations are disabled
```

It is safer to stop organizing than to continue without reliable state and history.

## Unavailable Destinations

If a configured destination root or mount disappears, LzyFlow must not blindly recreate it.

The source file remains untouched and the condition is surfaced for review or as a failure.

This is especially important for removable, network, and mounted storage.

## Crash Safety

A filesystem move and a SQLite update cannot form one atomic transaction.

LzyFlow therefore persists enough state to reconcile interrupted moves on startup.

Recovery prefers preserving data over guessing:

| Filesystem state | Recovery |
| --- | --- |
| Source missing, destination exists | Treat as likely successful and repair persistent state. |
| Source exists, destination missing | Treat as not completed and restore processing state. |
| Both exist | Mark for review and delete neither. |
| Neither exists | Mark failed and surface the missing state. |

## Cleanup Safety

Cleanup may delete only files that LzyFlow can identify through its own history as files it placed in the cleanup-managed destination.

LzyFlow must never blindly empty a directory.

## MCP and Future Integrations

MCP is optional and deferred beyond the first usable V1.

Any future adapter must call the same application services and inherit the same authorization, path-validation, overwrite, and review rules.

An agent should receive bounded LzyFlow capabilities, not unrestricted filesystem access.

## V1 Threat Model

V1 is designed to reduce risks from:

- accidental destructive file moves;
- API calls from unauthenticated local clients;
- path traversal;
- symlink-based managed-root escape;
- destination collisions;
- unsafe classifier output;
- stale classification targets;
- missing destination mounts;
- interrupted filesystem operations;
- autonomous mutation while persistent state is unavailable.

## Non-Goals

V1 does not attempt to provide:

- remote or internet-facing administration;
- TLS for the local-only REST endpoint;
- multi-user authorization;
- privilege separation between multiple local users;
- OS sandboxing;
- container isolation;
- malware detection;
- content antivirus scanning;
- protection against a fully compromised user account.

Those concerns may require a different deployment model and are outside the current V1 scope.

## Security Invariants

The implementation should preserve these invariants:

1. LzyFlow never requires root for normal operation.
2. The V1 API remains localhost-only.
3. API authentication and filesystem authorization remain separate checks.
4. Every filesystem target is authorized using canonical/real paths.
5. Symlinks cannot escape managed roots.
6. Classifiers never choose arbitrary filesystem paths.
7. Existing files are never overwritten implicitly.
8. Uncertainty leaves the source file untouched.
9. Database failure disables autonomous mutations.
10. Missing or invalid destination roots do not trigger blind directory recreation.
