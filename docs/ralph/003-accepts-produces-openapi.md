# Ralph loop: implement and test `Accepts` / `Produces` and their OpenAPI transformers

**Goal:** Replace the stub `RouteHandlerBuilder.Produces<T>(int, Expression<Func<T, ISchema>>)` and `RouteHandlerBuilder.Accepts<T>(Expression<Func<T, ISchema>>)` extensions in `src/Turbine/Extensions/RouteHandlerBuilderExtensions.cs` with a real implementation that

- attaches structured Turbine metadata to the endpoint identifying the `SchemaConfiguration` subclass, the schema property selector, and (for `Produces`) the HTTP status code, and
- causes the ASP.NET Core 10 OpenAPI document produced by `Microsoft.AspNetCore.OpenApi` to contain
  - a JSON-schema component under `components/schemas/` for every Turbine schema referenced by any endpoint, named deterministically and deduplicated across endpoints, and
  - a request body (`Accepts`) and/or response (`Produces`) on each operation that `$ref`s the corresponding component schema with the right content type and status code.

Stop when the two extensions are real (no more `/* metadata */` placeholder), the OpenAPI transformer(s) emit the structures above, every public path is covered by tests in `tests/Turbine.Tests/Unit/` and `tests/Turbine.Tests/Integration/`, and `dotnet test tests/Turbine.Tests/Turbine.Tests.csproj` is green with no skipped tests in this area.

## Per-iteration workflow

1. **Orient.** Read `CLAUDE.md`, `docs/Architecture.md`, and the `docs/adr/` index (especially `008-host-integration-registry-and-cycle-detection.md`). Re-read `src/Turbine/Extensions/RouteHandlerBuilderExtensions.cs`, `src/Turbine/Extensions/ServiceCollectionExtensions.cs`, `src/Turbine/Extensions/WebApplicationExtensions.cs`, and `src/Turbine/Extensions/TurbineSchemaRegistry.cs`. List `src/Turbine/Schemas/*.cs` to refresh which `ISchema` shapes have to round-trip into JSON Schema. List `tests/Turbine.Tests/Unit/` and `tests/Turbine.Tests/Integration/`. Identify the next slice that is either (a) unimplemented, (b) lacking tests covering its public behaviour, or (c) missing a documented decision the rest of the loop will rely on. Pick exactly one slice. If everything in scope is implemented, tested, and green, stop and report done.

   Suggested slice ordering (split further if any one grows past ~400 LOC of diff combined tests + impl):
   - **Endpoint metadata type.** Introduce an internal, immutable metadata record (e.g. `TurbineEndpointSchemaMetadata`) that captures `(Type ConfigurationType, string SchemaPropertyName, EndpointSchemaRole Role, int? StatusCode, string ContentType)`. Decide and document the role enum (`Request` vs `Response`) and the default content type (`application/json` per project convention — see `feedback_json_default`). Pure type, easy unit tests for value semantics.
   - **`Accepts<T>` / `Produces<T>` attach metadata.** Make the extensions extract the `(ConfigurationType, PropertyName)` pair from the `Expression<Func<T, ISchema>>` selector (reject non-property-access expressions with a clear `ArgumentException` whose message names the offending expression) and attach exactly one `TurbineEndpointSchemaMetadata` per call via `WithMetadata`. Multiple `Produces` calls with different status codes on the same endpoint must each contribute their own metadata entry.
   - **Schema → `OpenApiSchema` conversion.** Build an internal converter that turns each `ISchema` implementation into a `Microsoft.OpenApi.Models.OpenApiSchema` capturing the constraints actually present on the schema (e.g. `StringSchema.MinLength`, `NumericSchema.Minimum`/`Maximum`/exclusivity, `ArraySchema` item schema and bounds, `ObjectSchema` properties + required list, `OneOfSchema` discriminator + mappings, `BooleanSchema`, `EnumSchema` values, `DateOnlySchema`/`DateTimeOffsetSchema` formats, nullability). Nested object/array/oneOf schemas should `$ref` other components rather than inline whenever the nested schema is itself owned by a `SchemaConfiguration` property; truly anonymous nested schemas may inline. Keep one slice per cluster of schema types if needed (value types, then object, then array, then oneOf) — never bundle all schema types into one iteration if it pushes past the diff cap.
   - **Component naming + dedup.** Deterministic component name per `(ConfigurationType, SchemaPropertyName)` — pick a convention (e.g. `${ConfigurationTypeName}_${SchemaPropertyName}`, stripping a trailing `Schemas` suffix from the configuration type) and document it in the ADR if it's anything other than the most obvious choice. The same `(ConfigurationType, SchemaPropertyName)` referenced from multiple endpoints must produce exactly one entry under `components/schemas`.
   - **Document/operation transformer.** Implement an `IOpenApiDocumentTransformer` (or a pair of `IOpenApiOperationTransformer` + `IOpenApiDocumentTransformer`, your call — document it in an ADR if you split it) that walks the operations, reads the Turbine metadata, ensures each referenced schema is materialised under `components/schemas/`, and sets `requestBody.content[contentType].schema` / `responses[statusCode].content[contentType].schema` to a `$ref` pointing at that component. Pre-existing operation metadata from the built-in `Produces`/`Accepts` (the non-Turbine ones, e.g. `.Produces(204)`, `.Produces<decimal>()`) must be left alone.
   - **Wire-up.** Make `AddTurbine` register the transformer(s) so that the call site retains the existing two-line shape (`services.AddOpenApi(); services.AddTurbine();`). Use `IConfigureOptions<OpenApiOptions>` (or the equivalent `services.ConfigureOpenApi(...)` API in ASP.NET Core 10 — verify the actual API surface against the installed `Microsoft.AspNetCore.OpenApi` 10.0.4 package before committing). The transformer must read schemas from the `TurbineSchemaRegistry` populated by `MapTurbine`, so it must not run before the registry is built; either resolve lazily on first transformer invocation or fail with a clear exception if the registry is unbuilt.

2. **Reference, don't modify.** `src/Turbine.Starfleet/**` and `tests/Turbine.Starfleet.Tests/**` are reference material **only** for this loop. Read `Program.cs`, `Resources/Personnel/PersonnelSchemas.cs`, `Resources/Personnel/PersonnelEndpoints.cs`, and `Resources/Starships/StarshipEndpoints.cs` to understand how `Accepts`/`Produces` are called from a real consumer. The sample's correctness this loop is irrelevant — never edit Starfleet, never `dotnet build` the Starfleet projects, never `dotnet run` Starfleet, never hit `/personnel`, `/starships`, or `/openapi/v1.json` against the sample. The Starfleet sample is read-only reference material.

3. **Test first (red).**
   - **Unit tests** live in `tests/Turbine.Tests/Unit/`. Use them for pure logic: metadata equality and parsing, expression-selector validation (property access, member chain, lambda body shape), schema-to-`OpenApiSchema` conversion for each `ISchema` type, component-name derivation, dedup of repeated schemas, and the rejection of non-property selectors. Construct `OpenApiSchema` and `OpenApiDocument` instances directly; do not spin up a host for unit work.
   - **Integration tests** live in `tests/Turbine.Tests/Integration/`. Use them for end-to-end OpenAPI emission: build a `WebApplication` (or `WebApplicationFactory<TEntry>` — the project already references `Microsoft.AspNetCore.Mvc.Testing`) with a few in-test `SchemaConfiguration` subclasses, map a few minimal endpoints using `Accepts`/`Produces`, call `AddOpenApi` + `AddTurbine` + `MapTurbine` + `MapOpenApi`, request `/openapi/v1.json`, parse the response, and assert on the resulting structure: the presence and shape of `components/schemas/X`, the presence of `paths./foo.post.requestBody.content.application/json.schema.$ref`, status-code-keyed responses, dedup across endpoints, and that endpoints with no Turbine metadata are unaffected. Use `System.Text.Json` to inspect the document; do not introduce a JSON-assertion library.
   - All test fixtures (test-only `SchemaConfiguration` subclasses, POCO domain types, sample assemblies, and any `Program` partial used as the `TEntry` for `WebApplicationFactory`) live inside the test project. Never add fixtures to `src/Turbine`. Never depend on Starfleet types from a test.
   - Cover happy path + at least one failure mode per slice (per `CLAUDE.md`). Required failure modes for this loop include at minimum:
     - `null` builder, `null` selector → `ArgumentNullException`.
     - Selector that is not a property access (e.g. method call, indexer, constant) → `ArgumentException` whose message names the bad expression.
     - `Produces` called with a status code outside the OpenAPI-permissible range (decide and document — reject `< 100` and `>= 600`, or accept any `int` and let OpenAPI bucket them; whichever you pick, an ADR sentence and a test).
     - Endpoint references a `(ConfigurationType, PropertyName)` that the registry does not contain after `MapTurbine` → clear exception during transformation, naming both the configuration type and the property.
     - `Accepts` and `Produces` declared on the same endpoint both end up on the operation.
     - Two endpoints producing the same schema deduplicate to one component.
     - Endpoint mapped without `Accepts`/`Produces` is unaffected by the transformer.
   - Run `dotnet test tests/Turbine.Tests/Turbine.Tests.csproj --filter <relevant>` and confirm the new tests fail for the right reason **before** writing any production code.

4. **Implement (green).** Make the new tests pass. Conventions:
   - Public surface stays on `RouteHandlerBuilderExtensions`. Anything else introduced for this work is `internal` unless an ADR justifies otherwise — `Turbine.Tests` already has `InternalsVisibleTo` (verify; if not, add it via the project's existing pattern, not a new attribute file).
   - Re-use `TurbineSchemaRegistry` for schema lookup. Do not duplicate the registry in the OpenAPI layer.
   - Keep `SchemaConfiguration`, the builders in `src/Turbine/Builders/`, and the schemas in `src/Turbine/Schemas/` untouched unless strictly necessary; if you must change one of them (e.g. to expose a constraint the OpenAPI converter needs), write an ADR and proceed without waiting for approval.
   - JSON content type defaults to `application/json` (see `feedback_json_default`). If a slice grows a content-type override, that's a public-surface decision and needs an ADR.
   - Selector-parsing failures must produce a message a user can debug from alone — name the expression, the configuration type, and the expected shape (`x => x.SomeSchemaProperty`).
   - After focused tests are green, run the full `Turbine.Tests` suite (`dotnet test tests/Turbine.Tests/Turbine.Tests.csproj`) to catch regressions. **Never** run `dotnet build` or `dotnet test` against the solution as a whole, against `Turbine.Starfleet`, or against `Turbine.Starfleet.Tests`.

5. **Refactor.** Tidy duplication introduced this iteration only. Do not refactor unrelated code (per `CLAUDE.md`).

6. **ADR if needed.** Hard-to-reverse decisions in this loop almost certainly include:
   - The shape of the endpoint metadata record (public vs. internal, the role enum, content-type field).
   - Whether the OpenAPI work uses one document transformer, one operation transformer, or both, and where the schema-component materialisation happens.
   - The deterministic component-naming convention (e.g. stripping `Schemas` suffix vs. raw type name) and how clashes are resolved.
   - The expression-shape contract for selectors (what counts as a "property access" — direct property, chained property, ignored conversions, etc.).
   - Status-code validation policy.
   - How nested schemas decide between inline `OpenApiSchema` and `$ref` to a component.

   When you make one, write `docs/adr/NNN-short-slug.md` from `docs/adr/000-template.md`, mark it Accepted, link any superseded ADR, and proceed without waiting for approval. Skip ADRs for purely local choices.

7. **Commit.** One commit per iteration: tests + implementation + any ADR together. Conventional message describing the slice finished. Do not push.

## Hard constraints

- **Never** modify anything under `src/Turbine.Starfleet/**` or `tests/Turbine.Starfleet.Tests/**`. **Never** run `dotnet build` or `dotnet run` against either Starfleet project. **Never** invoke the sample as a smoke test (no requests to `/personnel`, `/starships`, `/stardate`, or `/openapi/v1.json` against Starfleet). The sample is read-only reference material this loop.
- **Never** run `dotnet build` or `dotnet test` on the solution as a whole — scope to `tests/Turbine.Tests/Turbine.Tests.csproj`. The Turbine.Tests project transitively builds `src/Turbine`, which is the only production code that needs to compile this loop.
- **Don't** add NuGet dependencies. `Microsoft.AspNetCore.OpenApi` (10.0.4) is already referenced by `src/Turbine/Turbine.csproj`; `Microsoft.AspNetCore.Mvc.Testing` (10.0.4) is already referenced by `tests/Turbine.Tests/Turbine.Tests.csproj`. If a test or the implementation genuinely needs another dependency, stop and report instead.
- **Don't** change the public signatures of `Produces<T>` / `Accepts<T>` (parameter order, generic constraint, return type) without an ADR — endpoints in the wider project already call them with the existing shape.
- **Don't** silently change anything in `src/Turbine/Schemas/**` or `src/Turbine/Builders/**` to make OpenAPI conversion easier. If a schema or builder type genuinely needs a change, write an ADR and proceed.
- **Don't** introduce a parallel schema registry — extend `TurbineSchemaRegistry` (its addition surface is `internal`) or read from it; do not invent a second source of truth.
- Keep diffs small. A slice past ~400 LOC of diff (tests + impl combined) gets split across iterations.

## Stop when

`Accepts<T>` and `Produces<T>` carry real metadata, the OpenAPI document produced by `MapOpenApi` after `AddTurbine` + `MapTurbine` contains a `components/schemas/` entry for every Turbine schema reachable from any endpoint plus correct request/response wiring on each operation, schemas are deduplicated across endpoints, and the relevant unit + integration tests in `tests/Turbine.Tests/` are all green with no skipped tests. Report a one-paragraph summary listing the slices implemented, the component-naming convention that landed, the transformer shape (single document, operation+document, etc.), and any ADRs written.
