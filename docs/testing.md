# Testing

## Deterministic services

Run `dotnet test --project tests/FatStacks.Tests/FatStacks.Tests.csproj`. These tests run on Linux
or Windows. They cover profile identity, protected-storage failure boundaries, schema comparison,
session switching, write completion, and cancellation. The test-only reversible secret protector
is never registered in production. Windows DPAPI behavior is covered by the Windows journeys.

## SQL Server boundary

Run [CreateTestMaintenanceLogin.sql](../database/CreateTestMaintenanceLogin.sql) in SSMS
as a SQL Server administrator. Set its password in the query window. Do not save the
password in the script. It creates the FatStacksTestMaintenance login, its master
database user, and the CREATE DATABASE grant. The login owns the databases it creates,
so it can install their schema and remove them after tests. It does not need sysadmin
or the dbcreator role. The grant permits database creation across this development
instance; it cannot restrict database names to the test prefix.

Use this login in FATSTACKS_MAINTENANCE_CONNECTION. Keep normal application credentials
separate. The fixture uses maintenance credentials for schema and failure tests. Its separate runtime-login
check maps the runtime login into each isolated database and grants FatStacksRuntime membership.
It verifies book opening and DDL denial through that login. A second check impersonates a
database-local runtime user. Configure FATSTACKS_RUNTIME_CONNECTION or the existing test UDL
for the separate login check; it reports Inconclusive when those credentials are absent.

Set FATSTACKS_MAINTENANCE_CONNECTION outside the repository. FATSTACKS_TEST_CONNECTION is an
alternative when that identity also has maintenance privileges. An ignored
`.credentials/fatstacks_maintenance.udl` is the local maintenance fallback. The older
`.credentials/fatstacks_test.udl` is used only when the maintenance file is absent. No credential value is logged. Use
FATSTACKS_TEST_SERVER to override just its server address; from Docker this is commonly
`host.docker.internal,1433`. A Windows `(local)` connection may use shared memory; enable SQL
TCP/IP and its firewall access for Linux tests.

The fixture creates only randomly named FatStacks_Test_ databases and drops only those databases
in cleanup. It never drops or resets the database named in the supplied UDL. The test identity
needs CREATE DATABASE and schema-maintenance privileges. This test requirement does not grant
those privileges to normal application access. The schema tests impersonate a database-local
runtime-role user to verify DDL denial.

```powershell
dotnet test --project tests/FatStacks.SqlTests/FatStacks.SqlTests.csproj
```

Configure FATSTACKS_SQL_VALID_TLS, FATSTACKS_SQL_PLAINTEXT, FATSTACKS_SQL_SELF_SIGNED, and
FATSTACKS_SQL_EXPIRED only when dedicated transport fixtures exist. Missing cases report
Inconclusive/skipped; they are not passing coverage. These variables contain complete SqlClient
connection strings, including a database and credentials. They are independent of the maintenance
UDL and FATSTACKS_TEST_SERVER override. Use dedicated TCP endpoints with these properties:

| Variable | Required endpoint condition | Expected result |
| --- | --- | --- |
| FATSTACKS_SQL_VALID_TLS | Certificate chains to a client-trusted CA, is current, and matches the hostname | Encrypted session, certificate checking enabled, no warning |
| FATSTACKS_SQL_PLAINTEXT | Server explicitly reports encryption unsupported; this is not merely Force Encryption disabled | Plaintext session, only the not-encrypted warning |
| FATSTACKS_SQL_SELF_SIGNED | Current self-signed certificate, untrusted by the client, matching hostname | Initial certificate rejection, encrypted retry, only the certificate warning |
| FATSTACKS_SQL_EXPIRED | Expired certificate with otherwise trusted chain and matching hostname | Initial certificate rejection, encrypted retry, only the certificate warning |

All fixture strings must start with TrustServerCertificate=False and without ServerCertificate
pinning. Provision certificate properties separately; a connection string cannot establish that
a certificate is specifically self-signed or expired. The tests independently verify initial
certificate rejection, but do not inspect certificate validity dates or chain configuration.
The fixture login must be able to query its own sys.dm_exec_connections row so actual session
encryption can be asserted. Tests disable pooling, use a five-second connection timeout, and
only open connections and read session encryption; they do not modify these fixture databases.
Each endpoint is tested with both Mandatory and Optional profile settings, and with deliberately
invalid SQL credentials to verify preserved login failures and sanitized service errors.

Run the deterministic policy and dedicated fixture tests without creating maintenance databases:

```bash
dotnet test --project tests/FatStacks.SqlTests/FatStacks.SqlTests.csproj --filter 'FullyQualifiedName~TransportPolicyTests|FullyQualifiedName~ConfiguredTransportMatrix|FullyQualifiedName~DedicatedFixtureAuthenticationFailureIsPreserved'
```

Transport convergence check on 2026-11-18, Linux / .NET SDK 10.0.115: 14 deterministic policy
cases passed, zero failed. These cover TLS-first Optional, bounded certificate/encryption retries
in either order, authentication/protocol failure preservation, Strict, and cancellation.
Dedicated live results are recorded separately below; skipped cases are unverified:

| Scenario | Mandatory / Optional | Invalid-login check |
| --- | --- | --- |
| Trusted TLS | Both skipped: fixture absent | Skipped: fixture absent |
| Plaintext | Both skipped: fixture absent | Skipped: fixture absent |
| Self-signed | Both skipped: fixture absent | Skipped: fixture absent |
| Expired | Both skipped: fixture absent | Skipped: fixture absent |

The combined targeted run had 26 cases: 14 passed, 12 skipped, zero failed. This supports T070's
policy regression; T077 live-fixture acceptance remains open. A subsequent sequential
`dotnet build FatStacks.slnx --no-restore` passed with zero warnings and zero errors;
`git diff --check` also passed. The coordinator separately reported
that the full SQL suite failed 13 fixture opens with SQL category 0, including with the
host.docker.internal,1433 override; TCP reachability does not establish a usable SQL session.
No live transport acceptance follows from that run. Windows execution remains unavailable on Linux.

The SQL CI job generates a temporary password for its disposable SQL Server container, masks
it in logs, waits for an authenticated health check, and supplies the same credential to the
tests. It publishes SQL only on a random loopback port and removes the container after the run.
No repository SQL password secret is required. Dedicated transport fixtures and a separate
runtime login remain additional checks; missing fixtures are reported as skipped.

## Interactive Windows

Build on Windows and run the following from an unlocked interactive desktop:

```powershell
dotnet test --project tests/FatStacks.WindowsChecks/FatStacks.WindowsChecks.csproj
```

The suite launches the built Desktop DLL with dotnet. Override FATSTACKS_DESKTOP_DLL if needed.
It uses a temporary FATSTACKS_PROFILE_PATH so it cannot overwrite real profile settings. It
creates isolated SQL fixture databases, exercises actual WPF controls using Windows UI Automation,
and cleans up its own processes/settings. It covers switch/restart/rename, failed-switch state,
and explicit reassociation. The source compiles on Linux; execution requires Windows Desktop.

Manual US1 checks still include entering runtime/maintenance credentials, first-run initialization,
startup cancellation, encrypted/plaintext/certificate warning visibility, keyboard focus, and
profile-removal restrictions. Record steps/results in the US1 validation report. Account/register
manual cases are listed in the current checklist below; editor workflows remain deferred.
A hosted Windows build without an interactive desktop
must not be reported as successful UI acceptance.

## Current manual acceptance

Use [the US1/US2 manual checklist](../specs/001-sql-desktop-foundation/validation/manual-us2.md).
It separates appearance/scaling, keyboard and real account operations, shutdown/session checks,
and history-dependent register cases. Record missing sample data as Blocked; the automated SQL
fixtures are temporary and do not leave a book for manual browsing. The recorded 33-case
WindowsChecks pass does not automatically complete these manual observations.

## Diagnostics

Do not add raw provider exceptions, SQL parameter logging, or credential values to failing tests.
Use safe failure categories. Check database configuration outside test output when authentication
or network access fails. Keep unavailable target-environment checks marked unverified.

## Sharing WindowsChecks results

Paste the console output into the development conversation, or save it under
`.workspace/ph01-us02/unit-test-logs/` and provide the filename. Keep the command,
pass/fail/skip summary, and any failure details together. For example, from PowerShell
at the repository root:

```powershell
New-Item -ItemType Directory -Force .workspace/ph01-us02/unit-test-logs | Out-Null
$resultsPath = ".workspace/ph01-us02/unit-test-logs/windowschecks-$(Get-Date -Format 'yyyy.MM.dd_HH.mm').txt"
dotnet test --project tests/FatStacks.WindowsChecks/FatStacks.WindowsChecks.csproj 2>&1 | Tee-Object -FilePath $resultsPath
```

The workspace folder holds raw local evidence. Reviewed US2 outcomes belong in
`specs/001-sql-desktop-foundation/validation/us2.md`; results are unverified until the
actual run output is reviewed.
