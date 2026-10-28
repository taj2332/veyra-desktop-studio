using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
namespace Veyra {
 public static class Brand {
  public static DrawingImage Mark(string color){var group=new DrawingGroup();using(var d=group.Open()){d.DrawGeometry(new SolidColorBrush(SceneView.ColorOf(color)),null,Geometry.Parse("M 13,14 L 26,14 L 49,56 L 72,14 L 85,14 L 49,78 Z M 39,14 L 49,32 L 59,14 Z"));}group.Freeze();return new DrawingImage(group);}
  public static void Export(string folder){Directory.CreateDirectory(folder);Render(Path.Combine(folder,"Veyra-Discord-1024.png"),1024,false);Render(Path.Combine(folder,"Veyra-Mark-Transparent-1024.png"),1024,true);
   File.WriteAllText(Path.Combine(folder,"Veyra-Mark.svg"),"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 100 100\"><path fill=\"#B9ED80\" d=\"M13 14h13l23 42 23-42h13L49 78zM39 14l10 18 10-18z\"/></svg>");
  }
  static void Render(string path,int size,bool transparent){var visual=new DrawingVisual();using(var d=visual.RenderOpen()){if(!transparent){var bg=new LinearGradientBrush(SceneView.ColorOf("#182231"),SceneView.ColorOf("#090C12"),60);d.DrawRectangle(bg,null,new Rect(0,0,size,size));var glow=new RadialGradientBrush(Color.FromArgb(48,185,237,128),Colors.Transparent);d.DrawEllipse(glow,null,new Point(size*.5,size*.48),size*.54,size*.54);}
    d.DrawImage(Mark("#B9ED80"),new Rect(size*.17,size*.20,size*.66,size*.66));}
   var b=new RenderTargetBitmap(size,size,96,96,PixelFormats.Pbgra32);b.Render(visual);var png=new PngBitmapEncoder();png.Frames.Add(BitmapFrame.Create(b));using(var output=File.Create(path))png.Save(output);
  }
 }
}
