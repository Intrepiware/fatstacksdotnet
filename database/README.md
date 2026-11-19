# Database package

Grate 2.3.0 owns all database definitions and deployment. The application calls the grate library
from its explicit maintenance adapter. Runtime opening never deploys schema. The local tool
manifest supports development use; do not pass credentials as command-line arguments.

## Folder ownership

- `up/`: immutable one-time creation/evolution scripts, in filename order.
- `types/`: SQL parameter type definitions when journal writes are delivered.
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
