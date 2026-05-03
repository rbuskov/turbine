# Turbine architecture

This document describes the high-level architecture of Turbine: what the library
does, the building blocks it exposes, and how they fit together. It is meant as
a starting point for anyone planning a feature that touches more than one layer.

For project conventions (testing, ADRs, code style) see `CLAUDE.md`.
For specific decisions see `docs/adr/`.

## What Turbine is

Turbine is an experimental ASP.NET Core library that lets you describe the JSON
shape of an HTTP request or response *directly from your domain types*, without
hand-written DTOs. A Turbine **schema** is a runtime object that knows two things
about a domain type:

- how to project an instance of that type to a `JsonElement` (`ToJson`)
- how to bind a `JsonElement` back to an instance of that type (`FromJson`)

Schemas double as the source of truth for OpenAPI spec generation and for
runtime request validation, so model shape, validation rules and published
specification cannot drift apart.

## Guiding principles

1. **Domain types are the source of truth.** Schemas are built *from* domain
   types via expression selectors. There are no parallel DTO classes.
2. **`JsonElement` is the wire boundary.** Endpoints accept and return
   `JsonElement`; schemas convert in both directions. Turbine does not impose
   a serializer on the caller and never materialises typed DTOs at the edge.
3. **Schemas are grouped on `SchemaConfiguration` subclasses.** Schemas live
   as properties on a `SchemaConfiguration` subclass and endpoints reference
   them by selector, which keeps the wiring strongly typed without losing the
   ability to share or compose schemas. How you slice the groupings is up to
   you — one per resource is a natural default, but multiple configurations
   per resource (e.g. read vs. write, or per sub-area) are equally valid.
4. **Composition over duplication.** Schemas can pull properties or mappings
   from other schemas (`AddPropertiesFrom`, `AddMappingsFrom`), so variants
   like Create / Update / Patch are derived rather than restated.

## The pieces

```
┌──────────────────────────────────────────────────────────────────┐
│  ASP.NET Core endpoint  (JsonElement in / IResult out)           │
│    ├── Produces / Accepts  ──►  references schema by selector    │
│    └── handler  ──►  schema.FromJson(body)  /  schema.ToJson(x)  │
└──────────────────────────────────────────────────────────────────┘
                               │
┌──────────────────────────────▼───────────────────────────────────┐
│  SchemaConfiguration  (groups related schemas; one or more)      │
│    Configure(SchemaConfigurator builder) { ... fluent DSL ... }  │
└──────────────────────────────┬───────────────────────────────────┘
                               │  builds
┌──────────────────────────────▼───────────────────────────────────┐
│  Schemas                                                         │
│    Reference types: ObjectSchema<T>, ArraySchema<T>, OneOfSchema │
│    Value types:     String, Numeric<T>, Boolean, Enum<T>,        │
│                     DateTimeOffset, DateOnly                     │
└──────────────────────────────┬───────────────────────────────────┘
                               │  reads / writes
┌──────────────────────────────▼───────────────────────────────────┐
│  Domain types  (your code — the source of truth)                 │
└──────────────────────────────────────────────────────────────────┘
```

### Schemas (`Turbine.Schemas`)

The schema hierarchy splits along a single axis: whether a value can be bound
*in place* into an existing instance.

- `ISchema` — root marker.
- `IValueTypeSchema<T>` — `FromJson(JsonElement) → T` and `ToJson(T) → JsonElement`.
  Implemented by `StringSchema`, `NumericSchema<TNumber>`, `BooleanSchema`,
  `EnumSchema<TEnum>`, `DateTimeOffsetSchema`, `DateOnlySchema`.
- `IReferenceTypeSchema<T> : IValueTypeSchema<T>` — adds an in-place overload
  `FromJson(JsonElement, T)`. Implemented by `ObjectSchema<T>`, `ArraySchema<T>`,
  and `OneOfSchema<T>`. The in-place form is what enables PUT/PATCH semantics
  against an existing tracked entity without reconstructing it.

`ObjectSchema<T>` carries an ordered list of `ObjectProperty` entries
(`Name`, `Schema`, `Required`). `OneOfSchema<TBase>` carries a discriminator
plus a map from discriminator values to `IObjectSchema` mappings.

### Builders (`Turbine.Builders`)

The fluent DSL lives in a separate set of types so the schemas themselves stay
pure data. Authors subclass `SchemaConfiguration`, declare schema-typed
properties, and override `Configure(SchemaConfigurator builder)`:

- `SchemaConfigurator` exposes `Schema(() => Property)` overloads that return
  the matching builder.
- `PropertySchemaBuilder<TDomain, TSelf>` is the shared base for object-like
  builders. It exposes `Add(...)` overloads for every supported value-type
  selector, `AddObject` / `AddArray` / `AddOneOf` for nested reference shapes,
  `AddCustom` for computed properties, and `AddPropertiesFrom` /
  `AddAtomicProperties` / `Remove` for composition.
- Per-schema-type builders (`StringSchemaBuilder`, `NumericSchemaBuilder<T>`,
  `ArraySchemaBuilder<T>`, `OneOfSchemaBuilder<TBase>`, …) carry the
  schema-specific knobs (`MinLength`, `Pattern`, `Minimum`, `Discriminator`,
  `AddMapping<T>`, …).

The DSL uses `Expression<Func<TDomain, TProperty>>` selectors so property
names, types, and OpenAPI metadata can be inferred from the domain type.

### ASP.NET Core integration (`Turbine.Extensions`)

Three extension surfaces tie schemas into the host:

- `IServiceCollection.AddTurbine(...)` — discovers `SchemaConfiguration`
  subclasses in the entry assembly (or a supplied assembly) and registers
  them with DI so handlers can inject them.
- `WebApplication.MapTurbine()` — installs the request-validation middleware
  and contributes schema definitions to the OpenAPI document produced by
  `Microsoft.AspNetCore.OpenApi`.
- `RouteHandlerBuilder.Produces<T>(statusCode, schemaSelector)` and
  `Accepts<T>(schemaSelector)` — let an endpoint reference a schema on a
  `SchemaConfiguration` subclass via expression, e.g.
  `.Produces(200, (PersonnelSchemas p) => p.Summary)`. The metadata is what
  the OpenAPI generator and validation middleware read at runtime.

## Request lifecycle

A typical write endpoint looks like this (from the Starfleet sample):

```csharp
endpoints.MapPost("", async (
        [FromBody] JsonElement body,
        [FromServices] ICreatePersonnelHandler handler) => await handler.Create(body))
    .Produces(201, (PersonnelSchemas p) => p.CreateResult);
```

1. ASP.NET Core deserialises the request body into a `JsonElement` — no DTO.
2. The validation middleware (registered by `MapTurbine`) looks up the schema
   referenced by `Accepts` and validates the `JsonElement` against it before
   the handler runs.
3. The handler resolves the resource's `SchemaConfiguration` from DI and calls
   `schema.FromJson(body)` to materialise a domain instance (or
   `schema.FromJson(body, existing)` for PUT/PATCH).
4. The handler does its domain work and returns either a `JsonElement`
   produced by `schema.ToJson(...)` or a status-only result.
5. OpenAPI generation reads the same schema metadata and emits a spec that
   matches what the endpoint actually accepts and produces.

## The Starfleet sample

`src/Turbine.Starfleet` is the reference consumer. It exists primarily to
exercise Turbine end-to-end against a non-trivial domain (Star-Trek-themed
personnel and starships, persisted in EF Core / SQLite). When in doubt
about how a Turbine feature should *feel* from the outside, look there
first.

## External dependencies

- `Microsoft.AspNetCore.App` (framework reference) — host integration.
- `Microsoft.AspNetCore.OpenApi` — OpenAPI document generation.
- `JsonSchema.Net` — JSON Schema model used for validation.