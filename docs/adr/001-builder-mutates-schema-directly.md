# ADR-001: Builders mutate the schema instance directly

- **Status:** Accepted
- **Date:** 2026-05-03
- **Deciders:** team

## Context

The builders under `src/Turbine/Builders/` currently have placeholder bodies
that return `this` but do not affect any schema state. We need a convention
for how a builder method ultimately ends up on the corresponding
`Turbine.Schemas` object so authors can wire up `StringSchemaBuilder`,
`NumericSchemaBuilder<T>`, `ArraySchemaBuilder<T>`, etc. consistently.

Two shapes are plausible:

1. **Direct mutation** — the builder holds a reference to a freshly created
   schema and writes to its properties on each call.
2. **Deferred build** — the builder accumulates a list of configuration
   actions (or its own state) and produces a schema only when asked.

This ADR picks one before the second builder is implemented so they all
follow the same shape.

## Decision

Builders hold an internal reference to the schema they configure and mutate
it directly. Concretely:

- Each per-schema builder (`StringSchemaBuilder`, `NumericSchemaBuilder<T>`,
  etc.) declares an internal constructor that accepts the schema instance it
  wraps and stores it on an internal `Schema` property.
- `SchemaConfigurationBuilder.Schema(...)` overloads construct a new schema,
  wrap it in the matching builder, and return that builder. The schema
  instance is the single mutable target for every chained call.
- Builder methods are imperative: they validate inputs, write to the schema,
  and return `this`. They do not record a list of pending operations.
- Validation of inputs happens at the call site (e.g. negative
  `MinLength` throws `ArgumentOutOfRangeException`). The schema itself stays
  a passive data holder.

## Consequences

- ✅ The fluent DSL is trivially debuggable — after each call the schema
  already reflects the configuration, so a developer can inspect it without
  running a "build" step.
- ✅ Tests can construct a schema and a builder directly (via the internal
  constructor exposed through `InternalsVisibleTo`) and assert on schema
  state after each call.
- ✅ Composition primitives (`AddPropertiesFrom`, `AddMappingsFrom`) read
  from a fully-populated source schema rather than replaying recorded
  actions.
- ⚠️ Order of calls matters; later calls overwrite earlier ones (e.g.
  `MinLength(5).MinLength(10)` ends with `10`). This matches authors'
  expectations for a fluent DSL but is worth noting.
- ⚠️ A builder cannot be reused against a different schema instance, since
  it's bound to one at construction time. This is intentional — sharing a
  builder across schemas would be surprising.
- 🔄 Subsequent builder implementations (`NumericSchemaBuilder<T>`,
  `ArraySchemaBuilder<T>`, …) follow the same pattern: internal `Schema`
  property + constructor, validation at the call site, mutate-and-return.

## Alternatives considered

- **Deferred build (record actions, materialise later):** rejected because
  it adds a phase split with no current beneficiary — there is no need to
  inspect or rewrite the configuration before producing the schema, and
  composition primitives become harder to reason about when they have to
  merge two action lists rather than two finished schemas.
- **Fluent builder produces a new schema instance per call (immutable):**
  rejected because the schemas are already mutable POCOs and an immutable
  builder would force allocations on every chained call without buying
  thread-safety we don't need (configuration runs once at startup).
