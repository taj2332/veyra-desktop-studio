using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Security.Cryptography;

namespace Veyra {
 public static class SelfTest {
  static List<string> results=new List<string>();
  static void Check(bool ok,string name){if(!ok)throw new Exception("FAILED: "+name);results.Add("PASS: "+name);}
  static void Reject(Action action,string name){bool rejected=false;try{action();}catch(Exception){rejected=true;}Check(rejected,name);}
  public static int Run(string root) {
   Directory.CreateDirectory(root);
   try {
    string testDir=Path.Combine(root,"run-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(testDir);
    var store=new Store(testDir);Check(store.State.Packs.Count==16,"sixteen starter packs");
    Check(store.State.DiscordUrl==AppState.CommunityInvite,"configured Discord invite is available by default");
    foreach(var p in store.State.Packs){PackCodec.Validate(p);string json=PackCodec.Encode(p);var decoded=PackCodec.Json().Deserialize<ThemePack>(json);Check(decoded.Id==p.Id && decoded.Accent==p.Accent,"pack roundtrip: "+p.Name);}
    var pack=store.State.Packs[0].Copy();
    pack.Id="../escape";Reject(delegate{PackCodec.Validate(pack);},"reject path traversal ID");pack=store.State.Packs[0].Copy();
    pack.Scene="javascript";Reject(delegate{PackCodec.Validate(pack);},"reject executable scene type");pack=store.State.Packs[0].Copy();
    pack.Accent="red;script";Reject(delegate{PackCodec.Validate(pack);},"reject invalid color");pack=store.State.Packs[0].Copy();
    pack.SchemaVersion=2;Reject(delegate{PackCodec.Validate(pack);},"reject unsupported schema");pack=store.State.Packs[0].Copy();
    pack.Motion=-1;Reject(delegate{PackCodec.Validate(pack);},"reject invalid motion");pack=store.State.Packs[0].Copy();
    pack.License="";Reject(delegate{PackCodec.Validate(pack);},"require content rights statement");
    store.State.Favorites.Add(store.State.Packs[0].Id);store.Save();var reloaded=new Store(testDir);Check(reloaded.State.Favorites.Count==1,"favorites persist");
    var original=reloaded.State.Packs[0];var duplicate=original.Copy();reloaded.Add(duplicate);Check(duplicate.Id!=original.Id && reloaded.State.Packs.Count==17,"duplicate import gets independent ID");
    pack=original.Copy();pack.LocalImage=@"C:\Windows\arbitrary.png";string export=Path.Combine(testDir,"pack.veyra.json");File.WriteAllText(export,PackCodec.Encode(pack));Check(PackCodec.Read(export).LocalImage==null,"shared local paths are discarded");
    store.State.DockItems.Add(new DockItem {Name="Private shortcut",Path=@"C:\private\personal-app.exe"});store.Save();Check(!PackCodec.Encode(original).Contains("personal-app.exe"),"local dock executable paths are not exported");
    // Render every original scene and decode it again. No Windows settings are changed.
    foreach(var p in store.State.Packs){string image=Path.Combine(root,p.Scene+".png");SceneView.SavePng(p,image,1200,750);using(var input=File.OpenRead(image)){var bitmap=BitmapDecoder.Create(input,BitmapCreateOptions.None,BitmapCacheOption.OnLoad);Check(bitmap.Frames[0].PixelWidth==1200 && bitmap.Frames[0].PixelHeight==750,"render scene: "+p.Name);}}
    foreach(var p in store.State.Packs){string a=Path.Combine(testDir,"frame-a.png"),b=Path.Combine(testDir,"frame-b.png");SceneView.SaveFrame(p,a,480,300,0,false);SceneView.SaveFrame(p,b,480,300,2.6,false);Check(!File.ReadAllBytes(a).SequenceEqual(File.ReadAllBytes(b)),"animation changes rendered pixels: "+p.Name);}
    var still=original.Copy();still.Animated=false;string stillA=Path.Combine(testDir,"still-a.png"),stillB=Path.Combine(testDir,"still-b.png");SceneView.SaveFrame(still,stillA,480,300,0,false);SceneView.SaveFrame(still,stillB,480,300,3,false);Check(File.ReadAllBytes(stillA).SequenceEqual(File.ReadAllBytes(stillB)),"static toggle freezes rendered frames");
    foreach(var size in new[]{new[]{1920,1080},new[]{1720,720},new[]{720,1280}}){string file=Path.Combine(root,"clock-"+size[0]+"x"+size[1]+".png");SceneView.SaveFrame(original,file,size[0],size[1],1,true);Check(new FileInfo(file).Length>1000,"responsive clock and artwork: "+size[0]+"x"+size[1]);}
    Check(DesktopService.AccentBgr("#112233")==0xff332211,"Windows accent channel order");
    var invalid=original.Copy();invalid.ClockX=101;Reject(delegate{PackCodec.Validate(invalid);},"reject clock outside viewport");invalid=original.Copy();invalid.TaskbarStyle="script";Reject(delegate{PackCodec.Validate(invalid);},"reject unknown taskbar mode");
    var shared=PackCodec.Json().Deserialize<ThemePack>(PackCodec.Encode(original));Check(shared.Scene=="image" && !string.IsNullOrEmpty(shared.ImageBase64),"bundled artwork becomes self-contained on export");
    pack=original.Copy();pack.Id="test-image";pack.Scene="image";pack.ImageExtension=".png";pack.ImageBase64=Convert.ToBase64String(File.ReadAllBytes(Path.Combine(root,"aurora.png")));PackCodec.Validate(pack);Check(true,"embedded PNG accepted");
    string photoFile=Path.Combine(root,"photo-roundtrip.png");SceneView.SavePng(pack,photoFile,640,400);Check(new FileInfo(photoFile).Length>1000,"render embedded image");
    pack.ImageBase64=Convert.ToBase64String(new byte[]{1,2,3,4,5,6,7,8});Reject(delegate{PackCodec.Validate(pack);},"reject image signature mismatch");
    byte[] oversized=File.ReadAllBytes(Path.Combine(root,"aurora.png"));oversized[16]=0x7f;pack.ImageBase64=Convert.ToBase64String(oversized);Reject(delegate{PackCodec.Validate(pack);},"reject oversized compressed image before decoding");
    using(var input=File.OpenRead(Path.Combine(root,"aurora.png"))){var decoder=BitmapDecoder.Create(input,BitmapCreateOptions.None,BitmapCacheOption.OnLoad);var jpeg=new JpegBitmapEncoder();jpeg.Frames.Add(decoder.Frames[0]);using(var output=new MemoryStream()){jpeg.Save(output);pack.ImageExtension=".jpg";pack.ImageBase64=Convert.ToBase64String(output.ToArray());PackCodec.Validate(pack);Check(true,"JPEG dimensions and signature accepted");}}
    var templates=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"..","templates");
    if(Directory.Exists(templates)){foreach(string file in Directory.GetFiles(templates,"*.veyra.json")){PackCodec.Read(file);Check(true,"distributed template parses: "+Path.GetFileName(file));}}
    string legacyDir=Path.Combine(testDir,"legacy");Directory.CreateDirectory(legacyDir);File.WriteAllText(Path.Combine(legacyDir,"library.json"),"{\"Packs\":[{\"SchemaVersion\":1,\"Id\":\"old-remix\",\"Name\":\"Old remix\",\"Author\":\"Creator\",\"License\":\"CC0\",\"Scene\":\"aurora\",\"Accent\":\"#B9ED80\",\"Motion\":35}],\"Favorites\":[\"old-remix\"]}");var migrated=new Store(legacyDir);Check(migrated.State.Packs.Count==17 && migrated.State.Favorites.Contains("old-remix") && migrated.State.Packs[0].ClockStyle=="Editorial","old libraries migrate without losing remixes or favorites");
    var oldStarter=Store.Defaults().First(x=>x.Scene=="summit");oldStarter.Effect="None";var oldLibrary=new AppState();oldLibrary.Packs.Add(oldStarter);File.WriteAllText(Path.Combine(legacyDir,"library.json"),PackCodec.Json().Serialize(oldLibrary));var upgraded=new Store(legacyDir);Check(upgraded.State.Packs.First(x=>x.Scene=="summit").Effect=="Snow"&&upgraded.State.LibraryVersion==4,"old official starters receive the new animation defaults");
    var v2=new AppState {LibraryVersion=2};var remix=Store.Defaults()[0].Copy();remix.Id="legacy-custom";remix.Author="Creator";remix.Dock=true;remix.Motion=73;remix.TaskbarStyle="Blur";v2.Packs.Add(remix);v2.Favorites.Add(remix.Id);File.WriteAllText(Path.Combine(legacyDir,"library.json"),PackCodec.Json().Serialize(v2));var noDock=new Store(legacyDir);var preserved=noDock.State.Packs.First(x=>x.Id==remix.Id);Check(!preserved.Dock&&preserved.Motion==73&&noDock.State.Favorites.Contains(remix.Id),"v0.2 dock removed without losing creator settings or favorites");var exported=PackCodec.Json().Deserialize<ThemePack>(PackCodec.Encode(remix));Check(!exported.Dock&&exported.TaskbarStyle=="System","new exports omit retired dock and experimental taskbar behavior");Check(Store.Defaults().All(x=>!x.Dock&&x.TaskbarAccent&&x.WindowsTransparency),"starters use Windows taskbar appearance controls");
    var flow=original.Copy();flow.Mood="Night";flow.Seed=91357;var recipe=PackCodec.Json().Deserialize<ThemePack>(PackCodec.Encode(flow));Check(recipe.Mood=="Night"&&recipe.Seed==91357,"Flow mood and deterministic seed travel with a pack");flow.Mood="<script>";Reject(delegate{PackCodec.Validate(flow);},"reject unknown Flow mood");flow.Mood="Studio";flow.Seed=-1;Reject(delegate{PackCodec.Validate(flow);},"reject negative scene seed");flow.Seed=1000001;Reject(delegate{PackCodec.Validate(flow);},"bound procedural scene seed");flow=original.Copy();flow.Mood="Night";var tone=SceneView.ColorOf(Flow.MoodAccent(flow));var baseColor=SceneView.ColorOf(flow.Accent);Check(tone.R<baseColor.R&&tone.G<baseColor.G&&tone.B<baseColor.B,"Night coordinates a darker Windows accent");
    DateTime fixedNow=new DateTime(2026,1,1,12,0,0,DateTimeKind.Utc);Check(Math.Abs(Flow.Remaining(fixedNow.AddMinutes(25).ToString("o"),fixedNow).TotalSeconds-1500)<.1,"focus countdown follows UTC rather than rendered frame count");Check(Flow.Remaining("malformed",fixedNow)==TimeSpan.Zero&&Flow.Remaining(fixedNow.AddMinutes(-1).ToString("o"),fixedNow)==TimeSpan.Zero,"invalid or expired focus sessions cannot become negative");
    var photo=original.Copy();photo.Scene="image";photo.ImageExtension=".png";using(var image=new System.Drawing.Bitmap(16,16)){using(var graphics=System.Drawing.Graphics.FromImage(image))graphics.Clear(System.Drawing.Color.FromArgb(55,125,220));using(var memory=new MemoryStream()){image.Save(memory,System.Drawing.Imaging.ImageFormat.Png);photo.ImageBase64=Convert.ToBase64String(memory.ToArray());}}var sampled=SceneView.ColorOf(Flow.PhotoAccent(photo));Check(sampled.B>sampled.G&&sampled.G>sampled.R,"photo palette preserves dominant hue");
    var savedSession=new Store(Path.Combine(testDir,"session"));savedSession.State.AppliedId=recipe.Id;savedSession.State.AppliedPack=recipe;savedSession.State.FocusEndsUtc=DateTime.UtcNow.AddMinutes(25).ToString("o");savedSession.Save();var restoredSession=new Store(savedSession.Root);Check(restoredSession.State.AppliedPack.Seed==91357&&restoredSession.State.AppliedPack.Mood=="Night","last applied recipe survives reopening");Check(!PackCodec.Encode(recipe).Contains("FocusEndsUtc"),"private focus countdown is excluded from shared recipes");
    using(var xaml=typeof(Program).Assembly.GetManifestResourceStream("Veyra.MainWindow.xaml")){var window=(Window)System.Windows.Markup.XamlReader.Load(xaml);Check(window.FindName("NavFlow")!=null,"production XAML loads with Flow navigation");window.Close();}
    savedSession.State.AppliedPack.Mood="Focus";savedSession.State.FocusReturnMood="Night";savedSession.State.FocusEndsUtc=DateTime.UtcNow.AddMinutes(-1).ToString("o");savedSession.Save();var expiredSession=new Store(savedSession.Root);Check(expiredSession.State.FocusEndsUtc==null&&expiredSession.State.AppliedPack.Mood=="Night","expired offline focus session restores its previous mood");
    string corrupt=Path.Combine(testDir,"library.json");File.WriteAllText(corrupt,"{broken");var recovered=new Store(testDir);Check(recovered.Warning!=null && recovered.State.Packs.Count==16 && Directory.GetFiles(testDir,"*.recovered-*").Length==1,"corrupt library is preserved and recovered");
    Check(!File.Exists(Path.Combine(testDir,"desktop-backup.json")),"tests do not change wallpaper or appearance");
    File.WriteAllLines(Path.Combine(root,"test-results.txt"),results.Concat(new[]{"",results.Count+" checks passed. No desktop settings modified."}));
    return 0;
   } catch(Exception ex){results.Add(ex.ToString());File.WriteAllLines(Path.Combine(root,"test-results.txt"),results);return 1;}
  }
 }
}

