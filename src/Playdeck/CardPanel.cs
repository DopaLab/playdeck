using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Playdeck.Core;
namespace Playdeck;
public sealed class CardPanel:VirtualizingPanel {
 public double CardWidth{get;set;}=218;
 public IReadOnlyList<Game> Items{get;set;}=Array.Empty<Game>();
 public Func<Game,double,FrameworkElement>? Factory{get;set;}
 public Func<double,double>? HeightFactory{get;set;}
 double GetHeight(double width)=>HeightFactory?.Invoke(width)??HeightFor(width);
 public int ItemCount=>Items.Count;
 double offset,viewport=820,widthNow,rowHeight;int columns=1,first=-1,last=-1;
 public static double HeightFor(double width)=>width*1.22+77;
 public void SetViewport(double y,double height){offset=Math.Max(0,y);viewport=Math.Max(1,height);InvalidateMeasure();}
 protected override Size MeasureOverride(Size available){double width=double.IsInfinity(available.Width)?1100:Math.Max(1,available.Width);columns=Math.Max(1,(int)Math.Floor((width+18)/(CardWidth+18)));double actual=(width-18*(columns-1))/columns;rowHeight=GetHeight(actual)+18;int rows=(Items.Count+columns-1)/columns;int from=Math.Max(0,Math.Min(rows-1,(int)(offset/rowHeight))-1)*columns;int to=Math.Min(Items.Count,Math.Max(from+columns,((int)((offset+viewport)/rowHeight)+2)*columns));if(first<0||Math.Abs(widthNow-actual)>.1||from>=last||to<=first){RemoveInternalChildRange(0,InternalChildren.Count);first=from;last=from;widthNow=actual;}
 // Retain the overlapping rows. Only cards entering the viewport need construction.
 while(first<from&&InternalChildren.Count>0){RemoveInternalChildRange(0,1);first++;}
 while(last>to&&InternalChildren.Count>0){RemoveInternalChildRange(InternalChildren.Count-1,1);last--;}
 while(first>from){first--;if(Factory!=null)InsertInternalChild(0,Factory(Items[first],actual));}
 while(last<to){if(Factory!=null)AddInternalChild(Factory(Items[last],actual));last++;}foreach(UIElement child in InternalChildren)child.Measure(new Size(actual,GetHeight(actual)));return new Size(width,Math.Max(0,rows*rowHeight-18));}
 protected override Size ArrangeOverride(Size final){for(int i=0;i<InternalChildren.Count;i++){int index=first+i;InternalChildren[i].Arrange(new Rect((index%columns)*(widthNow+18),(index/columns)*rowHeight,widthNow,GetHeight(widthNow)));}return final;}
}
