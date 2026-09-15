using System;
using System.Linq;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Playdeck.Core;
namespace Playdeck;
static class ChartInk {
 static readonly System.Collections.Generic.Dictionary<string,SolidColorBrush> colors=[];
 public static SolidColorBrush Brush(string hex){if(colors.TryGetValue(hex,out var existing))return existing;var b=new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));b.Freeze();colors[hex]=b;return b;}
 public static readonly Typeface Body=new(new FontFamily("Segoe UI"),FontStyles.Normal,FontWeights.Normal,FontStretches.Normal);
 public static readonly Typeface Display=new(new FontFamily(new Uri("pack://application:,,,/"),"./Assets/Fonts/#Lilita One"),FontStyles.Normal,FontWeights.Normal,FontStretches.Normal);
 public static void Text(DrawingContext dc,string value,Point point,double size,string color,double dpi,bool display=false){dc.DrawText(new FormattedText(value,CultureInfo.CurrentCulture,FlowDirection.LeftToRight,display?Display:Body,size,Brush(color),dpi),point);}
}
public sealed class PlayChart:FrameworkElement {
 readonly DailyPlay[] days;readonly bool heat;int hover=-1;readonly Rect[] cells;
 public PlayChart(DailyPlay[] data,bool heatmap){days=data;heat=heatmap;cells=new Rect[data.Length];ToolTip="Recorded playtime";SnapsToDevicePixels=true;MouseMove+=Hover;MouseLeave+=(_,_)=>{hover=-1;InvalidateVisual();};}
 void Hover(object sender,MouseEventArgs args){int index=Array.FindIndex(cells,r=>r.Contains(args.GetPosition(this)));if(index==hover)return;hover=index;if(index>=0)ToolTip=$"{days[index].Date:dddd, dd MMM yyyy} · {days[index].Seconds/3600:0.##} hours";InvalidateVisual();}
 protected override void OnRender(DrawingContext dc){base.OnRender(dc);double dpi=VisualTreeHelper.GetDpi(this).PixelsPerDip;dc.DrawRectangle(Brushes.Transparent,null,new Rect(RenderSize));if(days.Length==0)return;double max=Math.Max(3600,days.Max(d=>d.Seconds));
  if(heat){int lead=((int)days[0].Date.DayOfWeek+6)%7;int cols=(days.Length+lead+6)/7;double w=(ActualWidth-35)/cols,h=(ActualHeight-28)/7;for(int row=0;row<7;row++)if(row%2==0)ChartInk.Text(dc,new[]{"M","T","W","T","F","S","S"}[row],new Point(0,row*h+2),10,"#96919F",dpi);
   for(int i=0;i<days.Length;i++){int slot=i+lead;var rect=new Rect(27+(slot/7)*w,(slot%7)*h,Math.Max(1,w-5),Math.Max(1,h-4));cells[i]=rect;double level=days[i].Seconds/max;string color=level<=0?"#39383F":level<.25?"#606F37":level<.5?"#889E3D":level<.75?"#B3CE45":"#E1FF46";dc.DrawRoundedRectangle(ChartInk.Brush(color),i==hover?new Pen(Brushes.White,1.5):null,rect,3,3);}ChartInk.Text(dc,"LESS",new Point(27,ActualHeight-19),9,"#AAA5B2",dpi);for(int i=0;i<5;i++)dc.DrawRoundedRectangle(ChartInk.Brush(new[]{"#39383F","#606F37","#889E3D","#B3CE45","#E1FF46"}[i]),null,new Rect(64+i*18,ActualHeight-18,13,10),2,2);ChartInk.Text(dc,"MORE",new Point(160,ActualHeight-19),9,"#AAA5B2",dpi);return;
  }
  max=Math.Ceiling(max/3600)*3600;double left=35,top=16,bottom=ActualHeight-29,plot=bottom-top,slotW=(ActualWidth-left)/days.Length;
  for(int line=0;line<3;line++){double y=top+plot*line/2;dc.DrawLine(new Pen(ChartInk.Brush("#3D3A44"),1),new Point(left,y),new Point(ActualWidth,y));ChartInk.Text(dc,(max*(1-line/2.0)/3600).ToString("0.#")+"h",new Point(0,y-7),10,"#98929F",dpi);}
  for(int i=0;i<days.Length;i++){double x=left+i*slotW+Math.Min(3,slotW*.12),height=Math.Max(2,days[i].Seconds/max*plot);var rect=new Rect(x,bottom-height,Math.Max(1,slotW-Math.Min(6,slotW*.24)),height);cells[i]=new Rect(left+i*slotW,top,slotW,plot+12);dc.DrawRoundedRectangle(ChartInk.Brush(i==hover?"#FF957F":i==days.Length-1?"#E1FF46":days[i].Seconds>0?"#76DCCB":"#49464F"),null,rect,Math.Min(4,slotW/4),Math.Min(4,slotW/4));if(Enumerable.Range(0,5).Any(tick=>i==(int)Math.Round((days.Length-1)*tick/4.0))){double labelX=Math.Min(ActualWidth-35,x);ChartInk.Text(dc,days[i].Date.ToString("dd MMM"),new Point(labelX,bottom+11),10,"#B2ADBB",dpi);}}
 }
}
public sealed class FocusChart:FrameworkElement {
 readonly double foreground,total;
 public FocusChart(double foreground,double total){this.foreground=foreground;this.total=total;}
 protected override void OnRender(DrawingContext dc){double dpi=VisualTreeHelper.GetDpi(this).PixelsPerDip;var center=new Point(ActualWidth/2,84);double radius=58;double ratio=total>0?Math.Clamp(foreground/total,0,1):0;var pen=new Pen(ChartInk.Brush(total>0?"#76DCCB":"#44414B"),16);dc.DrawEllipse(null,pen,center,radius,radius);
  if(ratio>=.9999)dc.DrawEllipse(null,new Pen(ChartInk.Brush("#E1FF46"),16),center,radius,radius);else if(ratio>0){double theta=ratio*Math.PI*2-Math.PI/2;var arc=new StreamGeometry();using(var g=arc.Open()){g.BeginFigure(new Point(center.X,center.Y-radius),false,false);g.ArcTo(new Point(center.X+radius*Math.Cos(theta),center.Y+radius*Math.Sin(theta)),new Size(radius,radius),0,ratio>.5,SweepDirection.Clockwise,true,false);}arc.Freeze();dc.DrawGeometry(null,new Pen(ChartInk.Brush("#E1FF46"),16){StartLineCap=PenLineCap.Round,EndLineCap=PenLineCap.Round},arc);}
  string text=total>0?ratio.ToString("P0"):"—";var ft=new FormattedText(text,CultureInfo.CurrentCulture,FlowDirection.LeftToRight,ChartInk.Display,35,ChartInk.Brush("#F3F0E9"),dpi);dc.DrawText(ft,new Point(center.X-ft.Width/2,60));ChartInk.Text(dc,"IN FOCUS",new Point(center.X-27,103),10,"#BBB5C3",dpi);double start=Math.Max(0,center.X-107);dc.DrawEllipse(ChartInk.Brush("#E1FF46"),null,new Point(start+4,187),3.5,3.5);ChartInk.Text(dc,"FOREGROUND",new Point(start+13,181),10,"#C0BAC7",dpi);dc.DrawEllipse(ChartInk.Brush("#76DCCB"),null,new Point(start+113,187),3.5,3.5);ChartInk.Text(dc,"BACKGROUND",new Point(start+122,181),10,"#C0BAC7",dpi);
 }
}
