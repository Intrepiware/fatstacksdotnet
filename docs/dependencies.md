# Dependency review

The SDK is pinned to 10.0.115. Production targets .NET 10; desktop targets Windows.

| Direct dependency | Version | Purpose | Simpler alternative |
|---|---|---|---|
| EF Core SQL Server | 10.0.15 | Required mapped SQL access | Raw SQL adds mapping work |
| Microsoft.Data.SqlClient | 7.1.2 | Required parameterized SQL transport/procedure boundary | EF alone does not supply transport policy |
| System.Security.Cryptography.ProtectedData | 10.0.15 | Windows current-user DPAPI | Plaintext storage violates FR-002 |
| grate.sqlserver | 2.3.0 | Required schema owner; in-process explicit maintenance | EF migrations violate the constitution |
| Autofac | 9.3.4 | Constitution-required property injection | Built-in DI lacks that required behavior |
| MSTest.Sdk | 4.5.3 | Test runner, assertions, and required test evidence | A custom test runner adds maintenance |
| grate tool | 2.3.0 | Reproduce the selected schema tool for maintenance development | Runtime uses the library without CLI secrets |

All listed direct packages declare MIT licenses. Grate uses Microsoft's DI/logging APIs internally;
its small service provider is confined to the maintenance adapter. The application composition
root uses Autofac. Grate has no registered log sinks, which prevents SQL/connection details from
reaching application logs. WPF, System.Text.Json, and Windows UI Automation use platform APIs.
Existing Fluent assets retain their supplied license under src/FatStacks.Desktop/Assets/Fluent.

The restored dependency graph below is the reviewed baseline. It includes development/SDK assets;
it does not describe each release's final publish output. Framework/vendor libraries provide most
transitive dependencies. Their declared licenses are compatible with redistribution when notices
are retained. License-file entries must be retained with the relevant distribution. Follow SDK,
EF, SqlClient, Autofac, grate, and MSTest release/security notices before each release. These
projects have published maintained .NET-compatible releases; that alone does not prove reliability.
The current test/build results are recorded separately in the US1 validation report.

`dotnet list FatStacks.slnx package --vulnerable --include-transitive` reported no known vulnerable
packages in the configured NuGet advisory source during implementation. Re-run it before release;
this is an advisory-feed check, not an independent security audit.

| Package | Resolved version | Declared license |
|---|---|---|
| Autofac | 9.3.4 | MIT |
| Azure.Core | 1.51.1 | MIT |
| Azure.Identity | 1.18.0 | MIT |
| Dapper | 2.1.89 | Apache-2.0 |
| MSTest.Analyzers | 4.5.3 | MIT |
| MSTest.TestAdapter | 4.5.3 | MIT |
| MSTest.TestFramework | 4.5.3 | MIT |
| Microsoft.ApplicationInsights | 2.23.0 | MIT |
| Microsoft.Bcl.AsyncInterfaces | 10.0.2 | MIT |
| Microsoft.Bcl.Cryptography | 9.0.21 | MIT |
| Microsoft.Data.SqlClient | 7.1.2 | MIT |
| Microsoft.Data.SqlClient.Extensions.Abstractions | 7.1.2 | MIT |
| Microsoft.Data.SqlClient.Extensions.Azure | 7.1.1 | MIT |
| Microsoft.Data.SqlClient.Internal.Logging | 7.1.2 | MIT |
| Microsoft.Data.SqlClient.SNI.runtime | 7.1.2 | LICENSE.txt |
| Microsoft.DiaSymReader | 2.2.10 | MIT |
| Microsoft.EntityFrameworkCore | 10.0.15 | MIT |
| Microsoft.EntityFrameworkCore.Abstractions | 10.0.15 | MIT |
| Microsoft.EntityFrameworkCore.Analyzers | 10.0.15 | MIT |
| Microsoft.EntityFrameworkCore.Relational | 10.0.15 | MIT |
| Microsoft.EntityFrameworkCore.SqlServer | 10.0.15 | MIT |
| Microsoft.Extensions.Caching.Abstractions | 10.0.15 | MIT |
| Microsoft.Extensions.Caching.Memory | 10.0.15 | MIT |
| Microsoft.Extensions.Configuration | 10.0.14 | MIT |
| Microsoft.Extensions.Configuration.Abstractions | 10.0.15 | MIT |
| Microsoft.Extensions.Configuration.Binder | 10.0.14 | MIT |
| Microsoft.Extensions.Configuration.CommandLine | 10.0.14 | MIT |
| Microsoft.Extensions.Configuration.EnvironmentVariables | 10.0.14 | MIT |
| Microsoft.Extensions.Configuration.FileExtensions | 10.0.14 | MIT |
| Microsoft.Extensions.Configuration.Json | 10.0.14 | MIT |
| Microsoft.Extensions.Configuration.UserSecrets | 10.0.14 | MIT |
| Microsoft.Extensions.DependencyInjection | 10.0.15 | MIT |
| Microsoft.Extensions.DependencyInjection.Abstractions | 10.0.15 | MIT |
| Microsoft.Extensions.DependencyModel | 10.0.10 | MIT |
| Microsoft.Extensions.Diagnostics | 10.0.14 | MIT |
| Microsoft.Extensions.Diagnostics.Abstractions | 10.0.14 | MIT |
| Microsoft.Extensions.FileProviders.Abstractions | 10.0.14 | MIT |
| Microsoft.Extensions.FileProviders.Physical | 10.0.14 | MIT |
| Microsoft.Extensions.FileSystemGlobbing | 10.0.14 | MIT |
| Microsoft.Extensions.Hosting | 10.0.14 | MIT |
| Microsoft.Extensions.Hosting.Abstractions | 10.0.14 | MIT |
| Microsoft.Extensions.Logging | 10.0.15 | MIT |
| Microsoft.Extensions.Logging.Abstractions | 10.0.15 | MIT |
| Microsoft.Extensions.Logging.Configuration | 10.0.14 | MIT |
| Microsoft.Extensions.Logging.Console | 10.0.14 | MIT |
| Microsoft.Extensions.Logging.Debug | 10.0.14 | MIT |
| Microsoft.Extensions.Logging.EventLog | 10.0.14 | MIT |
| Microsoft.Extensions.Logging.EventSource | 10.0.14 | MIT |
| Microsoft.Extensions.Options | 10.0.15 | MIT |
| Microsoft.Extensions.Options.ConfigurationExtensions | 10.0.14 | MIT |
| Microsoft.Extensions.Primitives | 10.0.15 | MIT |
| Microsoft.Identity.Client | 4.84.2 | MIT |
| Microsoft.Identity.Client.Broker | 4.84.2 | MIT |
| Microsoft.Identity.Client.Extensions.Msal | 4.78.0 | MIT |
| Microsoft.Identity.Client.NativeInterop | 0.20.6 | LICENSE |
| Microsoft.IdentityModel.Abstractions | 8.16.0 | MIT |
| Microsoft.IdentityModel.JsonWebTokens | 8.16.0 | MIT |
| Microsoft.IdentityModel.Logging | 8.16.0 | MIT |
| Microsoft.IdentityModel.Protocols | 8.16.0 | MIT |
| Microsoft.IdentityModel.Protocols.OpenIdConnect | 8.16.0 | MIT |
| Microsoft.IdentityModel.Tokens | 8.16.0 | MIT |
| Microsoft.SqlServer.Server | 1.0.0 | MIT |
| Microsoft.TestPlatform.ObjectModel | 18.10.2 | MIT |
| Microsoft.Testing.Extensions.CodeCoverage | 18.12.0 | License.txt |
| Microsoft.Testing.Extensions.Telemetry | 2.5.3 | MIT |
| Microsoft.Testing.Extensions.TrxReport | 2.5.3 | MIT |
| Microsoft.Testing.Extensions.TrxReport.Abstractions | 2.5.3 | MIT |
| Microsoft.Testing.Platform | 2.5.3 | MIT |
| Microsoft.Testing.Platform.MSBuild | 2.5.3 | MIT |
| Mono.Cecil | 0.11.6 | MIT |
| System.ClientModel | 1.9.0 | MIT |
| System.Configuration.ConfigurationManager | 9.0.21 | MIT |
| System.Diagnostics.EventLog | 10.0.14 | MIT |
| System.IdentityModel.Tokens.Jwt | 8.16.0 | MIT |
| System.Memory.Data | 10.0.1 | MIT |
| System.Security.Cryptography.Pkcs | 10.0.14 | MIT |
| System.Security.Cryptography.ProtectedData | 10.0.15 | MIT |
| System.Threading.RateLimiting | 9.0.21 | MIT |
| grate.core | 2.3.0 | MIT |
| grate.sqlserver | 2.3.0 | MIT |
