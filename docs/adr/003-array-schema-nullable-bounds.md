# ADR-003: ArraySchema item bounds become nullable

- **Status:** Accepted
- **Date:** 2026-05-03
- **Deciders:** team

## Context

`ArraySchema<TItem>` stores its item-count bounds as non-nullable `int`:

```csharp
public int MinItems { get; set; }
public int MaxItems { get; set; }
```

Both default to `0`. That's fine for `MinItems` (JSON Schema's default for
the `minItems` keyword *is* `0`), but it conflates "no upper bound" with
"empty array required" for `MaxItems` — a schema where `MaxItems == 0`
cannot be told apart from one where the keyword was never set. Both are
valid JSON Schema, with very different meanings.

Wiring `ArraySchemaBuilder<TItem>.MaxItems(...)` to write through to the
schema requires a way to express "no upper bound." The same applies to
`MinItems` for symmetry and to keep "set to 0 explicitly" distinguishable
from "never configured."

## Decision

Change both properties to nullable:

```csharp
public int? MinItems { get; set; }
public int? MaxItems { get; set; }
```

`null` means "no bound" / "keyword absent in JSON Schema output." The
builder writes integers through unchanged; emitters (OpenAPI, validation
middleware) skip the keyword when the property is `null`.

This mirrors `NumericSchema<TNumber>`'s `Minimum` / `Maximum` /
`MultipleOf`, which are already nullable.

## Consequences

- ✅ Emitters can faithfully round-trip "no maxItems" vs "maxItems = 0."
- ✅ Consistent with `NumericSchema<TNumber>` — bounds are nullable across
  schemas that have them.
- ⚠️ Any code that previously treated `0` as "no bound" must now treat
  `null` as "no bound." No such code exists today.
- 🔄 `ArraySchemaBuilder<TItem>.MinItems` / `.MaxItems` are wired up in
  the same commit; both reject negative inputs.

## Alternatives considered

- **Sentinel value (e.g. `-1` for "no bound"):** rejected because it's
  brittle and trains every reader of the schema to remember the magic
  number.
- **Two extra `bool` flags (`HasMinItems`, `HasMaxItems`):** rejected
  because it duplicates information that `int?` already carries.
