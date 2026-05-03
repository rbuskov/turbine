# ADR-008: Host integration — schema registry, dependency ordering, cycle detection

- **Status:** Accepted
- **Date:** 2026-05-03
- **Deciders:** team

## Context

`AddTurbine` / `MapTurbine` need a runtime data structure that other Turbine
pieces (route handler metadata, validation middleware, OpenAPI contribution)
can read schemas from at request time, and a build process that runs every
`SchemaConfiguration.Configure(...)` once at host startup. Because schemas can
pull from one another via `AddPropertiesFrom` / `AddMappingsFrom`, the build
order across `SchemaConfiguration` subclasses matters: a downstream
configuration that inherits from an upstream one must see the upstream
populated when its `Configure(...)` reaches the inheriting call.

Three closely-related decisions had to be made together:

1. The shape and lifetime of the runtime registry.
2. How cross-`SchemaConfiguration` dependencies are discovered without
   modifying the public DSL.
3. The exception type and message contract used when those dependencies form
   a cycle.

## Decision

### Registry

`TurbineSchemaRegistry` is an `internal sealed` type, registered as a singleton
by `AddTurbine` and built once by `MapTurbine`. The registry is keyed by
`(SchemaConfiguration type, property name)` and exposes an internal
`Resolve(Type, string)` that returns the populated `ISchema` instance. The
type stays internal because the only consumers in scope are inside the
Turbine assembly (route handler metadata, validation middleware, OpenAPI
contribution); a public hook can be exposed later if a downstream library
needs one, with no caller migration.

### Dependency discovery

Dependencies are discovered by **instrumenting the existing builders** rather
than analysing `Configure` via expression trees or running a try-and-defer
multipass. Three internal moving parts make this work:

- **Slot pre-allocation.** Before any `Configure(...)` runs,
  `TurbineSchemaRegistry.Build(...)` reflects over each configuration's
  schema-typed properties / fields and instantiates an empty schema in every
  un-set slot via `Activator.CreateInstance(..., nonPublic: true)`. After
  this pass, `() => other.Foo` always resolves to a real instance, never
  `null`, regardless of whether `other`'s `Configure` has run yet.
- **Ambient build context.** An `internal` `TurbineBuildContext`
  (AsyncLocal-scoped) tracks the currently-being-configured
  `SchemaConfiguration` and a `schema-instance → owning configuration` map
  populated by `SchemaConfigurationBuilder.Schema(...)` and by the
  pre-allocation pass.
- **Deferred application of cross-config calls.** `AddPropertiesFrom` and
  `AddMappingsFrom` consult the ambient context. Same-configuration calls
  apply immediately (lexical order is the user's responsibility, as today).
  Cross-configuration calls are deferred — captured as a closure together
  with the insertion index inside the receiving schema — and a graph edge
  `currentConfig → sourceConfig` is recorded. After every `Configure(...)`
  has run, the deferred closures are executed in topological order, splicing
  inherited properties into their original lexical position so output order
  matches what the user wrote.

`SchemaConfigurationBuilder.Schema(...)` was changed to **reuse** an existing
non-null slot value instead of always creating a fresh schema. This is a
behaviour change but a non-breaking one for existing tests, and is what
makes pre-allocation feasible.

### Cycle exception

Cyclic dependencies surface as a new public type
`SchemaDependencyCycleException : InvalidOperationException`. The exception's
message names every participant as `{ConfigurationType}.{SchemaProperty}` and
is paired with a structured `Cycle` property so callers can inspect the
chain programmatically. A new public exception type is justified because
this is the one failure mode where users genuinely need to differentiate
"my dependency wiring is wrong" from generic `InvalidOperationException`
flowing out of host startup.

## Consequences

- ✅ Cross-`SchemaConfiguration` dependencies resolve in the right order
  without the user having to think about it. Output order of inherited
  vs. local properties matches lexical declaration order.
- ✅ `Configure(...)` keeps its current public contract. The DSL surface is
  unchanged.
- ✅ Cycles fail fast at host startup with a message that names every
  participant.
- ⚠️ Slot pre-allocation requires every concrete `ISchema` to expose a
  parameterless constructor (public or internal). All current schema types
  satisfy this and the convention is recorded here.
- ⚠️ The reuse-existing-slot semantics in `SchemaConfigurationBuilder.Schema(...)`
  means a user who calls `Schema(() => X)` twice now appends to the same
  schema instead of overwriting it. No tests rely on the old behaviour and
  the new one matches the additive feel of the rest of the DSL, but the
  change is documented here in case it surprises anyone.
- ⚠️ Same-configuration `AddPropertiesFrom` / `AddMappingsFrom` calls still
  rely on lexical order inside `Configure`, just as before. Inverting the
  declarations silently produces an empty source read. Acceptable; same as
  today.
- 🔄 If a future feature needs the registry as a public extensibility hook
  (e.g. for a third-party validator), the type can be promoted from
  internal to public without a breaking change for existing users.

## Alternatives considered

- **Analyse `Configure` via expression trees.** Rejected: the DSL hands
  builders `Func<TSchema>` (not `Expression<Func<...>>`) for
  `AddPropertiesFrom` / `AddMappingsFrom`, and changing those signatures is
  off-limits without a separate ADR. IL-walking the captured method is
  fragile.
- **Try-and-defer multipass over whole `Configure` calls.** Rejected: a
  partial `Configure` call leaves the configuration's schemas in a half-
  populated state, and "reset between attempts" forces every schema and
  builder to grow a `Reset()` API. Deferring only the cross-config calls
  contains the surface area.
- **Make `SchemaDependencyCycleException` internal.** Rejected: cycles are a
  user-facing failure mode that warrant their own catch target — not the
  same as a generic `InvalidOperationException` thrown for, say, a missing
  `AddTurbine`.
- **Public `TurbineSchemaRegistry`.** Deferred: there is no in-tree consumer
  outside the Turbine assembly today. Promoting it later is non-breaking;
  exposing it now would lock in a public surface before the consuming code
  has shaped it.
