# Database package

Grate 2.3.0 owns all database definitions and deployment. The application calls the grate library
from its explicit maintenance adapter. Runtime opening never deploys schema. The local tool
manifest supports development use; do not pass credentials as command-line arguments.

## Folder ownership

- `up/`: immutable one-time creation/evolution scripts, in filename order.
- `types/`: journal table-valued parameter definitions. The maintenance adapter maps grate’s
  `RunFirstAfterUp` stage to this folder so types deploy before dependent procedures.
  For direct grate maintenance, include `--folders "runfirstafterup=types"`.
- `sprocs/` and `views/`: one current definition per object. Git uses readable text and `-merge`.
- `permissions/`: repeatable runtime-role grants, run with maintenance privileges.
- `afterMigration/`: publish expected schema version only after preceding deployment steps succeed.

Write the schema version only after all deployment steps succeed. Use afterMigration instead
of an earlier up script so procedure and permission changes finish first. Initial schema version is 1.
Applied migrations must stay immutable. Add future table changes as new up scripts. These
creation and alteration scripts are the authoritative table definitions; keep no separate copies.
Increment the stored version with incompatible application updates.

## Permissions and repeat setup

Use an existing empty database and a maintenance identity that can deploy its schema. Grate
runs the deployment transaction with database creation disabled. The adapter includes permissions
and afterMigration in this transaction. This avoids waiting on its own uncommitted schema locks
and keeps schema-version publication atomic with deployment. It keeps its own migration
history conventions. Normal application credentials use the FatStacksRuntime role and have
no schema-change privilege. Operators create/map the runtime login separately. This package
creates no server login and does not change server-wide configuration.

Explicit setup rejects unrelated/incompatible schemas before alteration. A compatible Book is
returned unchanged. A compatible schema with no Book can complete book setup. An interrupted
or versionless deployment stays disconnected; inspect grate history and perform maintenance
explicitly. Do not restore access by manually setting the schema version before verifying the schema.

The initial migration creates no user financial records. Recover a failed first deployment by
inspection and explicit maintenance; transaction rollback should preserve the prior state.
There is no automated repair, database provisioning, or application backup subsystem.

## US3 journal package compatibility

This increment adds two parameter types, SaveJournalEntry/DeleteJournalEntry, and their runtime
EXECUTE/REFERENCES grants. It changes no tables or applied one-time scripts; the stored schema
version stays 1. New databases receive the entire package through explicit initialization.
Existing books require an explicit maintenance deployment of the current grate package, with
`RunFirstAfterUp` mapped to `types` as above, before using journal actions. The Initialize
button preserves an already compatible book; opening a book does not deploy new procedures.
Use separate maintenance credentials through a secure local invocation; keep them out of command
arguments and logs. Confirm both procedure definitions and the runtime type/procedure grants.

Type scripts are conditional creation definitions. A future type shape change needs an explicit
migration plan for dependent procedures and type replacement; editing an applied table migration
is not an upgrade. Deployment failure must not publish a new schema version. Inspect grate history
and rerun maintenance explicitly after resolving a failed deployment. Journal operations roll
back on validation/conflict/SQL failure; an interrupted client response requires reloading before
retrying because a commit may already have succeeded. No financial history copies are retained
by these procedures, and deletion is physical.

The register's payee display also requires the current `sprocs/ReadAccountRegister.sql` definition.
It appends Payee to the result consumed by the application. Deploy that procedure to existing
books before running the updated build. Executing its CREATE OR ALTER definition directly in
SSMS with maintenance privileges is sufficient for this procedure-only update and retains its
runtime grants. It changes no tables or stored schema version.

## US4 per-posting register

Deploy the current `sprocs/ReadAccountRegister.sql` to existing books with the maintenance
package before using the per-posting register. It returns one row per selected-account line,
including the line ID and both version tokens used by reconciliation. Existing financial data,
table definitions, and runtime grants already support this operation. Schema version remains 1.
