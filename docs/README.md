# Documentation guide

This directory contains Librago's project documentation.

The goal is to keep different kinds of decisions in the right place so the documentation remains useful to both humans and development agents.

## Principles

- The root `README.md` is the public-facing introduction.
- Product documentation explains what Librago should do and why.
- Library-network documentation describes available data and connector constraints.
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
- `product/features/reservations.md`
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

### `design/`

The design system documents Librago's shared visual direction and foundations.

- `design/principles.md` describes the durable visual and interaction principles;
- `design/foundations.md` defines visual tokens, themes, and their values.

Feature-specific behavior and domain compositions belong in the relevant product specification. Link to the design system for shared visual rules instead of duplicating them.

### `networks/<network>.md`

Current reference for a library network's connector.

Contains:
- supported capabilities and available fields;
- source-to-domain mappings;
- account scope and source limitations;
- completeness and identity requirements;
- integration questions still to resolve.

Keep these references concise and useful for implementation and maintenance. Record the current contract and its limitations. Exploration chronology, manual test reports, raw captures, and copies of third-party source code do not belong in these documents. Network details should link to the relevant product specification without duplicating its behavior rules.

Use the same structure for each network: scope and implementation entry points, access/session handling, then retrieval, mapping, identity, and validation for each interaction. State which interactions are implemented and which are planned. Mark assumptions and missing information explicitly. Document only the endpoints, selectors, and fields needed by Librago's current or next slice.

### `adr/`

Structural technical decisions.

An ADR should generally contain:
- context;
- decision;
- consequences;
- status.

ADRs should explain **why** a decision was made.

### `architecture/`

Living technical references describe **how** Librago's accepted target architecture works.

- [Overview](architecture/overview.md): application shape, interactions, data flow, and trust boundaries.
- [Application](architecture/application.md): application responsibilities, synchronization/read flows, connector boundaries, and persistence guarantees.
- [Web](architecture/web.md): frontend toolchains, hosting, routing, and design-system integration.
- [Authentication](architecture/authentication.md): identity admission, application sessions, antiforgery, and Data Protection.
- [Security](architecture/security.md): enduring browser, runtime, and network security requirements.

Each topic has a primary reference; other documents summarize and link to it. Provider-specific connector details belong in `networks/`. ADRs explain the structural choices and their rationale; migration progress belongs in the product roadmap.

### `deployment/`

Operational deployment references. The [validation checklist](deployment/validation.md) tracks checks of the selected identity provider, reverse proxy, persistent keys, browser hardening, and egress controls in the target environment. Architecture references define the enduring requirements; this checklist records their operational verification.

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
| What shared visual language and component guidance should the interface follow? | `design/` |
| Which data and constraints does a library network expose? | `networks/<network>.md` |
| What integration question is unresolved? | The relevant `networks/<network>.md` |
| Why did we choose an architecture or technology? | `adr/` |
| How is Librago architecturally designed, and what deployment requirements apply? | `architecture/` |
| Which deployment controls still need operational validation? | `deployment/validation.md` |
| How should an agent work in the repo? | `AGENTS.md` |
| What should a new visitor know first? | root `README.md` |
