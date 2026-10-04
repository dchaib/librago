# Librago UI Components

## Scope

This document defines Librago's reusable UI primitives.

It specifies:

- component purpose;
- supported variants;
- visual contract;
- meaningful states;
- interaction contract;
- usage rules.

Visual values and token definitions come from `foundations.md`.

Domain-specific compositions and product behaviour belong with the corresponding product or feature specifications.

Framework choice, markup structure, CSS architecture, component APIs, and implementation planning are outside the scope of this document.

---

# Shared rules

## Reuse before creating

Before introducing a new component or visual variant, check whether an existing component can satisfy the use case.

Do not create page-specific variants merely to solve local styling preferences.

A new component or variant should represent a recurring interaction, visual role, or semantic difference.

---

## Semantic design tokens

Components use semantic roles from `foundations.md`.

They must not depend directly on:

- a theme name;
- a specific font family;
- a literal palette color;
- arbitrary spacing or radius values.

For example, a component uses:

```text
color-brand
color-on-brand
font-family-interface
radius-md
space-4
```

rather than values specific to Ocean, Natural, Inter, or Playfair Display.

---

## Typography

Basic UI primitives use `font-family-interface`.

`font-family-heading` is used only where a component explicitly exposes an expressive heading role.

Components do not reference concrete font families directly.

---

## States

Interactive components expose only states that have behavioural meaning.

Possible states include:

- default;
- hover;
- active / pressed;
- focus;
- disabled;
- selected;
- loading;
- error.

Not every component requires every state.

---

## Theme independence

Themes may alter visual tokens but do not alter component purpose or interaction behaviour.

The same component structure is used across supported themes and modes.

---

## Mobile interaction

Components must not depend exclusively on hover.

Normal touch-oriented controls use an effective interactive target of approximately 44 px or larger.

Compact visual controls may use a smaller visible size when their effective target remains appropriate.

---

# Button

## Purpose

Use Button for explicit user actions.

Examples include:

- applying filters;
- saving changes;
- confirming an operation;
- retrying a failed operation.

## Variants

- `primary`
- `secondary`
- `ghost`
- `danger`

### Variant usage

#### Primary

Represents the main action within a local interaction context.

Avoid multiple visually competing primary actions within the same small interaction area.

#### Secondary

Represents an important action that is subordinate to the primary action.

#### Ghost

Represents a low-emphasis action where a filled or bordered treatment would add unnecessary visual weight.

#### Danger

Reserved for destructive actions.

Do not use the danger variant merely to communicate warning or urgency.

### Visual mapping

| Variant | Background | Foreground | Border |
| --- | --- | --- | --- |
| `primary` | `color-brand` | `color-on-brand` | `color-brand` |
| `secondary` | `color-surface` | `color-text-primary` | `color-control-border` |
| `ghost` | transparent | `color-interactive` | transparent |
| `danger` | `color-danger-action` | `color-on-danger-action` | `color-danger-action` |

On hover:

- `primary` uses `color-brand-hover`;
- `danger` uses `color-danger-action-hover`;
- secondary and ghost treatments should gain visible emphasis without changing semantic role.

## Sizes

| Property | `sm` | `md` |
| --- | ---: | ---: |
| Visible height | `control-height-sm` | `control-height-md` |
| Horizontal padding | `space-3` | `space-4` |
| Content gap | `space-2` | `space-2` |
| Text size | `text-sm` | `text-sm` |
| Weight | `font-weight-semibold` | `font-weight-semibold` |
| Radius | `radius-md` | `radius-md` |

`md` is the default.

## Content

A button may contain:

```text
[optional icon] Label
```

Icons use `icon-sm` or `icon-md` according to available space.

## Behaviour

### Disabled

A disabled button:

- cannot be activated;
- remains recognizable as the same action;
- is visually de-emphasized without becoming illegible.

### Loading

When an action enters a loading state:

- repeated activation is prevented;
- the component preserves its dimensions where practical;
- progress is visibly indicated;
- the action label should remain present when space allows.

### Focus

Uses the standard focus treatment defined in `foundations.md`.

---

# IconButton

## Purpose

Use IconButton for compact actions whose meaning is reliably understandable from a familiar icon and context.

Examples include:

- close;
- previous / next;
- open contextual menu.

## Visual contract

| Property | Value |
| --- | --- |
| Default visible size | `control-height-md` |
| Icon | `icon-md` |
| Radius | `radius-md` |
| Typeface | not applicable |

Compact contexts may use a smaller visible control while preserving an appropriate interaction target.

## Behaviour

An IconButton must always have an accessible name, whether or not a visible label is present.

If the action cannot reasonably be understood without explanatory text, use a regular Button instead.

Support:

- default;
- hover;
- active;
- focus;
- disabled.

---

# TextField

## Purpose

Use TextField for free-form text input.

## Structure

A TextField may contain:

```text
Label
Input
Supporting text or error
```

It may additionally contain an appropriate leading or trailing icon.

## Visual contract

| Property | Value |
| --- | --- |
| Input height | `control-height-md` |
| Typeface | `font-family-interface` |
| Text size | `text-base` |
| Horizontal padding | `space-3` |
| Radius | `radius-md` |
| Border | `border-width-default`, `color-control-border` |
| Background | `color-surface` |
| Foreground | `color-text-primary` |

## Behaviour

A visible label is preferred to placeholder-only identification.

Label, supporting description, and error text must remain associated with the input semantically as well as visually.

Error state:

- uses semantic danger treatment;
- includes a textual explanation;
- does not rely on color alone.

Support:

- default;
- hover;
- focus;
- disabled;
- read-only;
- error.

---

# Select

## Purpose

Use Select for choosing one value from a manageable set.

Examples include:

- borrower;
- network;
- library;
- sort order.

## Visual contract

Select follows the same core visual metrics as TextField:

| Property | Value |
| --- | --- |
| Height | `control-height-md` |
| Typeface | `font-family-interface` |
| Text size | `text-base` |
| Horizontal padding | `space-3` |
| Radius | `radius-md` |
| Border | `border-width-default`, `color-control-border` |
| Background | `color-surface` |
| Foreground | `color-text-primary` |

## Usage

Use Select when:

- exactly one value is selected;
- the option set is relatively small.

For large option sets, a searchable selection interaction is more appropriate.

## Behaviour

Support:

- default;
- hover;
- focus;
- disabled;
- error.

Label and error information follow the same association requirements as TextField.

---

# SearchField

## Purpose

SearchField is the standard search input.

Examples include:

- library catalogue search;
- filtering displayed content.

## Structure

```text
[search icon] Query                         [optional clear action]
```

## Visual contract

SearchField follows the core TextField metrics.

| Property | Value |
| --- | --- |
| Height | `control-height-md` |
| Icon | `icon-md` |
| Content gap | `space-2` |
| Radius | `radius-md` |
| Background | `color-surface` |
| Border | `color-control-border` |

## Behaviour

The clear action:

- appears only when there is content to clear;
- has an accessible name;
- clears the query without changing unrelated filters.

Support:

- default;
- hover;
- focus;
- disabled;
- populated.

SearchField contains search input only. Related filters remain separate controls.

---

# Checkbox

## Purpose

Use Checkbox when several independent choices may be selected.

## Behaviour

A Checkbox has an explicit label.

The label and control should form one convenient interaction target.

Support:

- unchecked;
- checked;
- indeterminate when genuinely required;
- focus;
- disabled.

Keyboard interaction follows the native checkbox model.

---

# Radio

## Purpose

Use Radio when exactly one option must be selected from a small visible set.

## Behaviour

Radio controls are presented as a labelled group.

Support:

- unselected;
- selected;
- focus;
- disabled.

Keyboard interaction follows the standard radio-group model.

For strongly visual choices, a dedicated selection component may be preferable.

---

# Badge

## Purpose

Use Badge for compact identification or categorization.

Examples include:

- library network;
- item type;
- source;
- category.

Badge does not communicate urgency or application state.

## Visual contract

| Property | Value |
| --- | --- |
| Typeface | `font-family-interface` |
| Text size | `text-sm` |
| Weight | `font-weight-medium` |
| Vertical padding | `space-1` |
| Horizontal padding | `space-2` |
| Radius | `radius-pill` |
| Treatment | restrained, non-semantic |

`text-xs` may be used only in deliberately compact secondary-metadata contexts where the smaller treatment remains clearly readable.

A Badge may contain a small icon or marker when it provides useful identification.

Strong semantic colors should not be used for ordinary metadata.

---

# StatusPill

## Purpose

Use StatusPill for concise semantic application state.

Examples include:

- available;
- reservation ready;
- due soon;
- overdue;
- synchronization failed.

## Variants

- `success`
- `info`
- `warning`
- `danger`

## Visual mapping

Each variant uses its corresponding pair from `foundations.md`:

```text
color-status-<variant>-bg
color-status-<variant>-fg
```

This automatically provides:

- softer treatment in light mode;
- stronger treatment in dark mode.

## Visual contract

| Property | Value |
| --- | --- |
| Typeface | `font-family-interface` |
| Text size | `text-sm` |
| Weight | `font-weight-semibold` |
| Vertical padding | `space-1` |
| Horizontal padding | `space-2` |
| Content gap | `space-1` |
| Radius | `radius-pill` |

`text-xs` may be used for a compact secondary status when the status is not the primary information carried by the item.

An optional `icon-sm` may reinforce the state.

## Usage

Labels remain concise.

StatusPill represents real semantic state.

It is not a general-purpose decorative label.

---

# Card

## Purpose

Use Card when grouping related content in a distinct surface improves comprehension.

## Variants

- `default`
- `raised`
- `interactive`

## Visual mapping

### Default

| Property | Value |
| --- | --- |
| Background | `color-surface` |
| Border | `border-width-default`, `color-border` |
| Radius | `radius-lg` |
| Shadow | `shadow-none` |
| Padding | `space-4` |

`space-5` may be used for a major standalone card when its content hierarchy genuinely benefits from additional breathing room.

### Raised

Uses:

```text
color-surface-raised
shadow-sm
```

Elevation should correspond to genuine visual emphasis.

### Interactive

Starts from the default visual treatment and adds:

- hover feedback;
- focus indication;
- active feedback.

If the entire Card is interactive, it should have one clear primary interaction.

Avoid ambiguous combinations of a clickable whole-card surface with competing nested actions.

## Usage

Card is not the default wrapper for every piece of content.

---

# Alert

## Purpose

Use Alert for important contextual information requiring explicit attention.

## Variants

- `info`
- `success`
- `warning`
- `danger`

## Visual mapping

Alerts use the matching semantic foreground/background pair from `foundations.md`.

## Structure

```text
[optional icon] Short title
                Concise explanation
                [optional action]
```

## Behaviour

An alert already present when the page loads is ordinary page content.

When an alert is introduced dynamically because of an important outcome or failure, the application should expose that change appropriately to assistive technology.

Alerts should remain relatively uncommon.

---

# Tabs

## Purpose

Use Tabs for switching between a small number of closely related views within the same context.

Tabs do not replace primary navigation.

## States

- default;
- hover;
- focus;
- selected;
- disabled where genuinely required.

## Behaviour

Tabs and their corresponding panels form one associated control.

Keyboard navigation uses the standard tab-list model:

- arrow keys move between tabs;
- Home and End move to the first and last tab;
- focus remains clearly visible.

Automatic activation is preferred when switching panels is immediate and local.

If activation would trigger expensive or delayed work, explicit activation may be used instead.

---

# Table

## Purpose

Use Table for structured data that benefits from column comparison on sufficiently wide screens.

## Visual contract

Prefer:

- `font-family-interface`;
- `text-sm` or `text-base`;
- 44–48 px minimum row height;
- subtle horizontal separation;
- clear headers;
- predictable column alignment.

Avoid unnecessary vertical borders.

## Responsive scope

Table itself defines tabular presentation only.

If a feature requires a different representation on narrow screens, that responsive composition is defined by the relevant feature or page design rather than by Table.

---

# ListItem

## Purpose

ListItem is a reusable structural primitive for compact list content.

It exposes conceptual areas such as:

```text
leading
primary content
secondary content
trailing content
```

## Visual contract

| Property | Value |
| --- | --- |
| Typeface | `font-family-interface` |
| Minimum interactive height | approximately 44 px when interactive |
| Vertical padding | `space-3` |
| Horizontal padding | `space-4` |
| Content gap | `space-3` |
| Separator | `color-border` where needed |

`space-3` horizontal padding may be used for a deliberately compact list.

## Usage

ListItem remains intentionally generic.

Domain-specific compositions are defined with the product or feature that owns them rather than by multiplying generic ListItem variants.

---

# Dialog

## Purpose

Use Dialog for focused interactions that temporarily require user attention.

Examples include:

- confirmation;
- editing a small set of values;
- destructive-action confirmation.

## Visual contract

| Property | Value |
| --- | --- |
| Background | `color-surface-raised` |
| Radius | `radius-xl` |
| Shadow | `shadow-lg` |
| Padding | `space-5` to `space-6` |
| Heading role | `font-family-heading` where an expressive dialog title is appropriate |

## Behaviour

When opened:

- focus moves into the dialog at an appropriate initial location;
- keyboard focus remains within the modal dialog while it is open.

The dialog can be dismissed with Escape unless doing so would be unsafe for the interaction.

When closed:

- focus returns to the element that opened it when that element still exists and remains appropriate.

Dialogs should remain focused and relatively small.

Do not move ordinary page workflows into dialogs merely to avoid creating a proper page or section.

---

# DropdownMenu

## Purpose

Use DropdownMenu for a small set of secondary contextual actions.

Primary actions should not be hidden inside a contextual menu without a strong reason.

## Visual contract

| Property | Value |
| --- | --- |
| Background | `color-surface-raised` |
| Border | `border-width-default`, `color-border` |
| Radius | `radius-md` |
| Shadow | `shadow-md` |
| Item height | approximately 40–44 px |
| Typeface | `font-family-interface` |

## Behaviour

When opened:

- keyboard focus can enter and navigate the menu;
- arrow keys move between available actions;
- Escape closes the menu;
- closing returns focus to the trigger where appropriate.

Destructive actions use semantic danger emphasis.

---

# Component composition

Prefer composition of simple primitives over components with large numbers of variants.

For example:

```text
Card
  + StatusPill
  + Button
```

is preferable to creating a highly specialized generic component unless a recurring need demonstrates that such a component deserves its own definition.

---

# Component variants

Variants remain deliberately limited.

A new variant should represent a recurring:

- semantic difference;
- interaction difference;
- structural need that cannot be expressed cleanly through composition.

Do not create visual variants for isolated page-specific preferences.
