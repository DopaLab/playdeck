using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Playdeck.Core;
namespace Playdeck;
public sealed partial class MainWindow {
 void SmokeV6(List<string> checks){
 void Check(string name,bool ok){if(!ok)throw new InvalidOperationException(name);checks.Add("PASS "+name);}
 var p=new CardPanel{CardWidth=100,Items=Enumerable.Range(0,100).Select(i=>new Game()).ToArray(),HeightFactory=_=>100,Factory=(_,_)=>new Border()};
 p.SetViewport(0,200);p.Measure(new Size(340,double.PositiveInfinity));var retained=p.Children[6];p.SetViewport(236,200);p.Measure(new Size(340,double.PositiveInfinity));Check("Scrolling preserves overlapping card instances",p.Children.Contains(retained));Check("Virtualization keeps realized cards bounded",p.Children.Count<25);
 var g=new Game{IconPath=Environment.ProcessPath!};CacheIcon(g);var a=LoadImage(CachedIcon(g),64);Check("Decoded images are reused and frozen",a!=null&&a.IsFrozen&&ReferenceEquals(a,LoadImage(CachedIcon(g),64)));
 for(int i=1;i<=170;i++)LoadImage(CachedIcon(g),i);Check("Decoded image cache has an enforced memory and entry bound",imageCache.Count<=160&&imageBytes<=48*1024*1024);
 }
}
