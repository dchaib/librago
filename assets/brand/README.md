# Librago brand assets

This directory contains the official Librago logo, its approved variants, and the small set of raster assets needed for common web and application use cases.

The **symbol** is the book-and-sun artwork; the **full logo** combines it with the Librago wordmark. The color symbol uses a fixed palette across application themes. Full logos adapt the wordmark color to the background, and monochrome variants provide single-color artwork. Choose an approved variant for the context rather than applying theme colors to the assets.

## Files and roles

| Location | Purpose |
| --- | --- |
| `source/librago-symbol-inkscape.svg` | Editable **symbol** master, with Inkscape-specific construction and corner effects. Preserve this file. |
| `svg/librago-symbol.svg` | Plain SVG of the approved book-and-sun symbol. |
| `svg/librago-symbol-monochrome-{dark,light}.svg` | Single-color symbol for contexts that require monochrome artwork. |
| `svg/librago-logo-horizontal*.svg` | **Primary** logo, with vector-outlined wordmark. |
| `svg/librago-logo-vertical*.svg` | Secondary, stacked logo, with vector-outlined wordmark. |
| `png/librago-symbol-{size}.png` | Transparent, square exports for environments requiring PNG. |
| `favicon.ico` | Multi-resolution favicon (16, 32 and 48 px). |

The `*-on-dark.svg` **full logo** variants change only the wordmark color.

All SVG and PNG files have **transparent backgrounds**. The application or document supplies its own background.

## Choose the right asset

| Context | Recommended asset |
| --- | --- |
| Website header / branding | `svg/librago-logo-horizontal.svg` |
| Website header on dark background | `svg/librago-logo-horizontal-on-dark.svg` |
| Compact branded layout | `svg/librago-logo-vertical.svg` or `*-on-dark.svg` |
| Favicon supporting SVG | `svg/librago-symbol.svg` |
| Classic browser favicon | `favicon.ico` |
| Web app manifest | `png/librago-symbol-192.png` and `png/librago-symbol-512.png` |
| Apple touch icon | `png/librago-symbol-180.png` (verify the target platform's background treatment) |
| UI illustration or documentation | Prefer the SVG; use the corresponding PNG when necessary |
| Monochrome artwork on light background | `*-monochrome-dark.svg` |
| Monochrome artwork on dark background | `*-monochrome-light.svg` |

The PNG exports are available at **16, 24, 32, 48, 128, 180, 192 and 512 px**. They are all derived from the same symbol and are **not separate artwork**. The source SVG remains resolution-independent.

**PWA note:** These transparent icons are suitable as ordinary app icons. Do not declare them as `maskable` in the web app manifest without first designing and testing a dedicated icon with sufficient safe-area padding and background.

## Brand specifications

| Role | Value |
| --- | --- |
| Book | Cobalt `#2E67DA` |
| Sun | Golden Yellow `#FFC629` |
| Wordmark on light background | Deep Blue `#082A40` |
| Wordmark on dark background | Warm Ivory `#F8F3E9` |
| Reference dark background | Deep Blue `#082A40` |
| Wordmark typeface | Fraunces, weight `700`, fixed optical size (`opsz`) `60` |

The full-logo wordmark is **converted to vector paths**, so rendering requires no installed font or external font service. The existing outlines are the visual reference; the Fraunces settings above are reconstruction guidance, as the font version and other variation settings can affect the contours.

The symbol geometry and proportions are approved. Preserve the artwork's proportions and the chosen variant's colors; do not stretch, reshape, add gradients or add shadows.

## Maintenance

1. **Symbol changes:** edit the Inkscape master in `source/`, export a plain SVG, then rebuild any affected variants and raster assets. Keep the symbol geometry consistent across all lockups.
2. **Wordmark changes:** use the Fraunces settings above as a starting point for rebuilding the horizontal and vertical wordmarks, compare the contours visually with the reference outlines, then replace the relevant SVG files.
3. **New sizes:** render from `svg/librago-symbol.svg`, not from an existing small PNG. Verify legibility at 16, 24 and 32 px.
4. **Verification:** check transparency, contrast on light and dark backgrounds, and the ICO's embedded resolutions before merging updates.

This is the canonical brand asset library. Copy only the necessary generated assets into the web application's public/static directory as required by its build and deployment conventions.
