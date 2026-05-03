# ADR-004: Nullability is a per-schema property

- **Status:** Accepted
- **Date:** 2026-05-03
- **Deciders:** team

## Context

Every `SchemaBuilder<TSelf>` exposes a `Nullable(bool?)` configuration
method (and `OneOfSchemaBuilder<TBase>` declares its own copy). Until
now the method was a placeholder that returned `this` without storing
the choice anywhere. Wiring `BooleanSchemaBuilder` — whose entire
public surface is the inherited `Nullable` — forces the question of
where nullability actually lives on the schema side.

ASP.NET Core's `JsonElement` boundary doesn't carry C#'s nullable
reference annotations, so the schema needs an explicit signal whether
`null` is acceptable for a field. JSON Schema does the same thing via
`"type": ["string", "null"]` (or its 2020-12 equivalent). Validation
and OpenAPI emission both need to read this.

## Decision

Each schema class carries its own `public bool? Nullable { get; set; }`
property. `null` means "unconfigured" — emitters fall back to the
domain type's annotation (`T?` ⇒ nullable, `T` ⇒ not nullable).

`SchemaBuilder<TSelf>.Nullable(bool?)` becomes `virtual` with a
no-op default body. Concrete builders that wrap a real schema instance
(`StringSchemaBuilder`, `NumericSchemaBuilder<TNumber>`,
`ArraySchemaBuilder<TItem>`, `BooleanSchemaBuilder`, …) override it
to write through to `Schema.Nullable`. Builders whose schema isn't
wired yet keep the inherited no-op until they're wired.

We deliberately do **not** add `Nullable` to the `ISchema` interface.
Reasons:

- `ISchema` stays a marker interface, matching its current role.
- New schemas can opt into nullability when they need it without
  forcing every implementer (real or test fixture) to declare a
  property.
- The override pattern makes the wiring explicit at the builder level,
  which is where the validation logic for "did the user actually call
  `Nullable`?" tends to live.

## Consequences

- ✅ Wiring a new builder is a self-contained change: add `Nullable` to
  the schema, override `Nullable` on the builder, done.
- ✅ Existing already-wired builders (`StringSchemaBuilder`,
  `NumericSchemaBuilder<TNumber>`, `ArraySchemaBuilder<TItem>`) are
  updated in the same commit so their `Nullable` calls stop being
  silent no-ops.
- ⚠️ Three different `Nullable` declarations now exist
  (`SchemaBuilder<TSelf>` virtual, `OneOfSchemaBuilder<TBase>` shadow,
  the per-schema property). Acceptable — `OneOfSchemaBuilder<TBase>`
  is its own type because OneOf composition is structurally different,
  and the duplication is a small price for keeping the inheritance
  hierarchies independent.
- 🔄 Future builder iterations override `Nullable` and add the
  property to their schema following the same shape.

## Alternatives considered

- **Add `Nullable` to `ISchema`:** rejected because it would force
  every fixture, test schema, and future schema to declare the
  property even when nullability is meaningless (e.g. a hypothetical
  schema that always serialises a value). The marker interface is
  worth keeping for low-friction extension.
- **Introduce an abstract `Schema` base class with `Nullable`:**
  rejected because the concrete schemas have unrelated generic
  parameters and conflicting hierarchies (some implement
  `IReferenceTypeSchema<T>`, some don't). Forcing them under a single
  base would couple unrelated decisions.
- **Track nullability in a separate `SchemaConfiguration`-level map:**
  rejected because it splits a schema's information across two
  objects, complicating composition primitives like
  `AddPropertiesFrom`.
