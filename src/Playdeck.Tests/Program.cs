using System.Diagnostics;
using System.Text.Json;
using Playdeck.Core;

internal static class Program {
 static int failures, passed;
 static string root=Path.Combine(AppContext.BaseDirectory,"fixtures",Guid.NewGuid().ToString("N"));
 static async Task<int> Main(string[] args){
  if(args.Length==3&&args[0]=="--release-audit"){await ReleaseTests.Audit(args[1],args[2]);return 0;}
  if(args.Length==2&&args[0]=="--external-child"){
   var psi=new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),"WindowsPowerShell","v1.0","powershell.exe")){UseShellExecute=false,CreateNoWindow=true};psi.ArgumentList.Add("-NoProfile");psi.ArgumentList.Add("-Command");psi.ArgumentList.Add("Start-Sleep -Seconds 30");using var child=Process.Start(psi)!;File.WriteAllText(args[1],child.Id.ToString());await Task.Delay(6500);return 0;
  }
  if(args.Length>0&&args[0]=="--handoff"){var child=new ProcessStartInfo(args[1]){UseShellExecute=false,CreateNoWindow=true};child.ArgumentList.Add("--fixture");child.ArgumentList.Add("6500");using var c=Process.Start(child);await Task.Delay(2500);return 0;}
  if(args.Length>0&&args[0]=="--fixture"){await Task.Delay(args.Length>1?int.Parse(args[1]):6500);return 0;}
  Directory.CreateDirectory(root);
  try{
   Test("Accounting rejects sleep and invalid gaps",()=>{var s=new Session();foreach(double gap in new[]{-1d,0,10,3600,double.NaN,double.PositiveInfinity})SessionAccounting.Accrue(s,DateTimeOffset.Now,gap,true);Assert(s.Seconds==0);});
   Test("Accounting splits midnight and separates focus",()=>{var s=new Session();var end=new DateTimeOffset(DateTime.Today.AddSeconds(2));SessionAccounting.Accrue(s,end,4,true);SessionAccounting.Accrue(s,end.AddSeconds(2),2,false);Assert(s.Seconds==6&&s.ForegroundSeconds==4&&SessionAccounting.OnDay(s,DateTime.Today.AddDays(-1))==2&&SessionAccounting.OnDay(s,DateTime.Today)==4);});
   Test("Legacy statistics retain history",()=>{var s=new Session{Started=DateTimeOffset.Now,Seconds=90};Assert(SessionAccounting.OnDay(s,DateTime.Today)==90);});
   Test("Reused tracker PID cannot keep session alive",()=>{var data=Path.Combine(root,"pid-reuse");Store.Atomic(Path.Combine(data,"sessions","one.json"),new Session{TrackerPid=Environment.ProcessId,TrackerBornUtcTicks=1,Status="Playing",Seconds=42});Assert(Store.Sessions(data).Single().Status=="Interrupted");});
   Test("Activity splits daily totals across reporting periods",()=>{var game=new Game();var end=DateTime.Today;var session=new Session{GameId=game.Id,Started=new DateTimeOffset(end.AddDays(-8)),Seconds=200,DailySeconds=new(){{end.ToString("yyyy-MM-dd"),50},{end.AddDays(-8).ToString("yyyy-MM-dd"),150}}};var report=ActivityReport.Build(new[]{game},new[]{session},7,end);Assert(report.Seconds==50&&report.PreviousSeconds==150&&report.Launches==0&&report.GamesPlayed==1&&report.ActiveDays==1);});
   Test("Activity excludes removed games and preserves archives",()=>{var a=new Game{Archived=true};var b=new Game{Removed=true};var report=ActivityReport.Build(new[]{a,b},new[]{new Session{GameId=a.Id,Seconds=60},new Session{GameId=b.Id,Seconds=900}},30,DateTime.Today);Assert(report.Seconds==60&&report.Launches==1);});
   Test("Focus coverage excludes unknown history",()=>{var game=new Game();var report=ActivityReport.Build(new[]{game},new[]{new Session{GameId=game.Id,Seconds=100},new Session{GameId=game.Id,Seconds=40,HasFocusData=true,ForegroundSeconds=30}},30,DateTime.Today);Assert(report.AllSeconds==140&&report.FocusMeasuredSeconds==40&&report.FocusSeconds==30);});
   Test("Empty analytics are finite and stable",()=>{var report=ActivityReport.Build(Array.Empty<Game>(),Array.Empty<Session>(),30,DateTime.Today);Assert(report.Seconds==0&&report.AverageSession==0&&report.Days.Length==30&&report.Ranking.Length==0);});
   await VersionTests.Run(Test,Assert,root,args.Contains("--network"));
   await ReleaseTests.Run(Test,Assert,root,args.Contains("--network"));
   Test("Name cleanup",()=>Assert(Names.Clean("Hollow_Knight - Shortcut")=="Hollow Knight"));
   Test("Strict normalization",()=>Assert(Names.Normalize("Hollow Knight™")==Names.Normalize("Hollow Knight")));
   Test("Non-Latin names remain distinguishable",()=>Assert(Names.Normalize("游戏甲")!=Names.Normalize("游戏乙")));
   Test("Helper exclusion without game-title collisions",()=>Assert(Names.Helper("CrashReportClient.exe")&&Names.Helper("unins000.exe")&&!Names.Helper("Hades.exe")&&!Names.Helper("Observer.exe")));
   Test("Atomic library round trip",()=>{var l=new Library();l.Games.Add(new Game{Name="Test",Notes="Unicode ✓",Archived=true});Store.Save(root,l);Assert(Store.Load(root).Games[0].Notes=="Unicode ✓");});
   Test("Profile fields survive storage",()=>{string data=Path.Combine(root,"profile-roundtrip");Store.Save(data,new Library{UserName="Attract",AvatarPath="cached-avatar.png"});var saved=Store.Load(data);Assert(saved.UserName=="Attract"&&saved.AvatarPath=="cached-avatar.png");});
   Test("Backup on save",()=>{Store.Save(root,new Library());Assert(File.Exists(Path.Combine(root,"library.json.bak")));});
   Test("Corrupt library preserved",()=>{string path=Path.Combine(root,"broken");Directory.CreateDirectory(path);File.WriteAllText(Path.Combine(path,"library.json"),"{bad");try{Store.Load(path);throw new Exception("Read should fail");}catch(InvalidDataException){}Assert(File.ReadAllText(Path.Combine(path,"library.json"))=="{bad");});
   string folder=Path.Combine(root,"games");Directory.CreateDirectory(folder);
   File.WriteAllText(Path.Combine(folder,"Hollow Knight.url"),"[InternetShortcut]\nURL=steam://rungameid/367520\n");
   File.WriteAllText(Path.Combine(folder,"Browser.url"),"[InternetShortcut]\nURL=https://example.com\n");
   File.WriteAllText(Path.Combine(folder,"unins000.exe"),"");
   Test("Only game URL schemes imported",()=>{var scan=Discovery.Scan(new[]{folder});Assert(scan.Games.Count==1&&scan.Games[0].SteamId==367520);});
   Test("Repeated folder deduplication",()=>Assert(Discovery.Scan(new[]{folder,folder}).Games.Count==1));
   Test("Desktop conservative classification",()=>Assert(Discovery.Scan(new[]{folder},true).Games.Count==1));
   Test("Unavailable folders reported",()=>Assert(Discovery.Scan(new[]{Path.Combine(root,"absent")}).Warnings.Count==1));
   Test("One recommended executable per game tree with exclusions",()=>{
    string games=Path.Combine(root,"recommendations");
    foreach(string rel in new[]{"Alpha/Alpha.exe","Alpha/ConfigTool.exe","Alpha/Binaries/Win64/Alpha-Win64-Shipping.exe","Beta/Beta.exe","Beta/Launcher.exe","Excluded/Hidden.exe","Alpha/Extras/Tools.exe"}){string file=Path.Combine(games,rel);Directory.CreateDirectory(Path.GetDirectoryName(file)!);File.WriteAllText(file,"fixture");}
    var scan=Discovery.Scan(new[]{games},false,new[]{Path.Combine(games,"Excluded"),Path.Combine(games,"Alpha","Extras")});
    Assert(scan.Games.Count==2&&scan.AllGames.Count==5&&scan.Games.Any(g=>g.Name=="Alpha")&&scan.Games.Any(g=>g.Name=="Beta"));
    Assert(Discovery.Scan(new[]{games},false,new[]{Path.Combine(games,"Excluded"),Path.Combine(games,"Alpha","Extras")},false).Games.Count==5);
    Assert(Discovery.Scan(new[]{Path.Combine(games,"Alpha")},false,new[]{Path.Combine(games,"Alpha","Extras")}).Games.Count==1);
   });
   Test("Folder exclusions respect path boundaries",()=>Assert(Discovery.Within(Path.Combine(root,"Alpha","Tools"),Path.Combine(root,"Alpha"))&&!Discovery.Within(Path.Combine(root,"Alpha2"),Path.Combine(root,"Alpha"))));
   Test("Loose executables do not collapse neighboring game folders",()=>{string games=Path.Combine(root,"recommendations");File.WriteAllText(Path.Combine(games,"Loose.exe"),"x");Assert(Discovery.Scan(new[]{games},false,new[]{Path.Combine(games,"Excluded"),Path.Combine(games,"Alpha","Extras")}).Games.Count==3);});
   Test("Import keyword matching uses names and paths",()=>{var g=new Game{Name="Alpha",LaunchPath=@"C:\Games\Alpha\Tools\Editor.exe"};Assert(LibraryActions.Matches(g,"benchmark, editor")&&LibraryActions.Matches(g,"tools")&&!LibraryActions.Matches(g,"beta, delta"));});
   Test("Pins reorder in both directions and persist",()=>{var l=new Library();var a=new Game{Name="A"};var b=new Game{Name="B"};var c=new Game{Name="C"};l.Games.AddRange(new[]{a,b,c});foreach(var g in l.Games)LibraryActions.Pin(l,g,true);LibraryActions.MovePin(l,a.Id,c.Id,true);Assert(l.Games.OrderBy(g=>g.PinOrder).Last()==a);LibraryActions.MovePin(l,a.Id,b.Id);Assert(l.Games.OrderBy(g=>g.PinOrder).First()==a);Store.Save(Path.Combine(root,"pins"),l);Assert(Store.Load(Path.Combine(root,"pins")).Games.OrderBy(g=>g.PinOrder).First().Id==a.Id);});
   Test("Removing a card preserves metadata and excludes reimport",()=>{var l=new Library();var g=new Game{Name="Keep history",LaunchPath="example.exe",Favorite=true,Notes="preserve"};l.Games.Add(g);LibraryActions.Remove(l,g);Assert(g.Removed&&!g.Favorite&&l.Games.Count==1&&g.Notes=="preserve"&&l.Ignored.Contains("example.exe"));Store.Save(Path.Combine(root,"removed"),l);Assert(Store.Load(Path.Combine(root,"removed")).Games.Single().Removed);});
   Test("Windows shortcut preserves target and arguments",()=>{
    string target=Path.Combine(root,"TinyGame.exe"),link=Path.Combine(root,"Tiny Game.lnk");File.Copy(Environment.ProcessPath!,target);
    object shell=Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell")!)!;object? shortcut=null;
    try{dynamic sh=shell;shortcut=sh.CreateShortcut(link);dynamic sc=shortcut;sc.TargetPath=target;sc.Arguments="--fixture";sc.WorkingDirectory=root;sc.IconLocation=Environment.ProcessPath+",0";sc.Save();var g=Discovery.Read(link);Assert(g!=null&&g.TrackPath==target&&g.Arguments=="--fixture"&&g.LaunchPath==link&&g.IconPath==Environment.ProcessPath);}
    finally{if(shortcut!=null)System.Runtime.InteropServices.Marshal.FinalReleaseComObject(shortcut);System.Runtime.InteropServices.Marshal.FinalReleaseComObject(shell);}
   });
   Test("Interrupted session preserves time",()=>{Store.Atomic(Path.Combine(root,"sessions","interrupted.json"),new Session{TrackerPid=int.MaxValue,Seconds=65,Status="Playing"});var s=Store.Sessions(root).Single();Assert(s.Status=="Interrupted"&&s.Seconds==65);});
   Test("Truncated session does not break history",()=>{File.WriteAllText(Path.Combine(root,"sessions","bad.json"),"{");Assert(Store.Sessions(root).Count==1);});
   using(var metadata=new Metadata()){
    var g=new Game{MetadataAttempted=true,MetadataStatus="sentinel"};await metadata.Fetch(g,root);Test("Cached failures do not repeat requests",()=>Assert(g.MetadataStatus=="sentinel"));
    if(args.Contains("--network")){
     var suggestions=await metadata.Search("Hollow",root);Test("Partial Steam title returns selectable matches",()=>Assert(suggestions.Any(x=>x.Id==367520)));var cached=await metadata.Search("Hollow",root);Test("Steam suggestions are cached locally",()=>Assert(cached.SequenceEqual(suggestions)));
     var live=new Game{Name="Hollow Knight"};await metadata.Fetch(live,root);Test("Live Steam exact title, date and cached artwork",()=>Assert(live.SteamId==367520&&live.ReleaseDate.HasValue&&File.Exists(live.CoverPath)));
     var stamp=File.GetLastWriteTimeUtc(live.CoverPath);await metadata.Fetch(live,root);Test("Cached artwork is not fetched twice",()=>Assert(File.GetLastWriteTimeUtc(live.CoverPath)==stamp));
     var absent=new Game{Name="Playdeck No Such Game 94871026"};await metadata.Fetch(absent,root);Test("Unknown title keeps local fallback",()=>Assert(absent.MetadataAttempted&&absent.SteamId==0&&absent.CoverPath==""));
    }
   }
   if(args.Length>0){string tracker=Path.GetFullPath(args[0]);await TrackerTest(tracker,false);await TrackerTest(tracker,true);await TrackerTest(tracker,false,true);await ToolTest(tracker);await ExternalChildTest(tracker);}
   Console.WriteLine($"{passed} passed, {failures} failed. Fixtures: {root}");
  }catch(Exception e){Console.WriteLine(e);failures++;}
  return failures==0?0:1;
 }
 static async Task ToolTest(string tracker){string data=Path.Combine(root,"tool-launch");Directory.CreateDirectory(data);string request=Path.Combine(data,"request.json"),reply=Path.Combine(data,"reply.json");var tool=new Game{IsTool=true,LaunchPath=Environment.ProcessPath!,Arguments="--fixture 4000",WorkingDirectory=AppContext.BaseDirectory};Store.Atomic(request,new LaunchRequest{Game=tool,DataRoot=data,ReplyPath=reply});var psi=new ProcessStartInfo(tracker){UseShellExecute=false,CreateNoWindow=true};psi.ArgumentList.Add(request);using var process=Process.Start(psi)!;using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(20));await process.WaitForExitAsync(timeout.Token);using var result=JsonDocument.Parse(File.ReadAllText(reply));Test("Tool launch succeeds without creating game history",()=>Assert(result.RootElement.GetProperty("ok").GetBoolean()&&Store.Sessions(data).Count==0));int pid=result.RootElement.GetProperty("toolPid").GetInt32();using var running=Process.GetProcessById(pid);Test("Tool launches independently of the tracking helper",()=>Assert(!running.HasExited&&process.HasExited));await running.WaitForExitAsync(timeout.Token);Test("Tools stay out of game analytics without erasing history",()=>{var session=new Session{GameId=tool.Id,Seconds=80};Assert(ActivityReport.Build(new[]{tool},new[]{session},30,DateTime.Today).AllSeconds==0);tool.IsTool=false;Assert(ActivityReport.Build(new[]{tool},new[]{session},30,DateTime.Today).AllSeconds==80);});Store.Save(data,new Library{Games=new(){tool}});tool.IsTool=true;Store.Save(data,new Library{Games=new(){tool}});Test("Tool classification survives library storage",()=>Assert(Store.Load(data).Games.Single().IsTool));}
 static async Task TrackerTest(string tracker,bool missing,bool handoff=false){
  string data=Path.Combine(root,missing?"missing":handoff?"handoff":"tracker");Directory.CreateDirectory(data);string request=Path.Combine(data,"request.json"),reply=Path.Combine(data,"reply.json");
  string exe=Path.Combine(AppContext.BaseDirectory,"FixtureGame-"+Guid.NewGuid().ToString("N")+".exe");File.Copy(Environment.ProcessPath!,exe);
  string child=Path.Combine(AppContext.BaseDirectory,"ChildGame-"+Guid.NewGuid().ToString("N")+".exe");if(handoff)File.Copy(Environment.ProcessPath!,child);
  Store.Atomic(request,new LaunchRequest{DataRoot=data,ReplyPath=reply,Game=new Game{LaunchPath=missing?Path.Combine(root,"absent.exe"):exe,TrackPath=exe,Arguments=handoff?"--handoff \""+child+"\"":"--fixture",WorkingDirectory=AppContext.BaseDirectory}});
  var psi=new ProcessStartInfo(tracker){UseShellExecute=false,CreateNoWindow=true};psi.ArgumentList.Add(request);using var p=Process.Start(psi)!;
  using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(35));await p.WaitForExitAsync(timeout.Token);
  using var result=JsonDocument.Parse(File.ReadAllText(reply));
  Test(missing?"Failed launch reported without session":"Tracker reports successful launch",()=>Assert(result.RootElement.GetProperty("ok").GetBoolean()!=missing));
  if(missing){Test("Failed launch creates no history",()=>Assert(Store.Sessions(data).Count==0));return;}
  var session=Store.Sessions(data).Single();Test("Real process duration and completed status",()=>Assert(session.Status=="Completed"&&session.Seconds>=3&&session.Seconds<12));
  Test("Tracker records focus capability and daily totals",()=>Assert(session.HasFocusData&&Math.Abs(session.DailySeconds.Values.Sum()-session.Seconds)<.01&&session.ForegroundSeconds<=session.Seconds));
  Test("Tracker exits after game closes",()=>Assert(p.HasExited));
  Test("Closed fixture game leaves no running process",()=>{var remaining=Process.GetProcessesByName(Path.GetFileNameWithoutExtension(exe));try{Assert(remaining.Length==0);}finally{foreach(var process in remaining)process.Dispose();}});
 }
 static async Task ExternalChildTest(string tracker){
  string data=Path.Combine(root,"external-child");Directory.CreateDirectory(data);string request=Path.Combine(data,"request.json"),reply=Path.Combine(data,"reply.json"),pidFile=Path.Combine(data,"child.pid");string exe=Path.Combine(AppContext.BaseDirectory,"ExitGame-"+Guid.NewGuid().ToString("N")+".exe");File.Copy(Environment.ProcessPath!,exe);
  Store.Atomic(request,new LaunchRequest{DataRoot=data,ReplyPath=reply,Game=new Game{LaunchPath=exe,TrackPath=exe,Arguments="--external-child \""+pidFile+"\"",WorkingDirectory=AppContext.BaseDirectory}});
  var psi=new ProcessStartInfo(tracker){UseShellExecute=false,CreateNoWindow=true};psi.ArgumentList.Add(request);using var observer=Process.Start(psi)!;using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(45));await observer.WaitForExitAsync(timeout.Token);
  using var child=Process.GetProcessById(int.Parse(File.ReadAllText(pidFile)));Test("Unrelated child cannot prolong game tracking after game exit",()=>Assert(!child.HasExited&&Store.Sessions(data).Single().Status=="Completed"));await child.WaitForExitAsync(timeout.Token);
  int directPid=ToolLaunch.Start(new Game{LaunchPath=exe,Arguments="--fixture 2500",WorkingDirectory=AppContext.BaseDirectory});using var direct=Process.GetProcessById(directPid);await direct.WaitForExitAsync(timeout.Token);Test("Direct game launch exits independently and creates no new tracked session",()=>Assert(Store.Sessions(data).Count==1&&direct.HasExited));
 }
 static void Test(string name,Action action){try{action();passed++;Console.WriteLine("PASS "+name);}catch(Exception e){failures++;Console.WriteLine("FAIL "+name+": "+e.Message);}}
 static void Assert(bool condition){if(!condition)throw new Exception("Assertion failed");}
}
