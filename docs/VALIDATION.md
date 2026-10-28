# Validation — 0.3.0 preview

## Non-destructive checks

The release build runs data validation, pack roundtrips, bounded image loading, legacy fallback renders, migration, recovery and production XAML loading. New checks cover Flow moods/seeds, dominant photo hue, deadline arithmetic, private timer exclusion, applied-recipe persistence and expired-session recovery. Distributed templates are checked when running from the final source layout. Exact count: automated-test-results.txt.

## GPU rendering

23 GPU checks exercise all 16 scenes, temporal pixel changes, static freeze, pause, Night luminance and virtual viewports of 3840×2160, 3440×1440 and 720×1280. Clock bounds are tested at edge positions. Static/pause comparisons tolerate 3 channel levels and at most 0.1% significant sampled-pixel changes to allow GPU capture quantization; animated captures must change more than 1% of sampled pixels. These are actual WebView2 capture results, not WPF layout-preview images.

The 1080p test measured about 60 FPS and a 16.8 ms p95 animation-loop interval on an NVIDIA GTX 1650. The legacy comparison measures WPF rasterization plus pixel copy, without PNG encoding. The FPS values are render-loop measurements rather than DWM present-time/scanout measurements. Raw results and definitions are in performance-results.json and gpu-test-results.txt.

## Native desktop and Flow

The native transaction test applies and restores wallpaper, colors, transparency, taskbar auto-hide and icon visibility; it checks all 11 captured appearance values after restoration. It also updates a live scene. The Flow integration scenario starts a real focus session, verifies that its mode/deadline reach the GPU renderer, expires it, checks return to the prior mood, navigates repeatedly among GPU views and ensures theme changes do not restart the renderer. Both scenarios retain a snapshot until restoration succeeds.

The clock and live scene were also visually inspected through a DWM thumbnail of Explorer's actual desktop. That monitor has no SceneView or studio re-render. Earlier counters-only evidence and failed prototypes are not used as proof of correct composition.

## Limits

Only one physical 1920×1080 display and Windows build 26300 were available. Virtual viewport tests are not physical mixed-DPI, HDR or multi-monitor tests. Other GPUs/builds, sleep/resume, hot-plug and prolonged memory/performance behavior need broader coverage. No sustained 4K FPS or universal compatibility claim is made. The WebView2 Runtime is required for live scenes.

Developer commands --integration-test and --flow-test temporarily change Windows appearance; use an isolated data folder and an interactive desktop. --self-test and --gpu-test do not change Windows appearance. Run native transactions sequentially, never concurrently.
