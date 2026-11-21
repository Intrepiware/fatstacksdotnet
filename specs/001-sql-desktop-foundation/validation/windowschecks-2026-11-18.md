# WindowsChecks — user-reported execution

Received: 2026-11-18. Source: console output supplied by the user in the development conversation.
The agent did not execute this Windows run. No test filter was supplied.

| Result | Value |
| --- | --- |
| Project | tests/FatStacks.WindowsChecks |
| Target | net10.0-windows, x64 |
| Total / succeeded | 33 / 33 |
| Failed / skipped | 0 / 0 |
| Summary duration | 57s 170ms |
| Assembly execution duration | 56s 830ms |

## Supplied output

```text
PS C:\Working\Personal\fat-stacks-net> dotnet test --project tests/FatStacks.WindowsChecks
Running tests from tests\FatStacks.WindowsChecks\bin\Debug\net10.0-windows\FatStacks.WindowsChecks.dll (net10.0|x64)
tests\FatStacks.WindowsChecks\bin\Debug\net10.0-windows\FatStacks.WindowsChecks.dll (net10.0|x64) passed (56s 830ms)

Test run summary: Passed!
  total: 33
  failed: 0
  succeeded: 33
  skipped: 0
  duration: 57s 170ms
```

The passing run supplies execution evidence for the current WindowsChecks suite, including
its WPF, connection, account-dialog, register-model, and lifetime checks. The supplied output
is a suite summary, not an individual test listing or TRX report. Windows version, SDK version,
hardware, SQL placement, and a tested commit identifier were not supplied.

This supersedes earlier statements that the automated Windows suite had not run. It does
not establish screenshot parity, manual high-DPI/keyboard/accessibility review, performance
targets, or completion of the separate SQL/transport test suite. Those checks retain their
existing status. T036 remains open for visual comparisons; task checkboxes are unchanged.

A local copy of the supplied output is saved at
`.workspace/ph01-us02/unit-test-logs/windowschecks-2026.11.18-user-reported.txt`.
