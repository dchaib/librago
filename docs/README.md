# Documentation guide

This directory contains Librago's project documentation.

The goal is to keep different kinds of decisions in the right place so the documentation remains useful to both humans and development agents.

## Principles

- The root `README.md` is the public-facing introduction.
- Product documentation explains what Librago should do and why.
- Architecture Decision Records explain structural technical decisions and their rationale.
- Agent working rules belong in `AGENTS.md`.
- Open questions should remain explicit instead of being silently resolved in implementation.
- Detailed documentation should be written only for features close to implementation.
- The repository is the source of truth for validated decisions.

## Structure

### `product/vision.md`

Why Librago exists and the long-term product direction.

Contains:
- problem and target users;
- product principles;
- long-term priorities;
- broad concepts spanning multiple features.

### `product/roadmap.md`

Sequencing of features and deferred or cross-cutting concerns.

Contains:
- broad feature increments;
- short descriptions of future capabilities;
- intentionally postponed topics.

It should not become a detailed specification or project-management backlog.

### `product/features/<feature>.md`

Detailed specification of a feature close to implementation.

Examples:
- `product/features/loans.md`
- later, potentially `product/features/reservations.md`
- later, potentially `product/features/search.md`

May contain:
- user stories;
- information hierarchy;
- behavior;
- sorting and filtering rules;
- states and edge cases;
- acceptance criteria;
- validated product decisions;
- UX questions intentionally left open.

### `product/open-questions.md`

Only unresolved product questions.

Once answered, remove the question and move the resulting decision into the appropriate product document when relevant.

### `adr/`

Structural technical decisions.

An ADR should generally contain:
- context;
- decision;
- consequences;
- status.

ADRs should explain **why** a decision was made.

## Other project files

### Root `README.md`

Public-facing project introduction.

It should focus on:
- what Librago is;
- the problem it solves;
- major capabilities;
- basic project status;
- links to deeper documentation.

It should not be used for agent rules or detailed version scope.

### Root `AGENTS.md`

Instructions for development agents working on the repository.

It may contain:
- repository conventions;
- privacy constraints;
- documentation rules;
- implementation workflow;
- domain language guidance.

## Where should a decision go?

| Question | Location |
| --- | --- |
| Why does Librago exist? | `product/vision.md` |
| What should be built next? | `product/roadmap.md` |
| How should a specific feature behave? | `product/features/<feature>.md` |
| What product question is unresolved? | `product/open-questions.md` |
| Why did we choose an architecture or technology? | `adr/` |
| How should an agent work in the repo? | `AGENTS.md` |
| What should a new visitor know first? | root `README.md` |
