using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using System.Text.RegularExpressions;

namespace Veyra {
    public sealed class ThemePack {
        public int SchemaVersion { get; set; }
        public string Id { get; set; }
        public string Name { get; set; }
        public string Author { get; set; }
        public string Description { get; set; }
        public string License { get; set; }
        public string Scene { get; set; }
        public string Accent { get; set; }
        public bool Animated { get; set; }
        public bool Clock { get; set; }
        public bool LightMode { get; set; }
        public int Motion { get; set; }
        public string ImageBase64 { get; set; }
        public string ImageExtension { get; set; }
        public string LocalImage { get; set; }
        public string Effect { get; set; }
        public string ClockStyle { get; set; }
        public int ClockSize { get; set; }
        public int ClockX { get; set; }
        public int ClockY { get; set; }
        public bool Dock { get; set; }
        public string DockStyle { get; set; }
        public bool HideIcons { get; set; }
        public bool AutoHideTaskbar { get; set; }
        public bool TaskbarAccent { get; set; }
        public bool WindowsTransparency { get; set; }
        public string TaskbarStyle { get; set; }
        public string Mood {get;set;}
        public int Seed {get;set;}
        public ThemePack() { Mood="Studio";Seed=42;TaskbarAccent=true; WindowsTransparency=true; ClockStyle="Editorial"; ClockSize=100; ClockX=50; ClockY=20; DockStyle="Glass"; TaskbarStyle="System"; Effect="None"; }
        public ThemePack Copy() { return (ThemePack)MemberwiseClone(); }
    }
    public sealed class AppState {
        public const string CommunityInvite="https://discord.gg/wuZSKphzmP";
        public string RenderQuality {get;set;}
        public string FocusEndsUtc {get;set;}
        public string FocusReturnMood {get;set;}
        public int LibraryVersion { get; set; }
        public List<ThemePack> Packs { get; set; }
        public List<string> Favorites { get; set; }
        public string AppliedId { get; set; }
        public ThemePack AppliedPack {get;set;}
        public bool AppliedColors {get;set;}
        public bool ResumeLive {get;set;}
        public bool PauseOnBattery { get; set; }
        public bool PauseFullscreen { get; set; }
        public string DiscordUrl { get; set; }
        public string DonateUrl { get; set; }
        public bool AllMonitors { get; set; }
        public int TargetFps { get; set; }
        public List<DockItem> DockItems { get; set; }
        public AppState() { AppliedColors=true;ResumeLive=true;Packs=new List<ThemePack>(); Favorites=new List<string>(); PauseOnBattery=true; PauseFullscreen=true; AllMonitors=true; TargetFps=60;RenderQuality="Ultra"; DockItems=new List<DockItem>(); DiscordUrl=CommunityInvite; }
    }
    public sealed class DockItem {public string Name {get;set;} public string Path {get;set;} }
    public static class PackCodec {
        public const int MaxBytes = 24 * 1024 * 1024;
        public static readonly string[] Scenes = { "aurora", "dune", "orbit", "bloom", "tide", "summit", "alpine", "neon", "silk", "ember", "cosmos", "rain", "forest", "mono", "sakura", "lagoon", "image" };
        public static JavaScriptSerializer Json() { return new JavaScriptSerializer { MaxJsonLength=MaxBytes, RecursionLimit=16 }; }
        public static void Validate(ThemePack p) {
            if(p!=null&&(!new[]{"Studio","Focus","Night"}.Contains(p.Mood)||p.Seed<0||p.Seed>1000000))throw new InvalidDataException("Unsupported Flow mood or scene seed.");
            if(p==null || p.SchemaVersion!=1) throw new InvalidDataException("This pack needs schemaVersion 1.");
            if(string.IsNullOrWhiteSpace(p.Id) || !Regex.IsMatch(p.Id, @"^[a-zA-Z0-9_-]{1,64}$")) throw new InvalidDataException("Pack ID must use 1–64 letters, numbers, underscores or hyphens.");
            if(string.IsNullOrWhiteSpace(p.Name) || p.Name.Length>80) throw new InvalidDataException("Give your pack a name (up to 80 characters).");
            if(string.IsNullOrWhiteSpace(p.Author) || p.Author.Length>80) throw new InvalidDataException("A creator name is required (up to 80 characters).");
            if(p.Description!=null && p.Description.Length>600) throw new InvalidDataException("Description is too long (600 characters maximum).");
            if(string.IsNullOrWhiteSpace(p.License) || p.License.Length>160) throw new InvalidDataException("A content license or rights statement is required.");
            if(!Scenes.Contains(p.Scene)) throw new InvalidDataException("This scene type is not supported.");
            if(p.Accent==null || !Regex.IsMatch(p.Accent,@"^#[0-9a-fA-F]{6}$")) throw new InvalidDataException("Accent must be a #RRGGBB color.");
            if(p.Motion<0 || p.Motion>100) throw new InvalidDataException("Motion must be between 0 and 100.");
            if(p.ClockSize<60 || p.ClockSize>160 || p.ClockX<5 || p.ClockX>95 || p.ClockY<5 || p.ClockY>80)throw new InvalidDataException("Clock position or size is outside the supported range.");
            if(!new[]{"Editorial","Digital","Minimal"}.Contains(p.ClockStyle))throw new InvalidDataException("Unsupported clock style.");
            if(!new[]{"None","Mist","Rain","Snow","Fireflies"}.Contains(p.Effect))throw new InvalidDataException("Unsupported photo effect.");
            if(!new[]{"Glass","Solid","Outline"}.Contains(p.DockStyle))throw new InvalidDataException("Unsupported dock style.");
            if(!new[]{"System","Clear","Tinted","Blur"}.Contains(p.TaskbarStyle))throw new InvalidDataException("Unsupported taskbar style.");
            if(p.Scene=="image") {
                if(p.ImageExtension!=".png" && p.ImageExtension!=".jpg") throw new InvalidDataException("Only PNG and JPEG images are supported.");
                if(string.IsNullOrEmpty(p.ImageBase64)) throw new InvalidDataException("Image packs must embed their image.");
                byte[] bytes;
                try { bytes=Convert.FromBase64String(p.ImageBase64); } catch(FormatException) { throw new InvalidDataException("The embedded image is not valid base64."); }
                if(bytes.Length>16*1024*1024 || bytes.Length<8) throw new InvalidDataException("Image must be between 8 bytes and 16 MB.");
                bool png=bytes[0]==137 && bytes[1]==80 && bytes[2]==78 && bytes[3]==71 && bytes[4]==13 && bytes[5]==10 && bytes[6]==26 && bytes[7]==10;
                bool jpg=bytes[0]==255 && bytes[1]==216 && bytes[2]==255;
                if((p.ImageExtension==".png" && !png) || (p.ImageExtension==".jpg" && !jpg)) throw new InvalidDataException("Image content does not match its file type.");
                ValidateDimensions(bytes,png);
            } else if(!string.IsNullOrEmpty(p.ImageBase64)) throw new InvalidDataException("Only image scenes may contain an image.");
        }
        static void ValidateDimensions(byte[] bytes,bool png) {
            int w=0,h=0;
            if(png) {
                if(bytes.Length<24)throw new InvalidDataException("PNG header is incomplete.");
                long width=((long)bytes[16]<<24)|((long)bytes[17]<<16)|((long)bytes[18]<<8)|bytes[19];
                long height=((long)bytes[20]<<24)|((long)bytes[21]<<16)|((long)bytes[22]<<8)|bytes[23];
                if(width>8192 || height>8192 || width*height>40000000 || width<1 || height<1)throw new InvalidDataException("Images must be at most 8192 pixels per side and 40 megapixels.");
                return;
            }
            int offset=2;
            while(offset+3<bytes.Length) {
                if(bytes[offset++]!=255)throw new InvalidDataException("Malformed JPEG header.");
                while(offset<bytes.Length && bytes[offset]==255)offset++;
                if(offset>=bytes.Length)break;
                int marker=bytes[offset++];
                if(marker==0xD9 || marker==0xDA)break;
                if(marker==0x01 || (marker>=0xD0 && marker<=0xD7))continue;
                if(offset+1>=bytes.Length)break;
                int length=(bytes[offset]<<8)|bytes[offset+1];
                if(length<2 || offset+length>bytes.Length)throw new InvalidDataException("Malformed JPEG segment.");
                if(marker>=0xC0 && marker<=0xCF && marker!=0xC4 && marker!=0xC8 && marker!=0xCC) {
                    if(length<8)throw new InvalidDataException("Malformed JPEG frame.");
                    h=(bytes[offset+3]<<8)|bytes[offset+4];w=(bytes[offset+5]<<8)|bytes[offset+6];break;
                }
                offset+=length;
            }
            if(w<1 || h<1 || w>8192 || h>8192 || (long)w*h>40000000)throw new InvalidDataException("JPEG images must be at most 8192 pixels per side and 40 megapixels.");
        }
        public static ThemePack Read(string file) {
            if(new FileInfo(file).Length>MaxBytes) throw new InvalidDataException("Pack is too large (24 MB maximum).");
            ThemePack p=Json().Deserialize<ThemePack>(File.ReadAllText(file));
            Validate(p);
            // Never trust a path supplied by a shared pack.
            p.LocalImage=null;p.Dock=false;
            return p;
        }
        public static string Encode(ThemePack p) {
            Validate(p); ThemePack clean=p.Copy(); clean.LocalImage=null;clean.Dock=false;clean.TaskbarStyle="System";
            if(clean.Scene=="alpine" || clean.Scene=="neon"){clean.ImageBase64=Convert.ToBase64String(Assets.Bytes(clean.Scene));clean.ImageExtension=".png";clean.Scene="image";}
            return Json().Serialize(clean);
        }
    }
    public sealed class Store {
        public string Root {get; private set;}
        public AppState State {get; private set;}
        public string Warning {get; private set;}
        public Store(string root) {
            Root=root; Directory.CreateDirectory(root); Directory.CreateDirectory(Path.Combine(root,"images"));
            State=new AppState(); string file=Path.Combine(root,"library.json");
            if(File.Exists(file)) {
                try {
                    if(new FileInfo(file).Length>PackCodec.MaxBytes*4L) throw new InvalidDataException();
                    AppState loaded=new JavaScriptSerializer { MaxJsonLength=PackCodec.MaxBytes*4, RecursionLimit=24 }.Deserialize<AppState>(File.ReadAllText(file));
                    if(loaded==null || loaded.Packs==null || loaded.Favorites==null) throw new InvalidDataException();
                    foreach(var p in loaded.Packs) { PackCodec.Validate(p); p.LocalImage=null;p.Dock=false; }
                    if(loaded.AppliedPack!=null){try{PackCodec.Validate(loaded.AppliedPack);loaded.AppliedPack.LocalImage=null;loaded.AppliedPack.Dock=false;}catch{loaded.AppliedPack=null;}}
                    State=loaded;
                } catch(Exception) {
                    File.Copy(file, file+".recovered-"+DateTime.UtcNow.Ticks, false);
                    Warning="Your library could not be read. A recovery copy was saved; the starter collection is available.";
                }
            }
            if(State.DockItems==null)State.DockItems=new List<DockItem>();
            if(string.IsNullOrWhiteSpace(State.DiscordUrl))State.DiscordUrl=AppState.CommunityInvite;
            if(State.RenderQuality!="Balanced"&&State.RenderQuality!="Ultra")State.RenderQuality="Ultra";
            if(!new[]{"Studio","Focus","Night"}.Contains(State.FocusReturnMood))State.FocusReturnMood="Studio";
            DateTime focusEnd;if(!DateTime.TryParse(State.FocusEndsUtc,null,System.Globalization.DateTimeStyles.RoundtripKind,out focusEnd)||focusEnd.ToUniversalTime()<DateTime.UtcNow){if(State.FocusEndsUtc!=null&&State.AppliedPack!=null&&State.AppliedPack.Mood=="Focus")State.AppliedPack.Mood=State.FocusReturnMood;State.FocusEndsUtc=null;}
            if(State.LibraryVersion<4)State.TargetFps=60;
            if(State.TargetFps!=15 && State.TargetFps!=30 && State.TargetFps!=60)State.TargetFps=30;
            // Refresh obsolete official starters, while preserving creator remixes.
            var defaults=Defaults();
            if(State.LibraryVersion<2)foreach(var p in defaults){int index=State.Packs.FindIndex(x=>x.Id==p.Id&&x.Author=="Veyra Studio");if(index>=0)State.Packs[index]=p;}
            foreach(var p in defaults)if(!State.Packs.Any(x=>x.Id==p.Id))State.Packs.Add(p);
            foreach(var pack in State.Packs)pack.Dock=false;
            State.LibraryVersion=4;
        }
        readonly object saveLock=new object();
        public void Save() {lock(saveLock){
            string file=Path.Combine(Root,"library.json"), temp=file+".tmp";
            File.WriteAllText(temp,new JavaScriptSerializer { MaxJsonLength=PackCodec.MaxBytes*4, RecursionLimit=24 }.Serialize(State));
            if(File.Exists(file)) File.Replace(temp,file,null); else File.Move(temp,file);
        }}
        public void Add(ThemePack p) {
            PackCodec.Validate(p);
            if(State.Packs.Any(x=>x.Id==p.Id)) p.Id="local-"+Guid.NewGuid().ToString("N");
            State.Packs.Add(p); try {Save();} catch {State.Packs.Remove(p);throw;}
        }
        public string Materialize(ThemePack p) {
            PackCodec.Validate(p);
            if(p.Scene!="image") return null;
            string file=Path.Combine(Root,"images",p.Id+p.ImageExtension);
            File.WriteAllBytes(file,Convert.FromBase64String(p.ImageBase64));
            return file;
        }
        public static List<ThemePack> Defaults() {
            string[] scenes={"alpine","neon","silk","aurora","dune","orbit","bloom","tide","summit","ember","cosmos","rain","forest","mono","sakura","lagoon"};
            string[] names={"Alpine Reverie","After Hours","Liquid Silk","Aurora","Dune","Orbit","Bloom","Tide","Summit","Ember","Deep Space","Rain Room","Woodland","Monochrome","Sakura","Lagoon"};
            string[] colors={"#83DEED","#C5AAFF","#D5C8FD","#B9ED80","#E9B989","#A9AAFF","#F4A4C5","#7EDBD5","#A5C9F3","#F3AE7C","#AFAAFF","#96BCD6","#AAD4A7","#D0D4DC","#F7BDD6","#85DDD6"};
            string[] descriptions={"A luminous alpine lake beneath the northern sky. Slow mist and drifting light.","Rain-soaked streets, violet neon and a city that never quite sleeps.","Sculpted light, flowing ribbons and a little space to breathe.","Northern light above layered mountain silhouettes.","Warm sand, shifting light and long shadows.","An orbiting world in a very big sky.","Sculptural petals with a soft blush glow.","Luminous currents, gently in motion.","Blue hour above the clouds. Snow settles quietly.","Warm embers floating through the dark.","A distant nebula, a thousand little possibilities.","A quiet window on a rainy evening.","Layered woodland and wandering fireflies.","Architectural light in shades of graphite.","Petals falling through the evening sky.","Sunlight beneath a clear tropical tide."};
            var result=new List<ThemePack>();
            for(int i=0;i<names.Length;i++) result.Add(new ThemePack { SchemaVersion=1,Id="veyra-"+scenes[i],Name=names[i],Author="Veyra Studio",Description=descriptions[i],License=(i<2?"Veyra generated artwork — redistribution permitted":"CC0-1.0"),Scene=scenes[i],Accent=colors[i],Animated=true,Clock=true,LightMode=false,Motion=50,Dock=false,Effect=i==0?"Mist":i==1?"Rain":i==8?"Snow":i==12?"Fireflies":"None" });
            return result;
        }
    }
}

