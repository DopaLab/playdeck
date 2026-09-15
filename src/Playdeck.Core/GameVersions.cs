using System.Diagnostics;
using System.Globalization;
using System.Net.Http;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Win32;
namespace Playdeck.Core;
public sealed class VersionRecord {
 public string Mode{get;set;}="Automatic";
 public string EvidencePath{get;set;}="";
 public string DetectedManifestPath{get;set;}="";
 public DateTimeOffset? CheckedAt{get;set;}
 public bool MatchesLatest{get;set;}
 public DateTimeOffset? ManualLatestAt{get;set;}
 public bool AutoCheck{get;set;}
 public bool LocalAvailable{get;set;}
 public string LocalPath{get;set;}="";
 public string EvidenceExecutable{get;set;}="";
 public bool InstallationReady{get;set;}=true;
 public string ManualVersion{get;set;}="";
 public string ManualLatest{get;set;}="";
 public string ManifestPath{get;set;}="";
 public string Installed{get;set;}="";
 public string Source{get;set;}="Not checked";
 public string InstalledBuild{get;set;}="";
 public string LatestBuild{get;set;}="";
 public string Branch{get;set;}="public";
 public int AppId{get;set;}
 public DateTimeOffset? ObservedAt{get;set;}
 public DateTimeOffset? RemoteCheckedAt{get;set;}
 public string Status{get;set;}="Not checked";
 public string Detail{get;set;}="";
 public bool NeedsUpdate{get;set;}
 [JsonIgnore]public string Summary=>Status+"  ·  "+(Installed.Length==0?"Version unknown":Installed);
}
public sealed class VdfNode {
 public string Value{get;set;}="";
 public Dictionary<string,VdfNode> Children{get;}=new(StringComparer.OrdinalIgnoreCase);
 public VdfNode? Node(string key)=>Children.GetValueOrDefault(key);
 public string Text(string key)=>Node(key)?.Value??"";
 public static VdfNode Parse(string text){
  if(text.Length>2*1024*1024)throw new InvalidDataException("Steam manifest is too large.");int pos=0,tokens=0;
  string? Token(out bool quoted){quoted=false;while(pos<text.Length){if(char.IsWhiteSpace(text[pos])||text[pos]=='\uFEFF'){pos++;continue;}if(text[pos]=='/'&&pos+1<text.Length&&text[pos+1]=='/'){while(pos<text.Length&&text[pos]!='\n')pos++;continue;}break;}if(pos>=text.Length)return null;if(++tokens>100000)throw new InvalidDataException("Steam manifest has too many tokens.");char c=text[pos++];if(c=='{'||c=='}')return c.ToString();if(c!='"')throw new InvalidDataException("Invalid Steam manifest token.");quoted=true;var b=new System.Text.StringBuilder();while(pos<text.Length){c=text[pos++];if(c=='"')return b.ToString();if(c=='\\'&&pos<text.Length){char next=text[pos];if(next=='\\'||next=='"'){b.Append(next);pos++;}else b.Append(c);}else b.Append(c);}throw new InvalidDataException("Unclosed Steam manifest string.");}
  VdfNode Read(int depth,bool nested){if(depth>16)throw new InvalidDataException("Steam manifest nesting exceeds the limit.");var n=new VdfNode();while(true){string? key=Token(out bool keyQuoted);if(key==null){if(nested)throw new InvalidDataException("Incomplete Steam manifest.");return n;}if(!keyQuoted&&key=="}"){if(!nested)throw new InvalidDataException("Unexpected closing brace.");return n;}if(!keyQuoted&&key=="{")throw new InvalidDataException("Missing Steam manifest key.");string? value=Token(out bool valueQuoted);if(value==null||(!valueQuoted&&value=="}"))throw new InvalidDataException("Missing Steam manifest value.");if(!n.Children.TryAdd(key,!valueQuoted&&value=="{"?Read(depth+1,true):new VdfNode{Value=value}))throw new InvalidDataException("Duplicate Steam manifest key.");}}
  return Read(0,false);
 }
}
public sealed class BuildCache {
 public int AppId{get;set;}
 public DateTimeOffset? CheckedAt{get;set;}
 public DateTimeOffset RetryAfter{get;set;}
 public Dictionary<string,string> Branches{get;set;}=new(StringComparer.OrdinalIgnoreCase);
 public string Error{get;set;}="";
}
public sealed class GameVersions:IDisposable {
 readonly HttpClient http;readonly SemaphoreSlim gate=new(1,1);DateTimeOffset nextRequest;
 public GameVersions(HttpMessageHandler? handler=null){http=handler==null?new HttpClient():new HttpClient(handler);http.Timeout=TimeSpan.FromSeconds(12);http.MaxResponseContentBufferSize=2*1024*1024;http.DefaultRequestHeaders.UserAgent.ParseAdd("Playdeck/6.2 (game-version-check)");}
 public void Dispose(){http.Dispose();}
 public static int? CompareLabels(string installed,string latest){
  static (BigInteger[] Numbers,string? Pre)? Parse(string text){if(text.Length>120)return null;var m=Regex.Match(text.Trim(),@"\Av?([0-9]+(?:\.[0-9]+){1,3})(?:-([0-9A-Za-z.-]+))?(?:\+[0-9A-Za-z.-]+)?\z",RegexOptions.CultureInvariant);if(!m.Success||text.Length>120)return null;var a=m.Groups[1].Value.Split('.').Select(BigInteger.Parse).Concat(Enumerable.Repeat(BigInteger.Zero,4)).Take(4).ToArray();return(a,m.Groups[2].Success?m.Groups[2].Value:null);}
  var a=Parse(installed);var b=Parse(latest);if(a==null||b==null)return installed.Trim().Equals(latest.Trim(),StringComparison.OrdinalIgnoreCase)?0:null;for(int i=0;i<4;i++){int c=b.Value.Numbers[i].CompareTo(a.Value.Numbers[i]);if(c!=0)return c;}if(a.Value.Pre==b.Value.Pre)return 0;if(a.Value.Pre==null)return -1;if(b.Value.Pre==null)return 1;var ap=a.Value.Pre.Split('.');var bp=b.Value.Pre.Split('.');for(int i=0;i<Math.Min(ap.Length,bp.Length);i++){bool an=BigInteger.TryParse(ap[i],out var ai),bn=BigInteger.TryParse(bp[i],out var bi);int c=an&&bn?bi.CompareTo(ai):an!=bn?(bn?-1:1):string.Compare(bp[i],ap[i],StringComparison.Ordinal);if(c!=0)return c;}return bp.Length.CompareTo(ap.Length);
 }
 static string ReadText(string path){if(new FileInfo(path).Length>2*1024*1024)throw new InvalidDataException("Steam metadata file is too large.");return File.ReadAllText(path);}
 public static VersionRecord ReadLocal(Game game,IEnumerable<string>? steamRoots=null,CancellationToken stop=default){
  var v=JsonSerializer.Deserialize<VersionRecord>(JsonSerializer.Serialize(game.VersionInfo,Store.Json),Store.Json)!;v.CheckedAt=DateTimeOffset.UtcNow;v.MatchesLatest=false;v.LocalAvailable=false;v.LocalPath="";v.ObservedAt=DateTimeOffset.UtcNow;v.Installed="";v.InstalledBuild="";v.LatestBuild="";v.RemoteCheckedAt=null;v.NeedsUpdate=false;v.InstallationReady=true;v.AppId=game.SteamId;v.Branch="public";v.Detail="";
  if(v.Mode=="Manual"){v.Installed=v.ManualVersion.Trim();v.LocalAvailable=v.Installed.Length>0;v.Source="Manual entry";v.Status=v.Installed.Length>0?"Recorded manually":"Enter an installed version";if(v.Installed.Length>0&&v.ManualLatest.Length>0){int? result=CompareLabels(v.Installed,v.ManualLatest);v.NeedsUpdate=result>0;v.MatchesLatest=result==0;v.Status=result==0?"Matches your latest version":result>0?"Newer version noted":result<0?"Installed version is newer":"Different labels · cannot order";v.Detail="Compared only with the latest version you entered, not a Steam build ID.";}return v;}
  string exe=!string.IsNullOrWhiteSpace(game.TrackPath)?game.TrackPath:game.LaunchPath;
  if(v.Mode=="File"){
   v.Source="Confirmed version file";v.LocalPath=v.EvidencePath;v.Status="Version file unavailable";
   try{v.Installed=VersionDiscovery.ReadLabel(v.EvidencePath)??"";v.LocalAvailable=v.Installed.Length>0;v.Status=v.LocalAvailable?"Recorded · latest version needed":"No readable version in this file";v.Detail="User-confirmed source: "+v.EvidencePath;if(v.LocalAvailable&&v.ManualLatest.Length>0){int? c=CompareLabels(v.Installed,v.ManualLatest);v.NeedsUpdate=c>0;v.MatchesLatest=c==0;v.Status=c==0?"Matches your latest version":c>0?"Newer version noted":c<0?"Installed version is newer":"Different labels · cannot order";}}
   catch(Exception e)when(e is IOException or UnauthorizedAccessException or JsonException or InvalidDataException){v.Detail=e.Message;}
   if(!v.LocalAvailable&&game.VersionInfo.EvidencePath==v.EvidencePath){v.Installed=game.VersionInfo.Installed;v.ObservedAt=game.VersionInfo.ObservedAt;v.Status="Source unavailable · last known version";}return v;
  }
  if(v.Mode!="Executable"){
   var candidates=new HashSet<string>(StringComparer.OrdinalIgnoreCase);bool explicitManifest=!string.IsNullOrWhiteSpace(v.ManifestPath);if(explicitManifest)candidates.Add(v.ManifestPath);
   if(!explicitManifest&&v.DetectedManifestPath.Length>0)candidates.Add(v.DetectedManifestPath);
   if(!explicitManifest&&candidates.Count==0&&game.SteamId==0){foreach(var evidence in VersionDiscovery.Find(game,steamRoots,stop:stop).Where(e=>e.Kind=="Steam"))candidates.Add(evidence.Path);}
   if(!explicitManifest&&game.SteamId>0){try{for(var dir=Directory.GetParent(exe);dir!=null;dir=dir.Parent)if(dir.Name.Equals("steamapps",StringComparison.OrdinalIgnoreCase))candidates.Add(Path.Combine(dir.FullName,$"appmanifest_{game.SteamId}.acf"));}catch{}foreach(var root in steamRoots??SteamRoots())candidates.Add(Path.Combine(root,"steamapps",$"appmanifest_{game.SteamId}.acf"));}
   foreach(string path in candidates){stop.ThrowIfCancellationRequested();if(!File.Exists(path))continue;try{var app=VersionDiscovery.Manifest(path).Node("AppState")??throw new InvalidDataException("No AppState section.");if(!int.TryParse(app.Text("appid"),out int id)||id<=0)continue;if(game.SteamId>0&&id!=game.SteamId){if(explicitManifest){v.Source="Steam app manifest";v.Status="Manifest AppID mismatch";v.Detail="Selected manifest belongs to AppID "+id+", not "+game.SteamId+". Choose the matching game manifest.";return v;}continue;}if(!ulong.TryParse(app.Text("buildid"),out ulong build)||build==0)continue;
    string install=app.Text("installdir");if(!explicitManifest){if(install.Length==0||Path.IsPathRooted(install)||install.Contains(".."))continue;string folder=Path.Combine(Path.GetDirectoryName(path)!,"common",install);bool matchingPath=Discovery.Within(exe,folder);bool matchingShortcut=false;if(Path.GetExtension(game.LaunchPath).Equals(".url",StringComparison.OrdinalIgnoreCase)){var link=Discovery.Read(game.LaunchPath);matchingShortcut=link?.SteamId==id;}if(!matchingPath&&!matchingShortcut)continue;}
    v.InstallationReady=ulong.TryParse(app.Text("StateFlags"),out ulong flags)&&(flags&4)!=0;v.LocalAvailable=true;v.LocalPath=path;if(!explicitManifest)v.DetectedManifestPath=path;v.EvidenceExecutable=exe;v.AppId=id;v.Source="Steam app manifest";v.InstalledBuild=build.ToString(CultureInfo.InvariantCulture);v.Installed="Build "+v.InstalledBuild;v.Branch=app.Node("UserConfig")?.Text("betakey")??"";if(v.Branch.Length==0)v.Branch=app.Node("MountedConfig")?.Text("betakey")??"";if(v.Branch.Length==0)v.Branch="public";v.Status=v.InstallationReady?"Installed build recorded":"Steam installation is incomplete";v.Detail="Read-only manifest: "+path;return v;
   }catch(Exception e) when(e is IOException or UnauthorizedAccessException or InvalidDataException){v.Detail="Manifest unavailable: "+e.Message;}}
  }
  v.Source="Executable metadata";v.Status="Version unavailable";try{if(File.Exists(exe)&&Path.GetExtension(exe).Equals(".exe",StringComparison.OrdinalIgnoreCase)){var info=FileVersionInfo.GetVersionInfo(exe);v.Installed=(string.IsNullOrWhiteSpace(info.ProductVersion)?info.FileVersion:info.ProductVersion)?.Trim()??"";v.LocalAvailable=v.Installed.Length>0;v.LocalPath=exe;v.Status=v.Installed.Length>0?"Recorded · no comparable online version":"Executable has no version metadata";v.Detail="Product version: "+info.ProductVersion+" · File version: "+info.FileVersion+". These may describe an engine or launcher, not the game release.";}else{v.Status="Executable unavailable";v.Detail="Choose the actual tracking executable or record a version manually.";}}catch(Exception e) when(e is IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception){v.Detail=e.Message;}
  var prior=game.VersionInfo;if(!v.LocalAvailable&&prior.Installed.Length>0&&prior.AppId==game.SteamId&&prior.LocalPath.Length>0&&(prior.LocalPath.Equals(exe,StringComparison.OrdinalIgnoreCase)||prior.LocalPath.Equals(v.ManifestPath,StringComparison.OrdinalIgnoreCase)||(prior.EvidenceExecutable.Length>0&&prior.EvidenceExecutable.Equals(exe,StringComparison.OrdinalIgnoreCase)))){v.Installed=prior.Installed;v.InstalledBuild=prior.InstalledBuild;v.LatestBuild=prior.LatestBuild;v.Source=prior.Source;v.LocalPath=prior.LocalPath;v.Branch=prior.Branch;v.ObservedAt=prior.ObservedAt;v.RemoteCheckedAt=prior.RemoteCheckedAt;v.Status="Source unavailable · last known version";v.Detail+=" Last known evidence retained; current installation cannot be verified.";}return v;
 }
 public static IEnumerable<string> SteamRoots(){var result=new HashSet<string>(StringComparer.OrdinalIgnoreCase);try{using var key=Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");if(key?.GetValue("SteamPath") is string path)result.Add(path);}catch{}foreach(var root in result.ToArray()){try{var folders=VdfNode.Parse(ReadText(Path.Combine(root,"steamapps","libraryfolders.vdf"))).Node("libraryfolders");if(folders!=null)foreach(var entry in folders.Children.Values){string p=entry.Text("path");if(p.Length>0)result.Add(p);}}catch{}}return result;}
 public static BuildCache ParseRemote(string json,int appId,DateTimeOffset now){using var doc=JsonDocument.Parse(json,new JsonDocumentOptions{MaxDepth=64});var root=doc.RootElement;if(!root.TryGetProperty("status",out var status)||status.GetString()!="success"||!root.TryGetProperty("data",out var data)||!data.TryGetProperty(appId.ToString(CultureInfo.InvariantCulture),out var app)||!app.TryGetProperty("depots",out var depots)||!depots.TryGetProperty("branches",out var branches))throw new InvalidDataException("Provider returned no build information for this AppID.");var result=new BuildCache{AppId=appId,CheckedAt=now,RetryAfter=now.AddHours(24)};foreach(var branch in branches.EnumerateObject()){if(branch.Value.TryGetProperty("pwdrequired",out var pwd)&&pwd.ToString()=="1")continue;if(branch.Value.TryGetProperty("buildid",out var id)&&ulong.TryParse(id.ToString(),out ulong n)&&n>0)result.Branches[branch.Name]=n.ToString(CultureInfo.InvariantCulture);}return result;}
 public async Task<VersionRecord> Check(Game game,string root,bool online,CancellationToken stop=default){
  var v=await Task.Run(()=>ReadLocal(game,stop:stop),stop);if(v.Mode is "Manual" or "File"||!v.LocalAvailable||v.AppId<=0||v.InstalledBuild.Length==0||!online)return v;
  await gate.WaitAsync(stop);try{string file=Path.Combine(root,"version-cache",v.AppId+".json");BuildCache cache=new(){AppId=v.AppId};try{if(File.Exists(file))cache=JsonSerializer.Deserialize<BuildCache>(ReadText(file),Store.Json)??cache;}catch(Exception e)when(e is IOException or JsonException or InvalidDataException){}if(cache.AppId!=v.AppId||cache.Branches==null)cache=new(){AppId=v.AppId};var now=DateTimeOffset.UtcNow;
   if(cache.RetryAfter<=now){try{var delay=nextRequest-DateTimeOffset.UtcNow;if(delay>TimeSpan.Zero)await Task.Delay(delay,stop);nextRequest=DateTimeOffset.UtcNow.AddSeconds(1);using var response=await http.GetAsync("https://api.steamcmd.net/v1/info/"+v.AppId,stop);if(!response.IsSuccessStatusCode){var retry=response.Headers.RetryAfter;var until=retry?.Date??DateTimeOffset.UtcNow+(retry?.Delta??TimeSpan.FromHours(24));cache.RetryAfter=until>DateTimeOffset.UtcNow.AddHours(24)?until:DateTimeOffset.UtcNow.AddHours(24);response.EnsureSuccessStatusCode();}string json=await response.Content.ReadAsStringAsync(stop);cache=ParseRemote(json,v.AppId,now);}catch(OperationCanceledException)when(stop.IsCancellationRequested){throw;}catch(Exception e)when(e is HttpRequestException or TaskCanceledException or JsonException or InvalidDataException or InvalidOperationException){cache.Error="Build lookup unavailable; cached evidence retained.";if(cache.RetryAfter<now.AddHours(24))cache.RetryAfter=now.AddHours(24);}stop.ThrowIfCancellationRequested();Store.Atomic(file,cache);}
   v.RemoteCheckedAt=cache.CheckedAt;v.LatestBuild=cache.Branches.FirstOrDefault(p=>p.Key.Equals(v.Branch,StringComparison.OrdinalIgnoreCase)).Value??"";
   if(!ulong.TryParse(v.LatestBuild,out ulong latestBuild)||latestBuild==0)v.LatestBuild="";
   if(v.InstalledBuild.Length>0){if(!v.InstallationReady){v.Status="Steam installation is incomplete";}else if(cache.Error.Length>0){v.Status="Online check unavailable";v.Detail+="\n"+cache.Error;}else if(v.LatestBuild.Length==0){v.Status="Branch build unavailable";v.Detail+="\nNo public build information for branch "+v.Branch;}else{ulong installed=ulong.Parse(v.InstalledBuild),latest=ulong.Parse(v.LatestBuild);v.NeedsUpdate=latest>installed;v.MatchesLatest=latest==installed;v.Status=latest==installed?"Matches reported Steam build":latest>installed?"Newer Steam build reported":"Different build · rollback or branch change";}}
   v.Detail+="\nOnline source: SteamCMD API (third party). AppID "+v.AppId+", branch "+v.Branch+". Executable version labels are never compared with Steam build IDs.";return v;
  }finally{gate.Release();}
 }
}
