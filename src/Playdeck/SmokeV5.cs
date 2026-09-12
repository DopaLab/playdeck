using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Playdeck.Core;
namespace Playdeck;
public sealed partial class MainWindow {
 void SmokeV5(List<string> checks){void Check(string name,bool ok){if(!ok)throw new InvalidOperationException(name);checks.Add("PASS "+name);}var game=new Game{IconPath=Environment.ProcessPath!};CacheIcon(game);var icon=LoadImage(CachedIcon(game),256);Check("High-resolution executable icon cached at 256 pixels",icon!=null&&icon.PixelWidth==256);
 var fixture=new Game{Name="Trash fixture",Removed=true};library.Games.Add(fixture);dialogProbe=w=>{var body=(StackPanel)((ScrollViewer)w.Content).Content;body.Children.OfType<WrapPanel>().Single().Children.OfType<Button>().Single(b=>(string)b.Content=="Empty Trash").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));};EmptyTrash();dialogProbe=null;Check("Empty Trash removes trashed records and preserves active games",!library.Games.Contains(fixture)&&library.Games.Count>0);
 var target=new Game{Name="Uncertain title",LaunchPath="keep.exe"};matchSearchProbe=_=>System.Threading.Tasks.Task.FromResult(new List<SteamMatch>{new(1,"Similar title"),new(2,"Chosen title")});matchFetchProbe=g=>{g.Name="Chosen title";g.MetadataStatus="Steam · cached";g.CoverPath=Path.Combine(root,"external-choice.png");File.Copy(CachedIcon(game),g.CoverPath,true);return System.Threading.Tasks.Task.CompletedTask;};dialogProbe=w=>{var body=(StackPanel)((ScrollViewer)w.Content).Content;body.Children.OfType<Button>().Single(b=>(string)b.Content=="Search Steam").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Check("Search results do not change a game until selection",target.Name=="Uncertain title");CaptureWindow(w,Path.ChangeExtension(capture!,"matching.png"));var results=(StackPanel)body.Children.OfType<ScrollViewer>().Single().Content;results.Children.OfType<Button>().Last().RaiseEvent(new RoutedEventArgs(Button.ClickEvent));};ChooseSteamMatch(target);dialogProbe=null;matchSearchProbe=null;matchFetchProbe=null;Check("Selected Steam result updates name and cover while retaining launch path",target.Name=="Chosen title"&&target.SteamId==2&&File.Exists(target.CoverPath)&&target.LaunchPath=="keep.exe");
 view="Library";Width=1240;Height=820;Render();var panel=content.Children.OfType<CardPanel>().Single();Check("Comfort grid is flush left",panel.Children.Count>0&&((FrameworkElement)panel.Children[0]).TranslatePoint(new Point(0,0),panel).X==0);Check("No pagination controls",content.Children.OfType<StackPanel>().Count()==0);Capture(Path.ChangeExtension(capture!,"v5-library.png"));
 }
}
