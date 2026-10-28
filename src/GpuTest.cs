using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Forms=System.Windows.Forms;
namespace Veyra {
 public static class GpuTest {
  public static int Run(string root){
   Directory.CreateDirectory(root);var app=new Application {ShutdownMode=ShutdownMode.OnExplicitShutdown};int exit=1;
   var state=new AppState {TargetFps=60,RenderQuality="Ultra",PauseFullscreen=false,PauseOnBattery=false};var pack=Store.Defaults().First(p=>p.Scene=="neon");
   double legacyFps=LegacyBenchmark(pack);var form=new Forms.Form {Text="Veyra · GPU validation",FormBorderStyle=Forms.FormBorderStyle.None,ClientSize=new System.Drawing.Size(1920,1080),StartPosition=Forms.FormStartPosition.CenterScreen};var canvas=new GpuCanvas(pack,state,root,false);form.Controls.Add(canvas);form.Show();
   Dispatcher.CurrentDispatcher.BeginInvoke(new Action(async delegate{
    var reports=new List<string>();try{
     await canvas.InitializeAsync();reports.Add("GPU: "+canvas.Metrics.Renderer);await Task.Delay(3500);
     reports.Add(await canvas.CoreWebView2.ExecuteScriptAsync("JSON.stringify({frames,ready,failed,width:innerWidth,height:innerHeight,visibility:document.visibilityState,error:document.getElementById('error').textContent})"));
     await canvas.CaptureAsync(Path.Combine(root,"neon-a.png"));await Task.Delay(1200);await canvas.CaptureAsync(Path.Combine(root,"neon-b.png"));
     reports.Add(PackCodec.Json().Serialize(canvas.Metrics));File.WriteAllText(Path.Combine(root,"performance.json"),PackCodec.Json().Serialize(new {Resolution="1920x1080",LegacyRasterFps=legacyFps,WebGlFps=canvas.Metrics.Fps,WebGlP95Ms=canvas.Metrics.P95,Renderer=canvas.Metrics.Renderer,Definition="Legacy WPF raster + pixel copy versus WebGL animation-loop rate. GPU rate is not a DWM present-time measurement."}));
     if(canvas.Metrics.Frames<20)throw new Exception("GPU frame metrics did not advance.");
     if(Difference(Path.Combine(root,"neon-a.png"),Path.Combine(root,"neon-b.png"))<.01)throw new Exception("Animated GPU frames do not change enough pixels.");reports.Add("PASS: temporal GPU capture changes pixels");
     pack.Animated=false;pack.Clock=false;canvas.UpdateScene(pack);await Task.Delay(1100);await canvas.CaptureAsync(Path.Combine(root,"static-a.png"));await Task.Delay(1100);await canvas.CaptureAsync(Path.Combine(root,"static-b.png"));
     if(Difference(Path.Combine(root,"static-a.png"),Path.Combine(root,"static-b.png"))>.001)throw new Exception("Static scene changed pixels beyond capture quantization.");reports.Add("PASS: static GPU scene freezes pixels within capture tolerance");
     pack.Animated=true;pack.Clock=true;pack.Mood="Studio";canvas.UpdateScene(pack);await Task.Delay(600);await canvas.CaptureAsync(Path.Combine(root,"mood-studio.png"));pack.Mood="Night";canvas.UpdateScene(pack);await Task.Delay(600);await canvas.CaptureAsync(Path.Combine(root,"mood-night.png"));
     if(Brightness(Path.Combine(root,"mood-night.png"))>=Brightness(Path.Combine(root,"mood-studio.png"))*.85)throw new Exception("Night mood did not lower luminance.");reports.Add("PASS: Night mood lowers scene luminance");
     pack.Clock=false;canvas.UpdateScene(pack);canvas.Pause(true);await Task.Delay(1100);await canvas.CaptureAsync(Path.Combine(root,"paused-a.png"));await Task.Delay(600);await canvas.CaptureAsync(Path.Combine(root,"paused-b.png"));if(Difference(Path.Combine(root,"paused-a.png"),Path.Combine(root,"paused-b.png"))>.001)throw new Exception("Paused scene changed pixels.");reports.Add("PASS: pause freezes GPU motion");canvas.Pause(false);
     form.ClientSize=new System.Drawing.Size(480,300);
     foreach(var p in Store.Defaults()){p.Clock=false;canvas.UpdateScene(p);await Task.Delay(600);await canvas.CaptureAsync(Path.Combine(root,p.Scene+".png"));if(canvas.Metrics.Error!=null)throw new Exception(canvas.Metrics.Error);if(Brightness(Path.Combine(root,p.Scene+".png"))<.002)throw new Exception("Black scene: "+p.Scene);reports.Add("PASS: WebGL scene "+p.Scene);}
     canvas.Dock=Forms.DockStyle.None;
     foreach(var size in new[]{new[]{3840,2160},new[]{3440,1440},new[]{720,1280}}){canvas.Size=new System.Drawing.Size(size[0],size[1]);var responsive=Store.Defaults()[0];responsive.ClockX=95;responsive.ClockY=80;canvas.UpdateScene(responsive);await Task.Delay(800);string raw=await canvas.CoreWebView2.ExecuteScriptAsync("JSON.stringify((()=>{const r=document.getElementById('clock').getBoundingClientRect();return {width:innerWidth,height:innerHeight,left:r.left,right:r.right,top:r.top,bottom:r.bottom}})())");var json=PackCodec.Json().Deserialize<string>(raw);var layout=PackCodec.Json().Deserialize<Dictionary<string,double>>(json);if(layout["left"]<0||layout["right"]>layout["width"]+1||layout["top"]<0||layout["bottom"]>layout["height"]+1)throw new Exception("Clock clipped at "+size[0]+"x"+size[1]);await canvas.CaptureAsync(Path.Combine(root,"layout-"+size[0]+"x"+size[1]+".png"));reports.Add("PASS: virtual GPU viewport and edge clock "+size[0]+"x"+size[1]);}
     exit=0;
    }catch(Exception ex){reports.Add("FAIL: "+ex);}finally{File.WriteAllLines(Path.Combine(root,"gpu-test-results.txt"),reports);form.Close();canvas.Dispose();app.Shutdown(exit);}
   }));app.Run();return exit;
  }
  static double LegacyBenchmark(ThemePack p){var scene=new SceneView(p){ShowClock=true};scene.Measure(new Size(1920,1080));scene.Arrange(new Rect(0,0,1920,1080));var target=new System.Windows.Media.Imaging.RenderTargetBitmap(1920,1080,96,96,System.Windows.Media.PixelFormats.Pbgra32);var pixels=new byte[1920*1080*4];var watch=System.Diagnostics.Stopwatch.StartNew();for(int i=0;i<12;i++){scene.Phase=i*.05;scene.InvalidateVisual();scene.UpdateLayout();target.Clear();target.Render(scene);target.CopyPixels(pixels,1920*4,0);}watch.Stop();return Math.Round(12/watch.Elapsed.TotalSeconds,2);}
  static double Brightness(string file){using(var image=new System.Drawing.Bitmap(file)){double sum=0,count=0;for(int y=0;y<image.Height;y+=13)for(int x=0;x<image.Width;x+=13){var c=image.GetPixel(x,y);sum+=(c.R*.2126+c.G*.7152+c.B*.0722)/255;count++;}return sum/count;}}
  static double Difference(string left,string right){using(var a=new System.Drawing.Bitmap(left))using(var b=new System.Drawing.Bitmap(right)){if(a.Size!=b.Size)return 1;double different=0,total=0;for(int y=0;y<a.Height;y+=7)for(int x=0;x<a.Width;x+=7){var c=a.GetPixel(x,y);var d=b.GetPixel(x,y);if(Math.Abs(c.R-d.R)>3||Math.Abs(c.G-d.G)>3||Math.Abs(c.B-d.B)>3)different++;total++;}return different/total;}}
 }
}
