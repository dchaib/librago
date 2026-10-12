# Librago Visual Foundations

## Purpose

This document records the current visual identity choices for Librago. It complements the durable guidance in [principles.md](principles.md).

The palettes and typefaces below are design references, not a prescribed CSS token schema, component API, or list of components to build. Names such as `color-brand` describe visual roles; implementation may map them onto the existing conventions of the chosen UI tools rather than exposing matching variables one for one.

Other details (control dimensions, spacing, radius, shadows, motion and component variants) should initially follow the UI toolkit's conventions. Adjust them only when an actual screen demonstrates a visual, usability or accessibility need. Do not build a separate component system to reproduce hypothetical design specifications.

The [brand asset guide](../../assets/brand/README.md) defines the logo artwork and its variants. Logo colors are independent from interface theme colors, and the outlined wordmark is independent from the interface typefaces.

## Typography

| Role | Current choice | Fallback stack |
| --- | --- | --- |
| Heading typeface | Playfair Display | Georgia, "Times New Roman", serif |
| Interface typeface | Inter | ui-sans-serif, system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif |

Use the interface typeface for ordinary UI, including controls and dense information. Use the heading typeface selectively to give page and major section headings character, without compromising scanability.

The two themes currently share the same typefaces. Specific weights, type sizes, line heights and responsive changes can follow the UI toolkit defaults until tested in actual layouts.

Meaningful text should wrap naturally. Truncate only when the space constraint is real and the full information remains accessible.

## Themes

Librago has two visual themes, each in light and dark mode:

- **Ocean:** deep blue and petrol; calm, refined, slightly warm. Its dark mode remains visibly blue rather than neutral black.
- **Natural:** petrol green, sage and warm sand; organic, mature, slightly autumnal. Its dark mode remains visibly green rather than neutral black.

The four combinations share the same layout, component behavior and semantic state meanings. Theme changes should not require separate page implementations.

The **reference palettes** below preserve the original visual direction of each theme. They help guide complementary color choices, but do not prescribe extra design tokens or override the current semantic colors for light and dark modes.

### Palette roles

The color roles describe how values are meant to be used, irrespective of CSS variable or utility names.

| Role | Meaning |
| --- | --- |
| `color-background` | Main page background |
| `color-surface` | Standard content surface |
| `color-surface-raised` | Elevated surface, when emphasis is warranted |
| `color-text-primary` | Main text |
| `color-text-secondary` | Supporting text and metadata |
| `color-border` | Subtle structural separation |
| `color-control-border` | Visible boundary for interactive fields where needed |
| `color-brand` | Dominant theme-specific action color |
| `color-brand-hover` | Stronger brand treatment on hover |
| `color-on-brand` | Text and icons placed on a filled brand surface |
| `color-interactive` | Interactive text and icons on ordinary surfaces |
| `color-accent` | Restrained secondary visual accent, not automatically a text color |
| `color-focus-ring` | Visible keyboard focus indicator |

`color-brand`, `color-interactive` and `color-accent` serve distinct visual purposes. In particular, `color-accent` should not be used for body text or meaningful icons unless sufficient contrast against the intended background has been verified.

### Ocean

#### Light mode

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

#### Dark mode

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

#### Reference palette

The original Ocean palette combines deep blues with warm neutrals and restrained accents. Its supporting tones prevent the theme from feeling monochromatic.

| Color | Reference | Contribution |
| --- | --- | --- |
| Night blue | `#0A2E45` | Core deep blue |
| Deep petrol | `#156E7A` | Core blue-green |
| Warm ivory | `#F4EBD7` | Warm light neutral |
| Sand | `#D6C9A8` | Supporting neutral |
| Copper | `#C97F4F` | Warm complementary accent |
| Muted coral | `#E16F5B` | Restrained contrasting accent |

These reference shades guide the artistic direction rather than defining mandatory component colors. Use complementary tones selectively, without competing with essential content or semantic status colors; check contrast in their actual context.

### Natural

#### Light mode

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

#### Dark mode

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

#### Reference palette

The original Natural palette combines petrol green, soft foliage tones and warm, slightly autumnal accents. Those complementary colors are important to its character, beyond the green functional surfaces.

| Color | Reference | Contribution |
| --- | --- | --- |
| Petrol green | `#2F6F5A` | Core green |
| Sage | `#A7B89A` | Soft supporting green |
| Light sand | `#F6F1E7` | Warm light neutral |
| Terracotta | `#C96F4F` | Warm earthy accent |
| Ochre | `#D9A658` | Golden complementary accent |
| Soft brown | `#8B6F56` | Muted earthy accent |

These reference shades guide the artistic direction rather than defining mandatory component colors. Use complementary tones selectively, without competing with essential content or semantic status colors; check contrast in their actual context.

## Semantic status colors

The following four meanings are shared across the themes: success, information, warning and danger. These colors are for **application state**, not decoration or the visual styling of destructive buttons.

### Light mode

| Role | Background | Foreground |
| --- | --- | --- |
| `success` | `#DDF2E4` | `#26714B` |
| `info` | `#DDECF7` | `#27689A` |
| `warning` | `#F8EAC9` | `#8C5400` |
| `danger` | `#F7DADA` | `#963737` |

### Dark mode

| Role | Background | Foreground |
| --- | --- | --- |
| `success` | `#256446` | `#E7F8ED` |
| `info` | `#285F88` | `#E9F5FD` |
| `warning` | `#806019` | `#FFF2C2` |
| `danger` | `#853944` | `#FFE9E9` |

The dark-mode variants intentionally use stronger filled backgrounds than their light-mode equivalents.

Use these as foreground/background pairs for compact status treatments. They are not a general-purpose palette for arbitrary text or controls.

A status must be understandable without color alone: use an explicit label and, where useful, an icon or another cue. The selected foreground and background values form a pair; do not assume that a status foreground also has adequate contrast directly on ordinary page or card surfaces.

The appearance of **destructive actions** and **inline validation errors** is intentionally not prescribed here. Use accessible styles from the selected UI toolkit, validate them against Librago's surfaces, and introduce a Librago-specific color only if a real use case cannot be covered appropriately.

## Application guidance

- **Contrast:** normal text should achieve at least 4.5:1 on its actual background. Important non-text indicators, such as a boundary needed to identify an input, should achieve at least 3:1 against adjacent colors. Check real screen states, not only the base palette.
- **Density and shape:** aim for a medium-to-compact interface, moderate rounding and restrained elevation. Use the existing component defaults first rather than prescribing a separate size, spacing, radius or shadow scale.
- **Touch and keyboard:** keep controls easy to activate on small screens, with visible focus and keyboard-accessible interactions. Do not rely on hover alone.
- **Icons:** Lucide is the current icon direction: outline, moderately rounded, with a restrained stroke. Start with the sizes provided by the UI toolkit.
- **Motion:** keep transitions short and functional, and respect reduced-motion preferences. No application-specific duration scale is required yet.

Component choice, variants, internal padding, responsive compositions and interaction implementation are decided as real screens are built, using existing UI primitives wherever practical. Product-specific states and behaviors remain defined by the relevant [feature specifications](../product/).
