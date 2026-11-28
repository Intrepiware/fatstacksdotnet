# US4 line reconciliation verification

## Portable and SQL results

The Linux x64 solution build included the WPF application and Windows checks. All 116 portable
tests passed. The repeated-posting view-model cases retained the selected line and its displayed
status and balance on conflict and unconfirmed writes.

On an isolated SQL Server 2022 instance, all 17 reconciliation, register, and journal write
tests passed. The register case checked two Bank postings in the same entry, one debit and one
credit, with distinct IDs, statuses, and running balances. The reconciliation tests covered all
six distinct ordered state pairs on a transfer, independent rereads, returned versions, and
stable financial values. A competing whole-entry edit and status edit could not both commit.
Rejecting the parent timestamp update left the status and both rowversions intact.

The full SQL suite had 46 passes and zero failures. Twelve transport cases skipped because
their dedicated connection fixtures were unavailable. The final solution build completed with
zero warnings and errors.

## Interactive Windows journey

Run on an unlocked Windows desktop with the project's SQL test connection configured:

```powershell
dotnet test --project tests/FatStacks.WindowsChecks/FatStacks.WindowsChecks.csproj --filter "FullyQualifiedName~ReconciliationJourneyTests"
```

The suite includes a repeated-account entry, the register C button, Space, Up/Down, R/C/U,
read-only detail markers, a stale update notice, selection after refresh, and process restart.
Six additional cases exercise each distinct transfer status pair through refresh and restart.
The Windows assembly cross-built on Linux. Launching it here stopped before test discovery
because Microsoft.WindowsDesktop.App is unavailable. Target-platform execution is pending.

The owner confirmed that the focused ReconciliationJourneyTests suite passed on Windows.
This completes the interactive Windows verification for T133.
