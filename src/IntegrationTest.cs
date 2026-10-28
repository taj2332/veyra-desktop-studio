using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Win32;
namespace Veyra {
 public static class IntegrationTest {
  // Explicit developer-only test: temporarily applies a desktop, then restores it.
  public static int Run(string folder){
   var app=new Application {ShutdownMode=ShutdownMode.OnExplicitShutdown};var store=new Store(folder);store.State.PauseFullscreen=false;store.State.PauseOnBattery=false;var desktop=new DesktopService(store);var report=new List<string>();bool restored=false;DesktopSnapshot snapshot=null;int exit=1;long frames=0;
   var timer=new DispatcherTimer {Interval=TimeSpan.FromSeconds(3)};int step=0;DateTime stageStarted=DateTime.UtcNow;
   Action finish=delegate{desktop.Dispose();File.WriteAllLines(Path.Combine(folder,"native-test-results.txt"),report);app.Shutdown(exit);};
   timer.Tick+=delegate{
    try{
     if(step==0){var p=Store.Defaults()[0];p.Dock=false;p.HideIcons=true;p.AutoHideTaskbar=true;p.Accent="#A9AAFF";p.TaskbarAccent=false;p.WindowsTransparency=false;desktop.Apply(p,true);snapshot=PackCodec.Json().Deserialize<DesktopSnapshot>(File.ReadAllText(desktop.BackupFile));report.Add("PASS: native wallpaper application and WebGL host initialization");if((Native.TaskbarState()&1)==0)throw new Exception("Taskbar auto-hide was not applied.");IntPtr icons=Native.IconList();if(icons!=IntPtr.Zero&&Native.IsWindowVisible(icons))throw new Exception("Desktop icons did not hide.");report.Add("PASS: taskbar auto-hide and desktop icon visibility applied");using(var key=Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize")){if((int)key.GetValue("ColorPrevalence")!=0||(int)key.GetValue("EnableTransparency")!=0)throw new Exception("Native taskbar appearance options did not apply.");}report.Add("PASS: native taskbar accent and transparency options applied");step=1;stageStarted=DateTime.UtcNow;}
     else if(step==1){if((DateTime.UtcNow-stageStarted).TotalSeconds<4)return;frames=desktop.FrameCount;if(frames<3)throw new Exception("Live frames did not advance.");report.Add("PASS: live frame count advanced to "+frames);var p=desktop.Active.Copy();p.Accent="#7EDBD5";p.ClockStyle="Digital";desktop.RefreshLive(p);step=2;}
     else if(step==2){if(desktop.FrameCount<=frames)throw new Exception("Live update stopped the renderer.");report.Add("PASS: live palette and clock style update while rendering");desktop.Restore();restored=true;step=3;}
     else{var mismatches=new List<string>();foreach(var v in snapshot.Values){using(var key=Registry.CurrentUser.OpenSubKey(v.Key)){object value=key==null?null:key.GetValue(v.Name,null,RegistryValueOptions.DoNotExpandEnvironmentNames);string actual=value is byte[]?Convert.ToBase64String((byte[])value):value==null?null:Convert.ToString(value,System.Globalization.CultureInfo.InvariantCulture);if(v.Exists?actual!=v.Value:value!=null)mismatches.Add(v.Name);}}if(mismatches.Count>0)throw new Exception("Appearance values differ after restore: "+string.Join(", ",mismatches));report.Add("PASS: all "+snapshot.Values.Count+" saved appearance values restored after settling");if(Native.TaskbarState()!=snapshot.TaskbarState)throw new Exception("Taskbar state was not restored.");IntPtr icons=Native.IconList();if(icons!=IntPtr.Zero&&Native.IsWindowVisible(icons)!=snapshot.IconsVisible)throw new Exception("Desktop icon visibility was not restored.");report.Add("PASS: original taskbar and icon visibility restored");report.Add("PASS: original wallpaper restoration completed");exit=0;timer.Stop();finish();}
    }catch(Exception ex){timer.Stop();report.Add("FAIL: "+ex.Message);if(!restored&&desktop.HasBackup)try{desktop.Restore();report.Add("Recovery: original desktop restored.");}catch(Exception recovery){report.Add("Recovery failed: "+recovery.Message);}finish();}
   };timer.Start();app.Run();return exit;
  }
 }
}

