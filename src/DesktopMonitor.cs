using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
namespace Veyra {
 // This diagnostic is a DWM thumbnail of Explorer's actual desktop surface.
 // It deliberately does not render SceneView or a studio preview.
 public sealed class DesktopMonitor:Window {
  IntPtr thumbnail;
  [StructLayout(LayoutKind.Sequential)]struct Properties {public uint Flags;public Native.RECT Destination,Source;public byte Opacity;[MarshalAs(UnmanagedType.Bool)]public bool Visible;[MarshalAs(UnmanagedType.Bool)]public bool ClientOnly;}
  [DllImport("dwmapi.dll")]static extern int DwmRegisterThumbnail(IntPtr destination,IntPtr source,out IntPtr id);
  [DllImport("dwmapi.dll")]static extern int DwmUpdateThumbnailProperties(IntPtr id,ref Properties properties);
  [DllImport("dwmapi.dll")]static extern int DwmUnregisterThumbnail(IntPtr id);
  public DesktopMonitor(){Title="Veyra · Windows desktop monitor";Width=960;Height=580;Background=Brushes.Black;WindowStartupLocation=WindowStartupLocation.CenterScreen;
   SourceInitialized+=delegate{int result=DwmRegisterThumbnail(new WindowInteropHelper(this).Handle,Native.FindWindow("Progman",null),out thumbnail);if(result!=0){Content=new System.Windows.Controls.TextBlock {Text="Windows could not expose its desktop thumbnail (0x"+result.ToString("X8")+").",Foreground=Brushes.White,Margin=new Thickness(20)};}else ResizeThumbnail();};
   SizeChanged+=delegate{ResizeThumbnail();};Closed+=delegate{if(thumbnail!=IntPtr.Zero)DwmUnregisterThumbnail(thumbnail);};
  }
  void ResizeThumbnail(){if(thumbnail==IntPtr.Zero)return;Native.RECT rect;Native.GetWindowRect(new WindowInteropHelper(this).Handle,out rect);var properties=new Properties {Flags=1|4|8|16,Destination=new Native.RECT {Left=8,Top=38,Right=Math.Max(9,rect.Right-rect.Left-8),Bottom=Math.Max(39,rect.Bottom-rect.Top-8)},Opacity=255,Visible=true,ClientOnly=true};DwmUpdateThumbnailProperties(thumbnail,ref properties);}
 }
}
