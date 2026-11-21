# US2 register performance

**Status** — Run 3 passed SC-008 with the revised SQL procedure.

The benchmark creates a disposable SQL test book with 100 accounts and 10,000 balanced
entries. It uses the desktop app to select accounts, enter or clear search terms, and invoke
Reload. It measures each operation until the register shows the expected result. Refresh
runs confirm a new result by changing the selected account name before the timed interval.
The benchmark uses a running app and an initialized book. It does not clear SQL caches
between runs. The benchmark locates the status and account title controls before timing
and polls those controls directly during each measurement.

On the agreed Windows machine, configure the SQL test fixture as described in
[the testing guide](../../../docs/testing.md). Use a signed-in desktop and build the
application at its default output location.

```powershell
dotnet build FatStacks.slnx
$env:FATSTACKS_RUN_REGISTER_BENCHMARK = '1'
dotnet test --project tests/FatStacks.WindowsChecks/FatStacks.WindowsChecks.csproj --filter 'FullyQualifiedName~RepresentativeRegisterMeetsWindowsResponseTarget' --output Detailed
Remove-Item Env:FATSTACKS_RUN_REGISTER_BENCHMARK
```

Record the CPU, RAM, Windows version, app build, SQL Server placement, and cache conditions
with the output. The test prints all 20 measurements and the count within two seconds for
each operation. SC-008 requires at least 19 of 20 runs within two seconds for each operation.

| Operation | Run 1 within two seconds | Run 2 within two seconds | Run 3 within two seconds |
| --- | --- | --- | --- |
| Account selection | 1/20 | 0/20 | 20/20 |
| Register search | 0/20 | 0/20 | 20/20 |
| Register refresh | 0/20 | 0/20 | 20/20 |

Run 1 at `555bc94` timed desktop-wide UI Automation lookups. Run 2 polled the controls
directly and still missed the target for all three operations.

CPU: 12th Gen i7-12800H, 2400 Mhz, 14 cores
RAM: 32GB
Windows version: Windows 11 Professional 26H2
app build: 555bc94, Debug
SQL Server placement: local

## WindowsChecks Test Run 1 - Results

```
PS C:\Working\Personal\fat-stacks-net>
>> dotnet build FatStacks.slnx
>> $env:FATSTACKS_RUN_REGISTER_BENCHMARK = '1'
>> dotnet test --project tests/FatStacks.WindowsChecks/FatStacks.WindowsChecks.csproj --filter 'FullyQualifiedName~RepresentativeRegisterMeetsWindowsResponseTarget' --output Detailed
>> Remove-Item Env:FATSTACKS_RUN_REGISTER_BENCHMARK
Restore complete (0.8s)
  FatStacks.Core net10.0 succeeded (0.4s) → src\FatStacks.Core\bin\Debug\net10.0\FatStacks.Core.dll
  FatStacks.Infrastructure net10.0 succeeded (0.4s) → src\FatStacks.Infrastructure\bin\Debug\net10.0\FatStacks.Infrastructure.dll
  FatStacks.Desktop net10.0-windows succeeded (0.4s) → src\FatStacks.Desktop\bin\Debug\net10.0-windows\FatStacks.Desktop.dll
  FatStacks.SqlTests net10.0 succeeded (0.5s) → tests\FatStacks.SqlTests\bin\Debug\net10.0\FatStacks.SqlTests.dll
  FatStacks.Tests net10.0 succeeded (0.6s) → tests\FatStacks.Tests\bin\Debug\net10.0\FatStacks.Tests.dll
  FatStacks.WindowsChecks net10.0-windows succeeded (0.6s) → tests\FatStacks.WindowsChecks\bin\Debug\net10.0-windows\FatStacks.WindowsChecks.dll

Build succeeded in 3.0s
Running tests from tests\FatStacks.WindowsChecks\bin\Debug\net10.0-windows\FatStacks.WindowsChecks.dll (net10.0|x64)
failed RepresentativeRegisterMeetsWindowsResponseTarget (3m 33s 396ms)
  from tests\FatStacks.WindowsChecks\bin\Debug\net10.0-windows\FatStacks.WindowsChecks.dll (net10.0|x64)
  Assertion failed. Expected collection to be empty.
  Account selection, Search, Refresh

  expected count: 0
  actual count:   3

  Assert.IsEmpty(failures)
    at FatStacks.WindowsChecks.RegisterPerformanceTests.RepresentativeRegisterMeetsWindowsResponseTarget() in tests\FatStacks.WindowsChecks\RegisterPerformanceTests.cs:82
    at FatStacks.WindowsChecks.RegisterPerformanceTests.RepresentativeRegisterMeetsWindowsResponseTarget() in tests\FatStacks.WindowsChecks\RegisterPerformanceTests.cs:82
    at Microsoft.VisualStudio.TestPlatform.MSTest.TestAdapter.Execution.TestMethodInfo.ExecuteInternalAsync(Object[] arguments, CancellationTokenSource timeoutTokenSource, TestInvocationState invocationState)
  Standard output


    TestContext Messages:
    Account selection (ms): 1994, 3777, 2416, 3056, 2040, 3173, 2186, 3209, 2196, 3285, 2466, 7848, 2256, 3205, 2302, 3245, 2238, 3267, 2187, 3356
    Account selection within two seconds: 1/20
    Search (ms): 3145, 3181, 3156, 3092, 3246, 3160, 3131, 3197, 3114, 3243, 3057, 8101, 3334, 3341, 3321, 3127, 3229, 3306, 3143, 3304
    Search within two seconds: 0/20
    Refresh (ms): 3436, 3362, 3237, 3363, 3271, 3101, 3300, 3531, 3295, 3508, 3290, 3214, 3338, 3336, 3356, 3394, 2805, 3716, 3306, 3524
    Refresh within two seconds: 0/20

  Error output
tests\FatStacks.WindowsChecks\bin\Debug\net10.0-windows\FatStacks.WindowsChecks.dll (net10.0|x64) failed with 1 error(s) (3m 34s 038ms)
Exit code: 2

Test run summary: Failed!
  total: 1
  failed: 1
  succeeded: 0
  skipped: 0
  duration: 3m 34s 610ms
Test run completed with non-success exit code: 2. At least one test failed. See: https://aka.ms/testingplatform/exitcodes
```

## WindowsChecks Test Run 2 - Results

```
PS C:\Working\Personal\fat-stacks-net>
>> dotnet build FatStacks.slnx
>> $env:FATSTACKS_RUN_REGISTER_BENCHMARK = '1'
>> dotnet test --project tests/FatStacks.WindowsChecks/FatStacks.WindowsChecks.csproj --filter 'FullyQualifiedName~RepresentativeRegisterMeetsWindowsResponseTarget' --output Detailed
>> Remove-Item Env:FATSTACKS_RUN_REGISTER_BENCHMARK
Restore complete (0.7s)
  FatStacks.Core net10.0 succeeded (0.2s) → src\FatStacks.Core\bin\Debug\net10.0\FatStacks.Core.dll
  FatStacks.Infrastructure net10.0 succeeded (0.2s) → src\FatStacks.Infrastructure\bin\Debug\net10.0\FatStacks.Infrastructure.dll
  FatStacks.Desktop net10.0-windows succeeded (0.3s) → src\FatStacks.Desktop\bin\Debug\net10.0-windows\FatStacks.Desktop.dll
  FatStacks.Tests net10.0 succeeded (0.3s) → tests\FatStacks.Tests\bin\Debug\net10.0\FatStacks.Tests.dll
  FatStacks.SqlTests net10.0 succeeded (0.3s) → tests\FatStacks.SqlTests\bin\Debug\net10.0\FatStacks.SqlTests.dll
  FatStacks.WindowsChecks net10.0-windows succeeded (0.3s) → tests\FatStacks.WindowsChecks\bin\Debug\net10.0-windows\FatStacks.WindowsChecks.dll

Build succeeded in 1.9s
Running tests from tests\FatStacks.WindowsChecks\bin\Debug\net10.0-windows\FatStacks.WindowsChecks.dll (net10.0|x64)
failed RepresentativeRegisterMeetsWindowsResponseTarget (3m 59s 616ms)
  from tests\FatStacks.WindowsChecks\bin\Debug\net10.0-windows\FatStacks.WindowsChecks.dll (net10.0|x64)
  Assertion failed. Expected collection to be empty.
  Account selection, Search, Refresh

  expected count: 0
  actual count:   3

  Assert.IsEmpty(failures)
    at FatStacks.WindowsChecks.RegisterPerformanceTests.RepresentativeRegisterMeetsWindowsResponseTarget() in tests\FatStacks.WindowsChecks\RegisterPerformanceTests.cs:84
    at FatStacks.WindowsChecks.RegisterPerformanceTests.RepresentativeRegisterMeetsWindowsResponseTarget() in tests\FatStacks.WindowsChecks\RegisterPerformanceTests.cs:84
    at Microsoft.VisualStudio.TestPlatform.MSTest.TestAdapter.Execution.TestMethodInfo.ExecuteInternalAsync(Object[] arguments, CancellationTokenSource timeoutTokenSource, TestInvocationState invocationState)
  Standard output


    TestContext Messages:
    Account selection (ms): 3245, 3923, 2643, 3828, 2462, 3533, 2777, 4649, 3063, 4989, 7659, 3922, 2617, 3843, 2929, 3808, 2468, 3501, 2599, 3686
    Account selection within two seconds: 0/20
    Search (ms): 3891, 4224, 3838, 3712, 3780, 3774, 3788, 4224, 5247, 3850, 3911, 3777, 3703, 3602, 3836, 3744, 3750, 3634, 3579, 3553
    Search within two seconds: 0/20
    Refresh (ms): 3964, 3746, 3568, 3599, 4070, 4117, 3634, 3744, 3683, 3746, 3683, 3630, 3790, 3761, 3794, 3743, 3711, 3702, 3708, 3762
    Refresh within two seconds: 0/20

  Error output
tests\FatStacks.WindowsChecks\bin\Debug\net10.0-windows\FatStacks.WindowsChecks.dll (net10.0|x64) failed with 1 error(s) (4m 00s 265ms)
Exit code: 2

Test run summary: Failed!
  total: 1
  failed: 1
  succeeded: 0
  skipped: 0
  duration: 4m 00s 854ms
Test run completed with non-success exit code: 2. At least one test failed. See: https://aka.ms/testingplatform/exitcodes
```

## Register database comparison

A SQL Server 2022 container on Linux used the same 100-account, 10,000-entry fixture.
The comparison timed calls through the app's register repository, including result transfer
and materialization. It ran one warm-up and five timed calls per search term for each
procedure version.

| Register read         | Previous median | Revised median |
| --------------------- | --------------- | -------------- |
| No filter, 6,000 rows | 1,164 ms        | 91 ms          |
| Search, 1 row         | 680 ms          | 90 ms          |

The revised procedure builds account paths once and groups counter-account labels by entry.
Both versions returned identical rows and balances in the comparison. Both register SQL
regressions passed. Windows run 3 below measured the end-to-end result with the revised procedure.

## WindowsChecks Test Run 3 - Results

```

>> dotnet build FatStacks.slnx
>> $env:FATSTACKS_RUN_REGISTER_BENCHMARK = '1'
>> dotnet test --project tests/FatStacks.WindowsChecks/FatStacks.WindowsChecks.csproj --filter 'FullyQualifiedName~RepresentativeRegisterMeetsWindowsResponseTarget' --output Detailed
>> Remove-Item Env:FATSTACKS_RUN_REGISTER_BENCHMARK
Restore complete (1.2s)
  FatStacks.Core net10.0 succeeded (0.2s) → src\FatStacks.Core\bin\Debug\net10.0\FatStacks.Core.dll
  FatStacks.Infrastructure net10.0 succeeded (0.1s) → src\FatStacks.Infrastructure\bin\Debug\net10.0\FatStacks.Infrastructure.dll
  FatStacks.Desktop net10.0-windows succeeded (0.2s) → src\FatStacks.Desktop\bin\Debug\net10.0-windows\FatStacks.Desktop.dll
  FatStacks.Tests net10.0 succeeded (0.6s) → tests\FatStacks.Tests\bin\Debug\net10.0\FatStacks.Tests.dll
  FatStacks.SqlTests net10.0 succeeded (0.6s) → tests\FatStacks.SqlTests\bin\Debug\net10.0\FatStacks.SqlTests.dll
  FatStacks.WindowsChecks net10.0-windows succeeded (0.6s) → tests\FatStacks.WindowsChecks\bin\Debug\net10.0-windows\FatStacks.WindowsChecks.dll

Build succeeded in 2.7s
Running tests from tests\FatStacks.WindowsChecks\bin\Debug\net10.0-windows\FatStacks.WindowsChecks.dll (net10.0|x64)
passed RepresentativeRegisterMeetsWindowsResponseTarget (36s 860ms)
  from tests\FatStacks.WindowsChecks\bin\Debug\net10.0-windows\FatStacks.WindowsChecks.dll (net10.0|x64)
  Standard output


    TestContext Messages:
    Account selection (ms): 386, 551, 418, 576, 394, 543, 394, 531, 389, 548, 388, 510, 369, 405, 355, 457, 340, 439, 305, 424
    Account selection within two seconds: 20/20
    Search (ms): 346, 380, 325, 346, 326, 344, 317, 375, 316, 346, 330, 346, 326, 399, 321, 377, 322, 391, 327, 396
    Search within two seconds: 20/20
    Refresh (ms): 481, 445, 455, 431, 440, 481, 398, 362, 433, 425, 440, 452, 429, 459, 399, 426, 393, 418, 428, 393
    Refresh within two seconds: 20/20

  Error output
tests\FatStacks.WindowsChecks\bin\Debug\net10.0-windows\FatStacks.WindowsChecks.dll (net10.0|x64) passed (37s 380ms)

Test run summary: Passed!
  total: 1
  failed: 0
  succeeded: 1
  skipped: 0
```
