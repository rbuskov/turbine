# Ralph loop: implement and unit-test all schema builders

**Goal:** Implement and unit-test every schema builder in `src/Turbine/Builders/`. One builder per iteration. Stop when all builders are fully implemented, all unit tests pass, and `dotnet test tests/Turbine.Tests/Turbine.Tests.csproj` is green with no skipped tests in the schema-builder area.

## Per-iteration workflow

1. **Orient.** Read `CLAUDE.md`, `docs/Architecture.md`, and `docs/adr/` index. List `src/Turbine/Builders/*.cs` and `tests/Turbine.Tests/Unit/`. Identify the next builder that is either (a) a stub/placeholder, or (b) lacking unit tests covering its public surface. Pick exactly one. If every builder is implemented and tested, run the full unit test suite; if green, stop and report done.
2. **Reference, don't modify.** Read relevant `src/Turbine.Starfleet/**` files only as a usage reference. Do **not** edit anything under `src/Turbine.Starfleet/` or `tests/Turbine.Starfleet.Tests/`. Do **not** build or run the sample. Do **not** invoke `dotnet run` for any project.
3. **Test first (red).** In `tests/Turbine.Tests/Unit/`, add unit tests for the chosen builder. Create test-only `SchemaConfiguration` subclasses (and any small POCO entities) inside the test project as needed — do not add fixtures to the production project. Cover happy path + at least one failure mode per `CLAUDE.md`. Prefer integration-style tests at the builder's public boundary; only unit-test pure internals when the public surface can't reach them. Run `dotnet test tests/Turbine.Tests/Turbine.Tests.csproj --filter <relevant>` and confirm the new tests fail for the right reason.
4. **Implement (green).** Make the builder pass. Follow the project's existing conventions — internal constructors, the configurator/builder split, and the patterns visible in already-implemented builders (`PropertySchemaBuilder.cs`, `NumericSchemaBuilder.cs`, `StringSchemaBuilder.cs`, `OneOfSchemaBuilder.cs`). Re-run the focused tests until green, then run the full `Turbine.Tests` unit suite to catch regressions.
5. **Refactor.** Clean up duplication you introduced in this iteration only. Do not "improve" code outside the current builder's scope (per `CLAUDE.md`).
6. **ADR if needed.** If this iteration required a hard-to-reverse decision (public API shape, a cross-cutting pattern other builders will follow, a meaningful deviation from an existing ADR), write `docs/adr/NNN-short-slug.md` from `docs/adr/000-template.md`, mark it Accepted, and proceed without waiting for approval. Skip ADRs for purely local choices.
7. **Commit.** One commit per iteration: tests + implementation + any ADR together. Conventional message describing the builder finished. Do not push.

## Hard constraints

- Never modify `src/Turbine.Starfleet/**` or `tests/Turbine.Starfleet.Tests/**`.
- Never run `dotnet run`, `dotnet build` on the Starfleet projects, or any smoke test against `/personnel`. The sample is reference material only this loop.
- Don't add NuGet dependencies. If a test genuinely needs one, stop and report instead.
- Keep diffs small. If a single builder's surface is too large for one iteration, split it into smaller sub-surfaces and tackle them across iterations — but always commit tests and implementation together.
- Don't silently change anything in `src/Turbine/Schemas/**` to make a builder easier. If a schema type genuinely needs a change, write an ADR and proceed.

## Stop when

Every file in `src/Turbine/Builders/` has corresponding unit tests in `tests/Turbine.Tests/Unit/`, the full `Turbine.Tests` unit suite is green, and there is no remaining stub/placeholder behavior in any builder. Report a one-paragraph summary listing builders implemented and any ADRs written.
