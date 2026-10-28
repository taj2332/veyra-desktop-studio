# Security

Veyra 0.2 is a local alpha. It has no hosted account system, background network client, auto-updater or telemetry. Discord, donation and browser shortcuts open only through explicit user actions.

Packs are declarative JSON, limited to 24 MB. Embedded PNG/JPEG images are limited to 16 MB, 8192 pixels per dimension and 40 megapixels. The parser validates schema version, IDs, colors, scene types and content metadata. Paths from shared packs are discarded. Unknown scene types are rejected; no pack-supplied code is executed.

Images are decoded by the Windows imaging stack. Keep Windows updated and treat untrusted media as untrusted. Bounds checks do not make this a full image-decoder sandbox or a security audit.

The application runs as the current user and does not require administrator elevation. Applying a pack changes wallpaper and selected appearance / shell settings. Windows accent synchronization is a separate checkbox. The backup and local renderer diagnostics remain on the PC. Explorer hosting and optional taskbar effects use undocumented Windows behavior and are not a security boundary.

Custom executable shortcuts are configured locally by the user. They are not accepted from shared theme packs. Single-instance signals use per-library local named events. The explicit `--integration-test` developer command temporarily changes appearance and attempts restoration; normal data / rendering tests do not change system settings.

A public security contact has not been configured yet. A maintainer should enable GitHub private vulnerability reporting before a public release. Avoid posting exploit files or personal library data publicly.
