# Ralph loop: implement and test the host-integration extensions

**Goal:** Implement `IServiceCollection.AddTurbine(...)` and `WebApplication.MapTurbine(...)` so that

- `AddTurbine` discovers every concrete, public `SchemaConfiguration` subclass in the target assemblies (entry assembly by default, or one or more assemblies passed explicitly via a `params Assembly[]` overload) and registers each as a singleton in DI, alongside any other services the runtime needs (e.g. a runtime schema registry). Repeated calls and overlapping assembly sets must not produce duplicate registrations.
- `MapTurbine` triggers schema initialization at runtime: it resolves each registered `SchemaConfiguration`, runs its `Configure(...)` so all schema properties are populated, and builds a runtime registry that other Turbine pieces (route handler metadata, validation middleware, OpenAPI contribution) can read.
- Cross-schema dependencies introduced via `AddPropertiesFrom` / `AddMappingsFrom` (and any other builder method that pulls from another schema) are honoured: schemas are built in dependency order. Cyclic dependencies are detected and surface as a clear exception, not a stack overflow or silent corruption.

Stop when both extensions are implemented to the spec above, every public path is covered by tests in `tests/Turbine.Tests/Unit/` and `tests/Turbine.Tests/Integration/`, and `dotnet test tests/Turbine.Tests/Turbine.Tests.csproj` is green with no skipped tests in this area.

## Per-iteration workflow

1. **Orient.** Read `CLAUDE.md`, `docs/Architecture.md`, and the `docs/adr/` index. Re-read `src/Turbine/Extensions/ServiceCollectionExtensions.cs` and `src/Turbine/Extensions/WebApplicationExtensions.cs`, plus any registry / lifecycle types that already exist. List `tests/Turbine.Tests/Unit/` and `tests/Turbine.Tests/Integration/`. Identify the next slice that is either (a) unimplemented, (b) lacking tests covering its public behaviour, or (c) missing a documented decision the rest of the loop will rely on. Pick exactly one slice. If everything in scope is implemented, tested, and green, stop and report done.

   Suggested slice ordering (split further if any one grows past ~400 LOC of diff):
   - Assembly discovery in `AddTurbine` — concrete public `SchemaConfiguration` subclasses, abstract / nested / generic exclusions, default-to-entry-assembly behaviour, the `params Assembly[]` overload accepting one or many assemblies, deduplication when the same assembly (or the same type across overlapping assembly sets) is seen more than once, idempotence on repeated calls.
   - DI registration shape — singleton lifetime, registration of any auxiliary types needed for `MapTurbine` (e.g. a registry / initializer), no clobbering of pre-existing registrations.
   - Schema initialization in `MapTurbine` — resolving each `SchemaConfiguration`, invoking `Configure`, capturing the populated schemas.
   - Dependency-ordered build — detect inter-schema dependencies introduced by `AddPropertiesFrom` / `AddMappingsFrom`, build in topological order, fail loudly on cycles.
   - Runtime registry surface — what the rest of the library reads to look schemas up by `(SchemaConfiguration type, schema selector / property name)`. Keep the surface internal unless a public hook is genuinely required.

2. **Reference, don't modify.** `src/Turbine.Starfleet/**` and `tests/Turbine.Starfleet.Tests/**` are reference material only. Read them to learn how Turbine is consumed (especially `Program.cs`, `Resources/Personnel/PersonnelSchemas.cs`, and the `*Endpoints.cs` files) — never edit them, never `dotnet build` them, never `dotnet run` them, never invoke the sample as a smoke test. The sample's correctness is irrelevant to this loop.

3. **Test first (red).**
   - Unit tests live in `tests/Turbine.Tests/Unit/`. Use them for pure logic: discovery filtering, dependency-graph building, cycle detection, registry lookup behaviour. Construct `IServiceCollection` directly; do not spin up a host.
   - Integration tests live in `tests/Turbine.Tests/Integration/`. Use them for the end-to-end wiring: build a `WebApplication` (or `WebApplicationFactory`) with a few in-test `SchemaConfiguration` subclasses, call `AddTurbine` + `MapTurbine`, and assert on the resolved configurations and on the populated schema state. `Microsoft.AspNetCore.Mvc.Testing` is already referenced by the test project.
   - All test fixtures (test-only `SchemaConfiguration` subclasses, POCO domain types, sample assemblies) live inside the test project. Never add fixtures to `src/Turbine`.
   - Cover happy path + at least one failure mode per slice (per `CLAUDE.md`). Required failure modes for this loop include at minimum: null arguments (including a null entry in the `params Assembly[]` array), no entry assembly available when no assemblies are passed, empty `params` array behaviour (decide and document — fall back to entry assembly, or treat as user error), no `SchemaConfiguration` subclasses found across the supplied assemblies, abstract / non-public types ignored, the same configuration type discovered through multiple assemblies registered only once, cyclic dependency between two schemas, cyclic dependency in a longer chain, `MapTurbine` called without `AddTurbine`.
   - Run `dotnet test tests/Turbine.Tests/Turbine.Tests.csproj --filter <relevant>` and confirm the new tests fail for the right reason before writing any production code.

4. **Implement (green).** Make the new tests pass. Conventions:
   - Public surface goes on the existing static classes (`ServiceCollectionExtensions`, `WebApplicationExtensions`). Anything else introduced for this work should be `internal` unless an ADR justifies otherwise — `Turbine.Tests` already has `InternalsVisibleTo`.
   - Keep `SchemaConfiguration` and the builders untouched unless strictly necessary; if you must change a schema or builder type, write an ADR (per `CLAUDE.md`) and proceed.
   - Cycle detection must produce an exception whose message names the cycle's participants (configuration type + schema property), so a user can debug from the message alone. Pick or introduce an exception type and document the choice if it's a new public type.
   - After focused tests are green, run the full `Turbine.Tests` suite to catch regressions. `dotnet test` only the test project — never build or test Starfleet.

5. **Refactor.** Tidy duplication introduced this iteration. Do not refactor unrelated code (per `CLAUDE.md`).

6. **ADR if needed.** Hard-to-reverse decisions in this loop almost certainly include:
   - The shape and lifetime of the runtime schema registry (singleton vs. scoped, public vs. internal).
   - How dependencies between schemas are discovered (e.g. instrumenting builders to record a dependency edge vs. analyzing `Configure` via Expression trees vs. a try-and-defer multipass).
   - The exception type and message contract for cyclic references.

   When you make one, write `docs/adr/NNN-short-slug.md` from `docs/adr/000-template.md`, mark it Accepted, link any superseded ADR, and proceed without waiting for approval. Skip ADRs for purely local choices.

7. **Commit.** One commit per iteration: tests + implementation + any ADR together. Conventional message describing the slice finished. Do not push.

## Hard constraints

- Never modify `src/Turbine.Starfleet/**` or `tests/Turbine.Starfleet.Tests/**`. Never run `dotnet build` or `dotnet run` against the Starfleet projects, and never hit `/personnel` or any other Starfleet endpoint as a smoke test. The sample is read-only reference material this loop.
- Never run `dotnet build` or `dotnet test` on the solution as a whole — scope to `tests/Turbine.Tests/Turbine.Tests.csproj`.
- Don't add NuGet dependencies. If a test or the implementation genuinely needs one, stop and report instead.
- Don't change the public signatures of `SchemaConfiguration`, `SchemaConfigurationBuilder`, or any builder in `src/Turbine/Builders/` to make discovery / ordering easier without an ADR. `internal` additions are fine.
- Don't change `RouteHandlerBuilder.Produces<T>` / `Accepts<T>` in this loop. They consume the registry but aren't in scope; if they need a registry-side hook, expose an `internal` API and stop short of wiring them.
- Keep diffs small. A slice past ~400 LOC of diff (tests + impl combined) gets split.
- Don't silently change anything in `src/Turbine/Schemas/**` or `src/Turbine/Builders/**` to make the extensions easier. If a type genuinely needs a change, write an ADR and proceed.

## Stop when

`AddTurbine` discovers and registers `SchemaConfiguration` subclasses correctly, `MapTurbine` builds a fully populated runtime schema registry honouring inter-schema dependencies, cyclic references throw a clear exception, and the relevant unit + integration tests in `tests/Turbine.Tests/` are all green with no skipped tests. Report a one-paragraph summary listing the slices implemented, the registry shape that landed, and any ADRs written.
