# Contributing to Veyra

Start with one focused change or a reproducible bug report. Include the Windows build, display scale, monitor setup and steps to reproduce. Remove personal wallpaper paths from logs and screenshots before sharing them.

Build with `./build.ps1 -Test`. Use `VEYRA_DATA_DIR` for development. Native desktop changes must remain user-initiated, reversible and clearly described. Do not apply a theme during startup, import or preview.

## Pull requests

- Describe the user problem and the resulting behavior.
- Run the existing checks and add coverage for parsing, persistence or restoration changes.
- For UI changes, inspect the running application at the minimum supported window size.
- For Explorer integration, record exactly which Windows builds were tested. Keep the static fallback.
- Never accept executable code, shell commands, registry scripts, arbitrary file paths, remote assets or custom XAML from a shared pack.
- Use original or appropriately licensed code and assets. Do not copy proprietary application code or Steam Workshop content without permission.

## Creator submissions

For this alpha, share a `.veyra.json` file with a screenshot and a clear content license. Do not include private information. A hosted submission / moderation process is not implemented yet.

## Governance

The code is MIT licensed. The Veyra community invite is https://discord.gg/wuZSKphzmP. Creator-credit, reporting and moderation rules should accompany shared packs. Donations must be voluntary and transparent; no donation account is configured by this repository.
