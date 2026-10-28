using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
namespace Veyra {
 public sealed partial class Studio {
  // Bounded developer integration scenario; temporarily applies and restores a desktop.
  public static int RunFlowTest(string folder){
   var app=new Application {ShutdownMode=ShutdownMode.OnExplicitShutdown};var errors=new List<string>();app.DispatcherUnhandledException+=delegate(object sender,DispatcherUnhandledExceptionEventArgs e){errors.Add(e.Exception.ToString());e.Handled=true;};
   var store=new Store(folder);store.State.PauseFullscreen=false;store.State.PauseOnBattery=false;var ui=new Studio(store);app.MainWindow=ui.Window;int exit=1;var report=new List<string>();ui.Window.Show();
   app.Dispatcher.BeginInvoke(new Action(async delegate{
    try{
     ui.draft.Mood="Night";ui.BeginFocus(25);DateTime readyDeadline=DateTime.UtcNow.AddSeconds(25);while(DateTime.UtcNow<readyDeadline&&(ui.applying||!ui.desktop.IsLive||!ui.desktop.LiveMetrics.All(m=>m.Ready&&m.Mood=="Focus"&&m.FocusRemaining>0)))await Task.Delay(250);if(ui.applying||!ui.desktop.IsLive)throw new Exception("Focus scene failed to initialize.");
     if(store.State.FocusEndsUtc==null||ui.desktop.Active.Mood!="Focus"||!ui.desktop.LiveMetrics.All(m=>m.Mood=="Focus"&&m.FocusRemaining>0))throw new Exception("Focus timer did not reach the actual renderer.");report.Add("PASS: focus session reaches the GPU desktop and countdown");
     store.State.FocusEndsUtc=DateTime.UtcNow.AddSeconds(2).ToString("o");ui.Changed();await Task.Delay(4200);
     if(store.State.FocusEndsUtc!=null||ui.draft.Mood!="Night"||ui.desktop.Active.Mood!="Night")throw new Exception("Focus expiry did not restore the prior mood.");report.Add("PASS: focus expiry restores Night and clears private countdown");
     for(int cycle=0;cycle<2;cycle++){ui.ShowFlow();await Task.Delay(1200);ui.ShowLibrary();await Task.Delay(1200);ui.ShowDesktopStudio();await Task.Delay(1200);ui.ShowSettings();await Task.Delay(300);if(errors.Count>0)throw new Exception(errors[0]);}report.Add("PASS: repeated GPU view navigation without hosted-window errors");
     int starts=File.ReadAllLines(Path.Combine(folder,"renderer-lifecycle.txt")).Count(line=>line.Contains(" Start "));if(starts!=1)throw new Exception("Theme or navigation changes restarted the renderer "+starts+" times.");report.Add("PASS: theme changes and view navigation keep one desktop renderer");
     ui.desktop.Restore();if(ui.desktop.HasBackup)throw new Exception("Restore did not complete.");report.Add("PASS: original desktop restored after Flow integration scenario");exit=0;
    }catch(Exception ex){report.Add("FAIL: "+ex);if(ui.desktop.HasBackup)try{ui.desktop.Restore();report.Add("Recovery: original desktop restored.");}catch(Exception restore){report.Add("Recovery failed: "+restore.Message);}}
    finally{File.WriteAllLines(Path.Combine(folder,"flow-test-results.txt"),report);ui.Exit();app.Shutdown(exit);}
   }));app.Run();return exit;
  }
 }
}
