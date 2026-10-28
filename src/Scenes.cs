using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Veyra {
 public static class Assets {
  static readonly Dictionary<string,BitmapImage> images=new Dictionary<string,BitmapImage>();
  public static byte[] Bytes(string scene) {
   if(scene!="alpine" && scene!="neon")throw new InvalidDataException("Unknown bundled artwork.");
   using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("Veyra.Art."+scene+".png")){
    if(stream==null)throw new FileNotFoundException("The bundled artwork is missing. Rebuild Veyra with its assets.");
    using(var output=new MemoryStream()){stream.CopyTo(output);return output.ToArray();}
   }
  }
  public static BitmapImage Image(string scene){lock(images){BitmapImage b;if(!images.TryGetValue(scene,out b)){b=Decode(Bytes(scene));images.Add(scene,b);}return b;}}
  public static BitmapImage Thumbnail(string scene){lock(images){string key="thumbnail-"+scene;BitmapImage b;if(images.TryGetValue(key,out b))return b;using(var input=typeof(Assets).Assembly.GetManifestResourceStream("Veyra.Preview."+scene+".png")){if(input==null)return null;using(var buffer=new MemoryStream()){input.CopyTo(buffer);b=Decode(buffer.ToArray());images[key]=b;return b;}}}}
  public static BitmapImage Decode(byte[] data){var b=new BitmapImage();b.BeginInit();b.CacheOption=BitmapCacheOption.OnLoad;b.StreamSource=new MemoryStream(data);b.EndInit();b.Freeze();return b;}
 }
 public sealed class SceneView : FrameworkElement {
  public ThemePack Pack;
  public double Phase;
  public bool ShowClock;
  public bool UseThumbnail;
  BitmapImage photo;
  string photoData;
  static readonly Dictionary<string,SolidColorBrush> brushes=new Dictionary<string,SolidColorBrush>();
  static readonly Dictionary<string,DrawingGroup> stars=new Dictionary<string,DrawingGroup>();
  public SceneView(ThemePack p){Pack=p;ClipToBounds=true;SnapsToDevicePixels=true;}
  public static Color ColorOf(string value){return (Color)ColorConverter.ConvertFromString(value);}
  static SolidColorBrush B(string c){lock(brushes){SolidColorBrush b;if(!brushes.TryGetValue(c,out b)){b=new SolidColorBrush(ColorOf(c));b.Freeze();if(brushes.Count<300)brushes.Add(c,b);}return b;}}
  static SolidColorBrush A(Color c,double alpha){return new SolidColorBrush(Color.FromArgb((byte)(255*Math.Max(0,Math.Min(1,alpha))),c.R,c.G,c.B));}
  static LinearGradientBrush G(string a,string b,double angle){var g=new LinearGradientBrush(ColorOf(a),ColorOf(b),angle);g.Freeze();return g;}
  static void Poly(DrawingContext d,Brush b,params double[] xy){var g=new StreamGeometry();using(var c=g.Open()){c.BeginFigure(new Point(xy[0],xy[1]),true,true);for(int i=2;i<xy.Length;i+=2)c.LineTo(new Point(xy[i],xy[i+1]),true,false);}g.Freeze();d.DrawGeometry(b,null,g);}
  static void Ellipse(DrawingContext d,Brush b,double x,double y,double rx,double ry){d.DrawEllipse(b,null,new Point(x,y),rx,ry);}
  static void Glow(DrawingContext d,Color color,double opacity,double x,double y,double rx,double ry){var g=new RadialGradientBrush(Color.FromArgb((byte)(opacity*255),color.R,color.G,color.B),Colors.Transparent);Ellipse(d,g,x,y,rx,ry);}
  static void Stars(DrawingContext d,int seed){DrawingGroup group;lock(stars){string key=seed.ToString();if(!stars.TryGetValue(key,out group)){group=new DrawingGroup();using(var c=group.Open()){var r=new Random(seed);for(int i=0;i<160;i++){c.PushOpacity(.15+r.NextDouble()*.65);double x=r.NextDouble()*1200,y=r.NextDouble()*650,sz=.45+r.NextDouble()*1.2;Ellipse(c,Brushes.White,x,y,sz,sz);c.Pop();}}group.Freeze();stars.Add(key,group);}}d.DrawDrawing(group);}
  static void Curve(DrawingContext d,Pen pen,Point start,Point a,Point b,Point end){var g=new StreamGeometry();using(var c=g.Open()){c.BeginFigure(start,false,false);c.BezierTo(a,b,end,true,false);}d.DrawGeometry(null,pen,g);}
  void Image(DrawingContext d,BitmapImage image,double t){double zoom=Pack.Animated?1.035+Math.Sin(t*.18)*.018:1;double s=Math.Max(1200/image.Width,750/image.Height)*zoom,w=image.Width*s,h=image.Height*s;d.DrawImage(image,new Rect((1200-w)/2+Math.Sin(t*.14)*7,(750-h)/2,w,h));}
  protected override void OnRender(DrawingContext d){
   base.OnRender(d);if(Pack==null || ActualWidth<=0 || ActualHeight<=0)return;
   if(UseThumbnail){var thumbnail=Assets.Thumbnail(Pack.Scene);if(thumbnail!=null){double factor=Math.Max(ActualWidth/thumbnail.PixelWidth,ActualHeight/thumbnail.PixelHeight);d.DrawImage(thumbnail,new Rect((ActualWidth-thumbnail.PixelWidth*factor)/2,(ActualHeight-thumbnail.PixelHeight*factor)/2,thumbnail.PixelWidth*factor,thumbnail.PixelHeight*factor));return;}}
   double t=Pack.Animated?Phase:0;Color accent=ColorOf(Pack.Accent);double scale=Math.Max(ActualWidth/1200,ActualHeight/750);
   d.PushTransform(new TranslateTransform((ActualWidth-1200*scale)/2,(ActualHeight-750*scale)/2));d.PushTransform(new ScaleTransform(scale,scale));
   double drift=Math.Sin(t*.55)*38;
   switch(Pack.Scene){
    case "alpine":case "neon":
     Image(d,Assets.Image(Pack.Scene),t);
     if(Pack.Scene=="alpine"){for(int i=0;i<3;i++){d.PushTransform(new RotateTransform(-18,650,160));Glow(d,accent,.12,400+i*220+Math.Sin(t*.6+i)*80,110+i*27,260,35+10*Math.Sin(t+i));d.Pop();}}
     else Glow(d,accent,.14+.05*Math.Sin(t*1.5),930,390,320,400);break;
    case "image":
     if(photo==null || !object.ReferenceEquals(photoData,Pack.ImageBase64)){photo=Assets.Decode(Convert.FromBase64String(Pack.ImageBase64));photoData=Pack.ImageBase64;}
     Image(d,photo,t);break;
    case "aurora":
     d.DrawRectangle(G("#06151F","#183C41",90),null,new Rect(0,0,1200,750));Stars(d,51);
     for(int i=0;i<6;i++){d.PushTransform(new RotateTransform(-30,600,240));Glow(d,i%2==0?accent:ColorOf("#6DABCF"),.17,220+i*160+Math.Sin(t*.6+i*.5)*65,155+i*24,350,45+Math.Sin(t+i)*12);d.Pop();}
     Ellipse(d,B("#EEEAD5"),925,158,29,29);Ellipse(d,B("#142E38"),937,148,28,28);
     Poly(d,B("#2C4D51"),0,550,150,382,270,470,435,310,610,455,790,357,1000,502,1200,390,1200,750,0,750);
     Poly(d,B("#182F3A"),0,630,190,490,320,575,525,440,720,580,990,420,1200,575,1200,750,0,750);
     Poly(d,B("#0B1C29"),0,660,210,615,400,685,650,568,890,650,1060,590,1200,640,1200,750,0,750);break;
    case "dune":
     d.DrawRectangle(G("#936471","#EDBE96",90),null,new Rect(0,0,1200,750));Glow(d,ColorOf("#FFE4B8"),.34,850,230,250,220);Ellipse(d,B("#FFE4B8"),850,210,70,70);
     for(int i=0;i<5;i++){var g=new StreamGeometry();using(var c=g.Open()){c.BeginFigure(new Point(-100,490+i*62),true,true);c.BezierTo(new Point(260,340+i*55+Math.Sin(t*.25+i)*15),new Point(660,550+i*45),new Point(1300,410+i*80),true,false);c.LineTo(new Point(1300,800),true,false);c.LineTo(new Point(-100,800),true,false);}string[] a={"#DFAC88","#E6B88D","#CE9B78","#B67F69","#8E6159"};d.DrawGeometry(G(a[i],"#815A58",90),null,g);}break;
    case "orbit":
     d.DrawRectangle(G("#0A0F26","#252245",35),null,new Rect(0,0,1200,750));Stars(d,7);Glow(d,accent,.28,690,355,470,350);
     d.PushTransform(new RotateTransform(-24+Math.Sin(t*.24)*8,680,375));d.DrawEllipse(null,new Pen(A(accent,.25),13),new Point(680,375),290,65);d.Pop();
     Ellipse(d,G("#B9B8D8","#252C51",35),680,375,155,155);
     d.PushTransform(new RotateTransform(-24+Math.Sin(t*.24)*8,680,375));Curve(d,new Pen(A(accent,.8),5),new Point(390,375),new Point(400,460),new Point(960,460),new Point(970,375));d.Pop();
     Ellipse(d,B("#EBD9D5"),680+Math.Cos(t*.45)*350,375+Math.Sin(t*.45)*180,8,8);break;
    case "bloom":
     d.DrawRectangle(G("#211A2B","#64445B",65),null,new Rect(0,0,1200,750));
     for(int i=11;i>=0;i--){d.PushTransform(new RotateTransform(i*30+t*2.5,630,380));Ellipse(d,G(i%2==0?"#E3B4C9":"#B57EA4","#4F3654",90),630,275,62+i*5,160+Math.Sin(t*.7+i*.3)*12);d.Pop();}Ellipse(d,G("#F5CBC8","#996A89",110),630,380,67,67);break;
    case "tide":case "lagoon":
     bool lagoon=Pack.Scene=="lagoon";d.DrawRectangle(G(lagoon?"#0B505E":"#0C2038",lagoon?"#388F98":"#28586A",65),null,new Rect(0,0,1200,750));
     for(int i=0;i<14;i++){var wave=new StreamGeometry();using(var c=wave.Open()){c.BeginFigure(new Point(-100,180+i*48),false,false);c.BezierTo(new Point(300,20+i*50+Math.Sin(t+i*.4)*45),new Point(760,610+i*14+Math.Cos(t*.7)*35),new Point(1300,180+i*52),true,false);}d.DrawGeometry(null,new Pen(A(accent,.12+i*.025),i%4==0?3:1.3),wave);}Glow(d,accent,.24,700+drift,270,530,300);break;
    case "summit":
     d.DrawRectangle(G("#263B5A","#9EB3C9",90),null,new Rect(0,0,1200,750));Ellipse(d,B("#EBE7DC"),875,180,44,44);
     Poly(d,B("#7D94AC"),0,600,180,430,330,510,610,220,800,470,970,330,1200,550,1200,750,0,750);Poly(d,B("#DEE5E9"),494,351,610,220,710,351,622,322,592,285,565,336);
     Poly(d,B("#455F7B"),0,680,190,510,370,665,620,415,790,595,1040,450,1200,620,1200,750,0,750);Poly(d,B("#203A54"),0,750,210,650,390,720,650,600,840,710,1070,620,1200,705,1200,750);break;
    case "silk":case "mono":
     bool mono=Pack.Scene=="mono";d.DrawRectangle(G(mono?"#11151C":"#16152A",mono?"#343B45":"#35324E",45),null,new Rect(0,0,1200,750));Glow(d,accent,.18,820,210,500,450);
     for(int i=0;i<22;i++){double y=100+i*20;Curve(d,new Pen(A(accent,.1+i*.018),i%4==0?3:1),new Point(-120,y+260),new Point(320+drift,y-310),new Point(700-drift*2,y+510),new Point(1340,y-180));}break;
    case "ember":d.DrawRectangle(G("#120F16","#3D2225",90),null,new Rect(0,0,1200,750));Glow(d,accent,.35,650,760,650,450);break;
    case "cosmos":
     d.DrawRectangle(G("#090D20","#231E40",25),null,new Rect(0,0,1200,750));Stars(d,64);for(int i=0;i<6;i++)Glow(d,i%2==0?accent:ColorOf("#387B9B"),.15,450+Math.Cos(i+t*.15)*280,300+Math.Sin(i*2+t*.12)*140,300,190);break;
    case "rain":
     d.DrawRectangle(G("#111A2B","#345166",90),null,new Rect(0,0,1200,750));for(int i=0;i<20;i++)Glow(d,i%3==0?ColorOf("#E5B67F"):accent,.22,(i*137)%1200,400+(i*67)%300,28+(i%3)*12,60+(i%4)*30);d.DrawRectangle(A(ColorOf("#091423"),.35),null,new Rect(0,0,1200,750));break;
    case "forest":
     d.DrawRectangle(G("#111F2B","#4A6E66",90),null,new Rect(0,0,1200,750));Glow(d,accent,.23,650,180,400,300);
     for(int layer=0;layer<3;layer++)for(int i=0;i<19;i++){double x=i*78-65+layer*22,y=380+layer*90+(i*37%100),height=160+layer*35+(i*47%130);string[] shade={"#36544F","#213C3B","#102A2D"};Poly(d,B(shade[layer]),x,y-height,x-48-layer*9,y+90,x-26,y+60,x-63-layer*7,y+145,x+64+layer*7,y+145,x+25,y+60,x+49+layer*9,y+90);d.DrawRectangle(B(shade[layer]),null,new Rect(x-5,y+80,10,750-y));}break;
    case "sakura":
     d.DrawRectangle(G("#30273F","#9A7187",90),null,new Rect(0,0,1200,750));Ellipse(d,B("#EAD7CD"),830,200,68,68);Glow(d,accent,.2,830,200,230,230);
     Curve(d,new Pen(B("#2C2638"),15),new Point(0,180),new Point(350,390),new Point(430,100),new Point(760,210));Curve(d,new Pen(B("#2C2638"),7),new Point(320,240),new Point(430,150),new Point(430,140),new Point(480,70));break;
   }
   DrawEffects(d,t,accent);d.Pop();d.Pop();
   if(ShowClock && Pack.Clock)DrawClock(d,ActualWidth,ActualHeight);
  }
  void DrawEffects(DrawingContext d,double t,Color accent){
   string effect=Pack.Effect;if(Pack.Scene=="neon" || Pack.Scene=="rain")effect="Rain";if(Pack.Scene=="forest")effect="Fireflies";
   if(effect=="Mist")for(int i=0;i<5;i++)Glow(d,ColorOf("#C9EBF1"),.065,((i*320+t*27)%1800)-300,475+Math.Sin(t*.3+i)*25,380,60);
   bool rain=effect=="Rain",snow=effect=="Snow",fire=effect=="Fireflies",ember=Pack.Scene=="ember",petals=Pack.Scene=="sakura",space=Pack.Scene=="cosmos";
   if(!rain && !snow && !fire && !ember && !petals && !space)return;
   var random=new Random(147);int count=rain?110:snow?80:55;
   for(int i=0;i<count;i++){
    double x0=random.NextDouble()*1400-100,y0=random.NextDouble()*900,speed=.5+random.NextDouble(),size=.8+random.NextDouble()*2;
    if(rain){double y=(y0+t*210*speed)%900-70,x=x0-y*.14;d.DrawLine(new Pen(A(ColorOf("#CDDFEC"),.12+size*.035),.55+size*.25),new Point(x,y),new Point(x-6,y+22+size*5));}
    else if(snow){double y=(y0+t*32*speed)%850-40;Ellipse(d,A(Colors.White,.25+size*.12),x0+Math.Sin(t*.5+i)*24,y,size,size);}
    else if(petals){double y=(y0+t*38*speed)%850-40,x=x0+Math.Sin(t*.4+i)*55;d.PushTransform(new RotateTransform(i*31+t*20,x,y));Ellipse(d,A(accent,.4+size*.13),x,y,size*2.5,size);d.Pop();}
    else {double y=ember?750-((y0+t*48*speed)%850):y0+Math.Sin(t*.5+i)*30,x=x0+Math.Sin(t*.3+i)*28,alpha=.15+.65*(.5+.5*Math.Sin(t*1.7+i));Glow(d,accent,alpha*.45,x,y,size*5,size*5);Ellipse(d,A(accent,alpha),x,y,size*.6,size*.6);}
   }
  }
  public void DrawClock(DrawingContext d,double w,double h){
   double unit=Math.Max(.15,Math.Min(w/1200,h/750)),font=80*unit*(Pack.ClockSize/100.0),x=w*Pack.ClockX/100.0,y=h*Pack.ClockY/100.0;
   bool editorial=Pack.ClockStyle=="Editorial",minimal=Pack.ClockStyle=="Minimal";
   var text=new FormattedText(DateTime.Now.ToString("HH:mm"),CultureInfo.InvariantCulture,FlowDirection.LeftToRight,new Typeface(minimal?"Segoe UI Light":"Segoe UI Semibold"),minimal?font*1.05:font,Brushes.White,1.0);
   var day=new FormattedText(DateTime.Now.ToString(editorial?"dddd":"ddd, d MMM",CultureInfo.InvariantCulture).ToUpperInvariant(),CultureInfo.InvariantCulture,FlowDirection.LeftToRight,new Typeface("Segoe UI Semibold"),font*(editorial?.2:.14),A(ColorOf(Pack.Accent),.95),1.0);
   double totalW=Math.Max(text.Width,day.Width),left=Math.Max(12,Math.Min(w-totalW-12,x-totalW/2)),totalH=text.Height+day.Height+font*.2,top=Math.Max(12,Math.Min(h-totalH-12,y));
   if(editorial){d.DrawText(day,new Point(left+(totalW-day.Width)/2,top));d.DrawText(text,new Point(left+(totalW-text.Width)/2,top+day.Height+font*.08));}
   else{d.DrawText(text,new Point(left+(totalW-text.Width)/2,top));d.DrawText(day,new Point(left+(totalW-day.Width)/2,top+text.Height+font*.05));}
  }
  public static void SavePng(ThemePack p,string file,int width,int height){SaveFrame(p,file,width,height,0,false);}
  public static void SaveFrame(ThemePack p,string file,int width,int height,double phase,bool clock){
   var view=new SceneView(p){ShowClock=clock,Phase=phase,Width=width,Height=height};view.Measure(new Size(width,height));view.Arrange(new Rect(0,0,width,height));view.UpdateLayout();var bitmap=new RenderTargetBitmap(width,height,96,96,PixelFormats.Pbgra32);bitmap.Render(view);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));using(var output=File.Create(file))encoder.Save(output);
  }
 }
}
