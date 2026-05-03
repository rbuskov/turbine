# ADR-006: ObjectProperty stores optional custom converter delegates

- **Status:** Accepted
- **Date:** 2026-05-03
- **Deciders:** team

## Context

`PropertySchemaBuilder.AddCustom(...)` is the DSL entry-point for
*computed* properties — properties whose JSON value is derived from a
function over the domain instance rather than a plain CLR property
access. Each overload accepts up to three delegates:

- `expr` — extract the value from `TDomain`
- `toJson` (optional) — full serialise override
- `fromJson` (optional) — full deserialise override

`ObjectProperty` currently carries only `Name`, `Schema`, and `Required`.
There is no slot for the converter delegates that `AddCustom` collects,
so wiring `AddCustom` requires somewhere on the schema-side to put them.

## Decision

Extend `ObjectProperty` with three optional `Delegate?` slots:

```csharp
public Delegate? ValueExpression { get; set; }   // Func<TDomain, T>
public Delegate? ToJson { get; set; }            // Func<TDomain, JsonValue>
public Delegate? FromJson { get; set; }          // Action<JsonValue, TDomain>
```

`ObjectProperty` stays non-generic, which is why the slots are typed as
`Delegate?` rather than the strongly-typed callback shapes. The closed
generic types live on the `AddCustom<...>` overloads that produce them;
the down-stream serialisation code (currently stubbed) will downcast to
the expected delegate type when materialising a property.

Properties added via `Add(p => p.Foo)` leave all three slots `null` —
the schema's own `ToJson` / `FromJson` is the converter. Properties
added via `AddCustom` carry the delegates the user supplied. A null
`ValueExpression` therefore means "this is a regular property, read it
off the CLR object."

## Consequences

- ✅ `AddCustom` wiring is a one-shot change: each overload sets the
  delegates on the `ObjectProperty` it builds.
- ✅ The split between regular and computed properties is data, not
  type — emitters and validators iterate the same `Properties` list
  and branch on whether `ValueExpression` is `null`.
- ⚠️ The `Delegate?` slots lose static typing — readers must downcast
  to the expected `Func<TDomain, T>` / `Func<TDomain, JsonValue>` /
  `Action<JsonValue, TDomain>` shape. Acceptable because (a) the
  `Delegate` is paired with a typed `Schema` that already encodes the
  shape, and (b) `ObjectProperty` is intentionally non-generic.
- 🔄 `AddCustom` overloads are wired in the same commit. Down-stream
  consumers (the binding/validation paths, currently stubbed) will read
  these slots when those paths are implemented.

## Alternatives considered

- **`ComputedObjectProperty : ObjectProperty` subclass:** rejected
  because every iteration of the `Properties` list would need an
  `if (p is ComputedObjectProperty) ...` branch, splitting code paths
  for what is essentially the same notion ("a property in the schema").
  A nullable slot keeps the data uniform.
- **A side dictionary on `ObjectSchema<T>` keyed by property name:**
  rejected because moving converter information away from the
  `ObjectProperty` it pertains to invites desync (rename a property,
  forget the dictionary entry).
- **Strongly-typed generic slots on a derived `ObjectProperty<T>`:**
  rejected because `ObjectSchema<TDomain>.Properties` is a
  heterogeneous list — every element has a different `T` for its
  schema, so a single typed property class won't fit.
