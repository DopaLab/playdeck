using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Playdeck.Core;
namespace Playdeck;
public sealed class CardPanel:VirtualizingPanel {
 public double CardWidth{get;set;}=218;
 public IReadOnlyList<Game> Items{get;set;}=Array.Empty<Game>();
 public Func<Game,double,FrameworkElement>? Factory{get;set;}
 public Func<double,double>? HeightFactory{get;set;}
 public int ItemCount=>Items.Count;
 public int CachedCards=>cache.Count;
 public int CreatedCards{get;private set;}
 const int Capacity=72;
 readonly Dictionary<int,FrameworkElement> cache=[];
 readonly LinkedList<int> lru=[];
 readonly Dictionary<int,LinkedListNode<int>> nodes=[];
 DispatcherOperation? warming;
 double offset,viewport=820,widthNow,rowHeight,heightNow;int columns=1,first=-1,last=-1;
 public CardPanel(){Loaded+=(_,_)=>Warm();Unloaded+=(_,_)=>{warming?.Abort();warming=null;};}
 public static double HeightFor(double width)=>width*1.22+77;
 double GetHeight(double width)=>HeightFactory?.Invoke(width)??HeightFor(width);
 (int From,int To) Range(){if(rowHeight<=0)return(0,0);int rows=(Items.Count+columns-1)/columns;int from=Math.Max(0,Math.Min(rows-1,(int)(offset/rowHeight))-1)*columns;return(from,Math.Min(Items.Count,Math.Max(from+columns,((int)((offset+viewport)/rowHeight)+2)*columns)));}
 public void SetViewport(double y,double height){offset=Math.Max(0,y);viewport=Math.Max(1,height);var r=Range();if(r.From!=first||r.To!=last)InvalidateMeasure();}
 FrameworkElement GetCard(int index){
  if(cache.TryGetValue(index,out var card)){lru.Remove(nodes[index]);lru.AddLast(nodes[index]);return card;}
  card=Factory!(Items[index],widthNow);CreatedCards++;cache[index]=card;nodes[index]=lru.AddLast(index);
  while(cache.Count>Capacity){var victim=lru.First;while(victim!=null&&victim.Value>=first&&victim.Value<last)victim=victim.Next;if(victim==null)break;cache.Remove(victim.Value);nodes.Remove(victim.Value);lru.Remove(victim);}
  return card;
 }
 void Warm(){
  if(!IsLoaded||warming?.Status==DispatcherOperationStatus.Pending)return;
  warming=Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle,new Action(()=>{
   warming=null;if(!IsLoaded||Factory==null)return;
   int candidate=Enumerable.Range(last,Math.Min(columns*2,Math.Max(0,Items.Count-last))).Concat(Enumerable.Range(Math.Max(0,first-columns*2),Math.Min(first,columns*2))).Take(Math.Max(0,Capacity-(last-first))).FirstOrDefault(i=>!cache.ContainsKey(i),-1);
   if(candidate<0)return;var card=GetCard(candidate);card.Measure(new Size(widthNow,heightNow));Warm();
  }));
 }
 protected override Size MeasureOverride(Size available){
  double width=double.IsInfinity(available.Width)?1100:Math.Max(1,available.Width);columns=Math.Max(1,(int)Math.Floor((width+18)/(CardWidth+18)));double actual=(width-18*(columns-1))/columns;double h=GetHeight(actual);rowHeight=h+18;
  var (from,to)=Range();
  if(Math.Abs(widthNow-actual)>.1||Math.Abs(heightNow-h)>.1){warming?.Abort();warming=null;cache.Clear();nodes.Clear();lru.Clear();RemoveInternalChildRange(0,InternalChildren.Count);first=last=from;widthNow=actual;heightNow=h;}
  if(first<0||from>=last||to<=first){RemoveInternalChildRange(0,InternalChildren.Count);first=last=from;}
  while(first<from&&InternalChildren.Count>0){RemoveInternalChildRange(0,1);first++;}
  while(last>to&&InternalChildren.Count>0){RemoveInternalChildRange(InternalChildren.Count-1,1);last--;}
  while(first>from){first--;if(Factory!=null)InsertInternalChild(0,GetCard(first));}
  while(last<to){int index=last++;if(Factory!=null)AddInternalChild(GetCard(index));}
  foreach(UIElement child in InternalChildren)child.Measure(new Size(actual,h));Warm();int rows=(Items.Count+columns-1)/columns;return new Size(width,Math.Max(0,rows*rowHeight-18));
 }
 protected override Size ArrangeOverride(Size final){for(int i=0;i<InternalChildren.Count;i++){int index=first+i;InternalChildren[i].Arrange(new Rect((index%columns)*(widthNow+18),(index/columns)*rowHeight,widthNow,heightNow));}return final;}
}
