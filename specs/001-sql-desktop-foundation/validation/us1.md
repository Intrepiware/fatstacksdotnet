# US1 validation

## Scope and status

Setup, shared schema, and the US1 connection workflow are implemented. US2–US5 remain planned.
T018 remains open for the full transport matrix. The user has now supplied a successful
WindowsChecks run: 33 passed, zero failed/skipped. See the current execution evidence below;
earlier sections retain the historical Linux-runner limitations.

## Current Windows execution evidence — 2026-11-18

The user ran `dotnet test --project tests/FatStacks.WindowsChecks` on Windows x64 and supplied
a full-suite pass: 33 succeeded, zero failed/skipped, 57s 170ms summary duration.
[The exact output](windowschecks-2026-11-18.md) is recorded with user attribution.
This supplies execution evidence for the suite's connection and window-behavior regressions;
manual visual/focus review and the separate SQL transport matrix retain their existing status.
The agent did not rerun these checks on Windows.

## Executed checks

The Linux sandbox uses .NET SDK 10.0.115. SQL checks connect to the host SQL Server over TCP.
Credentials come from ignored local files. The maintenance identity creates and removes only
isolated fixture databases. The separate runtime identity opens a prepared book and cannot
create a table. Tests do not reset the database named in either UDL.

- Solution build: passed with no warnings or errors, including the Windows-targeted projects.
- Deterministic service tests: 14 passed; none skipped.
- SQL connection/schema suite: 13 passed; four transport scenarios skipped because fixtures
  were absent. This combined run includes separate runtime-login access and failed deployment
  rollback. No SQL tests failed.
- Failed deployment: an injected final-stage SQL error rolls back application tables and
  prevents publication of the schema version.
- Repeated initialization preserves book identity, name, and currency.
- Formatting verification and staged/working diff whitespace checks: passed.
- Package advisory check: no known vulnerabilities reported by the configured source.
- Checklist gate: requirements.md has 16 checked items and no unchecked items.

The first live setup run exposed a lock wait between grate's schema transaction and its
separate permission connection. Permissions and final version publication now share the schema
transaction. The repeated-setup and failure-injection checks exercise that boundary. The empty
generated database left by the stopped diagnostic run was removed.

## Unverified checks

Windows UI Automation sources compile. This Linux sandbox cannot execute WPF or Windows DPAPI.
No Windows UI, accessibility, or DPAPI acceptance claim follows from the build result.

Valid trusted TLS, plaintext, self-signed fallback, and expired-certificate fallback need their
configured transport fixtures. An explicit TrustServerCertificate connection and its warning
passed. This does not establish all automatic certificate-fallback cases.

## Repeatable Windows verification

1. Build the solution on Windows 11 x64 and configure separate runtime/maintenance credentials.
2. Run the WindowsChecks project from an unlocked interactive desktop. Record its result.
3. Initialize two empty databases with different currencies. Open each with runtime credentials.
4. Switch books, restart, and rename a profile. Confirm the same book reopens after the rename.
5. Repoint a profile to the other book. Confirm opening requires explicit reassociation.
6. Test an unreachable profile and cancel startup. Confirm the dialog remains usable and a
   failed switch does not replace the remembered successful profile.
7. Confirm keyboard focus, accessible control names, progress, and cancellation in both dialogs.
8. Configure each transport fixture and verify the expected visible, non-blocking warning.

## Self-review

The delivered services remain independent of WPF. EF operations handle metadata; grate owns
DDL. There are no journal procedures or accounting workflows in US1. There is no operation
receipt, uncertain-outcome framework, automatic schema upgrade, or backup subsystem.

Profile protection and sanitized errors implement FR-002/029. Book identity checks implement
FR-005. Schema comparison implements FR-032. Session cancellation and write completion implement
FR-003/004. The migration transaction prevents the observed self-blocking risk and preserves
atomic schema/version publication. Startup refreshes cached profiles after identity pinning so
later edits retain the saved book identity. Windows execution must verify this UI path.

## Connection-screen corrections

The connection and maintenance dialogs disable SQL username/password fields while Windows
integrated credentials are selected. The shell TLS warning has a Dismiss button. Successful
initialization replaces Initialize and Cancel with Close. Failure keeps the retry controls.

The main window defers its final Close call until its first Closing event has returned. This
addresses a possible recursive Close when session disconnect completes synchronously. Startup
completion also checks whether closing has begun before showing a connection dialog. The user
reported a generic unhandled-error dialog; the exact runtime exception was not available.

The isolated Linux build passes with no warnings or errors. All 14 service tests pass. New
WindowBehaviorTests cover the credential fields, setup success/failure, warning dismissal, and
closing with an active book. They compile but remain unexecuted because WPF requires Windows.
Run `dotnet test --project tests/FatStacks.WindowsChecks/FatStacks.WindowsChecks.csproj --filter FullyQualifiedName~WindowBehaviorTests` on Windows. These checks use synthetic services
and need no SQL credentials. Also repeat the main-window X action during startup and after
opening a real book. Confirm normal exit without the unhandled-error dialog.

Generated grate migration records under output/ are excluded from source control.

## US1 convergence — 2026-11-18

Scope: T069, T070, T075, T076, T077, and T085. Other stories and feature-wide
release checks remain outside this implementation pass.

T069 validates stored profile records before dereferencing or decrypting fields. Null,
missing-field, and null protected-connection records return the existing redacted failure,
preserve the file, and block replacement. Explicit recovery by restoring valid content or
removing the file followed by Load clears the unreadable state. Before the fix, focused tests
reproduced the NullReferenceException and blocked-save-after-removal defects; afterward all
11 profile cases passed.

Current environment: Ubuntu 26.04, .NET SDK 10.0.115. A forced solution restore removed stale
Windows NuGet fallback paths from generated build assets; no project change was needed.

The initial full SQL run and a retry with FATSTACKS_TEST_SERVER=host.docker.internal,1433
each produced 13 fixture connection failures (safe SQL category 0) and four missing-fixture
skips. The host TCP port is reachable, but this does not establish successful SQL access.
These current failures do not supersede or erase the earlier successful SQL evidence above;
SQL behavior after the convergence changes requires a successful target run.

Windows WPF, DPAPI, interactive keyboard/focus checks, and actual window-close behavior remain
unverified in this Linux environment. Added Windows test source alone is not execution evidence.
T076/T027 and T077/T018 remain unchecked until their required target checks execute.

T070 promotes Optional to Mandatory for the first connection attempt. Certificate and
unsupported-encryption fallbacks can occur in either order and each is bounded to one change.
Authentication/protocol failures retain their failure outcome; Strict remains strict.
All 14 synthetic TransportPolicyTests passed. These tests exercise policy with constructed
provider errors and do not prove real endpoint negotiation.

The strengthened T077 matrix begins with certificate checking enabled, rejects preconfigured
trust bypasses, confirms the expected initial failure, reads actual session encryption, and
asserts the applicable warning independently. Each endpoint is tested with Mandatory and
Optional input plus an invalid-login case. All 12 live cases are skipped for missing dedicated
fixtures: trusted TLS (3), plaintext (3), self-signed (3), expired (3).

`dotnet test --project tests/FatStacks.WindowsChecks/FatStacks.WindowsChecks.csproj --no-build
--filter 'FullyQualifiedName~ConnectionJourneyTests|FullyQualifiedName~WindowBehaviorTests'`
was attempted and ran zero tests: the Microsoft.WindowsDesktop.App runtime is unavailable.
The sources include startup cancellation, close during startup, close with an active book,
close waiting for an outstanding write, Test preference preservation, and active-removal rejection.
The deterministic suite also covers all save/discard/cancel outcomes and stable profile ID/book
pin after connection edits and a fresh profile-store load.

For Windows verification, run the entire WindowsChecks project in an unlocked Windows 11
session, then repeat the keyboard/focus and real-book close checks listed above. Configure the
four dedicated transport endpoints as described in docs/testing.md and rerun the SQL suite.
No extensions.yml exists, so there are no before/after implementation hooks to dispatch.
The existing .NET ignore patterns cover generated outputs and local credentials; no new ignore
file is needed for this change.

T075 removes the pre-Open persistence gate. Activation and transport warnings remain visible
when remembering the profile fails; explicit Save still reports failure. A refresh failure
also preserves the successful activation result. Reload restores selection by profile ID, and
successful Open selects the associated profile so a later Save retains its identity pin even
if the initial preferences write failed. Empty/duplicate names are rejected before switching.

T085 renders the active profile's current friendly name from local metadata. It retains the
same active session, captured connection, profile ID, and logical Book identity. The model
regression also edits saved server details and verifies that the active connection stays unchanged.
The Windows shell test renames the profile and closes Connections before checking the label.

Final automated evidence: 28 deterministic service tests passed; 14 synthetic transport-policy
cases passed; 8 connection-view-model cases passed on Linux using linked UI-independent sources
in an external temporary net10.0 harness. The last result exercises the actual model source,
but does not validate WPF bindings or rendered windows. The solution builds with zero warnings
and zero errors. Focused ProfileStore formatting verification passed. Working-tree whitespace
checks passed; pre-existing staged CRLF documentation produces whitespace warnings in the
cached diff and was preserved.

T069, T070, T075, and T085 are marked complete. T076 and T077 retain unchecked markers because
Windows execution and dedicated live transport fixtures remain unavailable. No database schema,
migration, dependency, credential-storage format, or financial workflow changed in this pass.

The temporary Linux harness is `/tmp/fatstacks-t075-harness/Harness.csproj`; it links
`ConnectionViewModelTests.cs`, `ConnectionViewModel.cs`, `ObservableObject.cs`, and
`AsyncCommand.cs`. Its run used:

```bash
dotnet build /tmp/fatstacks-t075-harness/Harness.csproj --artifacts-path /tmp/fatstacks-t075-artifacts -p:RestoreFallbackFolders= -p:NuGetAudit=false
dotnet /tmp/fatstacks-t075-artifacts/bin/Harness/debug/Harness.dll
```

The temporary harness is environment-local. The maintained tests live in WindowsChecks and
run there on Windows. The final ordinary `dotnet build FatStacks.slnx --no-restore` also
passed with zero warnings/errors; it does not depend on the temporary harness or audit override.

## PR #1 pipeline correction — 2026-11-18

Downloaded job archives under `.workspace/ph01-us01/unit-test-logs/` confirm the Windows
format failure is ENDOFLINE and the SQL failures are login category 18456. The SQL test step
received an empty password because its repository secret was unavailable. No financial or
schema assertion was reached by those 13 failed cases.

`.gitattributes` now enforces LF on checkout while preserving SQL definition merge attributes.
A disposable Windows-style CRLF checkout reproduced 2,399 formatting errors before the fix;
a fresh checkout with the attribute passes. Local full-solution formatting verification passes.

The SQL workflow now generates and masks an ephemeral container password, waits for an
authenticated health check, passes the same credential through GITHUB_ENV, and removes the
container even if tests fail. It requires no repository password secret. The exact startup
script was executed locally with the same SQL Server image and a clean source checkout:
26 SQL-suite cases passed, 13 prerequisites were skipped, and zero failed. The skipped cases
are the separate runtime login and 12 dedicated transport cases. Hosted Actions rerun remains
pending; these results do not establish interactive Windows acceptance.

## PR review: repeated New profile password reset — 2026-11-18

Review finding `discussion_r4612693725` identified a stale PasswordBox after pressing New
while no saved profile was selected. The model cleared its password but emitted no Selected
notification, so the dialog still showed the previous credential. New now notifies the dialog
on every reset, including a null-to-null selection. No password is added to bindings or events.

The focused model regression failed before the fix with the previous synthetic password still
displayed. After the fix all nine linked connection-view-model cases passed on Linux, and
all 28 service cases passed. An isolated solution build compiled the added WPF PasswordBox
regression with zero warnings/errors. That test resets New twice, verifies the visible field
is empty, then enters and saves a replacement credential. Actual WPF execution remains
unverified on Linux; run WindowBehaviorTests on an interactive Windows desktop.

## WindowsChecks profile selection — 2026-11-18

The supplied `windowschecks-2026.11.18_05.54.txt` records an actual Windows run:
22 total, 18 passed, four failed, none skipped. All four ConnectionJourneyTests stop in
DesktopFixture.SelectProfileAsync with `InvalidOperationException: Unsupported Pattern`.
Their later switch/restart/reassociation/preference assertions were not reached.

The fixture searched all descendants by friendly name and requested SelectionItemPattern
from the first match. WPF DisplayMemberPath can expose the matching name on a Text element,
which cannot be selected. The helper now searches ListItem containers, matches either the
container name or its descendant display text, and selects the container. No application
behavior or database code changed.

The follow-up `windowschecks-2026.11.18_05.58.txt` records 23 total, 22 passed,
one failed, none skipped. All four original journeys now pass. The only failure is the
new in-process UI Automation regression timing out while waiting for its worker task.
Its generic timeout message mentioned closing, but no window-close operation failed.

Moved `ProfileAutomationSelectsContainerWhenNameBelongsToDisplayText` into ConnectionJourneyTests
and changed it to launch a separate desktop process using the existing fixture. It selects
Second/First/Second and waits for the friendly-name and database fields to match each selected
profile. No startup preference is set, so the synthetic profiles need no SQL connection.
This avoids same-process UI Automation dispatch/reentrancy and the custom dispatcher-pump wait.
The isolated solution build passed with zero warnings/errors, and changed-file whitespace
verification passed. The replacement regression remains unexecuted on Linux and requires a
Windows rerun:

```powershell
dotnet run --project tests/FatStacks.WindowsChecks/FatStacks.WindowsChecks.csproj
```

### Windows rerun confirmation — 2026-11-18

The owner reports that all WindowsChecks now pass after moving the profile-selection
regression to the separate-process fixture. This confirms the rerun on the owner's Windows
environment, including the four previously failing connection journeys and the replacement
profile-selection regression. The final run's detailed log and exact counts were not supplied.
This is owner-reported execution evidence, not a run performed in the Linux sandbox.
Dedicated SQL transport fixtures and remaining manual keyboard/focus checks are unchanged.
