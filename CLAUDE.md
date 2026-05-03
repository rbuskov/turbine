# Project operating guide

## How we work

- Tests-first: Red, green, refactor. Every task in a plan specifies its tests. Implementation without the test is
  incomplete.
- Small PRs. One vertical slice per PR. If a slice grows past ~400 lines of diff, split it.
- Significant decisions get an ADR in `docs/adr/`. See "When to write an ADR" below.

## Testing rules

- No feature is "done" until tests pass and cover the happy path + at least one failure mode.
- Prefer integration tests at the slice boundary over unit tests of internals. Unit-test pure logic.
- Tests that require mocking more than two collaborators are a smell — the design is probably wrong. Surface this in
  /review, don't paper over it.
- Coverage is a lagging indicator, not a goal. Target: meaningful assertions, not line counts.

## Architecture

- See `docs/Architecture.md` for the full picture. Always read it before planning a feature that touches multiple
  layers.
- Current architectural decisions live in `docs/adr/`. Read the index before proposing changes that might contradict
  one.

## When to write an ADR

Write one when a decision:

- Is hard to reverse (public API design, cross-cutting concerns, major library choices)
- Has a plausible alternative someone might later ask "why didn't we do X?"

Do NOT write an ADR for: library choices inside a module, formatting, naming conventions, anything you'd change on a
whim.

ADR format: `docs/adr/NNN-short-slug.md`, using the template at `docs/adr/000-template.md`. Status is Proposed →
Accepted → Superseded. Never edit an Accepted ADR; supersede it with a new one that links back.

## Code style

- Standard C# code style (do not use underscore prefix)
- No dead code. If it's not called, delete it. Version control remembers.
- Comments explain *why*, not *what*. The code says what.

## What Claude should NOT do

- Don't add dependencies without flagging it in the plan. New dependencies are decisions.
- Don't "improve" code outside the current task's scope. Note it and move on.
- Don't silently change public interfaces. Breaking changes should be considered carefully and need an ADR.
- Don't skip writing tests because "the logic is obvious."