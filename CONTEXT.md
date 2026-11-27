# Credential Rotation

Tracks when credentials used by services must be rotated and where they must be updated. Never holds credential values.

## Language

**Project**:
A group of one or more services.
_Avoid_: Product, solution

**Service**:
A piece of software whose credentials are tracked. Belongs to exactly one project.
_Avoid_: Application, app, system, consumer

**Credential**:
A secret used by a service, known here only by its name, type, and rotation metadata.
_Avoid_: Secret, key, password

**Rotation Policy**:
The interval at which a single credential must be rotated. Each credential has its own policy.
_Avoid_: Schedule, rule

**Rotation**:
Replacing a credential with a new value. A rotation is complete when a team member marks it complete.

**Location**:
A place where a credential is stored or referenced and must be updated on rotation (code, config, vault, pipeline).
_Avoid_: Target, destination

**Team**:
A dev team of several members that owns services and is responsible for rotating their credentials.
_Avoid_: Owner, user

## Open questions

- Shared credentials: may belong to a shared sub-service rather than to several services. Decide when a feature needs it.
