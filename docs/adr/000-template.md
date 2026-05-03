# ADR-NNN: <short title of the decision>

- **Status:** Proposed | Accepted | Superseded by ADR-XXXX | Deprecated
- **Date:** YYYY-MM-DD
- **Deciders:** <names / "team">

## Context

What's the situation that forced a decision? What constraints, requirements, or
problems are in play? Keep it to a few sentences — enough that someone new
(or Claude in a future session) can understand *why* this came up without
needing to read the whole codebase.

## Decision

What did we decide to do? Be specific and active:
"We will use Postgres with `pgvector` for embeddings storage."
Not: "We considered using Postgres."

## Consequences

What follows from this decision — both good and bad?
- ✅ Positive: things this enables or simplifies.
- ⚠️ Negative: trade-offs, costs, things we're now locked into.
- 🔄 Follow-ups: anything that needs to happen as a result (migrations, doc
  updates, dependencies to add).

## Alternatives considered

Brief — one line each is fine. Why didn't we pick them?
- **Option A:** rejected because …
- **Option B:** rejected because …