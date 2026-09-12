using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Playdeck.Core;
namespace Playdeck;
public sealed partial class MainWindow {
 void SmokeV4(List<string> checks){
 void Check(string label,bool ok){if(!ok)throw new InvalidOperationException(label);checks.Add("PASS "+label);}
 var typeface=new System.Windows.Media.Typeface(DisplayFont,FontStyles.Normal,FontWeights.Normal,FontStretches.Normal);Check("Bundled display font resolves without system installation",typeface.TryGetGlyphTypeface(out var glyph)&&glyph.FontUri.ToString().Contains("LilitaOne",StringComparison.OrdinalIgnoreCase));
 Check("No folder watchers in direct-import build",!GetType().GetFields(System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Any(f=>f.FieldType==typeof(List<FileSystemWatcher>)));
 var quoted=ExplorerIntegration.Command(@"C:\A folder\Playdeck.exe");Check("Explorer command quotes executable and selected path",quoted=="\"C:\\A folder\\Playdeck.exe\" --add \"%1\"");
 string ipcRoot=Path.Combine(root,Guid.NewGuid().ToString("N"));using(var stop=new CancellationTokenSource()){string[]? received=null;var listener=Task.Run(()=>ImportTransport.Listen(ipcRoot,files=>received=files,stop.Token));Task.Run(()=>ImportTransport.Send(ipcRoot,new[]{@"C:\A game folder\game.exe"})).GetAwaiter().GetResult();Check("Running-instance import preserves paths with spaces",received?.Single()==@"C:\A game folder\game.exe");stop.Cancel();listener.GetAwaiter().GetResult();}
 int before=library.Games.Count;dialogProbe=w=>{var body=(StackPanel)((ScrollViewer)w.Content).Content;var name=body.Children.OfType<TextBox>().Single();name.Text="A reviewed import";CaptureWindow(w,Path.ChangeExtension(capture!,"add.png"));body.Children.OfType<WrapPanel>().Single().Children.OfType<Button>().Single(b=>(string)b.Content=="Add to library").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));};QueueImports(new[]{Environment.ProcessPath!});dialogProbe=null;Check("Direct import adds the reviewed title without executing it",library.Games.Count==before+1&&library.Games.Last().Name=="A reviewed import");QueueImports(new[]{Environment.ProcessPath!});Check("Duplicate direct import does not create another card",library.Games.Count==before+1);library.Games.RemoveAt(library.Games.Count-1);search.Text="";
 dialogProbe=w=>{var body=(StackPanel)((ScrollViewer)w.Content).Content;body.Children.OfType<TextBox>().Single().Text="Attract";CaptureWindow(w,Path.ChangeExtension(capture!,"profile.png"));body.Children.OfType<WrapPanel>().Single().Children.OfType<Button>().Single(b=>(string)b.Content=="Save profile").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));};EditProfile();dialogProbe=null;Check("Profile saves username",library.UserName=="Attract");
 view="Library";Width=1240;Height=820;Render();Capture(Path.ChangeExtension(capture!,"final-fallback.png"));Width=960;Height=640;Render();Capture(Path.ChangeExtension(capture!,"final-small.png"));Check("Navigation has six usable tabs",navigation.Count==6&&navigation.Values.All(b=>b.ActualWidth>60));
 }
}
