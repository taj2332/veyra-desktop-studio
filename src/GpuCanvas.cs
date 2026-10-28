using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;
using Forms=System.Windows.Forms;

namespace Veyra {
 public sealed class GpuMetrics {
  public string Mood;public int FocusRemaining;public long Frames;public double Fps,P95;public int Width,Height;public string Renderer="Starting",Error;public bool Ready,Paused;
 }
 public sealed class GpuSession {
  CoreWebView2 CoreWebView2;
  const string Origin="https://veyra.local/";
  readonly AppState settings;readonly bool preview;
  ThemePack pack;byte[] art;string artIdentity;int artVersion;
  bool configured,disposed;readonly TaskCompletionSource<bool> ready=new TaskCompletionSource<bool>();
  public readonly GpuMetrics Metrics=new GpuMetrics();
  public GpuSession(ThemePack p,AppState state,bool isPreview){pack=p.Copy();settings=state;preview=isPreview;}
  public static Task<CoreWebView2Environment> EnvironmentAsync(string root,bool preview){var options=new CoreWebView2EnvironmentOptions("--disable-background-timer-throttling --disable-renderer-backgrounding --disable-backgrounding-occluded-windows --disable-features=CalculateNativeWinOcclusion");return CoreWebView2Environment.CreateAsync(null,Path.Combine(root,preview?"StudioRuntime":"DesktopRuntime"),options);}
  public static byte[] Resource(string name){using(var input=Assembly.GetExecutingAssembly().GetManifestResourceStream("Veyra.Renderer."+name)){if(input==null)throw new FileNotFoundException("Renderer asset missing: "+name);using(var output=new MemoryStream()){input.CopyTo(output);return output.ToArray();}}}
  public async Task AttachAsync(CoreWebView2 core,CoreWebView2Environment environment){
   if(disposed)return;CoreWebView2=core;
   CoreWebView2.Settings.AreDefaultContextMenusEnabled=false;CoreWebView2.Settings.AreDevToolsEnabled=false;CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled=false;CoreWebView2.Settings.IsStatusBarEnabled=false;CoreWebView2.Settings.IsZoomControlEnabled=false;CoreWebView2.Settings.AreHostObjectsAllowed=false;CoreWebView2.Settings.IsPasswordAutosaveEnabled=false;CoreWebView2.Settings.IsGeneralAutofillEnabled=false;
   CoreWebView2.PermissionRequested+=delegate(object sender,CoreWebView2PermissionRequestedEventArgs e){e.State=CoreWebView2PermissionState.Deny;};
   CoreWebView2.NewWindowRequested+=delegate(object sender,CoreWebView2NewWindowRequestedEventArgs e){e.Handled=true;};
   CoreWebView2.DownloadStarting+=delegate(object sender,CoreWebView2DownloadStartingEventArgs e){e.Cancel=true;};
   CoreWebView2.NavigationStarting+=delegate(object sender,CoreWebView2NavigationStartingEventArgs e){if(e.Uri!=Origin)e.Cancel=true;};
   CoreWebView2.AddWebResourceRequestedFilter("*",CoreWebView2WebResourceContext.All);
   CoreWebView2.WebResourceRequested+=delegate(object sender,CoreWebView2WebResourceRequestedEventArgs e){
    byte[] bytes=null;string type="text/plain";Uri uri;
    if(Uri.TryCreate(e.Request.Uri,UriKind.Absolute,out uri)&&uri.Scheme=="https"&&uri.Host=="veyra.local"&&uri.Port==443){
     switch(uri.AbsolutePath){case "/":bytes=Resource("stage.html");type="text/html; charset=utf-8";break;case "/stage.js":bytes=Resource("stage.js");type="application/javascript; charset=utf-8";break;case "/scene.frag":bytes=Resource("scene.frag");break;case "/art":bytes=art;type=pack.ImageExtension==".jpg"?"image/jpeg":"image/png";break;}
    }
    e.Response=environment.CreateWebResourceResponse(new MemoryStream(bytes??new byte[0],false),bytes==null?404:200,bytes==null?"Not found":"OK","Content-Type: "+type+"\r\nCache-Control: no-store\r\nX-Content-Type-Options: nosniff");
   };
   CoreWebView2.WebMessageReceived+=Message;
   CoreWebView2.ProcessFailed+=delegate(object sender,CoreWebView2ProcessFailedEventArgs e){Metrics.Error="Browser renderer stopped: "+e.ProcessFailedKind;};
   configured=true;UpdateScene(pack);CoreWebView2.Navigate(Origin);
   var timeout=Task.Delay(15000);if(await Task.WhenAny(ready.Task,timeout)!=ready.Task)throw new IOException("The GPU renderer did not start. Check the WebView2 Runtime in Preferences.");await ready.Task;
  }
  void Message(object sender,CoreWebView2WebMessageReceivedEventArgs e){
   if(disposed||e.Source!=Origin||e.WebMessageAsJson.Length>16000)return;
   try{var message=PackCodec.Json().Deserialize<Dictionary<string,object>>(e.WebMessageAsJson);string type=Convert.ToString(message["type"]);
    if(type=="boot"){SendScene();return;}
    if(type=="error"){Metrics.Error=Convert.ToString(message["message"]);ready.TrySetException(new IOException(Metrics.Error));return;}
    if(type=="ready"){Metrics.Renderer=Convert.ToString(message["renderer"]);Metrics.Ready=true;ready.TrySetResult(true);return;}
    if(type=="metrics"){Metrics.Mood=Convert.ToString(message["mood"]);if(message.ContainsKey("focusRemaining"))Metrics.FocusRemaining=Convert.ToInt32(message["focusRemaining"]);Metrics.Frames=Convert.ToInt64(message["frames"]);Metrics.Fps=Convert.ToDouble(message["fps"]);Metrics.P95=Convert.ToDouble(message["p95"]);Metrics.Width=Convert.ToInt32(message["width"]);Metrics.Height=Convert.ToInt32(message["height"]);Metrics.Paused=Convert.ToBoolean(message["paused"]);}
   }catch(Exception ex){if(ex is OutOfMemoryException)throw;Metrics.Error="Invalid renderer status: "+ex.Message;}
  }
  public void UpdateScene(ThemePack p){
   pack=p.Copy();string identity=p.Scene=="image"?p.ImageBase64:p.Scene;
   if(!object.ReferenceEquals(identity,artIdentity)&&identity!=artIdentity){artIdentity=identity;artVersion++;art=p.Scene=="alpine"||p.Scene=="neon"?Assets.Bytes(p.Scene):p.Scene=="image"?Convert.FromBase64String(p.ImageBase64):null;}
   if(configured&&Metrics.Ready)SendScene();
  }
  void SendScene(){if(disposed||!configured)return;var render=new {pack.Scene,pack.Accent,pack.Animated,pack.Motion,pack.Effect,pack.Clock,pack.ClockStyle,pack.ClockSize,pack.ClockX,pack.ClockY,pack.Mood,pack.Seed,Art=art==null?null:Origin+"art?v="+artVersion,TargetFps=preview?Math.Min(30,settings.TargetFps):settings.TargetFps,Quality=preview?"Balanced":settings.RenderQuality,Preview=preview,FocusEndsUtc=settings.FocusEndsUtc,Paused=Metrics.Paused};CoreWebView2.PostWebMessageAsJson(PackCodec.Json().Serialize(new {type="scene",scene=render}));}
  public void Pause(bool value){if(disposed||!configured||Metrics.Paused==value)return;Metrics.Paused=value;CoreWebView2.PostWebMessageAsJson(PackCodec.Json().Serialize(new {type="pause",paused=value}));}
  public async Task CaptureAsync(string file){using(var output=File.Create(file))await CoreWebView2.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png,output);}
  public void Dispose(){disposed=true;if(!ready.Task.IsCompleted)ready.TrySetCanceled();}
 }
 public sealed class GpuCanvas:Microsoft.Web.WebView2.WinForms.WebView2 {
  readonly GpuSession session;readonly string root;readonly bool preview;
  public GpuMetrics Metrics {get{return session.Metrics;}}
  public GpuCanvas(ThemePack p,AppState state,string folder,bool isPreview){root=folder;preview=isPreview;session=new GpuSession(p,state,isPreview);Dock=Forms.DockStyle.Fill;DefaultBackgroundColor=System.Drawing.Color.FromArgb(8,14,25);TabStop=false;}
  public async Task InitializeAsync(){var env=await GpuSession.EnvironmentAsync(root,preview);if(IsDisposed)return;await EnsureCoreWebView2Async(env);if(IsDisposed)return;await session.AttachAsync(CoreWebView2,env);}
  public void UpdateScene(ThemePack p){session.UpdateScene(p);}
  public void Pause(bool value){session.Pause(value);}
  public Task CaptureAsync(string file){return session.CaptureAsync(file);}
  protected override void Dispose(bool disposing){if(disposing)session.Dispose();base.Dispose(disposing);}
 }
 public sealed class GpuPreview:Microsoft.Web.WebView2.Wpf.WebView2 {
  readonly GpuSession session;readonly string root;bool started,finished;System.Windows.Window owner;
  public GpuPreview(ThemePack p,AppState settings,string folder){root=folder;session=new GpuSession(p,settings,true);DefaultBackgroundColor=System.Drawing.Color.Transparent;Loaded+=async delegate{if(started)return;started=true;owner=System.Windows.Window.GetWindow(this);if(owner!=null){owner.Activated+=VisibilityChanged;owner.Deactivated+=VisibilityChanged;}try{var env=await GpuSession.EnvironmentAsync(root,true);if(finished)return;await EnsureCoreWebView2Async(env);if(finished)return;await session.AttachAsync(CoreWebView2,env);VisibilityChanged(null,EventArgs.Empty);}catch(Exception ex){if(!finished){session.Metrics.Error=ex.Message;ToolTip="GPU preview: "+ex.Message;}}};IsVisibleChanged+=delegate{VisibilityChanged(null,EventArgs.Empty);};Unloaded+=delegate{Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,new Action(delegate{if(IsLoaded||finished)return;finished=true;if(owner!=null){owner.Activated-=VisibilityChanged;owner.Deactivated-=VisibilityChanged;}session.Dispose();Dispose();}));};}
  void VisibilityChanged(object sender,EventArgs e){if(!finished)session.Pause(!IsVisible||(owner!=null&&!owner.IsActive));}
  public void UpdateScene(ThemePack p){if(!finished)session.UpdateScene(p);}
 }
}
