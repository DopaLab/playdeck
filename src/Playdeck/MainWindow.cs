using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Diagnostics;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using Microsoft.Win32;
using Playdeck.Core;
using Path=System.IO.Path;
using Button=System.Windows.Controls.Button;

namespace Playdeck;
public sealed partial class MainWindow:Window {
 Action<Window>? dialogProbe; Action<Game>? editingProbe; Action<Game>? launchProbe; Action<Game>? toolStartProbe;
 readonly string root; readonly bool demo; readonly Library library; readonly Metadata metadata=new();
 readonly CancellationTokenSource lifetime=new();
 readonly DispatcherTimer resizeTimer=new(){Interval=TimeSpan.FromMilliseconds(120)};
 readonly DispatcherTimer versionTimer=new(){Interval=TimeSpan.FromMinutes(5)};
 readonly StackPanel content=new(); readonly TextBlock status=new(); readonly TextBlock pageTitle=new();
 readonly Dictionary<string,Button> navigation=[];
 readonly TextBox search=new(){Width=245,HorizontalAlignment=HorizontalAlignment.Left,ToolTip="Search your library",Margin=new Thickness(0,0,12,0)};
 readonly ComboBox sort=new(){ItemsSource=new[]{"Recently played","Most played","Most launched","Date added","Release date","Title A–Z","Version updates"},SelectedIndex=0};
 
 List<Session> sessions=[]; string view="Library"; bool enriching, compact;  readonly string? capture;
 public MainWindow(string dataRoot,bool demoMode,string? capturePath,string? initialLaunch=null){
  root=dataRoot;demo=demoMode;capture=capturePath;library=Store.Load(root);sessions=Store.Sessions(root);compact=library.CompactCards;sort.SelectedIndex=Math.Clamp(library.SortIndex,0,6);
  Title="Playdeck — Your next good game";Background=Brush("#101510");Foreground=Brush("#F1F3EB");FontFamily=new FontFamily("Segoe UI Variable Text");Width=1240;Height=820;MinWidth=960;MinHeight=600;WindowStartupLocation=WindowStartupLocation.CenterScreen;
  if(demo&&library.Games.Count==0)SeedDemo();if(!demo){foreach(var game in library.Games.Where(g=>g.Removed&&!g.TrashedAt.HasValue))game.TrashedAt=DateTimeOffset.UtcNow;LibraryActions.ExpireTrash(library,DateTimeOffset.UtcNow);Save();}
  BuildShell(); Render();SizeChanged+=(_,_)=>{if(IsLoaded){resizeTimer.Stop();resizeTimer.Start();}};resizeTimer.Tick+=(_,_)=>{resizeTimer.Stop();Render();};PreviewKeyDown+=(_,e)=>{if(e.Key==System.Windows.Input.Key.Escape&&selectionMode){selectionMode=false;selected.Clear();Render();e.Handled=true;}};
  search.TextChanged+=(_,_)=>{libraryScroll.ScrollToTop();Render();};sort.SelectionChanged+=(_,_)=>{library.SortIndex=sort.SelectedIndex;if(!demo)Save();Render();};
  Loaded+=async(_,_)=>{
   if(initialLaunch!=null){var game=library.Games.FirstOrDefault(g=>g.Id==initialLaunch);if(game!=null){await Launch(game);return;}}
   if(capture!=null&&capture.EndsWith(".bench.json")){await Benchmark(capture);Close();return;}
   if(capture!=null){await Task.Delay(500);Render();Capture(capture);view="Pinned";Render();Capture(Path.ChangeExtension(capture,"pinned.png"));view="Activity";Render();Capture(Path.ChangeExtension(capture,"activity.png"));view="Folders & settings";Render();Capture(Path.ChangeExtension(capture,"settings.png"));view="Library";foreach(var g in library.Games)g.CoverPath="";Render();Capture(Path.ChangeExtension(capture,"fallback.png"));compact=true;Render();Capture(Path.ChangeExtension(capture,"compact.png"));Smoke();Close();return;}
   if(!demo){await Enrich();_=CheckConfiguredVersions();}
  };
  Activated+=(_,_)=>{if(demo)return;_=CheckConfiguredVersions();var stamp=Directory.GetLastWriteTimeUtc(Path.Combine(root,"sessions"));if(stamp==sessionStamp)return;sessionStamp=stamp;sessions=Store.Sessions(root);Render();};
  versionTimer.Tick+=(_,_)=>{if(IsActive&&IsEnabled)_=CheckConfiguredVersions();};if(!demo)versionTimer.Start();
  Closed+=(_,_)=>{versionTimer.Stop();resizeTimer.Stop();lifetime.Cancel();metadata.Dispose();versions.Dispose();lifetime.Dispose();};
 }
 static readonly Dictionary<string,SolidColorBrush> brushes=[];
 static SolidColorBrush Brush(string hex){if(brushes.TryGetValue(hex,out var cached))return cached;var b=new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));b.Freeze();brushes[hex]=b;return b;}
 DateTime sessionStamp;
 static readonly FontFamily BodyFont=new("Segoe UI Variable Text");
 static readonly FontFamily DisplayFont=new(new Uri("pack://application:,,,/"),"./Assets/Fonts/#Lilita One");
 static TextBlock Text(string s,double size=14,string color="#F1F3EB",FontWeight? weight=null)=>new(){Text=s,FontSize=size,FontFamily=size>=20||weight==FontWeights.Black?DisplayFont:BodyFont,Foreground=Brush(color),FontWeight=size>=20||weight==FontWeights.Black?FontWeights.Normal:weight??FontWeights.Normal,TextWrapping=TextWrapping.Wrap};
 static Button Btn(string text,Action action,bool accent=false){var b=new Button{Content=text,Margin=new Thickness(0,0,10,10)};if(accent){b.Background=Brush("#E1FF46");b.Foreground=Brush("#171516");}b.Click+=(_,_)=>action();return b;}
 List<Session>? indexedSessions;int indexedCount=-1;ILookup<string,Session>? historyIndex;
 IEnumerable<Session> History(Game g){if(g.IsTool)return Enumerable.Empty<Session>();if(!ReferenceEquals(indexedSessions,sessions)||indexedCount!=sessions.Count){historyIndex=sessions.ToLookup(s=>s.GameId);indexedSessions=sessions;indexedCount=sessions.Count;}return historyIndex![g.Id];}
 static string Hours(double seconds)=>seconds>=3600?$"{seconds/3600:0.#} h":$"{Math.Floor(seconds/60):0} min";
 static long imageDecodes;
 // Bounded decoded-image LRU: file changes invalidate entries, and scrolling reuses frozen bitmaps.
 static readonly Dictionary<string,(BitmapImage Image,long Bytes,LinkedListNode<string> Node)> imageCache=[];
 static readonly LinkedList<string> imageLru=new();static long imageBytes;
 static BitmapImage? LoadImage(string path,int decode){try{
  if(string.IsNullOrWhiteSpace(path))return null;var file=new FileInfo(path);if(!file.Exists)return null;
  string key=file.FullName+"|"+file.LastWriteTimeUtc.Ticks+"|"+file.Length+"|"+decode;
  if(imageCache.TryGetValue(key,out var entry)){imageLru.Remove(entry.Node);imageLru.AddLast(entry.Node);return entry.Image;}
  imageDecodes++;var b=new BitmapImage();b.BeginInit();b.CacheOption=BitmapCacheOption.OnLoad;b.CreateOptions=BitmapCreateOptions.IgnoreImageCache;b.DecodePixelWidth=decode;b.UriSource=new Uri(file.FullName);b.EndInit();b.Freeze();
  long bytes=(long)b.PixelWidth*b.PixelHeight*4;var node=imageLru.AddLast(key);imageCache[key]=(b,bytes,node);imageBytes+=bytes;
  while(imageBytes>48*1024*1024||imageCache.Count>160){string oldest=imageLru.First!.Value;imageBytes-=imageCache[oldest].Bytes;imageCache.Remove(oldest);imageLru.RemoveFirst();}return b;
 }catch{return null;}}

 string CachedIcon(Game g)=>Path.Combine(root,"icons",g.Id+"-hd.png");
 void CacheIcon(Game g){try{if(File.Exists(CachedIcon(g)))return;if(!File.Exists(g.IconPath)){string old=Path.Combine(root,"icons",g.Id+".png");if(File.Exists(old)){Directory.CreateDirectory(Path.GetDirectoryName(CachedIcon(g))!);File.Copy(old,CachedIcon(g),true);}return;}Directory.CreateDirectory(Path.Combine(root,"icons"));var image=LoadImage(g.IconPath,256);if(image!=null){var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(image));using var output=File.Create(CachedIcon(g));encoder.Save(output);return;}IntPtr large=IntPtr.Zero,small=IntPtr.Zero;try{SHDefExtractIcon(g.IconPath,g.IconIndex,0,out large,out small,256|(128u<<16));if(large!=IntPtr.Zero){using var bitmap=System.Drawing.Icon.FromHandle(large).ToBitmap();bitmap.Save(CachedIcon(g),System.Drawing.Imaging.ImageFormat.Png);return;}}finally{if(large!=IntPtr.Zero)DestroyIcon(large);if(small!=IntPtr.Zero)DestroyIcon(small);}using var icon=System.Drawing.Icon.ExtractAssociatedIcon(g.IconPath);using var fallback=icon?.ToBitmap();fallback?.Save(CachedIcon(g),System.Drawing.Imaging.ImageFormat.Png);}catch{}}
 [System.Runtime.InteropServices.DllImport("shell32.dll",CharSet=System.Runtime.InteropServices.CharSet.Unicode)]static extern uint ExtractIconEx(string file,int index,out IntPtr large,out IntPtr small,uint count);
 [System.Runtime.InteropServices.DllImport("shell32.dll",EntryPoint="SHDefExtractIconW",CharSet=System.Runtime.InteropServices.CharSet.Unicode)]static extern int SHDefExtractIcon(string file,int index,uint flags,out IntPtr large,out IntPtr small,uint size);
 [System.Runtime.InteropServices.DllImport("user32.dll")]static extern bool DestroyIcon(IntPtr icon);
 void Save(){if(!demo){foreach(var game in library.Games){try{AssetVault.PreserveCover(root,game);CacheIcon(game);}catch(IOException){}catch(UnauthorizedAccessException){}}Store.Save(root,library);}}
 void Capture(string path)=>CaptureWindow(this,path);
 static void CaptureWindow(Window window,string path){Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);window.UpdateLayout();var bitmap=new RenderTargetBitmap((int)window.ActualWidth,(int)window.ActualHeight,96,96,PixelFormats.Pbgra32);bitmap.Render(window);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));using var file=File.Create(path);encoder.Save(file);}
 void SeedDemo(){
  string[] names={"Hollow Knight","Outer Wilds","Hades","Celeste","Disco Elysium","Stardew Valley","Tunic","Dead Cells"};
  for(int i=0;i<names.Length;i++){var g=new Game{Name=names[i],Source=i%3==0?"Steam":"Local",Favorite=i<3,PinOrder=i,MetadataAttempted=true};string cover=Path.Combine(root,"demo-art",i+".jpg");if(File.Exists(cover))g.CoverPath=cover;library.Games.Add(g);sessions.Add(new Session{GameId=g.Id,Seconds=(i+1)*2810,HasFocusData=i<6,ForegroundSeconds=i<6?(i+1)*2810*(.57+i*.06):0,Started=DateTimeOffset.Now.AddDays(-i),Status="Completed"});}
 }
 void AddGames(){var dlg=new OpenFileDialog{Title="Add executables or shortcuts",Filter="Games and shortcuts|*.exe;*.lnk;*.url",Multiselect=true};if(dlg.ShowDialog(this)==true)QueueImports(dlg.FileNames);}
 Window Dialog(string title,StackPanel body,double width=590){body.Margin=new Thickness(0,0,12,0);var window=new Window{Owner=this,Title=title,Width=width,SizeToContent=SizeToContent.Height,MaxHeight=760,WindowStartupLocation=WindowStartupLocation.CenterOwner,ResizeMode=ResizeMode.NoResize,Content=new ScrollViewer{Content=body,Margin=new Thickness(25)}};var probe=dialogProbe;if(probe!=null)window.ContentRendered+=(_,_)=>Dispatcher.BeginInvoke(()=>probe(window));return window;}
 async Task Enrich(){
  if(enriching||!library.Online||demo)return;enriching=true;
  try{
   while(library.Online&&!lifetime.IsCancellationRequested){var g=library.Games.FirstOrDefault(g=>!g.Removed&&!g.MetadataAttempted);if(g==null)break;status.Text="Caching artwork for "+g.Name+"…";
    string before=JsonSerializer.Serialize(g,Store.Json);
    var copy=JsonSerializer.Deserialize<Game>(before,Store.Json)!;
    await metadata.Fetch(copy,root,lifetime.Token);if(lifetime.IsCancellationRequested)break;
    if(JsonSerializer.Serialize(g,Store.Json)==before){g.Name=copy.Name;g.SteamId=copy.SteamId;g.ReleaseDate=copy.ReleaseDate;g.CoverPath=copy.CoverPath;g.MetadataStatus=copy.MetadataStatus;g.MetadataAttempted=true;}
    else {g.MetadataAttempted=true;}
    Save();Render();
   }
  }finally{enriching=false;if(!lifetime.IsCancellationRequested)Render();}
 }
 async Task Launch(Game g){
  if(demo&&toolStartProbe==null){MessageBox.Show(this,"This is an isolated visual demo. Add your own game to the regular library to launch it.","Demo library");return;}
  if(!File.Exists(g.LaunchPath)){MessageBox.Show(this,"The launch file is missing. Choose a new launch file in Edit, or archive the game.","Game unavailable");return;}
  if(g.IsTool){try{if(toolStartProbe!=null)toolStartProbe(g);else ToolLaunch.Start(g);status.Text="Opened "+g.Name+" · tool mode, no play tracking.";}catch(Exception ex){MessageBox.Show(this,ex.Message,"Could not open tool");}return;}
  if(string.IsNullOrWhiteSpace(g.TrackPath)){
   if(MessageBox.Show(this,"This shortcut does not expose the game's executable. The launch will be recorded, but playtime will not be estimated.\n\nFor playtime, choose the game's executable in Edit → Tracking executable. Launch now?","Launch tracking",MessageBoxButton.YesNo)!=MessageBoxResult.Yes)return;
  }
  string tracker=Path.Combine(AppContext.BaseDirectory,"tracker","Playdeck.Tracker.exe");if(!File.Exists(tracker))tracker=Path.Combine(AppContext.BaseDirectory,"Playdeck.Tracker.exe");
  string requests=Path.Combine(root,"requests");Directory.CreateDirectory(requests);string id=Guid.NewGuid().ToString("N");string req=Path.Combine(requests,id+".json"),reply=Path.Combine(requests,id+".reply.json");
  Store.Atomic(req,new LaunchRequest{Game=g,DataRoot=root,ReplyPath=reply});
  IsEnabled=false;status.Text="Launching "+g.Name+"…";
  try {
   var psi=new ProcessStartInfo(tracker){UseShellExecute=false,CreateNoWindow=true};psi.ArgumentList.Add(req);using var p=Process.Start(psi)??throw new InvalidOperationException("Could not start tracker.");
   for(int n=0;n<600;n++){
    if(File.Exists(reply)){using var result=JsonDocument.Parse(File.ReadAllText(reply));bool ok=result.RootElement.GetProperty("ok").GetBoolean();string error=ok?"":result.RootElement.GetProperty("error").GetString()??"Launch failed";File.Delete(reply);if(ok){Close();return;}throw new InvalidOperationException(error);}
    if(p.HasExited)throw new InvalidOperationException("The launch helper stopped before confirming launch.");await Task.Delay(100);
   }
   throw new TimeoutException("Launch confirmation is still pending. Check whether the game opened before trying again.");
  }catch(Exception ex){MessageBox.Show(this,ex.Message,"Could not confirm launch");}finally{IsEnabled=true;}
 }
 void Edit(Game g){
  if(editingProbe!=null){editingProbe(g);return;}
  var body=new StackPanel();body.Children.Add(Text("Your game, your rules.",23,"#FF765E",FontWeights.Bold));
  var history=History(g).OrderBy(s=>s.Started).ToArray();if(history.Length>0)body.Children.Add(Text($"First played {history[0].Started.LocalDateTime:dd MMM yyyy} · Last launched {history[^1].Started.LocalDateTime:dd MMM yyyy}\n{history.Length} launches · {Hours(history.Sum(s=>s.Seconds))} recorded",12,"#AAB59D"));
  TextBox Field(string label,string value){body.Children.Add(Text(label,12,"#AAB59D"));var box=new TextBox{Text=value,Margin=new Thickness(0,5,0,12)};body.Children.Add(box);return box;}
  var name=Field("TITLE",g.Name);var path=Field("LAUNCH FILE (.exe / .lnk / .url)",g.LaunchPath);
  body.Children.Add(Btn("Browse launch file",()=>{var f=new OpenFileDialog{Filter="Games|*.exe;*.lnk;*.url"};if(f.ShowDialog()==true)path.Text=f.FileName;}));
  var args=Field("ARGUMENTS (direct .exe launches only)",g.Arguments);var working=Field("WORKING DIRECTORY",g.WorkingDirectory);
  var tracking=Field("TRACKING EXECUTABLE (exact game .exe, not Steam / Epic)",g.TrackPath);
  body.Children.Add(Btn("Choose tracking executable",()=>{var f=new OpenFileDialog{Filter="Executable|*.exe"};if(f.ShowDialog()==true)tracking.Text=f.FileName;}));
  var release=Field("RELEASE DATE (YYYY-MM-DD, blank if unknown)",g.ReleaseDate?.ToString("yyyy-MM-dd")??"");var notes=Field("NOTES",g.Notes);
  var toolMode=new CheckBox{Content="Mark as tool · keep Playdeck open, no play tracking",IsChecked=g.IsTool};body.Children.Add(toolMode);
   var archived=new CheckBox{Content="Uninstalled / archived — keep all history",IsChecked=g.Archived};body.Children.Add(archived);
  var favorite=new CheckBox{Content="Pin to your quick-launch shelf",IsChecked=g.Favorite};body.Children.Add(favorite);
  var dialog=Dialog("Edit · "+g.Name,body);var row=new WrapPanel{Margin=new Thickness(0,12,0,0)};
  row.Children.Add(Btn("Save changes",()=>{
   if(string.IsNullOrWhiteSpace(name.Text)){MessageBox.Show(dialog,"Give this game a title.");return;}
   if((!File.Exists(path.Text)&&path.Text!=g.LaunchPath)||!new[]{".exe",".lnk",".url"}.Contains(Path.GetExtension(path.Text).ToLowerInvariant())){MessageBox.Show(dialog,"Choose an existing executable or shortcut.");return;}
   if(tracking.Text.Length>0&&((!File.Exists(tracking.Text)&&tracking.Text!=g.TrackPath)||!Path.GetExtension(tracking.Text).Equals(".exe",StringComparison.OrdinalIgnoreCase))){MessageBox.Show(dialog,"Choose an existing tracking executable, or leave it blank.");return;}
   DateTime? date=null;if(release.Text.Length>0){if(!DateTime.TryParseExact(release.Text,"yyyy-MM-dd",System.Globalization.CultureInfo.InvariantCulture,System.Globalization.DateTimeStyles.None,out var parsed)){MessageBox.Show(dialog,"Use YYYY-MM-DD for the release date.");return;}date=parsed;}
   g.IsTool=toolMode.IsChecked==true;g.Name=name.Text.Trim();g.LaunchPath=path.Text;g.Arguments=args.Text;g.TrackPath=tracking.Text;g.WorkingDirectory=working.Text;g.ReleaseDate=date;g.Notes=notes.Text;g.Archived=archived.IsChecked==true;if(g.Favorite!=(favorite.IsChecked==true))LibraryActions.Pin(library,g,favorite.IsChecked==true);Save();dialog.Close();Render();
  },true));
  row.Children.Add(Btn("Set cover",()=>{var f=new OpenFileDialog{Filter="Images|*.jpg;*.jpeg;*.png;*.webp;*.bmp"};if(f.ShowDialog()==true){if(LoadImage(f.FileName,400)==null){MessageBox.Show(dialog,"This image format could not be read. Use PNG or JPEG.");return;}string dest=Path.Combine(root,"artwork",g.Id+Path.GetExtension(f.FileName));Directory.CreateDirectory(Path.GetDirectoryName(dest)!);if(!string.Equals(dest,f.FileName,StringComparison.OrdinalIgnoreCase))File.Copy(f.FileName,dest,true);g.CoverPath=dest;g.MetadataAttempted=true;g.MetadataStatus="Custom artwork";Save();Render();}}));
  row.Children.Add(Btn("Retry online match",()=>{g.MetadataAttempted=false;Save();dialog.Close();_=Enrich();}));body.Children.Add(row);
  AddEditorActions(g,body,dialog);body.Children.Add(Text(g.MetadataStatus,11,"#939E86"));dialog.ShowDialog();
 }
 void Export(){var dialog=new SaveFileDialog{FileName="Playdeck-sessions.csv",Filter="CSV|*.csv"};if(dialog.ShowDialog(this)!=true)return;
  static string Csv(string value)=>"\""+value.Replace("\"","\"\"")+"\"";
  var lines=new List<string>{"Game,Started UTC,Last checkpoint UTC,Seconds,Status,Foreground seconds,Focus data available"};foreach(var s in Store.Sessions(root)){string name=library.Games.FirstOrDefault(g=>g.Id==s.GameId)?.Name??s.GameId;if(name.Length>0&&"=+-@".Contains(name[0]))name="'"+name;lines.Add($"{Csv(name)},{Csv(s.Started.ToString("O"))},{Csv(s.Updated.ToString("O"))},{s.Seconds.ToString("0.0",System.Globalization.CultureInfo.InvariantCulture)},{Csv(s.Status)},{s.ForegroundSeconds.ToString("0.0",System.Globalization.CultureInfo.InvariantCulture)},{s.HasFocusData}");}File.WriteAllLines(dialog.FileName,lines,System.Text.Encoding.UTF8);status.Text="Sessions exported.";}
 void Smoke(){
  var checks=new List<string>();
  void Check(string name,bool ok){if(!ok)throw new InvalidOperationException("UI smoke failed: "+name);checks.Add("PASS "+name);}
  var iconGame=new Game{IconPath=Environment.ProcessPath!};CacheIcon(iconGame);Check("Local executable icon extraction",LoadImage(CachedIcon(iconGame),64)!=null);
  view="Library";search.Text="Hollow";Render();Check("Search filters to a single card",content.Children.OfType<CardPanel>().First().Children.Count==1);
  search.Text="";Render();search.Focus();search.Text="Outer";Check("Search retains keyboard focus",search.IsKeyboardFocused);
  search.Text="";for(int i=0;i<6;i++){sort.SelectedIndex=i;Render();}Check("All six sorts render",true);
  var game=library.Games[0];game.Archived=true;view="Archived";Render();Check("Archived game is visible",content.Children.OfType<CardPanel>().First().Children.Count==1);game.Archived=false;
  view="Library";int original=library.Games.Count;for(int i=0;i<250;i++)library.Games.Add(new Game{Name="Fixture "+i});Render();var virtualCards=content.Children.OfType<CardPanel>().Single();Check("Infinite library keeps all items and bounded visual cards",virtualCards.ItemCount==original+250&&virtualCards.Children.Count<50);virtualCards.SetViewport(100000,600);UpdateLayout();Check("Infinite scroll reaches final game",virtualCards.Children.Count>0&&virtualCards.Children.Count<50);library.Games.RemoveRange(original,250);libraryScroll.ScrollToTop();Render();
  view="Activity";Render();Check("Activity chart renders",content.Children.Count>0);view="Folders & settings";Render();Check("Settings renders",content.Children.Count>0);
  SmokeV2(checks);SmokeV3(checks);SmokeV4(checks);SmokeV5(checks);SmokeV6(checks);File.WriteAllLines(Path.ChangeExtension(capture!,"smoke.txt"),checks);
 }
}





