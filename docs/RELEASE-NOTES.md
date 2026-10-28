# 0.3.0 preview — Veyra Flow

Replaces the per-frame WPF bitmap/copy presenter with original WebGL scenes hosted by Microsoft WebView2. The wallpaper has its own STA host under Explorer's wallpaper WorkerW; the studio uses the native WPF WebView2 control, with separate local runtime profiles. No Lively code is bundled. Static image fallback remains available if the live engine cannot start.

Adds Studio / Focus / Night moods, a private 25-/50-minute desktop focus timer, previous-mood restoration, photo palette sampling, deterministic scene seeds and signature remixes. Mood and seed travel with the descriptive pack; deadlines do not. The applied recipe is retained and resumes on the next app launch unless explicitly stopped or restored.

Updates the studio with GPU previews, actual GPU-rendered curated thumbnails, Flow navigation, calmer typography, animated switches, debounced search/resize and asynchronous Apply. Native taskbar colors follow the applied palette/mood when synchronization is enabled. The existing Windows taskbar remains in use.

Stabilizes renderer ownership, monitor-geometry checks, preview disposal, separate browser profiles and a single UI instance across portable folders. Geometry-preserving Windows theme notifications no longer restart the desktop renderer.

The preview was exercised on Windows 26300 / GTX 1650 / one 1080p screen. About 60 FPS was measured at native 1080p; virtual 4K/ultrawide/portrait layouts were checked. These observations do not establish compatibility or sustained FPS on all machines. See VALIDATION.md and performance-results.json.
