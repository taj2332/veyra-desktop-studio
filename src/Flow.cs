using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace Veyra {
 public static class Flow {
  public static string MoodAccent(ThemePack p){var c=SceneView.ColorOf(p.Accent);double factor=p.Mood=="Night"?.68:p.Mood=="Focus"?.82:1;return string.Format("#{0:X2}{1:X2}{2:X2}",(byte)(c.R*factor),(byte)(c.G*factor),(byte)(c.B*factor));}
  public static string PhotoAccent(ThemePack p){
   byte[] bytes=p.Scene=="image"?Convert.FromBase64String(p.ImageBase64):p.Scene=="alpine"||p.Scene=="neon"?Assets.Bytes(p.Scene):null;if(bytes==null)return p.Accent;
   var bins=new Dictionary<int,double>();var sums=new Dictionary<int,double[]>();
   using(var input=new MemoryStream(bytes))using(var original=new System.Drawing.Bitmap(input))using(var image=new System.Drawing.Bitmap(original,new System.Drawing.Size(96,64))){
    for(int y=0;y<image.Height;y++)for(int x=0;x<image.Width;x++){var c=image.GetPixel(x,y);double max=Math.Max(c.R,Math.Max(c.G,c.B))/255.0,min=Math.Min(c.R,Math.Min(c.G,c.B))/255.0,saturation=max==0?0:(max-min)/max;
     if(max<.18||max>.95||saturation<.18)continue;int key=(c.R/24)<<8|(c.G/24)<<4|(c.B/24);double weight=.15+saturation*max;
     if(!bins.ContainsKey(key)){bins[key]=0;sums[key]=new double[3];}bins[key]+=weight;sums[key][0]+=c.R*weight;sums[key][1]+=c.G*weight;sums[key][2]+=c.B*weight;
    }
   }
   if(bins.Count==0)return p.Accent;int best=bins.OrderByDescending(x=>x.Value).First().Key;var v=sums[best];double weightTotal=bins[best],peak=v.Max()/weightTotal,bright=Math.Min(1.7,Math.Max(1,205/peak));return string.Format("#{0:X2}{1:X2}{2:X2}",(byte)Math.Min(255,v[0]/weightTotal*bright),(byte)Math.Min(255,v[1]/weightTotal*bright),(byte)Math.Min(255,v[2]/weightTotal*bright));
  }
  public static TimeSpan Remaining(string utc,DateTime now){DateTime end;return DateTime.TryParse(utc,CultureInfo.InvariantCulture,DateTimeStyles.RoundtripKind,out end)?TimeSpan.FromSeconds(Math.Max(0,(end.ToUniversalTime()-now.ToUniversalTime()).TotalSeconds)):TimeSpan.Zero;}
 }
 public sealed partial class Studio {
  GpuPreview inspectorGpu,studioGpu;
  DispatcherTimer focusTimer,cardsDebounce;
  TextBlock focusStatus;
  async void ResumeAtmosphere(){if(applying||desktop.IsLive)return;applying=true;try{var p=draft.Copy();var complete=new System.Threading.Tasks.TaskCompletionSource<string>();var thread=new System.Threading.Thread(delegate(){try{complete.SetResult(desktop.Resume(p));}catch(Exception ex){complete.SetException(ex);}});thread.IsBackground=true;thread.SetApartmentState(System.Threading.ApartmentState.STA);thread.Start();Tell(await complete.Task);Find<TextBlock>("LiveBadge").Text="●  GPU DESKTOP ACTIVE";}catch(Exception ex){Tell("Resume failed: "+ex.Message);}finally{applying=false;}}
  void StartFlowTimer(){focusTimer=new DispatcherTimer {Interval=TimeSpan.FromSeconds(1)};focusTimer.Tick+=delegate{
    if(applying)return;if(string.IsNullOrEmpty(store.State.FocusEndsUtc)){if(desktop.IsLive)Find<TextBlock>("LiveBadge").Text="●  "+desktop.Performance();return;}var left=Flow.Remaining(store.State.FocusEndsUtc,DateTime.UtcNow);if(left.TotalSeconds<=0){EndFocus(true);return;}string text="FOCUS  "+((int)left.TotalMinutes).ToString("00")+":"+left.Seconds.ToString("00");if(focusStatus!=null)focusStatus.Text=text;Find<TextBlock>("LiveBadge").Text="◉  "+text;
   };focusTimer.Start();Window.Closed+=delegate{focusTimer.Stop();if(cardsDebounce!=null)cardsDebounce.Stop();};}
  void StopAtmosphere(){store.State.ResumeLive=false;store.Save();desktop.StopLive();}
  void UpdateGpuPreviews(){if(inspectorGpu!=null)inspectorGpu.UpdateScene(draft);if(studioGpu!=null)studioGpu.UpdateScene(draft);}
  void QueueCards(){if(cardsDebounce==null){cardsDebounce=new DispatcherTimer {Interval=TimeSpan.FromMilliseconds(140)};cardsDebounce.Tick+=delegate{cardsDebounce.Stop();if(current=="Explore packs"||current=="Favorites")RenderCards();};}cardsDebounce.Stop();cardsDebounce.Start();}
  void SetMood(string mood){draft.Mood=mood;Changed();PaletteChanged();Tell(mood=="Focus"?"Focus · calmer movement, quieter color, the same space.":mood=="Night"?"Night · a softer desktop for the evening.":"Studio · your original atmosphere.");}
  void BeginFocus(int minutes){store.State.FocusReturnMood=draft.Mood=="Focus"?"Studio":draft.Mood;store.State.FocusEndsUtc=DateTime.UtcNow.AddMinutes(minutes).ToString("o");store.Save();draft.Clock=true;SetMood("Focus");store.Save();if(!desktop.IsLive||desktop.Active.Id!=draft.Id)Apply();ShowFlow();}
  void EndFocus(bool completed){store.State.FocusEndsUtc=null;SetMood(store.State.FocusReturnMood??"Studio");store.Save();Find<TextBlock>("LiveBadge").Text=desktop.IsLive?"●  GPU DESKTOP ACTIVE":"●  LOCAL WORKSPACE";if(focusStatus!=null)focusStatus.Text=completed?"Session complete. Take a breath.":"Ready when you are.";Tell(completed?"Focus session complete. Your original mood is restored.":"Focus session ended.");}
  void RemixSignature(){var random=new Random();draft.Seed=random.Next(1,1000000);string[] palette={"#AACBFF","#C5AAFF","#EFADC4","#90D7CA","#D9BE96","#B9DCAF"};draft.Accent=palette[random.Next(palette.Length)];Changed();PaletteChanged();Tell("A new signature. Save a copy to keep or share it.");}
  void ShowFlow(){
   BeginPage("Flow studio");host.RowDefinitions.Add(new RowDefinition {Height=GridLength.Auto});host.RowDefinitions.Add(new RowDefinition());var header=Header("VEYRA FLOW","One space. Your rhythm.","A coordinated desktop that moves with your day. Your wallpaper, clock and Windows palette travel together.");header.Margin=new Thickness(0,0,0,23);host.Children.Add(header);
   var body=new Grid();body.ColumnDefinitions.Add(new ColumnDefinition());body.ColumnDefinitions.Add(new ColumnDefinition {Width=new GridLength(320)});Grid.SetRow(body,1);host.Children.Add(body);
   var art=new Grid {Margin=new Thickness(0,0,24,0),Background=Brush("#0A1220")};if(CaptureMode){heroScene=new SceneView(draft){ShowClock=true};art.Children.Add(heroScene);}else{art.Children.Add(new SceneView(draft){UseThumbnail=true});studioGpu=new GpuPreview(draft,store.State,store.Root);art.Children.Add(studioGpu);}body.Children.Add(Round(art,18));
   var options=new StackPanel {Margin=new Thickness(0,0,8,0)};var controls=new ScrollViewer {Content=options};Grid.SetColumn(controls,1);body.Children.Add(controls);var name=Text(draft.Name,24,"#F0F1F6");name.FontWeight=FontWeights.SemiBold;options.Children.Add(name);options.Children.Add(Text("Your selected space, thoughtfully remixed.",11,"#8C9AB0"));
   Section(options,"YOUR MOOD");Choices(options,new[]{"Studio","Focus","Night"},draft.Mood,SetMood);options.Children.Add(Text("Focus softens color and movement. Night lowers the light. The scene keeps its identity.",11,"#8F9EB4"));
   Section(options,"MAKE IT YOURS");Palette(options);var remix=Button("✦  Remix signature",delegate{RemixSignature();ShowFlow();},false);remix.Margin=new Thickness(0,8,0,8);options.Children.Add(remix);var match=Button("Match palette to photo",delegate{draft.Accent=Flow.PhotoAccent(draft);Changed();PaletteChanged();ShowFlow();Tell("Palette sampled from your photo. Color sync follows your applied scene.");},false);match.IsEnabled=draft.Scene=="alpine"||draft.Scene=="neon"||draft.Scene=="image";options.Children.Add(match);
   Section(options,"FOCUS SESSION");focusStatus=Text("Ready when you are.",15,"#E3EAF3");options.Children.Add(focusStatus);var sessions=new WrapPanel {Margin=new Thickness(0,10,0,0)};sessions.Children.Add(Button("25 minutes",delegate{BeginFocus(25);},false));var fifty=Button("50 minutes",delegate{BeginFocus(50);},false);fifty.Margin=new Thickness(8,0,0,0);sessions.Children.Add(fifty);options.Children.Add(sessions);if(!string.IsNullOrEmpty(store.State.FocusEndsUtc))options.Children.Add(Button("End session",delegate{EndFocus(false);ShowFlow();},false));options.Children.Add(Text("Starting a session applies this scene. The timer stays on your desktop; your previous mood returns when it ends.",10,"#7C8CA5"));
   Section(options,"ONE PORTABLE LOOK");options.Children.Add(Button("Apply this atmosphere  ↗",Apply,true));var save=Button("Save & share a copy",SaveCopy,false);save.Margin=new Thickness(0,8,0,0);options.Children.Add(save);options.Children.Add(Text("Mood, palette, scene seed, motion and clock settings are included in your .veyra.json pack. Your timer stays private.",10,"#7C8CA5"));PaletteChanged();
  }
 }
}
