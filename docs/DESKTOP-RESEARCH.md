# Renderer update in 0.3.0

The earlier GDI investigation below is historical. Version 0.3.0 uses a native WinForms child hosting WebView2/WebGL on the desktop and a native WPF WebView2 preview. The desktop clock is rendered in HTML. No Lively or TranslucentTB implementation is bundled. See VALIDATION.md and DEPENDENCIES.md for current evidence and SDK licensing.

# Desktop attachment investigation

REA skill consulted on 2026-10-27: https://github.com/morluto/rea/blob/main/.agents/skills/reverse-engineer-anything/SKILL.md
The skill routes analysis of a complete source repository to normal source tools. REA MCP tools were not connected. No REA binary execution or decompilation was performed or needed for this source investigation.

Primary source: https://github.com/lively-community/lively/blob/core-separation/src/Lively/Lively/Core/WinDesktopCore.cs
Inspected Initialize, TryAttachToDesktop and EnsureWorkerWZOrder. Modern Progman has WS_EX_NOREDIRECTIONBITMAP, a layered DefView and a static WorkerW. The application surface must compose between DefView and WorkerW. SetLayeredWindowAttributes(global alpha 255) supplies redirected opaque content; WPF per-pixel transparency did not display correctly on the target machine. The final implementation independently uses Win32 declarations and a WinForms/GDI presenter. No Lively source is bundled or copied into the MIT project.

Microsoft layered-window semantics: https://learn.microsoft.com/en-us/windows/win32/winmsg/window-features
DWM desktop thumbnail API and Progman example: https://learn.microsoft.com/en-us/windows/win32/api/dwmapi/nf-dwmapi-dwmregisterthumbnail

Taskbar source reviewed: https://github.com/TranslucentTB/TranslucentTB/blob/release/TranslucentTB/taskbar/taskbarattributeworker.cpp
Modern XAML taskbar surfaces use a separate Explorer appearance service / XAML diagnostics route. SetWindowCompositionAttribute success is insufficient evidence of visible modern taskbar blur. Veyra therefore removes these unreliable choices and provides actual Windows accent/mode/transparency/auto-hide settings. No Explorer injection or TranslucentTB code is included.

Microsoft taskbar/color behavior: https://support.microsoft.com/en-us/windows/experience/personalization/customize-the-taskbar-in-windows
https://support.microsoft.com/en-us/windows/experience/personalization/personalize-your-colors-in-windows

Observed: GDI global-alpha child plus corrected managed top-level state displays the desktop clock in Progman's DWM thumbnail, with native icons above it. Inference: the old WPF per-pixel surface and managed child state were incompatible with this shell path. Unverified: other builds, GPUs, HDR and mixed-DPI monitors.
