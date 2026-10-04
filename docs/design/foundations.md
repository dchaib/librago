# Librago Design Foundations

## Scope

This document defines the current visual foundations of Librago.

It records the concrete design tokens and visual values used by the design system.

Unlike `principles.md`, which describes durable design principles, this document contains choices that may evolve as the application is implemented and tested.

The token names used here are semantic design roles. Their technical representation is implementation-specific.

---

## Typography

### Font families

| Role | Current value | Fallback stack |
| --- | --- | --- |
| `font-family-heading` | Playfair Display | Georgia, "Times New Roman", serif |
| `font-family-interface` | Inter | ui-sans-serif, system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif |

The current themes share the same typography.

Theme-specific typography may be introduced later without changing these semantic typography roles.

### Font weights

| Token | Weight | Typical use |
| --- | ---: | --- |
| `font-weight-regular` | 400 | body text |
| `font-weight-medium` | 500 | UI emphasis |
| `font-weight-semibold` | 600 | headings, buttons, labels |
| `font-weight-bold` | 700 | strong status or metadata emphasis |

Avoid introducing arbitrary intermediate weights.

### Type scale

| Token | Size | Line height | Typical use |
| --- | ---: | ---: | --- |
| `text-xs` | 0.75rem | 1.4 | compact metadata |
| `text-sm` | 0.875rem | 1.4 | secondary UI text |
| `text-base` | 1rem | 1.5 | body text and controls |
| `text-lg` | 1.125rem | 1.4 | prominent UI text |
| `title-sm` | 1.25rem | 1.25 | card or section title |
| `title-md` | 1.5rem | 1.2 | section heading |
| `title-lg` | 2rem | 1.15 | page title |
| `title-xl` | 2.5rem | 1.1 | exceptional hero title |

Responsive adjustments may be introduced where required by real page layouts.

### Wrapping and truncation

Text should wrap naturally by default.

Do not truncate meaningful content merely to preserve a fixed component height.

Truncation is appropriate only when:

- the layout has a genuine single-line constraint;
- the full value remains available through another appropriate interaction or context;
- truncation does not hide information required to make a decision.

Compact controls such as buttons, tabs, and status pills normally remain single-line.

---

## Themes

The current Librago themes are:

- **Ocean**
- **Natural**

Each theme supports:

- light mode;
- dark mode.

The themes currently share:

- typography;
- spacing;
- density;
- radius;
- component structure;
- interaction patterns;
- semantic state model.

Theme-specific visual tokens may evolve independently while remaining compatible with the shared design system.

---

## Core color roles

The structural color roles are:

```text
color-background
color-surface
color-surface-raised

color-text-primary
color-text-secondary

color-border
color-control-border

color-brand
color-brand-hover
color-on-brand
color-interactive
color-accent

color-focus-ring
```

### Meaning

- `color-background` is the main page background.
- `color-surface` is the standard content surface.
- `color-surface-raised` is used for genuinely elevated or emphasized surfaces.
- `color-text-primary` is the default foreground.
- `color-text-secondary` is used for supporting information and metadata.
- `color-border` provides subtle structural separation.
- `color-control-border` identifies the boundary of interactive controls such as fields and selects.
- `color-brand` is the dominant theme-specific interaction color.
- `color-brand-hover` is its hover / emphasized state.
- `color-on-brand` is the foreground used on a filled brand surface.
- `color-interactive` is used for interactive foregrounds displayed directly on page or surface backgrounds, such as low-emphasis textual actions.
- `color-accent` provides restrained secondary emphasis.
- `color-focus-ring` is the standard focus indicator color.

`color-interactive` is distinct from `color-brand`, which may be optimized for filled surfaces.

`color-control-border` is intentionally stronger than the structural `color-border`.

---

## Ocean

Ocean is based on deep blue and petrol tones.

It should feel:

- deep;
- calm;
- refined;
- slightly warm rather than technical or cold.

Its dark mode is intentionally blue rather than neutral black.

### Ocean Light

| Role | Value | Intent |
| --- | --- | --- |
| `color-background` | `#F6F1E8` | warm ivory page background |
| `color-surface` | `#FFFDF8` | primary content surface |
| `color-surface-raised` | `#FFFFFF` | elevated surface |
| `color-text-primary` | `#102A3A` | deep blue text |
| `color-text-secondary` | `#55646D` | muted metadata |
| `color-border` | `#D8D5CD` | subtle structural border |
| `color-control-border` | `#7D8990` | interactive control boundary |
| `color-brand` | `#0A4660` | primary Ocean interaction color |
| `color-brand-hover` | `#08384D` | stronger interaction state |
| `color-on-brand` | `#FFFDF8` | foreground on brand surfaces |
| `color-interactive` | `#0A4660` | interactive foreground on light surfaces |
| `color-accent` | `#157687` | secondary petrol accent |
| `color-focus-ring` | `#0A4660` | focus indication |

### Ocean Dark

| Role | Value | Intent |
| --- | --- | --- |
| `color-background` | `#082A40` | deep blue page background |
| `color-surface` | `#0D344C` | primary content surface |
| `color-surface-raised` | `#123D56` | elevated surface |
| `color-text-primary` | `#F8F3E9` | warm light foreground |
| `color-text-secondary` | `#B9C6CE` | muted foreground |
| `color-border` | `#2B5065` | subtle blue structural border |
| `color-control-border` | `#7B99AA` | interactive control boundary |
| `color-brand` | `#4C9FBA` | primary interaction color |
| `color-brand-hover` | `#61B1CA` | stronger interaction state |
| `color-on-brand` | `#082A40` | foreground on brand surfaces |
| `color-interactive` | `#6CC5D9` | interactive foreground on dark surfaces |
| `color-accent` | `#64B3B8` | secondary petrol / teal accent |
| `color-focus-ring` | `#61B1CA` | focus indication |

### Supporting palette

Ocean may also use restrained supporting tones such as:

- warm sand;
- copper;
- muted coral.

These tones are not core interaction or semantic-state roles.

They may be introduced for secondary visual purposes when a concrete use case requires them.

---

## Natural

Natural is based on deep petrol green, sage, light sand, and restrained earth tones.

It should feel:

- warm;
- organic;
- mature;
- slightly autumnal;
- distinctive without becoming rustic.

Its dark mode remains visibly green / petrol rather than neutral black.

### Natural Light

| Role | Value | Intent |
| --- | --- | --- |
| `color-background` | `#F4EFE5` | light sand page background |
| `color-surface` | `#FBF8F0` | warm primary surface |
| `color-surface-raised` | `#FFFDF8` | elevated surface |
| `color-text-primary` | `#26332B` | deep green-neutral text |
| `color-text-secondary` | `#53605A` | muted metadata |
| `color-border` | `#D8D0C3` | warm structural border |
| `color-control-border` | `#7D877F` | interactive control boundary |
| `color-brand` | `#326B4B` | primary green interaction color |
| `color-brand-hover` | `#28583E` | stronger interaction state |
| `color-on-brand` | `#FFFDF8` | foreground on brand surfaces |
| `color-interactive` | `#326B4B` | interactive foreground on light surfaces |
| `color-accent` | `#8BA58F` | secondary sage accent |
| `color-focus-ring` | `#326B4B` | focus indication |

### Natural Dark

| Role | Value | Intent |
| --- | --- | --- |
| `color-background` | `#123C2D` | deep green / petrol page background |
| `color-surface` | `#194735` | primary content surface |
| `color-surface-raised` | `#20503D` | elevated surface |
| `color-text-primary` | `#F8F2E7` | warm light foreground |
| `color-text-secondary` | `#C2CEC5` | muted foreground |
| `color-border` | `#365E4B` | subtle green structural border |
| `color-control-border` | `#7C9C8A` | interactive control boundary |
| `color-brand` | `#69A27C` | primary interaction color |
| `color-brand-hover` | `#7BB28C` | stronger interaction state |
| `color-on-brand` | `#10261B` | foreground on brand surfaces |
| `color-interactive` | `#9BC6A4` | interactive foreground on dark surfaces |
| `color-accent` | `#A5BA99` | lighter sage accent |
| `color-focus-ring` | `#7BB28C` | focus indication |

### Supporting palette

Natural may also use:

- sage;
- light sand;
- terracotta;
- ochre;
- soft brown.

These tones are not replacements for semantic-state colors.

They may be introduced for secondary visual purposes when a concrete use case requires them.

---

## Brand and accent

Each theme has one dominant brand color.

`color-brand` carries the strongest theme-specific interactive emphasis.

`color-accent` remains visually subordinate and should be used selectively.

Brand, accent, and semantic-state colors should not compete for equal attention within the same visual area.

---

## Semantic state colors

Semantic state colors communicate application state independently from the selected theme.

The semantic model is:

- success;
- info;
- warning;
- danger.

Their meaning remains stable across Ocean and Natural.

Semantic states are represented by foreground / background pairs rather than by a single ambiguous color token.

### Light mode

| Role | Background | Foreground |
| --- | --- | --- |
| `success` | `#DDF2E4` | `#26714B` |
| `info` | `#DDECF7` | `#27689A` |
| `warning` | `#F8EAC9` | `#8C5400` |
| `danger` | `#F7DADA` | `#963737` |

Tokens:

```text
color-status-success-bg
color-status-success-fg

color-status-info-bg
color-status-info-fg

color-status-warning-bg
color-status-warning-fg

color-status-danger-bg
color-status-danger-fg
```

### Dark mode

| Role | Background | Foreground |
| --- | --- | --- |
| `success` | `#256446` | `#E7F8ED` |
| `info` | `#285F88` | `#E9F5FD` |
| `warning` | `#806019` | `#FFF2C2` |
| `danger` | `#853944` | `#FFE9E9` |

The dark-mode variants intentionally use stronger filled backgrounds than their light-mode equivalents.

The foreground/background pairs are the canonical semantic-state colors for components such as `StatusPill` and `Alert`.

---

## Destructive action colors

Destructive actions require a stronger treatment than a passive danger status.

### Light mode

```text
color-danger-action          #963737
color-danger-action-hover    #7F2E2E
color-on-danger-action       #FFFDF8
```

### Dark mode

```text
color-danger-action          #A54550
color-danger-action-hover    #B84D59
color-on-danger-action       #FFFDF8
```

These roles are intended for destructive interactive controls, not for general error messages or status display.

---

## Contrast

Normal-size text should target a minimum contrast ratio of 4.5:1 against its intended background.

The documented foreground/background pairs should be treated as combinations rather than interchangeable palette values.

In particular:

- `color-on-brand` must be used on filled brand surfaces;
- semantic status foregrounds must be used with their corresponding backgrounds;
- secondary text values must remain readable on both page and content surfaces;
- `color-interactive` must maintain at least 4.5:1 contrast against every surface on which it is intended to appear;
- `color-control-border` must maintain at least 3:1 contrast against adjacent colors when the border is required to identify an interactive control.

Contrast should be rechecked whenever one member of a documented pair changes.

---

## Spacing

Librago uses a 4 px spacing scale.

```text
space-1    4 px
space-2    8 px
space-3   12 px
space-4   16 px
space-5   20 px
space-6   24 px
space-8   32 px
space-10  40 px
space-12  48 px
```

Typical intent:

| Range | Typical use |
| --- | --- |
| 4–8 px | icon / label relationships |
| 8–12 px | tightly related content |
| 12–16 px | compact component padding |
| 16–20 px | standard card or panel padding |
| 24–32 px | section separation |
| 32–48 px | major page separation |

Prefer this scale over arbitrary spacing values.

---

## Radius

Librago uses moderate rounding.

```text
radius-xs     4 px
radius-sm     6 px
radius-md    10 px
radius-lg    14 px
radius-xl    18 px
radius-pill  999 px
```

Typical intent:

| Token | Typical use |
| --- | --- |
| `radius-xs` / `radius-sm` | compact UI elements |
| `radius-md` | standard controls |
| `radius-lg` | cards |
| `radius-xl` | dialogs and major panels |
| `radius-pill` | pills and compact badges |

---

## Borders

```text
border-width-default    1 px
border-width-emphasis   2 px
```

Borders should remain visually subordinate to content.

`color-border` is used for structural separation such as cards, sections, and table separators.

`color-control-border` is used where a visible boundary is needed to identify an interactive control.

---

## Shadows

```text
shadow-none   none
shadow-sm     0 1px 2px rgb(0 0 0 / 8%)
shadow-md     0 4px 12px rgb(0 0 0 / 14%)
shadow-lg     0 12px 32px rgb(0 0 0 / 22%)
```

Typical intent:

| Token | Typical use |
| --- | --- |
| `shadow-none` | standard surfaces |
| `shadow-sm` | subtly raised surface |
| `shadow-md` | dropdown, popover, floating menu |
| `shadow-lg` | dialog or modal |

The preferred hierarchy remains:

1. surface contrast;
2. border;
3. spacing;
4. shadow.

---

## Density and target size

Target visual density is **medium to compact**.

Baseline visible control sizes:

```text
control-height-sm    36 px
control-height-md    44 px
```

`control-height-md` is the default for normal interactive controls.

`control-height-sm` is intended for compact contexts where its effective interactive target remains appropriate.

On touch-oriented layouts, interactive targets should normally provide an effective target of approximately 44 px or larger even when the visible control is smaller.

Table rows typically use a minimum height of 44–48 px.

---

## Iconography

Current icon family:

```text
icon-family = Lucide
```

Current style:

- outline;
- medium stroke;
- slightly rounded geometry.

Sizes:

```text
icon-sm    16 px
icon-md    20 px
icon-lg    24 px
```

The icon family may change later while preserving these semantic size roles.

---

## Focus

```text
focus-ring-width     2 px
focus-ring-offset    2 px
```

The ring uses `color-focus-ring` from the active theme.

Focus indication must remain visible against both the page background and common component surfaces.

---

## Motion

Timing:

```text
motion-fast       120 ms
motion-normal     180 ms
motion-slow       240 ms
```

Easing:

```text
motion-easing-standard    cubic-bezier(0.2, 0, 0, 1)
```

Typical intent:

| Token | Typical use |
| --- | --- |
| `motion-fast` | hover and small state changes |
| `motion-normal` | menus and compact transitions |
| `motion-slow` | larger overlays and transitions |

Motion should remain short and functional.

Reduced-motion preferences should remove or substantially reduce non-essential movement rather than introducing a separate visual language.
