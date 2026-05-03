# ADR-002: NumericSchema gains ExclusiveMinimum and ExclusiveMaximum

- **Status:** Accepted
- **Date:** 2026-05-03
- **Deciders:** team

## Context

`NumericSchemaBuilder<TNumber>` already exposes `ExclusiveMinimum` and
`ExclusiveMaximum` configuration methods, but `NumericSchema<TNumber>`
only carries inclusive `Minimum`, `Maximum`, and `MultipleOf`. Per the
loop's hard constraint, schema changes need an ADR — even ones that
fill an obvious gap.

JSON Schema (draft 2020-12) defines `exclusiveMinimum` and
`exclusiveMaximum` as numeric keywords distinct from their inclusive
counterparts. Without them on the schema, the builder methods cannot
round-trip into the validation / OpenAPI emission paths.

## Decision

Add two nullable properties to `NumericSchema<TNumber>`:

```csharp
public TNumber? ExclusiveMinimum { get; set; }
public TNumber? ExclusiveMaximum { get; set; }
```

Both default to `null` (no bound). They are independent of the inclusive
`Minimum` / `Maximum` — authors may set either, both, or neither, and
downstream emitters decide the precedence rules.

## Consequences

- ✅ The builder can now express the full JSON Schema numeric vocabulary.
- ✅ Mirrors the inclusive properties' shape (nullable struct, public
  setter), so emitters and validators can treat the four bounds
  uniformly.
- ⚠️ Downstream code that switches on bound presence has two more
  cases to consider. None exists today, so this is a "future cost"
  rather than a current one.
- 🔄 `NumericSchemaBuilder<TNumber>.ExclusiveMinimum` /
  `.ExclusiveMaximum` are wired up in the same commit.

## Alternatives considered

- **Encode exclusivity as a flag alongside `Minimum`/`Maximum`:** rejected
  because JSON Schema treats them as independent keywords (you can have
  both an inclusive and an exclusive bound, e.g. when composing schemas),
  and a single flag would lose that.
- **Defer until a consumer actually needs them:** rejected because the
  builder already advertises the methods publicly; deferring leaves the
  DSL silently lying to its users.
