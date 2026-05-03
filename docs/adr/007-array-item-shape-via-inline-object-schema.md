# ADR-007: ArraySchema carries an inline item-shape ObjectSchema

- **Status:** Accepted
- **Date:** 2026-05-03
- **Deciders:** team

## Context

`ArraySchemaBuilder<TItem>` inherits from
`PropertySchemaBuilder<TItem, ArraySchemaBuilder<TItem>>`, which means
its callers can write item-shape configuration inline:

```csharp
.AddArray(s => s.Commendations, itemSchema: s =>
{
    s.Add(c => c.Name);
    s.Add(c => c.AwardedDate);
});
```

These `Add(...)` calls on the array-builder must produce per-property
descriptors for whatever shape the array's items have. Until now,
`ArraySchema<TItem>` had no slot for those descriptors, so
`ArraySchemaBuilder.AddProperty` threw `NotSupportedException`.

## Decision

`ArraySchema<TItem>` gains an optional inline item-shape:

```csharp
public ObjectSchema<TItem>? Items { get; set; }
```

`Items` defaults to `null` and is lazily allocated by
`ArraySchemaBuilder<TItem>.AddProperty(...)` on the first call.
`RemoveProperty(...)` is a no-op when `Items` is `null` and otherwise
removes by name. All inherited DSL methods (`Add(...)`, `AddObject(...)`,
`AddArray(...)`, `AddOneOf(...)`, `AddCustom(...)`, `AddPropertiesFrom`,
`AddAtomicProperties`, `Remove`) flow through these two hooks, so they
all "just work" against `Items.Properties`.

When `Items` is `null`, the array contains items whose shape is implicit
or scalar — nothing to validate or describe per property. Emitters that
need per-property metadata read from `Items` and skip the array if it's
`null`.

## Consequences

- ✅ The `itemSchema:` callback on `AddArray(...)` works as the
  Starfleet sample expects, without forcing a separate sibling builder
  type for arrays-of-objects vs arrays-of-scalars.
- ✅ Item-shape information lives on the array schema itself, so
  emitters and validators don't need a side dictionary or registry.
- ⚠️ Two ways to describe array items now coexist: the implicit `TItem`
  type (used today for, e.g., `IEnumerable<int>`) and the explicit
  `Items` schema. Down-stream code must check `Items` first and fall
  back to a default scalar schema if absent. Acceptable — the same
  branching exists in JSON Schema's own `items` keyword (present →
  schema constraints; absent → unconstrained).
- 🔄 `ArraySchemaBuilder.AddProperty` and `RemoveProperty` are wired in
  the same commit and tested via the inherited `Add(...)` / `Remove(...)`
  surface.

## Alternatives considered

- **Separate `ArraySchemaBuilder` for arrays of scalars vs arrays of
  objects:** rejected because callers would have to know which they
  need before writing the call site, breaking the symmetry of
  `AddArray<TItem>(...)` having a single overload.
- **Store per-item descriptors as a flat `IList<ObjectProperty>` on
  `ArraySchema<TItem>` directly (no inner `ObjectSchema`):** rejected
  because it forces re-implementing every `ObjectSchema` capability
  (composition, ToJson, FromJson) at the array level. The inner
  `ObjectSchema<TItem>` reuses the existing implementation for free.
- **Make `Items` non-nullable, default to an empty `ObjectSchema<TItem>`:**
  rejected because emitters can't tell "no item shape configured"
  from "configured but empty," and the latter is a different
  (trivially-valid) JSON Schema.
