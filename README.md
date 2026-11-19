# Fat Stacks.NET

A single-user Windows accounting application on .NET 10 and SQL Server.
The current implementation covers connection profiles, explicit book setup, and book switching
(US1). Account browsing and journal workflows are not yet delivered.

## Build and run

Use Windows 11 x64, the .NET SDK pinned in global.json, and SQL Server 2022 or later. Visual
Studio users need the .NET desktop development workload. Install the .NET 10 desktop runtime
to run a framework-dependent build without the SDK.

From the repository root:

```powershell
dotnet tool restore
dotnet restore FatStacks.slnx
dotnet build FatStacks.slnx --no-restore
dotnet test --project tests/FatStacks.Tests/FatStacks.Tests.csproj --no-build
dotnet run --project src/FatStacks.Desktop/FatStacks.Desktop.csproj
```

Linux can build the Windows target and run the service/SQL tests. It cannot run WPF or Windows
credential protection. A Linux build does not establish Windows runtime compatibility.

## Prepare a book

1. Use an existing empty SQL database. This application does not install SQL Server or create
   a database. SQL TCP/IP must be enabled when the server is reached over a network.
2. Open Connections. Enter a friendly name, server, database, and runtime authentication.
   Use a non-administrator runtime login with no schema-change permissions.
3. Select Initialize database. Enter a book name, USD/EUR/JPY currency, and Windows time zone.
   Supply a separate maintenance identity with schema deployment privileges. These maintenance
   credentials are used only for this action and are not saved.
4. Initialization runs the packaged grate migrations, creates the FatStacksRuntime database
   role, then creates one book. A repeat preserves an existing compatible book. Unrelated and
   incompatible schemas are rejected without alteration.
5. An operator maps the runtime login to a user in the selected database and adds that user to
   FatStacksRuntime. Do not add it to db_owner, db_ddladmin, or server administrator roles.
6. Save/Open the profile using the runtime login. Its logical Book ID is pinned after a
   successful association. Later identity mismatches require explicit Reassociate book.

The application expects stored schema version 1. Missing, unreadable, or unequal version is
fatal to that connection attempt. The connection dialog remains available. Opening never
initializes or upgrades a schema. Use explicit maintenance for later schema changes.

## Profiles and connection behavior

Profiles live in `%LOCALAPPDATA%/FatStacks/profiles.json`. Windows DPAPI protects each complete
connection string for the current user. Friendly names and Book/profile IDs remain readable.
To use an isolated settings file, set FATSTACKS_PROFILE_PATH before launch. Keep that path
outside the repository or in an ignored secret folder. A different Windows user cannot unlock
saved credentials. An unreadable profile file is preserved and must be restored or corrected
before it can be replaced.

A successful Open remembers the profile ID. Test and failed switches do not change that
preference. Renaming preserves IDs. Removing a profile changes local settings only; disconnect
it first. Switching clears prior session state and cancels obsolete reads. Future writers must
capture the active BookSession and hold its BeginWrite lease until their database work finishes.
US1 has no editor, so it has no unsaved journal drafts to resolve yet.

TLS is preferred. Self-signed/expired certificate trust and plaintext connections produce visible
non-blocking warnings. An unverified certificate does not authenticate the server. Authentication
failures remain errors. The connection dialog exposes transport options when needed.

## Tests and evidence

See [docs/testing.md](docs/testing.md), [database/README.md](database/README.md), and
[US1 validation](specs/001-sql-desktop-foundation/validation/us1.md). Credentials stay in environment
variables or ignored local files. Do not put passwords in commands, source, or logs.

The application does not manage backups, storage encryption, or automatic migration recovery.
Operators may manage their own backups. Release-specific migrations must document compatibility
and recovery effects. New databases are the current supported starting point.
