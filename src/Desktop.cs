using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Media;
using System.Windows.Interop;
using System.Windows.Threading;
using Forms=System.Windows.Forms;

namespace Veyra {
 public sealed class DesktopHost {public IntPtr Parent,Icons,Wallpaper;public bool Modern;}
 public static class Native {
  public delegate bool EnumProc(IntPtr h,IntPtr p);
  [DllImport("user32.dll",CharSet=CharSet.Unicode,SetLastError=true)]public static extern bool SystemParametersInfo(uint a,uint p,string v,uint f);
  [DllImport("user32.dll",CharSet=CharSet.Unicode,EntryPoint="SystemParametersInfoW")]static extern bool GetWallpaper(uint a,uint p,StringBuilder v,uint f);
  [DllImport("user32.dll",CharSet=CharSet.Unicode)]public static extern IntPtr FindWindow(string c,string t);
  [DllImport("user32.dll",CharSet=CharSet.Unicode)]public static extern IntPtr FindWindowEx(IntPtr p,IntPtr c,string cl,string t);
  [DllImport("user32.dll")]public static extern bool EnumWindows(EnumProc callback,IntPtr p);
  [DllImport("user32.dll")]public static extern bool EnumChildWindows(IntPtr parent,EnumProc callback,IntPtr p);
  [DllImport("user32.dll")]public static extern bool IsWindowEnabled(IntPtr h);
  [DllImport("user32.dll")]static extern IntPtr WindowFromPoint(POINT point);
  [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern int GetWindowText(IntPtr h,StringBuilder text,int size);
  [DllImport("user32.dll",CharSet=CharSet.Unicode)]public static extern IntPtr SendMessageTimeout(IntPtr h,uint m,IntPtr w,IntPtr l,uint f,uint t,out IntPtr r);
  [DllImport("user32.dll",SetLastError=true)]public static extern IntPtr SetParent(IntPtr c,IntPtr p);
  [DllImport("user32.dll")]public static extern IntPtr GetParent(IntPtr h);
  [DllImport("user32.dll")]public static extern bool IsWindow(IntPtr h);
  [DllImport("user32.dll")]public static extern bool IsWindowVisible(IntPtr h);
  [DllImport("user32.dll")]public static extern bool ShowWindow(IntPtr h,int cmd);
  [DllImport("user32.dll")]public static extern int GetWindowLong(IntPtr h,int i);
  [DllImport("user32.dll")]public static extern int SetWindowLong(IntPtr h,int i,int v);
  [DllImport("user32.dll")]public static extern bool SetWindowPos(IntPtr h,IntPtr after,int x,int y,int w,int height,uint flags);
  [DllImport("user32.dll")]public static extern bool ScreenToClient(IntPtr h,ref POINT p);
  [DllImport("user32.dll")]public static extern int GetSystemMetrics(int i);
  [DllImport("user32.dll")]public static extern IntPtr GetForegroundWindow();
  [DllImport("user32.dll")]public static extern bool GetWindowRect(IntPtr h,out RECT r);
  [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern int GetClassName(IntPtr h,StringBuilder t,int len);
  [DllImport("kernel32.dll")]public static extern bool GetSystemPowerStatus(out POWER p);
  [DllImport("shell32.dll")]static extern UIntPtr SHAppBarMessage(uint message,ref APPBARDATA data);
  [DllImport("user32.dll")]static extern bool SetWindowCompositionAttribute(IntPtr h,ref COMPOSITION data);
  [DllImport("dwmapi.dll")]public static extern int DwmSetWindowAttribute(IntPtr h,int attribute,ref int value,int size);
  [StructLayout(LayoutKind.Sequential)]public struct POINT {public int X,Y;}
  [StructLayout(LayoutKind.Sequential)]public struct RECT {public int Left,Top,Right,Bottom;}
  [StructLayout(LayoutKind.Sequential)]public struct POWER {public byte ACLineStatus,BatteryFlag,BatteryLifePercent,SystemStatusFlag;public uint BatteryLifeTime,BatteryFullLifeTime;}
  [StructLayout(LayoutKind.Sequential)]struct APPBARDATA {public int Size;public IntPtr Hwnd;public uint Callback,Edge;public RECT Rect;public IntPtr Param;}
  [StructLayout(LayoutKind.Sequential)]struct COMPOSITION {public int Attribute;public IntPtr Data;public int Size;}
  [StructLayout(LayoutKind.Sequential)]struct ACCENT {public int State,Flags,Color,Animation;}
  public static string Class(IntPtr h){var s=new StringBuilder(256);GetClassName(h,s,256);return s.ToString();}
  public static string Wallpaper(){var s=new StringBuilder(32768);if(!GetWallpaper(0x73,(uint)s.Capacity,s,0))throw new IOException("Windows could not read the current wallpaper.");return s.ToString();}
  public static void SetWallpaper(string file){if(!SystemParametersInfo(0x14,0,file,3))throw new IOException("Windows could not apply this wallpaper.");}
  public static void Broadcast(string section){IntPtr text=Marshal.StringToHGlobalUni(section),result;try{SendMessageTimeout(new IntPtr(0xffff),0x1A,IntPtr.Zero,text,2,700,out result);}finally{Marshal.FreeHGlobal(text);}}
  public static void RefreshColors(){Broadcast("ImmersiveColorSet");Broadcast("WindowsThemeElement");}
  public static DesktopHost FindDesktop(bool raise){
   IntPtr prog=FindWindow("Progman",null),r;if(prog==IntPtr.Zero)throw new IOException("Explorer's desktop is not running.");
   if(raise){SendMessageTimeout(prog,0x052C,new IntPtr(0xD),IntPtr.Zero,2,1000,out r);SendMessageTimeout(prog,0x052C,new IntPtr(0xD),new IntPtr(1),2,1000,out r);SendMessageTimeout(prog,0x052C,IntPtr.Zero,IntPtr.Zero,2,1000,out r);}
   IntPtr icons=FindWindowEx(prog,IntPtr.Zero,"SHELLDLL_DefView",null),worker=FindWindowEx(prog,IntPtr.Zero,"WorkerW",null);
   bool modern=(GetWindowLong(prog,-20)&0x00200000)!=0;
   if(icons!=IntPtr.Zero && (modern || worker!=IntPtr.Zero))return new DesktopHost {Parent=prog,Icons=icons,Wallpaper=worker,Modern=true};
   DesktopHost found=null;
   EnumWindows(delegate(IntPtr h,IntPtr p){IntPtr def=FindWindowEx(h,IntPtr.Zero,"SHELLDLL_DefView",null);if(def==IntPtr.Zero)return true;IntPtr next=FindWindowEx(IntPtr.Zero,h,"WorkerW",null);if(next!=IntPtr.Zero){found=new DesktopHost {Parent=next,Icons=def,Modern=false};return false;}return true;},IntPtr.Zero);
   if(found!=null)return found;
   throw new IOException("No compatible Explorer wallpaper host was found. Open Diagnostics to inspect this Windows build.");
  }
  [DllImport("user32.dll",SetLastError=true)]public static extern bool SetLayeredWindowAttributes(IntPtr h,uint key,byte alpha,uint flags);
  public static IntPtr IconList(){IntPtr result=IntPtr.Zero;EnumWindows(delegate(IntPtr h,IntPtr p){IntPtr def=FindWindowEx(h,IntPtr.Zero,"SHELLDLL_DefView",null);if(def!=IntPtr.Zero){result=FindWindowEx(def,IntPtr.Zero,"SysListView32",null);return false;}return true;},IntPtr.Zero);return result;}
  public static uint TaskbarState(){var data=new APPBARDATA {Size=Marshal.SizeOf(typeof(APPBARDATA))};return (uint)SHAppBarMessage(4,ref data).ToUInt64();}
  public static void TaskbarState(uint state){var data=new APPBARDATA {Size=Marshal.SizeOf(typeof(APPBARDATA)),Hwnd=FindWindow("Shell_TrayWnd",null),Param=new IntPtr(state)};SHAppBarMessage(10,ref data);}
  public static bool Fullscreen(System.Drawing.Rectangle bounds){IntPtr h=GetForegroundWindow();string cls=Class(h);if(cls=="Progman" || cls=="WorkerW" || h==IntPtr.Zero)return false;RECT r;return GetWindowRect(h,out r)&&r.Left<=bounds.Left+2&&r.Top<=bounds.Top+2&&r.Right>=bounds.Right-2&&r.Bottom>=bounds.Bottom-2;}
  public static string Diagnostics(){var sb=new StringBuilder();sb.AppendLine("Veyra 0.3 desktop diagnostics · WebGL / WebView2");sb.AppendLine("OS: "+Environment.OSVersion);sb.AppendLine("Screens: "+Forms.Screen.AllScreens.Length);sb.AppendLine("Foreground class: "+Class(GetForegroundWindow()));try{var host=FindDesktop(false);sb.AppendLine("Host: "+Class(host.Parent)+" / "+(host.Modern?"layered Progman":"legacy WorkerW"));sb.AppendLine("Parent EXSTYLE: 0x"+GetWindowLong(host.Parent,-20).ToString("X8"));sb.AppendLine("Icon view: "+(host.Icons!=IntPtr.Zero));EnumChildWindows(host.Parent,delegate(IntPtr h,IntPtr p){if(true){RECT r;GetWindowRect(h,out r);sb.AppendLine("Desktop child "+Class(h)+": style="+GetWindowLong(h,-16).ToString("X8")+" ex="+GetWindowLong(h,-20).ToString("X8")+" visible="+IsWindowVisible(h)+" rect="+r.Left+","+r.Top+","+r.Right+","+r.Bottom+" foreground="+(GetForegroundWindow()==h));}return true;},IntPtr.Zero);}catch(Exception ex){sb.AppendLine("Host: "+ex.Message);}EnumWindows(delegate(IntPtr h,IntPtr p){if(Class(h).Contains("Veyra")){var title=new StringBuilder(128);GetWindowText(h,title,128);sb.AppendLine("Veyra top-level: "+title+" enabled="+IsWindowEnabled(h)+" visible="+IsWindowVisible(h)+" foreground="+(GetForegroundWindow()==h));if(title.ToString()=="Veyra — Desktop Studio"){RECT r;GetWindowRect(h,out r);var hit=WindowFromPoint(new POINT {X=r.Left+(r.Right-r.Left)*80/100,Y=r.Top+(r.Bottom-r.Top)*60/100});sb.AppendLine("Studio input reaches: "+Class(hit)+" / own window="+(hit==h));}}return true;},IntPtr.Zero);return sb.ToString();}
 }
 [ComImport,Guid("B92B56A9-8B55-4E14-9A89-0199BBB6F93B"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
 interface IDesktopWallpaper {
  void SetWallpaper([MarshalAs(UnmanagedType.LPWStr)]string monitor,[MarshalAs(UnmanagedType.LPWStr)]string wallpaper);
  [return:MarshalAs(UnmanagedType.LPWStr)]string GetWallpaper([MarshalAs(UnmanagedType.LPWStr)]string monitor);
  [return:MarshalAs(UnmanagedType.LPWStr)]string GetMonitorDevicePathAt(uint index);
  uint GetMonitorDevicePathCount();void GetMonitorRECT([MarshalAs(UnmanagedType.LPWStr)]string id,out Native.RECT rect);
  void SetBackgroundColor(uint color);uint GetBackgroundColor();void SetPosition(uint position);uint GetPosition();
 }
 public sealed class MonitorWallpaper {public string Monitor {get;set;}public string File {get;set;}}
 public sealed class RegValue {public string Key {get;set;}public string Name {get;set;}public bool Exists {get;set;}public string Kind {get;set;}public string Value {get;set;}}
 public sealed class DesktopSnapshot {
  public string Wallpaper {get;set;}public string Created {get;set;}public List<RegValue> Values {get;set;}
  public bool TransparencyCaptured {get;set;}public bool ColorsCaptured {get;set;}public bool AccentCaptured {get;set;}public bool ShellCaptured {get;set;}
  public uint TaskbarState {get;set;}public bool IconsVisible {get;set;}
  public List<MonitorWallpaper> Monitors {get;set;}public uint WallpaperPosition {get;set;}
 }
 public sealed class DesktopService {
  readonly Store store;
  readonly List<LiveWindow> live=new List<LiveWindow>();
  LiveWindow[] Snapshot(){lock(live)return live.ToArray();}
  Thread renderThread;
  Dispatcher renderDispatcher;
  readonly DispatcherTimer metricsTimer;
  ThemePack active;
  bool syncColors;
  volatile bool applyingNative,renderStarting,disposed;
  readonly object rendererGate=new object();
  string screenLayout=LayoutSignature();
  static string LayoutSignature(){return string.Join(";",Forms.Screen.AllScreens.Select(screen=>screen.DeviceName+":"+screen.Bounds.ToString()));}
  void Lifecycle(string message){try{File.AppendAllText(Path.Combine(store.Root,"renderer-lifecycle.txt"),DateTime.UtcNow.ToString("o")+" "+message+Environment.NewLine);}catch(IOException){}}
  public string LastReport {get;private set;}
  const string Personalize=@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",DesktopKey=@"Control Panel\Desktop",DwmKey=@"Software\Microsoft\Windows\DWM",AccentKey=@"Software\Microsoft\Windows\CurrentVersion\Explorer\Accent";
  public string BackupFile {get{return Path.Combine(store.Root,"desktop-backup.json");}}
  public bool HasBackup {get{return File.Exists(BackupFile);}}
  public bool IsLive {get{return live.Count>0;}}
  public int LiveCount {get{return live.Count;}}
  public long FrameCount {get{return Snapshot().Sum(w=>w.FrameCount);}}
  public ThemePack Active {get{return active;}}
  public DesktopService(Store s){store=s;SystemEvents.DisplaySettingsChanged+=DisplayChanged;metricsTimer=new DispatcherTimer {Interval=TimeSpan.FromSeconds(2)};metricsTimer.Tick+=delegate{if(active!=null){try{File.WriteAllText(Path.Combine(store.Root,"runtime.json"),PackCodec.Json().Serialize(new {Scene=active.Scene,Accent=active.Accent,Clock=active.Clock,Displays=live.Count,Frames=Snapshot().Select(w=>w.FrameCount).ToArray(),Paused=Snapshot().Select(w=>w.Paused).ToArray(),Gpu=Snapshot().Select(w=>w.Metrics).ToArray()}));}catch(IOException){}}};metricsTimer.Start();}
  void DisplayChanged(object sender,EventArgs e){if(disposed||active==null||Application.Current==null)return;Application.Current.Dispatcher.BeginInvoke(new Action(delegate{if(disposed||applyingNative||renderStarting||active==null)return;string layout=LayoutSignature();if(layout==screenLayout)return;Lifecycle("Display geometry changed");try{StartLive(active);}catch(Exception ex){LastReport=ex.Message;}}));}
  static RegValue Capture(string key,string name){using(var k=Registry.CurrentUser.OpenSubKey(key)){object v=k==null?null:k.GetValue(name,null,RegistryValueOptions.DoNotExpandEnvironmentNames);return new RegValue {Key=key,Name=name,Exists=v!=null,Kind=v==null?null:k.GetValueKind(name).ToString(),Value=v is byte[]?Convert.ToBase64String((byte[])v):v==null?null:Convert.ToString(v,System.Globalization.CultureInfo.InvariantCulture)};}}
  void SaveSnapshot(DesktopSnapshot s){string temp=BackupFile+".tmp";File.WriteAllText(temp,PackCodec.Json().Serialize(s));if(File.Exists(BackupFile))File.Replace(temp,BackupFile,null);else File.Move(temp,BackupFile);}
  static IDesktopWallpaper WallpaperApi(){return (IDesktopWallpaper)Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid("C2CF3110-460E-4FC1-B9D0-8A1C0C9CC4BD")));}
  public void CaptureSnapshot(bool colors){
   DesktopSnapshot s;if(HasBackup)s=PackCodec.Json().Deserialize<DesktopSnapshot>(File.ReadAllText(BackupFile));
   else {s=new DesktopSnapshot {Wallpaper=Native.Wallpaper(),Created=DateTime.UtcNow.ToString("o"),Values=new List<RegValue>{Capture(DesktopKey,"WallpaperStyle"),Capture(DesktopKey,"TileWallpaper")},Monitors=new List<MonitorWallpaper>()};
    IDesktopWallpaper api=null;try{api=WallpaperApi();s.WallpaperPosition=api.GetPosition();for(uint i=0;i<api.GetMonitorDevicePathCount();i++){string id=api.GetMonitorDevicePathAt(i);s.Monitors.Add(new MonitorWallpaper {Monitor=id,File=api.GetWallpaper(id)});}}catch(Exception ex){if(!(ex is COMException)&&!(ex is UnauthorizedAccessException))throw;s.Monitors.Clear();if(Forms.Screen.AllScreens.Length>1)throw new IOException("Windows denied the per-monitor backup. No desktop changes were made.",ex);}finally{if(api!=null)Marshal.ReleaseComObject(api);}
   }
   if(colors&&!s.ColorsCaptured){s.Values.Add(Capture(Personalize,"AppsUseLightTheme"));s.Values.Add(Capture(Personalize,"SystemUsesLightTheme"));s.ColorsCaptured=true;}
   if(colors&&!s.AccentCaptured){foreach(string name in new[]{"AccentColor","ColorizationColor"})s.Values.Add(Capture(DwmKey,name));foreach(string name in new[]{"AccentColorMenu","StartColorMenu","AccentPalette"})s.Values.Add(Capture(AccentKey,name));s.Values.Add(Capture(Personalize,"ColorPrevalence"));s.AccentCaptured=true;}
   if(colors&&!s.TransparencyCaptured){s.Values.Add(Capture(Personalize,"EnableTransparency"));s.TransparencyCaptured=true;}
   if(!s.ShellCaptured){s.TaskbarState=Native.TaskbarState();IntPtr icons=Native.IconList();s.IconsVisible=icons==IntPtr.Zero||Native.IsWindowVisible(icons);s.ShellCaptured=true;}
   SaveSnapshot(s);
  }
  public static uint AccentBgr(string hex){Color c=SceneView.ColorOf(hex);return 0xff000000|(uint)c.B<<16|(uint)c.G<<8|c.R;}
  static void SetAccent(ThemePack original){var p=original.Copy();p.Accent=Flow.MoodAccent(original);Color c=SceneView.ColorOf(p.Accent);using(var key=Registry.CurrentUser.CreateSubKey(DwmKey)){key.SetValue("AccentColor",unchecked((int)AccentBgr(p.Accent)),RegistryValueKind.DWord);key.SetValue("ColorizationColor",unchecked((int)(0xff000000|(uint)c.R<<16|(uint)c.G<<8|c.B)),RegistryValueKind.DWord);}
   using(var key=Registry.CurrentUser.CreateSubKey(AccentKey)){key.SetValue("AccentColorMenu",unchecked((int)AccentBgr(p.Accent)),RegistryValueKind.DWord);key.SetValue("StartColorMenu",unchecked((int)AccentBgr(p.Accent)),RegistryValueKind.DWord);byte[] palette=new byte[32];for(int i=0;i<8;i++){double f=.45+i*.075;palette[i*4]=(byte)(c.R*f);palette[i*4+1]=(byte)(c.G*f);palette[i*4+2]=(byte)(c.B*f);palette[i*4+3]=255;}key.SetValue("AccentPalette",palette,RegistryValueKind.Binary);}
   using(var key=Registry.CurrentUser.CreateSubKey(Personalize)){key.SetValue("ColorPrevalence",p.TaskbarAccent&&!p.LightMode?1:0,RegistryValueKind.DWord);key.SetValue("EnableTransparency",p.WindowsTransparency?1:0,RegistryValueKind.DWord);key.SetValue("AppsUseLightTheme",p.LightMode?1:0,RegistryValueKind.DWord);key.SetValue("SystemUsesLightTheme",p.LightMode?1:0,RegistryValueKind.DWord);}Native.RefreshColors();
  }
  public string Apply(ThemePack p,bool colors){applyingNative=true;try{return ApplyCore(p,colors);}finally{applyingNative=false;}}
  string ApplyCore(ThemePack p,bool colors){
   PackCodec.Validate(p);syncColors=colors;string poster=Path.Combine(store.Root,"wallpaper.png");var b=Forms.Screen.PrimaryScreen.Bounds;SceneView.SavePng(p,poster,b.Width,b.Height);CaptureSnapshot(colors);StopLive();
   using(var key=Registry.CurrentUser.CreateSubKey(DesktopKey)){key.SetValue("WallpaperStyle","10");key.SetValue("TileWallpaper","0");}Native.SetWallpaper(poster);if(colors)SetAccent(p);
   active=p.Copy();var backup=PackCodec.Json().Deserialize<DesktopSnapshot>(File.ReadAllText(BackupFile));
   Native.TaskbarState(p.AutoHideTaskbar?(backup.TaskbarState|1):(backup.TaskbarState&~1u));
   IntPtr list=Native.IconList();if(list!=IntPtr.Zero)Native.ShowWindow(list,p.HideIcons?0:5);

   string status;
   try{StartLive(p);status=live.Count>0?"Live desktop running on "+live.Count+" display"+(live.Count==1?"":"s")+". GPU scene ready. Flow moods and clock are active.":"Static wallpaper applied.";}catch(Exception ex){status="Static wallpaper applied; live host failed: "+ex.Message;}
   if(colors&&p.LightMode&&p.TaskbarAccent)status+=" Windows shows taskbar accent color in dark mode only.";
   store.State.AppliedId=p.Id;store.State.AppliedPack=p.Copy();store.State.AppliedColors=colors;store.State.ResumeLive=true;store.Save();LastReport=status;File.WriteAllText(Path.Combine(store.Root,"diagnostics.txt"),Native.Diagnostics()+Environment.NewLine+status);return status;
  }
  void StartLive(ThemePack p){lock(rendererGate){renderStarting=true;try{StartLiveCore(p);}finally{renderStarting=false;}}}
  void StartLiveCore(ThemePack p){
   Lifecycle("Start "+p.Scene);screenLayout=LayoutSignature();
   StopRenderer();
   if(p.Animated||p.Clock){
    var screens=store.State.AllMonitors?Forms.Screen.AllScreens:new[]{Forms.Screen.PrimaryScreen};var copy=p.Copy();var ready=new ManualResetEventSlim(false);Exception failure=null;bool cancelled=false;
    renderThread=new Thread(delegate(){
     renderDispatcher=Dispatcher.CurrentDispatcher;var owned=new List<LiveWindow>();
     renderDispatcher.BeginInvoke(new Action(async delegate{
      try{var host=Native.FindDesktop(true);foreach(var screen in screens){if(cancelled)break;var w=new LiveWindow(copy.Copy(),store.State,screen.Bounds,host.Modern,store.Root);owned.Add(w);lock(live)live.Add(w);if(!w.Attach(host))throw new IOException("Explorer refused the GPU wallpaper host.");w.Show();await w.Start();}ready.Set();}
      catch(Exception ex){failure=ex;LastReport="GPU renderer stopped: "+ex.Message;try{File.WriteAllText(Path.Combine(store.Root,"renderer-error.txt"),ex.ToString());}catch(IOException){}ready.Set();renderDispatcher.BeginInvokeShutdown(DispatcherPriority.Background);}
     }));
     try{Dispatcher.Run();}finally{foreach(var w in owned)try{w.Close();}catch{}lock(live)foreach(var w in owned)live.Remove(w);}
    });renderThread.IsBackground=true;renderThread.Name="Veyra wallpaper renderer";renderThread.SetApartmentState(ApartmentState.STA);renderThread.Start();
    if(!ready.Wait(TimeSpan.FromSeconds(25))){cancelled=true;StopRenderer();throw new IOException("The desktop renderer did not initialize in time.");}
    if(failure!=null){StopRenderer();throw new IOException(failure.Message,failure);}string errorFile=Path.Combine(store.Root,"renderer-error.txt");if(File.Exists(errorFile))File.Delete(errorFile);
   }
  }
  void StopRenderer(){lock(rendererGate){StopRendererCore();}}
  void StopRendererCore(){Lifecycle("Stop renderer");var dispatch=renderDispatcher;if(dispatch!=null&&!dispatch.HasShutdownStarted){var operation=dispatch.BeginInvoke(DispatcherPriority.Send,new Action(delegate{foreach(var w in Snapshot())w.Close();lock(live)live.Clear();dispatch.BeginInvokeShutdown(DispatcherPriority.Background);}));operation.Wait(TimeSpan.FromSeconds(3));}if(renderThread!=null)renderThread.Join(1500);renderThread=null;renderDispatcher=null;}
  public string Resume(ThemePack p){if(!HasBackup)return "No saved desktop session.";syncColors=store.State.AppliedColors;active=p.Copy();if(syncColors)SetAccent(p);StartLive(p);LastReport="Your GPU atmosphere is running again.";File.WriteAllText(Path.Combine(store.Root,"diagnostics.txt"),Native.Diagnostics()+Environment.NewLine+LastReport);return LastReport;}
  public GpuMetrics[] LiveMetrics {get{return Snapshot().Select(w=>w.Metrics).ToArray();}}
  public string Performance(){return string.Join(" | ",Snapshot().Select(w=>w.Metrics.Ready&&w.Metrics.Width>0?w.Metrics.Fps.ToString("0.0")+" FPS · "+w.Metrics.Width+" × "+w.Metrics.Height+" · "+(w.Paused?"paused":"WebGL"):"Starting GPU"));}
  public void RefreshLive(ThemePack p){if(active==null||active.Id!=p.Id)return;bool paletteChanged=active.Accent!=p.Accent||active.Mood!=p.Mood;if(paletteChanged&&syncColors)SetAccent(p);active=p.Copy();store.State.AppliedPack=p.Copy();var copy=p.Copy();if(renderDispatcher!=null&&!renderDispatcher.HasShutdownStarted)renderDispatcher.BeginInvoke(new Action(delegate{foreach(var w in Snapshot())w.Update(copy);}));}
  public void StopLive(){StopRenderer();active=null;}
  public void Dispose(){disposed=true;metricsTimer.Stop();SystemEvents.DisplaySettingsChanged-=DisplayChanged;StopLive();}
  static bool Allowed(RegValue v){return (v.Key==DesktopKey&&(v.Name=="WallpaperStyle"||v.Name=="TileWallpaper"))||(v.Key==Personalize&&new[]{"AppsUseLightTheme","SystemUsesLightTheme","ColorPrevalence","EnableTransparency"}.Contains(v.Name))||(v.Key==DwmKey&&new[]{"AccentColor","ColorizationColor"}.Contains(v.Name))||(v.Key==AccentKey&&new[]{"AccentColorMenu","StartColorMenu","AccentPalette"}.Contains(v.Name));}
  public void Restore(){
   if(!HasBackup)throw new InvalidOperationException("There is no saved desktop to restore.");var s=PackCodec.Json().Deserialize<DesktopSnapshot>(File.ReadAllText(BackupFile));if(s==null||s.Values==null||s.Values.Any(v=>!Allowed(v)))throw new InvalidDataException("Unexpected setting in desktop backup.");
   var paths=(s.Monitors!=null&&s.Monitors.Count>0?s.Monitors.Select(m=>m.File):new[]{s.Wallpaper});if(paths.Any(file=>!string.IsNullOrEmpty(file)&&!File.Exists(file)))throw new IOException("An original wallpaper file is missing. The backup is kept.");
   StopLive();foreach(var v in s.Values)using(var k=Registry.CurrentUser.CreateSubKey(v.Key)){if(!v.Exists)k.DeleteValue(v.Name,false);else if(v.Kind=="DWord")k.SetValue(v.Name,int.Parse(v.Value),RegistryValueKind.DWord);else if(v.Kind=="Binary")k.SetValue(v.Name,Convert.FromBase64String(v.Value),RegistryValueKind.Binary);else if(v.Kind=="String")k.SetValue(v.Name,v.Value,RegistryValueKind.String);else throw new InvalidDataException("Unsupported backup value.");}
   if(s.Monitors!=null&&s.Monitors.Count>0){var api=WallpaperApi();try{api.SetPosition(s.WallpaperPosition);foreach(var m in s.Monitors)api.SetWallpaper(m.Monitor,m.File??"");}finally{Marshal.ReleaseComObject(api);}}else Native.SetWallpaper(s.Wallpaper??"");
   if(s.ShellCaptured){Native.TaskbarState(s.TaskbarState);IntPtr icons=Native.IconList();if(icons!=IntPtr.Zero)Native.ShowWindow(icons,s.IconsVisible?5:0);}Native.RefreshColors();
   // Explorer can regenerate AccentPalette while handling the theme broadcast.
   // Reassert the saved binary palette after that notification has settled.
   if(s.AccentCaptured){Thread.Sleep(350);foreach(var v in s.Values.Where(v=>v.Key==AccentKey&&v.Name=="AccentPalette"))using(var key=Registry.CurrentUser.CreateSubKey(v.Key)){if(v.Exists)key.SetValue(v.Name,Convert.FromBase64String(v.Value),RegistryValueKind.Binary);else key.DeleteValue(v.Name,false);}}
   File.WriteAllText(Path.Combine(store.Root,"desktop-last-restored.json"),PackCodec.Json().Serialize(s));File.Delete(BackupFile);store.State.AppliedId=null;store.State.AppliedPack=null;store.State.ResumeLive=false;store.Save();
  }
 }
 // A normal redirected child hosts WebView2's GPU-backed composition surface.
 public sealed class LiveWindow:Forms.Form {
  readonly GpuCanvas canvas;readonly AppState settings;readonly System.Drawing.Rectangle bounds;readonly DispatcherTimer timer;
  IntPtr parent;bool recovering;
  public long FrameCount {get{return canvas.Metrics.Frames;}}
  public bool Paused {get{return canvas.Metrics.Paused;}}
  public GpuMetrics Metrics {get{return canvas.Metrics;}}
  protected override bool ShowWithoutActivation {get{return true;}}
  protected override Forms.CreateParams CreateParams {get{var p=base.CreateParams;p.ExStyle|=0x80|0x20|0x08000000;return p;}}
  public LiveWindow(ThemePack p,AppState s,System.Drawing.Rectangle rect,bool modern,string root){settings=s;bounds=rect;FormBorderStyle=Forms.FormBorderStyle.None;ShowInTaskbar=false;Text="Veyra GPU Desktop";AutoScaleMode=Forms.AutoScaleMode.None;StartPosition=Forms.FormStartPosition.Manual;Bounds=rect;BackColor=System.Drawing.Color.Black;canvas=new GpuCanvas(p,s,root,false);Controls.Add(canvas);timer=new DispatcherTimer {Interval=TimeSpan.FromMilliseconds(500)};timer.Tick+=Tick;FormClosed+=delegate{timer.Stop();};}
  public async System.Threading.Tasks.Task Start(){await canvas.InitializeAsync();timer.Start();}
  public void Update(ThemePack p){canvas.UpdateScene(p);}
  void Tick(object sender,EventArgs e){
   if(!Native.IsWindow(parent)){if(!recovering){recovering=true;try{if(Attach(Native.FindDesktop(true)))Show();}catch{Hide();}finally{recovering=false;}}return;}
   Native.POWER power;bool pause=settings.PauseOnBattery&&Native.GetSystemPowerStatus(out power)&&power.ACLineStatus==0;
   if(settings.PauseFullscreen&&Native.Fullscreen(bounds))pause=true;canvas.Pause(pause);
  }
  public bool Attach(DesktopHost host){IntPtr handle=Handle;TopLevel=false;handle=Handle;parent=host.Modern&&host.Wallpaper!=IntPtr.Zero?host.Wallpaper:host.Parent;int style=Native.GetWindowLong(handle,-16);Native.SetWindowLong(handle,-16,(style&~unchecked((int)0x80000000))|0x40000000);if(host.Modern&&parent==host.Parent){Native.SetWindowLong(handle,-20,Native.GetWindowLong(handle,-20)|0x80000);if(!Native.SetLayeredWindowAttributes(handle,0,255,2))return false;}Native.SetParent(handle,parent);if(Native.GetParent(handle)!=parent)return false;var origin=new Native.POINT {X=bounds.Left,Y=bounds.Top};Native.ScreenToClient(parent,ref origin);bool ok=Native.SetWindowPos(handle,parent==host.Parent&&host.Modern?host.Icons:IntPtr.Zero,origin.X,origin.Y,bounds.Width,bounds.Height,0x70);if(host.Modern&&host.Wallpaper!=IntPtr.Zero)Native.SetWindowPos(host.Wallpaper,new IntPtr(1),0,0,0,0,0x13);return ok;}
 }
}

