# ADR-005: EnumSchemaBuilder is generic over TEnum

- **Status:** Accepted
- **Date:** 2026-05-03
- **Deciders:** team

## Context

`EnumSchema<TEnum>` is generic with `where TEnum : struct, Enum`, but
`EnumSchemaBuilder` was declared as a non-generic class with a `// Todo:
EnumSchemaBuilder` placeholder marker on its corresponding
`PropertySchemaBuilder.Add(...)` overload. The non-generic builder
can't hold a typed reference to `EnumSchema<TEnum>`, which blocks
wiring `Nullable` (and any future per-TEnum knobs like value
allow-lists or string-vs-integer serialisation toggles).

The non-generic `Add(Expression<Func<TDomain, Enum>>, ...)` overload
was also broken: `Expression<Func<TDomain, MyEnum>>` doesn't implicitly
convert to `Expression<Func<TDomain, Enum>>` (expression trees are
invariant). The `Todo` comment was already calling out the gap.

## Decision

`EnumSchemaBuilder` becomes generic: `EnumSchemaBuilder<TEnum> where
TEnum : struct, Enum`. It wraps an `EnumSchema<TEnum>` instance via
the same internal-ctor / `Schema` property pattern as the other
wired builders.

`PropertySchemaBuilder.Add` for enum-typed properties becomes:

```csharp
public TSelf Add<TEnum>(
    Expression<Func<TDomain, TEnum>> selector,
    string? name = null,
    bool? required = null,
    Action<EnumSchemaBuilder<TEnum>>? schema = null)
    where TEnum : struct, Enum;
```

This sits alongside the existing
`Add<TNumber>(... where TNumber : struct, INumber<TNumber>)` overload —
the constraints are disjoint (no enum implements `INumber<TEnum>`), so
overload resolution picks the right one based on which constraint a
given TEnum/TNumber satisfies.

`EnumSchema<TEnum>` gains a `bool? Nullable` property in keeping with
ADR-004.

## Consequences

- ✅ `EnumSchemaBuilder<TEnum>` can hold a strongly-typed schema
  reference and override `Nullable` like every other wired builder.
- ✅ The DSL signature now matches what the existing Starfleet sample
  expects (`schemas.Add(e => e.Rate)` where `Rate` is an enum); the
  former non-generic signature wouldn't have accepted that
  expression.
- ⚠️ Two `Add<T>` overloads now share the
  `Expression<Func<TDomain, T>>` shape, distinguished only by their
  type-parameter constraints. Future maintainers need to add new
  constrained overloads carefully to avoid ambiguity.
- 🔄 `PropertySchemaBuilder.Add` enum overload is changed in this
  commit. The body remains a placeholder until `PropertySchemaBuilder`
  is wired in a later iteration.

## Alternatives considered

- **Non-generic builder + non-generic abstract base on
  `EnumSchema`:** rejected because the non-generic base would need to
  re-declare `Nullable` (already on the generic schema) and the cast
  back to the typed schema for any per-TEnum knob would be ergonomic
  poison.
- **`IEnumSchema` non-generic interface with `Nullable`:** rejected
  for the same reason ADR-004 rejected adding `Nullable` to `ISchema`
  — it forces every implementer (including future test fixtures) to
  declare a property, and we already have a per-schema property
  available.
