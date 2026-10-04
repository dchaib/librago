# Librago Design Principles

## Purpose

Librago is a personal, practical application for aggregating multiple library accounts and networks into a single coherent interface.

Its interface should help users understand quickly:

- what requires attention;
- what is due soon;
- which borrower, library, or network is concerned;
- whether data is up to date;
- what action can or should be taken next.

The design should prioritize everyday usability over visual spectacle, while still giving Librago a distinctive and recognizable personality.

## Visual character

Librago should feel:

- sober;
- warm;
- efficient;
- modern;
- personal;
- polished without feeling premium for its own sake.

It should avoid feeling:

- municipal or institutional;
- academic or school-like;
- childish;
- austere;
- generic SaaS;
- excessively decorative.

Personality should come primarily from typography, color, contrast, and composition rather than from decorative graphic motifs.

## Content first

Librago is a utility application.

Content and status should always take precedence over decoration.

Visual hierarchy should make important information immediately apparent, especially:

- approaching due dates;
- overdue loans;
- reservations ready for pickup;
- stale or failed synchronization;
- actions requiring user attention.

Decorative UI should never compete with these signals.

## Warm, not playful

Warmth should come from:

- color;
- typography;
- spacing;
- moderately rounded shapes;
- carefully balanced contrast.

Avoid overly rounded "cute" interfaces, playful illustration styles, or visual treatments that make the application feel targeted at children.

## Compact, but not cramped

The target density is between medium and compact.

The interface should be able to display useful amounts of information without excessive scrolling or oversized cards.

Prefer:

- clear grouping;
- compact vertical rhythm;
- meaningful whitespace;
- concise metadata;
- restrained card padding.

Avoid using large cards as the default container for every piece of information.

## Mobile first

Mobile is a first-class use case, not a reduced desktop layout.

Pages should be designed for mobile explicitly.

Do not mechanically collapse desktop tables or layouts onto small screens.

Important information and actions should remain easy to scan and reach on a phone.

Desktop layouts may use additional width and density where useful.

## Dark mode first-class

Light and dark modes must be designed together.

Dark mode must not be implemented as an afterthought or as a simple inversion of the light theme.

Librago uses colored dark surfaces rather than neutral black whenever appropriate.

Both modes must preserve:

- hierarchy;
- readability;
- status visibility;
- brand personality.

## Themes

Librago may support multiple visual themes.

Themes should share the same underlying design system and application structure.

They should normally preserve:

- spacing and density;
- component structure;
- interaction patterns;
- layout behavior;
- semantic meaning of states.

Themes may vary visual expression through elements such as:

- color;
- typography;
- surface treatment;
- border and contrast emphasis;
- other visual tokens when justified by the theme.

Theme-specific variations should remain controlled and should not require separate page layouts or component implementations.

A theme should feel like a different visual expression of Librago, not a different application.

## Semantic color

Brand colors and semantic colors serve different purposes.

Semantic colors communicate meaning such as:

- success;
- information;
- warning;
- danger.

They must remain understandable across both themes.

Important state must never rely on color alone when ambiguity is possible. Text, icons, labels, or other visual cues should reinforce critical meaning.

## Reuse before invention

Pages should use shared UI components and Librago-specific patterns.

Do not create visually equivalent controls independently on different pages.

Prefer an existing design-system component or pattern before introducing a new variation.

New visual variants should exist because they represent a meaningful need, not because a specific page happens to look better with one.

## Restraint

Avoid:

- excessive shadows;
- decorative gradients;
- unnecessary animation;
- large ornamental illustrations;
- excessive use of accent colors;
- too many component variants;
- visual effects that reduce information density.

A distinctive interface does not require a large number of visual treatments.

## Consistency over perfection

The design system is intentionally small.

The goal is not to build a general-purpose component library, but to give Librago a clear and consistent visual language.

When no explicit rule exists, prefer the solution that is most consistent with existing Librago components and patterns rather than inventing a new one.
