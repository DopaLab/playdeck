using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Playdeck.Core;
namespace Playdeck;
public sealed partial class MainWindow {
 void SmokeV3(List<string> checks){
 void Check(string name,bool ok){if(!ok)throw new InvalidOperationException(name);checks.Add("PASS "+name);}
 var game=library.Games[0];LibraryActions.Pin(library,game,true);LibraryActions.Remove(library,game);Check("Trash preserves card and prior pin",game.Removed&&game.TrashedAt.HasValue&&game.PinBeforeTrash);LibraryActions.Restore(library,game);Check("Trash restores pin and reimport eligibility",!game.Removed&&game.Favorite&&!library.Ignored.Contains(game.LaunchPath));
 var test=new Library();var archived=new Game{Archived=true,Name="Retained"};var junk=new Game{TrashedAt=DateTimeOffset.UtcNow.AddDays(-8),Removed=true};test.Games.Add(archived);test.Games.Add(junk);LibraryActions.ExpireTrash(test,DateTimeOffset.UtcNow);Check("Trash expiry leaves archives intact",test.Games.Count==1&&test.Games[0]==archived);test.Folders.Add("fixture");LibraryActions.StartFresh(test);Check("Start fresh trashes cards and clears watchers",archived.Removed&&test.Folders.Count==0);LibraryActions.Restore(test,archived);Check("Archived state survives trash restoration",archived.Archived);
 string source=Path.Combine(root,"external-fixture.png");using(var stream=File.Create(source)){var encoder=new System.Windows.Media.Imaging.PngBitmapEncoder();encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(city));encoder.Save(stream);}archived.CoverPath=source;AssetVault.PreserveCover(root,archived);File.Delete(source);Check("Archive cover survives source deletion",File.Exists(archived.CoverPath)&&LoadImage(archived.CoverPath,64)!=null);
 view="Library";selectionMode=true;ActivateCard(game);Check("Selection click does not launch",selected.Contains(game.Id));selectionMode=false;selected.Clear();
 Width=960;Height=640;compact=false;view="Library";Render();Capture(Path.ChangeExtension(capture!,"small.png"));Check("Small window search fits",search.ActualWidth>=200);view="Activity";Render();Capture(Path.ChangeExtension(capture!,"small-activity.png"));view="Folders & settings";Render();Capture(Path.ChangeExtension(capture!,"small-settings.png"));LibraryActions.Remove(library,game);view="Trash";Render();Capture(Path.ChangeExtension(capture!,"trash.png"));LibraryActions.Restore(library,game); 
 }
}
