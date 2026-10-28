# Dependencies

Microsoft.Web.WebView2 SDK **1.0.4129.50**, unmodified assemblies and x64 loader, is included under vendor/webview2/.

Source package: https://api.nuget.org/v3-flatcontainer/microsoft.web.webview2/1.0.4129.50/microsoft.web.webview2.1.0.4129.50.nupkg
Package SHA-256: D3934F482D484B89FB4825DF720C710664E1143A1E90F7B3A60794EF33F473D2

Required files next to Veyra.exe: Microsoft.Web.WebView2.Core.dll, Microsoft.Web.WebView2.WinForms.dll, Microsoft.Web.WebView2.Wpf.dll, WebView2Loader.dll, Veyra.exe.config.

The Microsoft SDK's redistribution license and notice are preserved in vendor/webview2 and in the portable distribution's licenses/webview2 folder. The Evergreen Runtime is a separate installed Microsoft component; it is not bundled or installed silently. Runtime information: https://learn.microsoft.com/en-us/microsoft-edge/webview2/concepts/distribution

Other framework dependencies are .NET Framework 4.8 WPF, Windows Forms, System.Drawing and System.Web.Extensions, supplied by Windows/.NET Framework. No third-party font or icon pack is redistributed.

The wallpaper renderer serves only embedded app resources and validated image bytes at a private virtual origin. Navigation, downloads, new windows, host objects and browser permission requests are restricted. Creator packs cannot supply JavaScript, HTML, fetch URLs or executable paths.
