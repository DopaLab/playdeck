using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Playdeck.Core;
internal static partial class VersionTests {
 static void DiscoveryChecks(Action<string,Action> test,Action<bool> assert,string root){
  string steam=Path.Combine(root,"discovery-steam"),folder=Path.Combine(steam,"steamapps","common","Actual Game"),exe=Path.Combine(folder,"bin","game.exe");Directory.CreateDirectory(Path.GetDirectoryName(exe)!);File.WriteAllText(exe,"");string manifest=Path.Combine(steam,"steamapps","appmanifest_480.acf");File.WriteAllText(manifest,"\"AppState\" { \"appid\" \"480\" \"buildid\" \"12345\" \"StateFlags\" \"4\" \"installdir\" \"Actual Game\" }");var game=new Game{TrackPath=exe};
  test("Find Steam identity from installation without a preassigned AppID",()=>{var found=VersionDiscovery.Find(game,new[]{steam},Path.Combine(root,"no-epic"));assert(found.Any(e=>e.AppId==480&&e.Kind=="Steam"));var v=GameVersions.ReadLocal(game,new[]{steam});assert(v.AppId==480&&v.InstalledBuild=="12345");});
  test("Other Steam installations cannot donate a game's version",()=>{var other=new Game{TrackPath=Path.Combine(root,"unrelated","other.exe")};assert(!VersionDiscovery.Find(other,new[]{steam},Path.Combine(root,"no-epic")).Any(e=>e.Kind=="Steam"));});
  string version=Path.Combine(folder,"version.txt");File.WriteAllText(version,"game_version = 1.9.2");
  test("Nearby version files appear as evidence for user confirmation",()=>assert(VersionDiscovery.Find(game,Array.Empty<string>(),Path.Combine(root,"no-epic")).Any(e=>e.Path==version&&e.Value=="1.9.2"&&e.Kind=="File")));
  test("Confirmed file tracks changes without converting to a Steam build",()=>{var selected=new Game{VersionInfo=new(){Mode="File",EvidencePath=version,ManualLatest="1.10"}};var before=GameVersions.ReadLocal(selected);File.WriteAllText(version,"1.10");var after=GameVersions.ReadLocal(selected);assert(before.NeedsUpdate&&after.MatchesLatest&&after.InstalledBuild==""&&!after.NeedsUpdate);});
  test("GOG info schema version is never mistaken for game version",()=>{string p=Path.Combine(folder,"goggame-123.info");File.WriteAllText(p,"{\"version\":1,\"gameId\":\"123\"}");assert(VersionDiscovery.ReadLabel(p)==null);File.WriteAllText(p,"{\"version\":1,\"buildId\":\"456\"}");assert(VersionDiscovery.ReadLabel(p)=="456");});
  test("Oversized version files are rejected",()=>{string p=Path.Combine(folder,"big.txt");File.WriteAllText(p,new string('x',256*1024+1));bool rejected=false;try{VersionDiscovery.ReadLabel(p);}catch(InvalidDataException){rejected=true;}assert(rejected);});
  test("Nested unrelated files are not recursively scanned",()=>{string d=Path.Combine(folder,"unrelated","deep");Directory.CreateDirectory(d);File.WriteAllText(Path.Combine(d,"version.txt"),"9.9");assert(!VersionDiscovery.Find(game,Array.Empty<string>(),Path.Combine(root,"no-epic")).Any(e=>e.Value=="9.9"));});
  test("Discovery cancellation is honored",()=>{using var ct=new CancellationTokenSource();ct.Cancel();bool canceled=false;try{VersionDiscovery.Find(game,new[]{steam},stop:ct.Token);}catch(OperationCanceledException){canceled=true;}assert(canceled);});
  test("Steam parse cache invalidates changed evidence",()=>{var first=GameVersions.ReadLocal(game,new[]{steam});File.WriteAllText(manifest,File.ReadAllText(manifest).Replace("12345","123456"));var second=GameVersions.ReadLocal(game,new[]{steam});assert(first.InstalledBuild=="12345"&&second.InstalledBuild=="123456");});
  test("Card banners require fresh comparable evidence",()=>{var now=DateTimeOffset.UtcNow;var v=new VersionRecord{LocalAvailable=true,Installed="Build 100",CheckedAt=now,RemoteCheckedAt=now,MatchesLatest=true};assert(VersionBadge.For(v,now).Color=="#B6EC78");v.MatchesLatest=false;v.NeedsUpdate=true;assert(VersionBadge.For(v,now).Color=="#F4D35D");assert(VersionBadge.For(v,now.AddDays(2)).NeedsHelp);v.LocalAvailable=false;assert(VersionBadge.For(v,now).NeedsHelp);});
  test("Old manual latest labels cannot renew a green banner automatically",()=>{var now=DateTimeOffset.UtcNow;var v=new VersionRecord{Mode="File",LocalAvailable=true,Installed="1.2",ManualLatest="1.2",ManualLatestAt=now.AddDays(-2),CheckedAt=now,MatchesLatest=true};assert(VersionBadge.For(v,now).NeedsHelp);v.ManualLatestAt=now;assert(!VersionBadge.For(v,now).NeedsHelp);});
 }
}
