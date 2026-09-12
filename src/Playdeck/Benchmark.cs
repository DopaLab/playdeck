using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Threading;
using Playdeck.Core;
namespace Playdeck;
public sealed partial class MainWindow {
 async Task Benchmark(string path){
 library.Games.Clear();sessions.Clear();compact=false;view="Library";Width=1240;Height=820;
 for(int i=0;i<2000;i++)library.Games.Add(new Game{Id="bench-"+i,Name="Game "+i.ToString("D4"),CoverPath=Path.Combine(root,"demo-art",(i%8)+".jpg"),MetadataAttempted=true});
 for(int i=0;i<20000;i++)sessions.Add(new Session{GameId=library.Games[i%2000].Id,Seconds=600+i%2000,Started=DateTimeOffset.UtcNow.AddHours(-i),Status="Completed"});
 var rendering=new List<double>();long startDecodes=imageDecodes;long allocated=GC.GetTotalAllocatedBytes(true);var wall=Stopwatch.StartNew();
 for(int i=0;i<5;i++){var timer=Stopwatch.StartNew();Render();UpdateLayout();rendering.Add(timer.Elapsed.TotalMilliseconds);await Dispatcher.InvokeAsync(()=>{},DispatcherPriority.Background);}
 var panel=content.Children.OfType<CardPanel>().Single();var scrolling=new List<double>();int maxCards=0;
 foreach(int step in Enumerable.Range(0,50).Concat(Enumerable.Range(0,50).Reverse())){var timer=Stopwatch.StartNew();libraryScroll.ScrollToVerticalOffset(step*110);UpdateLayout();scrolling.Add(timer.Elapsed.TotalMilliseconds);maxCards=Math.Max(maxCards,panel.Children.Count);await Dispatcher.InvokeAsync(()=>{},DispatcherPriority.Background);}
 var results=new {Games=2000,Sessions=20000,ScrollSteps=scrolling.Count,RenderMedianMs=rendering.Order().ElementAt(2),ScrollMedianMs=scrolling.Order().ElementAt(scrolling.Count/2),ScrollP95Ms=scrolling.Order().ElementAt((int)(scrolling.Count*.95)),DecodedImages=imageDecodes-startDecodes,AllocatedMB=(GC.GetTotalAllocatedBytes(true)-allocated)/1048576.0,MaxRealizedCards=maxCards,ElapsedSeconds=wall.Elapsed.TotalSeconds,WorkingSetMB=Process.GetCurrentProcess().WorkingSet64/1048576.0};File.WriteAllText(path,JsonSerializer.Serialize(results,Store.Json));
 }
}
