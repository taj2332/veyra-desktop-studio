using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;
using System.Collections.Generic;

namespace Veyra {
 public static class Program {
  static System.Threading.Mutex instance;
  static System.Threading.EventWaitHandle showSignal,quitSignal;
  [STAThread] public static int Main(string[] args) {
   try {
    if(args.Length>0 && args[0]=="--self-test") return SelfTest.Run(args.Length>1?args[1]:Path.Combine(Path.GetTempPath(),"veyra-tests"));
    if(args.Length==2&&args[0]=="--export-brand"){Brand.Export(args[1]);return 0;}
    if(args.Length==2&&args[0]=="--diagnostics"){File.WriteAllText(args[1],Native.Diagnostics());return 0;}
    if(args.Length==2&&args[0]=="--restore-saved"){var recovery=new DesktopService(new Store(args[1]));try{recovery.Restore();File.WriteAllText(Path.Combine(args[1],"restore-result.txt"),"Saved desktop restored.");return 0;}finally{recovery.Dispose();}}
    if(args.Length==2&&args[0]=="--flow-test")return Studio.RunFlowTest(args[1]);
    if(args.Length==2&&args[0]=="--gpu-test")return GpuTest.Run(args[1]);
    if(args.Length==2&&args[0]=="--integration-test")return IntegrationTest.Run(args[1]);
    if(args.Length==2&&args[0]=="--export-starters"){Directory.CreateDirectory(args[1]);foreach(var pack in Store.Defaults())File.WriteAllText(Path.Combine(args[1],pack.Scene+".veyra.json"),PackCodec.Encode(pack));return 0;}
    if(args.Length==1&&args[0]=="--desktop-monitor"){var monitorApp=new Application();monitorApp.Run(new DesktopMonitor());return 0;}
    var app=new Application { ShutdownMode=ShutdownMode.OnMainWindowClose };
    app.DispatcherUnhandledException+=delegate(object s,DispatcherUnhandledExceptionEventArgs e) {MessageBox.Show(e.Exception.Message,"Veyra",MessageBoxButton.OK,MessageBoxImage.Error);e.Handled=true;};
    string root=Environment.GetEnvironmentVariable("VEYRA_DATA_DIR");
    if(string.IsNullOrEmpty(root))root=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"VeyraData");
    string instanceKey=args.Length>0&&args[0]=="--render-ui"?"Capture-"+Guid.NewGuid().ToString("N"):"DesktopStudio-v1";
    bool first;instance=new System.Threading.Mutex(true,@"Local\Veyra-"+instanceKey,out first);
    if(!first){try{using(var signal=System.Threading.EventWaitHandle.OpenExisting(@"Local\Veyra-"+(args.Contains("--quit")?"Quit-":"Show-")+instanceKey))signal.Set();}catch(System.Threading.WaitHandleCannotBeOpenedException){MessageBox.Show("Veyra is already running. Open it from the system tray.","Veyra");}instance.Dispose();return 0;}
    if(args.Contains("--quit")){instance.ReleaseMutex();instance.Dispose();return 0;}
    showSignal=new System.Threading.EventWaitHandle(false,System.Threading.EventResetMode.AutoReset,@"Local\Veyra-Show-"+instanceKey);
    quitSignal=new System.Threading.EventWaitHandle(false,System.Threading.EventResetMode.AutoReset,@"Local\Veyra-Quit-"+instanceKey);
    var store=new Store(root);
    var ui=new Studio(store,args.Length>0&&args[0]=="--render-ui");app.MainWindow=ui.Window;
    ui.CaptureMode=args.Length>0&&args[0]=="--render-ui";
    var signals=new DispatcherTimer {Interval=TimeSpan.FromMilliseconds(200)};signals.Tick+=delegate{if(showSignal.WaitOne(0))ui.ShowStudio();if(quitSignal.WaitOne(0))ui.Exit();};signals.Start();
    if(args.Length>=2 && args[0]=="--render-ui") {
     ui.Window.Loaded+=delegate {if(args.Length>2)ui.OpenCapturePage(args[2]);var captureTimer=new DispatcherTimer {Interval=TimeSpan.FromMilliseconds(800)};captureTimer.Tick+=delegate {captureTimer.Stop();
      var bitmap=new RenderTargetBitmap((int)ui.Window.ActualWidth,(int)ui.Window.ActualHeight,96,96,PixelFormats.Pbgra32);bitmap.Render(ui.Window);
      var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));using(var output=File.Create(args[1]))encoder.Save(output);ui.Exit();
     };captureTimer.Start();};
    }
    ui.Window.Show();
    int exit=app.Run();signals.Stop();showSignal.Dispose();quitSignal.Dispose();instance.ReleaseMutex();instance.Dispose();return exit;
   } catch(Exception ex){MessageBox.Show(ex.ToString(),"Veyra could not start",MessageBoxButton.OK,MessageBoxImage.Error);return 1;}
  }
 }
 public sealed partial class Studio {
  public Window Window {get;private set;}
  readonly Store store;
  readonly DesktopService desktop;
  readonly Grid host;
  ThemePack selected;
  ThemePack draft;
  string current="Explore packs",filter="All packs",query="";
  WrapPanel cards;
  Grid inspector;
  SceneView preview;
  DispatcherTimer previewTimer;
  bool favoritesOnly;
  bool applyColors=true;
  TextBlock resultCount;
  static SolidColorBrush Brush(string hex) {return new SolidColorBrush(SceneView.ColorOf(hex));}
  public Studio(Store s,bool capture=false) {
   CaptureMode=capture;
   store=s;desktop=new DesktopService(s);
   using(var stream=typeof(Program).Assembly.GetManifestResourceStream("Veyra.MainWindow.xaml"))Window=(Window)XamlReader.Load(stream);
   host=Find<Grid>("PageHost");selected=s.State.Packs.FirstOrDefault(p=>p.Id==s.State.AppliedId)??s.State.Packs[0];draft=s.State.AppliedPack==null?selected.Copy():s.State.AppliedPack.Copy();
   Find<Button>("Minimize").Click+=delegate{Window.WindowState=WindowState.Minimized;};
   Find<Button>("Maximize").Click+=delegate{Window.WindowState=Window.WindowState==WindowState.Maximized?WindowState.Normal:WindowState.Maximized;};
   Find<Button>("CloseApp").Click+=delegate{Window.Close();};
   Find<FrameworkElement>("TitleBar").MouseLeftButtonDown+=delegate(object sender,MouseButtonEventArgs e){if(e.ClickCount==2)Window.WindowState=Window.WindowState==WindowState.Maximized?WindowState.Normal:WindowState.Maximized;else Window.DragMove();};
   Find<Button>("NavLibrary").Click+=delegate{favoritesOnly=false;ShowLibrary();};
   Find<Button>("NavFavorites").Click+=delegate{favoritesOnly=true;ShowLibrary();};
   Find<Button>("NavCreator").Click+=delegate{ShowCreator();};
   Find<Button>("NavFlow").Click+=delegate{ShowFlow();};
   Find<Button>("NavDesktop").Click+=delegate{ShowDesktopStudio();};
   Find<Button>("NavCommunity").Click+=delegate{ShowCommunity();};
   Find<Button>("NavSettings").Click+=delegate{ShowSettings();};
   Window.Closed+=delegate{desktop.Dispose();if(previewTimer!=null)previewTimer.Stop();DisposeTray();};
   Window.AllowDrop=true;
   Window.DragOver+=delegate(object sender,DragEventArgs e){e.Effects=e.Data.GetDataPresent(DataFormats.FileDrop)?DragDropEffects.Copy:DragDropEffects.None;e.Handled=true;};
   Window.Drop+=delegate(object sender,DragEventArgs e){var files=e.Data.GetData(DataFormats.FileDrop) as string[];if(files!=null && files.Length==1)ImportFile(files[0]);else Tell("Drop one .veyra.json pack at a time.");};
   Window.PreviewKeyDown+=delegate(object sender,KeyEventArgs e){if(e.Key==Key.Q&&Keyboard.Modifiers==ModifierKeys.Control){e.Handled=true;Exit();}};
   Find<System.Windows.Controls.Image>("BrandLogo").Source=Brand.Mark("#B9ED80");
   Window.Width=Math.Min(Window.Width,SystemParameters.WorkArea.Width-24);Window.Height=Math.Min(Window.Height,SystemParameters.WorkArea.Height-24);
   SetupTray();StartFlowTimer();ShowLibrary();Window.Loaded+=delegate{if(!CaptureMode&&!desktop.IsLive&&store.State.ResumeLive&&desktop.HasBackup&&!string.IsNullOrEmpty(store.State.AppliedId))ResumeAtmosphere();};if(s.Warning!=null)Tell(s.Warning);
  }
  T Find<T>(string name) where T:class {return Window.FindName(name) as T;}
  void Tell(string message) {Find<TextBlock>("Status").Text=message;Find<TextBlock>("Status").ToolTip=message;}
  static TextBlock Text(string content,double size,string color) {return new TextBlock {Text=content,FontSize=size,Foreground=Brush(color),TextWrapping=TextWrapping.Wrap};}
  static Border Panel(UIElement child,string bg,int padding) {return new Border {Background=Brush(bg),CornerRadius=new CornerRadius(12),Padding=new Thickness(padding),Child=child};}
  static Button Button(string label,Action action,bool primary) {
   var b=new Button {Content=label};if(primary){b.Background=Brush("#B9ED80");b.Foreground=Brush("#142019");b.BorderBrush=Brush("#B9ED80");b.FontWeight=FontWeights.SemiBold;}
   if(primary){b.Tag="primary";b.SetResourceReference(Control.BackgroundProperty,"AccentBrush");b.SetResourceReference(Control.BorderBrushProperty,"AccentBrush");}
   b.Click+=delegate{action();};return b;
  }
  void BeginPage(string name) {
   current=name;host.Children.Clear();host.RowDefinitions.Clear();host.ColumnDefinitions.Clear();
   if(previewTimer!=null){previewTimer.Stop();previewTimer=null;}preview=null;
   AnimatePage();
   string[] names={"NavLibrary","NavFavorites","NavCreator","NavCommunity","NavSettings","NavDesktop","NavFlow"};
   int index=name=="Flow studio"?6:name=="Explore packs"?0:name=="Favorites"?1:name=="Creator studio"?2:name=="Community"?3:name=="Desktop studio"?5:4;
   for(int i=0;i<names.Length;i++){var b=Find<Button>(names[i]);b.Background=Brush(i==index?"#283125":"#141519");b.Foreground=Brush(i==index?"#C1EDA0":"#A7ADB9");}
  }
  StackPanel Header(string eyebrow,string title,string subtitle) {
   var s=new StackPanel();var eyebrowText=Text(eyebrow,9,"#B9ED80");eyebrowText.SetResourceReference(TextBlock.ForegroundProperty,"AccentBrush");s.Children.Add(eyebrowText);
   var h=Text(title,32,"#F4F5F6");h.FontWeight=FontWeights.SemiBold;h.Margin=new Thickness(0,7,0,7);s.Children.Add(h);
   s.Children.Add(Text(subtitle,13,"#9299A7"));return s;
  }


  static bool HasButtonAncestor(DependencyObject d){while(d!=null){if(d is Button)return true;d=VisualTreeHelper.GetParent(d);}return false;}
  void ToggleFavorite(string id){if(store.State.Favorites.Contains(id))store.State.Favorites.Remove(id);else store.State.Favorites.Add(id);store.Save();}
  void Select(ThemePack p){selected=p;draft=p.Copy();UpdateCardSelection();RenderInspector();PaletteChanged();}

  static void Divider(StackPanel p){p.Children.Add(new Border {Height=1,Background=Brush("#2B2E36"),Margin=new Thickness(0,17,0,17)});}

  void Preview() {
   var w=new Window {Title="Veyra · Desktop preview",Owner=Window,Width=1040,Height=650,Background=Brush("#11131B"),WindowStartupLocation=WindowStartupLocation.CenterOwner};w.Resources=Window.Resources;
   var grid=new Grid();var view=new SceneView(draft.Copy()){ShowClock=true};if(CaptureMode)grid.Children.Add(view);else grid.Children.Add(new GpuPreview(draft,store.State,store.Root));
   var exit=Button("Close preview · Esc",delegate{w.Close();},false);exit.HorizontalAlignment=HorizontalAlignment.Right;exit.VerticalAlignment=VerticalAlignment.Top;exit.Margin=new Thickness(20);grid.Children.Add(exit);w.Content=grid;
   var timer=new DispatcherTimer {Interval=TimeSpan.FromMilliseconds(1000.0/30)};timer.Tick+=delegate{if(CaptureMode&&view.Pack.Animated){view.Phase+=(.35+view.Pack.Motion*.018)/30;view.InvalidateVisual();}};w.Closed+=delegate{timer.Stop();};w.KeyDown+=delegate(object s,KeyEventArgs e){if(e.Key==Key.Escape)w.Close();};timer.Start();w.ShowDialog();
  }
  void Import(){var d=new OpenFileDialog {Title="Import a Veyra pack",Filter="Veyra pack (*.veyra.json)|*.veyra.json|JSON (*.json)|*.json"};if(d.ShowDialog(Window)==true)ImportFile(d.FileName);}
  void ImportFile(string file){try{var p=PackCodec.Read(file);ValidateImage(p);store.Add(p);selected=p;draft=p.Copy();favoritesOnly=false;filter="All packs";query="";ShowLibrary();Tell("Imported "+p.Name+" by "+p.Author+". Nothing was applied to Windows.");}catch(Exception ex){MessageBox.Show(Window,ex.Message,"Pack could not be imported",MessageBoxButton.OK,MessageBoxImage.Warning);}}
  static void ValidateImage(ThemePack p){if(p.Scene!="image")return;using(var input=new MemoryStream(Convert.FromBase64String(p.ImageBase64))){var dec=BitmapDecoder.Create(input,BitmapCreateOptions.None,BitmapCacheOption.OnLoad);if(dec.Frames.Count==0 || dec.Frames[0].PixelWidth>16384 || dec.Frames[0].PixelHeight>16384)throw new InvalidDataException("Image dimensions are too large (16384 px maximum).");}}
  void Export(){try{var d=new SaveFileDialog {Title="Export a shareable Veyra pack",FileName=draft.Id+".veyra.json",Filter="Veyra pack|*.veyra.json"};if(d.ShowDialog(Window)==true){File.WriteAllText(d.FileName,PackCodec.Encode(draft));Tell("Exported "+draft.Name+". Share only artwork you have permission to distribute.");}}catch(Exception ex){MessageBox.Show(Window,ex.Message,"Could not export pack");}}
  void SaveCopy(){EditPack(draft.Copy(),false);}
  void ShowCreator() {
   BeginPage("Creator studio");var stack=new StackPanel();host.Children.Add(new ScrollViewer {Content=stack});stack.Children.Add(Header("BUILT BY YOU","Make a space worth sharing.","Start from an original scene or bring your own photo. Every pack travels as one file."));
   var steps=new Grid {Margin=new Thickness(0,30,0,26)};for(int i=0;i<3;i++)steps.ColumnDefinitions.Add(new ColumnDefinition());
   string[] titles={"01  Choose your canvas","02  Make it yours","03  Share the file"};string[] copy={"Use a Veyra scene or import your own PNG / JPEG wallpaper.","Set the name, creator credit, palette, motion and desktop clock.","Export .veyra.json and share it on GitHub, Discord or anywhere you like."};
   for(int i=0;i<3;i++){var s=new StackPanel();s.Children.Add(Text(titles[i],16,"#E8EDE4"));var t=Text(copy[i],12,"#989FAD");t.Margin=new Thickness(0,12,0,0);t.LineHeight=21;s.Children.Add(t);var p=Panel(s,"#1B1E23",23);p.Margin=new Thickness(0,0,14,0);Grid.SetColumn(p,i);steps.Children.Add(p);}stack.Children.Add(steps);
   var buttons=new StackPanel {Orientation=Orientation.Horizontal};buttons.Children.Add(Button("✧  Create from this scene",delegate{EditPack(draft.Copy(),false);},true));var photo=Button("＋  Use my photo",CreatePhoto,false);photo.Margin=new Thickness(12,0,0,0);buttons.Children.Add(photo);stack.Children.Add(buttons);
   Divider(stack);var artwork=new SceneView(draft.Copy()){Height=235,ShowClock=false};stack.Children.Add(new Border {Child=artwork,CornerRadius=new CornerRadius(12),ClipToBounds=true});
   var note=Text("Packs contain settings and optional image data. They cannot run scripts or install software. Built-in artwork is CC0; credit and license your own work before sharing.",12,"#919AA7");note.Margin=new Thickness(0,18,0,0);stack.Children.Add(note);
  }
  void CreatePhoto() {
   var d=new OpenFileDialog {Title="Choose your wallpaper",Filter="Images|*.png;*.jpg;*.jpeg"};if(d.ShowDialog(Window)!=true)return;
   try{if(new FileInfo(d.FileName).Length>16*1024*1024)throw new InvalidDataException("Choose an image smaller than 16 MB.");var p=draft.Copy();p.Scene="image";p.Animated=true;p.Effect="Mist";p.ImageBase64=Convert.ToBase64String(File.ReadAllBytes(d.FileName));p.ImageExtension=Path.GetExtension(d.FileName).ToLowerInvariant()==".png"?".png":".jpg";p.Name=Path.GetFileNameWithoutExtension(d.FileName);p.License="Personal use — not licensed for redistribution";ValidateImage(p);EditPack(p,true);}catch(Exception ex){MessageBox.Show(Window,ex.Message,"Could not load image");}
  }
  void EditPack(ThemePack p,bool photo) {
   var w=new Window {Title="Veyra · Create a pack",Owner=Window,Width=530,Height=640,ResizeMode=ResizeMode.NoResize,Background=Brush("#17191F"),Foreground=Brush("#F2F3F5"),WindowStartupLocation=WindowStartupLocation.CenterOwner};w.Resources=Window.Resources;
   var s=new StackPanel {Margin=new Thickness(28)};w.Content=new ScrollViewer {Content=s};s.Children.Add(Text("A space with your signature.",24,"#F0F3EC"));
   var name=Field(s,"PACK NAME",p.Name+ (photo?"":" · remix"));var author=Field(s,"CREATOR", "Your name");var description=Field(s,"DESCRIPTION",p.Description??"");description.AcceptsReturn=true;description.Height=68;description.TextWrapping=TextWrapping.Wrap;
   var accent=Field(s,"ACCENT · #RRGGBB",p.Accent);var license=Field(s,"CONTENT LICENSE / RIGHTS",p.License);
   var notice=Text("Use your name and a license you can grant. Exporting an image does not give you rights to someone else's artwork.",11,"#A0A8B5");notice.Margin=new Thickness(0,12,0,14);s.Children.Add(notice);
   var error=Text("",11,"#F1AB9D");s.Children.Add(error);
   var save=Button("Save to my library",delegate{
    try{p.Id="local-"+Guid.NewGuid().ToString("N");p.Name=name.Text.Trim();p.Author=author.Text.Trim();p.Description=description.Text.Trim();p.Accent=accent.Text.Trim();p.License=license.Text.Trim();PackCodec.Validate(p);ValidateImage(p);store.Add(p);selected=p;draft=p.Copy();w.DialogResult=true;}catch(Exception ex){error.Text=ex.Message;}
   },true);s.Children.Add(save);
   if(w.ShowDialog()==true){favoritesOnly=false;filter="All packs";query="";ShowLibrary();Tell("Saved "+p.Name+". Export it when you're ready to share.");}
  }
  static TextBox Field(StackPanel s,string label,string value){var l=Text(label,9,"#9AA2B0");l.Margin=new Thickness(0,17,0,7);s.Children.Add(l);var t=new TextBox {Text=value};System.Windows.Automation.AutomationProperties.SetName(t,label);s.Children.Add(t);return t;}
  void ShowCommunity() {
   BeginPage("Community");var s=new StackPanel();host.Children.Add(new ScrollViewer {Content=s});s.Children.Add(Header("A SHARED SPACE","Small files. Endless worlds.","Veyra starts with an open format and a local library. The community can grow around it."));
   var content=new StackPanel();content.Children.Add(Text("Create → Export → Share → Import",24,"#D9ECC8"));var t=Text("Every pack is a portable .veyra.json file with creator credit, a license, scene settings and optional embedded artwork. You can share it without a Veyra account or a hosting service.",14,"#A6AEBB");t.LineHeight=24;t.Margin=new Thickness(0,18,0,22);content.Children.Add(t);content.Children.Add(Button("Import a creator's pack",Import,true));var panel=Panel(content,"#1C231E",28);panel.Margin=new Thickness(0,30,0,26);s.Children.Add(panel);
   s.Children.Add(Text("Veyra on Discord",20,"#F0F2F4"));var explanation=Text("Share your desktop, discover creator packs, suggest features and help shape Veyra. The community starts with you.",13,"#989FAD");explanation.Margin=new Thickness(0,10,0,18);s.Children.Add(explanation);
   var row=new StackPanel {Orientation=Orientation.Horizontal};var discord=Button("Join Discord  ↗",delegate{OpenUrl(store.State.DiscordUrl);},true);discord.IsEnabled=ValidUrl(store.State.DiscordUrl);row.Children.Add(discord);if(ValidUrl(store.State.DonateUrl)){var donate=Button("Support voluntarily  ↗",delegate{OpenUrl(store.State.DonateUrl);},false);donate.Margin=new Thickness(12,0,0,0);row.Children.Add(donate);}s.Children.Add(row);
   Divider(s);s.Children.Add(Text("Community gallery · planned",16,"#D1D6DE"));var roadmap=Text("Public submissions, moderation, reporting, creator pages and discovery need a hosted service and content rules. Local imports and exports work today.",12,"#8F98A6");roadmap.Margin=new Thickness(0,10,0,0);s.Children.Add(roadmap);
  }
  static bool ValidUrl(string url){Uri uri;return Uri.TryCreate(url,UriKind.Absolute,out uri) && uri.Scheme=="https" && string.IsNullOrEmpty(uri.UserInfo);}
  void OpenUrl(string url){if(!ValidUrl(url)){Tell("Use a valid HTTPS link in Preferences.");return;}try{Process.Start(new ProcessStartInfo(url){UseShellExecute=true});}catch(Exception ex){Tell(ex.Message);}}

 }
}


