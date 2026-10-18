# Librago Visual Foundations

## Purpose

This document records the current visual identity choices for Librago. It complements the durable guidance in [principles.md](principles.md).

The reference palettes establish Librago's visual direction; functional theme values are implementation choices that may evolve as real screens are designed.

Other details (control dimensions, spacing, radius, shadows, motion and component variants) should initially follow the UI toolkit's conventions. Adjust them only when an actual screen demonstrates a visual, usability or accessibility need. Do not build a separate component system to reproduce hypothetical design specifications.

The [brand asset guide](../../assets/brand/README.md) defines the logo artwork and its variants. Logo colors are independent from interface theme colors, and the outlined wordmark is independent from the interface typefaces.

## Typography

| Role | Current choice | Fallback stack |
| --- | --- | --- |
| Heading/editorial typeface | Source Serif 4 | Georgia, "Times New Roman", serif |
| Interface typeface | Inter | ui-sans-serif, system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif |

Use the interface typeface for ordinary UI, including controls and dense information. Use the heading/editorial typeface selectively for page and major section headings and item titles, without compromising scanability. Both variable fonts are self-hosted.

The two themes currently share the same typefaces. Specific weights, type sizes, line heights and responsive changes can follow the UI toolkit defaults until tested in actual layouts.

Meaningful text should wrap naturally. Truncate only when the space constraint is real and the full information remains accessible.

## Themes

Librago has two visual themes, each available in light and dark modes:

- **Ocean:** night blue and deep teal, balanced by warm ivory and sand, with restrained copper and coral accents. Calm, refined and slightly warm. Its dark mode remains visibly blue rather than neutral black.
- **Natural:** pine green and sage, softened by light ivory and complemented by warm, earthy tones of terracotta, ochre and brown. Organic, mature and slightly autumnal. Its dark mode remains visibly green rather than neutral black.

### Approved reference palettes

These palettes establish the visual direction. They are not complete functional themes, mandatory CSS tokens, or a requirement to use every color on every screen.

#### Ocean

| Color | Reference |
| --- | --- |
| Night blue | `#0A2E45` |
| Deep teal | `#156E7A` |
| Warm ivory | `#F4EBD7` |
| Sand | `#D6C9A8` |
| Copper | `#C97F4F` |
| Coral | `#E16F5B` |

#### Natural

| Color | Reference |
| --- | --- |
| Pine green | `#2F6F5A` |
| Sage | `#A7B89A` |
| Light ivory | `#F6F1E7` |
| Terracotta | `#C96F4F` |
| Ochre | `#D9A658` |
| Warm brown | `#8B6F56` |

### Current functional themes

The four combinations below describe the current implementation in [`web/src/index.css`](../../web/src/index.css), validated on the Loans page. The themes share the same application structure and semantic state meanings. In the surface column, values are the card and popover colors, respectively.

| Theme | Page background / text | Content surfaces (card / popover) | Primary / primary foreground | Secondary surface | Muted text |
| --- | --- | --- | --- | --- | --- |
| Ocean Light | `#F9FAFA` / `#082B44` | `#FFFEFC` / `#FFFFFF` | `#064464` / `#FFFFFF` | `#E0EFF7` | `#395A6D` |
| Ocean Dark | `#00192B` / `#F4F8FF` | `#05253C` / `#0A3049` | `#A8DAFD` / `#03273F` | `#103F60` | `#BCD6E9` |
| Natural Light | `#FAF7F0` / `#172E23` | `#FFFDF9` / `#FFFDF8` | `#385740` / `#FFFFFF` | `#F2E9D4` | `#4B5C50` |
| Natural Dark | `#071E16` / `#FAF8EE` | `#112D23` / `#1A382C` | `#345F4D` / `#FFFFFF` | `#50451D` | `#C6D8CC` |

The frontend follows shadcn's semantic styling conventions. Theme colors are adjusted for readable, accessible interactions, with semantic theme variables preferred over generated-component changes. Targeted component changes remain appropriate when a real requirement justifies them.

For filled high-emphasis actions, `primary` is the background and `primary-foreground` is its contrasting foreground. Treat them as a pair. Contrast against button text does not guarantee that `primary` itself contrasts sufficiently with every page or card background, so do not assume `text-primary` is suitable for standalone text links.

Prefer underlined `foreground` text for links unless a specific treatment has been verified on its actual backgrounds. No separate interactive-text token is defined; reconsider this convention if future application requirements justify one. Existing shadcn button variants remain the shared defaults.

## Semantic status colors

Success, information, warning and danger are shared meanings across themes. The pairs below are the current preview values and remain provisional; they are for **application state**, not decoration or the visual styling of destructive buttons.

### Light mode

| Role | Background | Foreground |
| --- | --- | --- |
| `success` | `#DDF2E4` | `#26714B` |
| `info` | `#DDECF7` | `#27689A` |
| `warning` | `#FFF1D2` | `#885000` |
| `danger` | `#FFE5DF` | `#AE2424` |

### Dark mode

| Role | Background | Foreground |
| --- | --- | --- |
| `success` | `#256446` | `#E7F8ED` |
| `info` | `#285F88` | `#E9F5FD` |
| `warning` | `#554213` | `#FFE5A4` |
| `danger` | `#501D2B` | `#FFE6E8` |

The dark-mode variants use darker filled backgrounds with light foregrounds.

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
