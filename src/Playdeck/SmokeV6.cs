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
 int created=p.CreatedCards;p.SetViewport(0,200);p.Measure(new Size(340,double.PositiveInfinity));Check("Returning to recently visited rows reuses controls",p.CreatedCards==created);
 for(int i=0;i<40;i++){p.SetViewport(i*118,200);p.Measure(new Size(340,double.PositiveInfinity));}Check("Card control cache stays bounded",p.CachedCards<=72);
 view="Activity";activityDays=7;Render();UpdateLayout();Check("Activity has interactive time ranges and charts",content.Children.Count>=6);Capture(System.IO.Path.ChangeExtension(capture!,"pulse-week.png"));activityDays=90;Render();UpdateLayout();Capture(System.IO.Path.ChangeExtension(capture!,"pulse-quarter.png"));activityDays=30;libraryScroll.ScrollToVerticalOffset(640);UpdateLayout();Capture(System.IO.Path.ChangeExtension(capture!,"pulse-details.png"));libraryScroll.ScrollToTop();
 var versionGame=new Game{Name="Version fixture",VersionInfo=new(){Mode="Manual",ManualVersion="1.9",ManualLatest="1.10"}};dialogProbe=window=>{var body=(StackPanel)((ScrollViewer)window.Content).Content;body.Children.OfType<WrapPanel>().Last().Children.OfType<Button>().Single(b=>(string)b.Content=="Save locally").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));};VersionDialog(versionGame);dialogProbe=null;Check("Manual version editor saves comparable evidence without network",versionGame.VersionInfo.Installed=="1.9"&&versionGame.VersionInfo.NeedsUpdate);dialogProbe=window=>{CaptureWindow(window,System.IO.Path.ChangeExtension(capture!,"version.png"));window.Close();};VersionDialog(versionGame);dialogProbe=null;
 var g=new Game{IconPath=Environment.ProcessPath!};CacheIcon(g);var a=LoadImage(CachedIcon(g),64);Check("Decoded images are reused and frozen",a!=null&&a.IsFrozen&&ReferenceEquals(a,LoadImage(CachedIcon(g),64)));
 for(int i=1;i<=170;i++)LoadImage(CachedIcon(g),i);Check("Decoded image cache has an enforced memory and entry bound",imageCache.Count<=160&&imageBytes<=48*1024*1024);
 }
}
