using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Playdeck.Core;
namespace Playdeck;
public sealed partial class MainWindow {
 void SmokeV2(List<string> checks){
  void Check(string name,bool ok){if(!ok)throw new InvalidOperationException("UI smoke failed: "+name);checks.Add("PASS "+name);}
  view="Pinned";search.Text="";Render();Check("Pinned view has no sort control",sort.Parent==null);var pinned=library.Games.Where(g=>g.Favorite).OrderBy(g=>g.PinOrder).First();sort.SelectedIndex=5;Render();Check("Pinned order is independent of library sort",System.Windows.Automation.AutomationProperties.GetName(content.Children.OfType<CardPanel>().Single().Children[0])=="Play "+pinned.Name);
  int launched=0,edited=0;launchProbe=_=>launched++;editingProbe=_=>edited++;var card=(Border)Card(pinned);var editor=((Grid)card.Child).Children.OfType<Button>().Single();
  card.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice,0,MouseButton.Left){RoutedEvent=UIElement.PreviewMouseLeftButtonDownEvent,Source=card});card.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice,1,MouseButton.Left){RoutedEvent=UIElement.MouseLeftButtonUpEvent,Source=card});Check("Card click routes to launch",launched==1);
  editor.RaiseEvent(new RoutedEventArgs(Button.ClickEvent,editor));Check("Three-dot button routes only to editor",edited==1&&launched==1);Check("Editor child is excluded from card input",InsideButton(editor));launchProbe=null;editingProbe=null;
  var coverGame=new Game();var image=BitmapSource.Create(2000,1000,96,96,PixelFormats.Bgra32,null,new byte[2000*1000*4],8000);SaveCover(coverGame,image);var loaded=new BitmapImage(new Uri(coverGame.CoverPath));Check("Pasted artwork is cached and bounded to 1400 pixels",loaded.PixelWidth==1400&&loaded.PixelHeight==700&&coverGame.MetadataAttempted);
  view="Library";Render();var g=library.Games[0];g.Removed=true;Render();Check("Removed card is absent from library",content.Children.OfType<CardPanel>().Single().Children.Count==library.Games.Count(x=>!x.Removed&&!x.Archived));g.Removed=false;
  dialogProbe=w=>{var body=(StackPanel)((ScrollViewer)w.Content).Content;Check("Paste and remove actions are at top of editor",body.Children[1] is WrapPanel);CaptureWindow(w,System.IO.Path.ChangeExtension(capture!,"editor.png"));w.Close();};Edit(g);dialogProbe=null;
 }
}
