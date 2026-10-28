# Veyra pack format, schema 1

UTF-8 .veyra.json, at most 24 MB. Descriptive data only; no scripts, executable paths, arbitrary XAML or web URLs.

Required: SchemaVersion=1, ASCII-safe Id (1–64 characters), Name/Author (1–80), License (1–160), supported Scene and Accent=#RRGGBB. Motion is 0–100. Existing schema-1 packs receive constructor defaults for newer fields.

| Fields | Values |
|---|---|
| Scene | alpine, neon, silk, aurora, dune, orbit, bloom, tide, summit, ember, cosmos, rain, forest, mono, sakura, lagoon, image |
| Animated / Clock / LightMode | Boolean |
| Mood | Studio, Focus, Night; default Studio |
| Seed | Integer 0–1000000; default 42 |
| Effect | None, Mist, Rain, Snow, Fireflies |
| ClockStyle | Editorial, Digital, Minimal |
| ClockSize | 60–160 |
| ClockX / ClockY | X 5–95, Y 5–80, clamped to visible viewport |
| HideIcons / AutoHideTaskbar | Boolean |
| TaskbarAccent / WindowsTransparency | Boolean; default true; used when Windows color sync is enabled |
| ImageBase64 / ImageExtension | Required for image scene; PNG/.png or JPEG/.jpg, max 16 MB image data and bounded decoded dimensions |
| Dock / DockStyle / TaskbarStyle | Legacy compatibility fields; extra dock removed, old experimental taskbar modes ignored |

The mood changes rendering and the synchronized Windows accent, while preserving the base accent in the recipe. Seeds change procedural composition and particles; they do not generate a new photograph. Built-in raster art embeds when exported.

Focus session deadlines, browser caches, machine performance settings, local backup paths and runtime metrics stay in local VeyraData and are not part of shared recipes. Creator credits and content licenses remain attached to each pack.
