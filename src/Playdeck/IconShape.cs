using System;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
namespace Playdeck;
public static class IconShape {
 sealed class Shape {public bool Square;}
 static readonly ConditionalWeakTable<ImageSource,Shape> cache=new();
 public static bool IsOpaqueSquare(ImageSource image)=>cache.GetValue(image,Classify).Square;
 static Shape Classify(ImageSource image){try{if(image is not BitmapSource bitmap||bitmap.PixelWidth!=bitmap.PixelHeight||bitmap.PixelWidth<4)return new();var pixels=bitmap.Format==PixelFormats.Bgra32?bitmap:new FormatConvertedBitmap(bitmap,PixelFormats.Bgra32,null,0);var data=new byte[4];int n=bitmap.PixelWidth;foreach(var p in new[]{new Int32Rect(0,0,1,1),new Int32Rect(n-1,0,1,1),new Int32Rect(0,n-1,1,1),new Int32Rect(n-1,n-1,1,1)}){pixels.CopyPixels(p,data,4,0);if(data[3]<240)return new();}return new(){Square=true};}catch{return new();}}
 public static void Draw(DrawingContext dc,ImageSource image,Rect rect){bool rounded=IsOpaqueSquare(image);if(rounded)dc.PushClip(new RectangleGeometry(rect,rect.Width*.2,rect.Height*.2));dc.DrawImage(image,rect);if(rounded)dc.Pop();}
}
