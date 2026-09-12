using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Net.Http;
using System.Text.Json;
using System.Globalization;

namespace Playdeck.Core;
public sealed class ScanResult {
 public List<Game> Games {get;}=[];
 public List<Game> AllGames {get;}=[];
 public List<string> Warnings {get;}=[];
}
public static class Discovery {
 public static bool Within(string path,string folder){try{string p=Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar);string f=Path.GetFullPath(folder).TrimEnd(Path.DirectorySeparatorChar);return p.Equals(f,StringComparison.OrdinalIgnoreCase)||p.StartsWith(f+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase);}catch{return false;}}
 public static ScanResult Scan(IEnumerable<string> folders,bool desktop=false,IEnumerable<string>? excluded=null,bool onePerFolder=true) {
  var result=new ScanResult(); var seen=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
  var exclusions=(excluded??[]).ToArray();
  foreach(string folder in folders.Distinct(StringComparer.OrdinalIgnoreCase)) Walk(folder,0,null);
  result.AllGames.AddRange(result.Games);
  if(!desktop&&onePerFolder){var chosen=result.Games.Where(g=>!g.LaunchPath.EndsWith(".exe",StringComparison.OrdinalIgnoreCase)).ToList();chosen.AddRange(result.Games.Where(g=>g.LaunchPath.EndsWith(".exe",StringComparison.OrdinalIgnoreCase)).GroupBy(g=>g.InstallRoot,StringComparer.OrdinalIgnoreCase).Select(group=>group.OrderByDescending(Score).ThenBy(g=>g.LaunchPath,StringComparer.OrdinalIgnoreCase).First()));result.Games.Clear();result.Games.AddRange(chosen);}
  return result;
  void Walk(string dir,int depth,string? gameRoot) {
   if(exclusions.Any(x=>Within(dir,x)))return;
   if(depth>5 || result.Games.Count>=1000) {result.Warnings.Add("Scan limit reached in "+dir);return;}
   try {
    if(!Directory.Exists(dir)) {result.Warnings.Add("Folder unavailable: "+dir); return;}
    var files=Directory.EnumerateFiles(dir).ToArray();bool hasExe=files.Any(f=>Path.GetExtension(f).Equals(".exe",StringComparison.OrdinalIgnoreCase)&&!Names.Helper(f));
    foreach(var f in files) {
     string ext=Path.GetExtension(f).ToLowerInvariant();
     if(ext is not (".exe" or ".lnk" or ".url") || Names.Helper(f)) continue;
     var g=Read(f); if(g==null || (desktop && !LikelyGame(g))) continue;
     g.InstallRoot=gameRoot??dir;
     string key=string.IsNullOrWhiteSpace(g.TrackPath)?g.LaunchPath:g.TrackPath+"|"+g.Arguments;
     if(seen.Add(key)) result.Games.Add(g);
    }
    if(!desktop) foreach(var sub in Directory.EnumerateDirectories(dir)) {
     if((File.GetAttributes(sub)&FileAttributes.ReparsePoint)!=0) continue;
     string name=Path.GetFileName(sub).ToLowerInvariant();
     if(name is "windows" or "node_modules" or "redist" or "_commonredist" or "support" or "__installer" or ".git") continue;
     string? next=gameRoot;
     if(next==null){if(name is "binaries" or "bin" or "win64" or "win32" or "x64" or "engine" or "content")next=dir;else if(name is not ("steamapps" or "common" or "games" or "epic games" or "gog games"))next=sub;}
     Walk(sub,depth+1,next);
    }
   } catch(Exception e) when(e is IOException or UnauthorizedAccessException) {result.Warnings.Add("Cannot scan: "+dir);}
  }
 }
 static int Score(Game g){string file=Names.Normalize(Path.GetFileNameWithoutExtension(g.LaunchPath));string folder=Names.Normalize(Path.GetFileName(g.InstallRoot));int score=file==folder?100:file.StartsWith(folder,StringComparison.OrdinalIgnoreCase)&&folder.Length>2?60:0;if(file.Contains("shipping"))score+=35;if(file.Contains("launcher"))score-=30;if(Path.GetDirectoryName(g.LaunchPath)==g.InstallRoot)score+=15;try{score+=(int)Math.Min(20,Math.Log10(Math.Max(1,new FileInfo(g.LaunchPath).Length))*2);}catch{}return score;}
 public static bool LikelyGame(Game g) => g.SteamId>0 || Regex.IsMatch(g.TrackPath,@"(?i)(steamapps|epic games|gog games|xboxgames|\\games\\|win64-shipping)") || g.Source is "Epic" or "GOG";
 public static Game? Read(string file,bool explicitImport=false) {
  try {
   var g=new Game {Name=Names.Clean(Path.GetFileNameWithoutExtension(file)), LaunchPath=Path.GetFullPath(file), WorkingDirectory=Path.GetDirectoryName(file)!};
   string ext=Path.GetExtension(file).ToLowerInvariant();
   if(ext==".exe") {g.TrackPath=g.LaunchPath;g.IconPath=g.LaunchPath;}
   else if(ext==".lnk") {
    Type? t=Type.GetTypeFromProgID("WScript.Shell"); if(t==null)return null;
    object shell=Activator.CreateInstance(t)!; object? shortcut=null;
    try {dynamic s=shell; shortcut=s.CreateShortcut(file);dynamic l=shortcut;
     string target=l.TargetPath; string args=l.Arguments;
     if(string.IsNullOrWhiteSpace(target)||(!explicitImport&&Names.Helper(target))||!Path.GetExtension(target).Equals(".exe",StringComparison.OrdinalIgnoreCase)) return null;
     if(Path.GetExtension(target).Equals(".exe",StringComparison.OrdinalIgnoreCase)) g.TrackPath=target;
     g.Arguments=args;g.IconPath=target;
     string iconLocation=l.IconLocation;if(!string.IsNullOrWhiteSpace(iconLocation)){int comma=iconLocation.LastIndexOf(',');string iconFile=iconLocation;if(comma>=0&&int.TryParse(iconLocation[(comma+1)..],out int index)){g.IconIndex=index;iconFile=iconLocation[..comma];}iconFile=Environment.ExpandEnvironmentVariables(iconFile.Trim().Trim('"'));if(File.Exists(iconFile))g.IconPath=iconFile;}
 g.WorkingDirectory=string.IsNullOrWhiteSpace((string)l.WorkingDirectory)?Path.GetDirectoryName(target)??g.WorkingDirectory:(string)l.WorkingDirectory;
     var app=Regex.Match(args,@"-applaunch\s+(\d+)"); if(app.Success){g.SteamId=int.Parse(app.Groups[1].Value);g.TrackPath="";g.Source="Steam";}
    } finally {if(shortcut!=null)Marshal.FinalReleaseComObject(shortcut);Marshal.FinalReleaseComObject(shell);}
   } else if(ext==".url") {
    string text=File.ReadAllText(file); var url=Regex.Match(text,@"(?im)^URL=(.+)$"); if(!url.Success)return null;
    string uri=url.Groups[1].Value.Trim();
    if(!uri.StartsWith("steam://",StringComparison.OrdinalIgnoreCase)&&!uri.StartsWith("com.epicgames.launcher://",StringComparison.OrdinalIgnoreCase)&&!uri.StartsWith("goggalaxy://",StringComparison.OrdinalIgnoreCase))return null;
    var app=Regex.Match(uri,@"steam://(?:rungameid|run)/(\d+)",RegexOptions.IgnoreCase);
    if(app.Success){g.SteamId=int.Parse(app.Groups[1].Value);g.Source="Steam";}
    else g.Source=uri.StartsWith("com.epic",StringComparison.OrdinalIgnoreCase)?"Epic":"GOG";
    var icon=Regex.Match(text,@"(?im)^IconFile=(.+)$");if(icon.Success)g.IconPath=icon.Groups[1].Value.Trim();
   } else return null;
   return g;
  } catch(Exception e) when(e is IOException or UnauthorizedAccessException or COMException or FormatException) {return null;}
 }
}
public sealed record SteamMatch(int Id,string Name);
public sealed class Metadata : IDisposable {
 readonly HttpClient http=new(){Timeout=TimeSpan.FromSeconds(12)};
 public Metadata(){http.DefaultRequestHeaders.UserAgent.ParseAdd("Playdeck/1.0");}
 public async Task<List<SteamMatch>> Search(string query,string root,CancellationToken ct=default){query=query.Trim();if(query.Length<2)return [];query=query[..Math.Min(query.Length,128)];string file=Path.Combine(root,"search-cache",Store.Key(query.ToLowerInvariant())+".json");if(File.Exists(file)&&DateTime.UtcNow-File.GetLastWriteTimeUtc(file)<TimeSpan.FromDays(1)){try{return JsonSerializer.Deserialize<List<SteamMatch>>(await File.ReadAllTextAsync(file,ct))??[];}catch(JsonException){}}
 using var json=await GetJson("https://store.steampowered.com/api/storesearch/?term="+Uri.EscapeDataString(query)+"&l=english&cc=us",ct);var results=new List<SteamMatch>();if(json.RootElement.TryGetProperty("items",out var items))foreach(var item in items.EnumerateArray().Take(20))if(item.TryGetProperty("id",out var id)&&item.TryGetProperty("name",out var name))results.Add(new(id.GetInt32(),name.GetString()??"Untitled"));Store.Atomic(file,results);return results;}
 public async Task Fetch(Game g,string root,CancellationToken ct=default) {
  if(g.MetadataAttempted)return;
  g.MetadataAttempted=true;
  try {
   int id=g.SteamId;
   if(id==0) {
    using var search=await GetJson("https://store.steampowered.com/api/storesearch/?term="+Uri.EscapeDataString(g.Name)+"&l=english&cc=us",ct);
    if(!search.RootElement.TryGetProperty("items",out var items)){g.MetadataStatus="No exact match · local artwork";return;}
    var matches=items.EnumerateArray().Where(x=>Names.Normalize(x.GetProperty("name").GetString()??"")==Names.Normalize(g.Name)).ToList();
    if(matches.Count!=1){g.MetadataStatus="No exact match · local artwork";return;}
    id=matches[0].GetProperty("id").GetInt32();
   }
   using var details=await GetJson($"https://store.steampowered.com/api/appdetails?appids={id}&l=english",ct);
   var item=details.RootElement.GetProperty(id.ToString());
   if(!item.GetProperty("success").GetBoolean()){g.MetadataStatus="Unavailable · local artwork";return;}
   var data=item.GetProperty("data");
   if(data.GetProperty("type").GetString()!="game"){g.MetadataStatus="Not a game · local artwork";return;}
   g.SteamId=id;g.Name=data.GetProperty("name").GetString()??g.Name;
   if(data.TryGetProperty("release_date",out var release)&&release.TryGetProperty("date",out var date)&&DateTime.TryParse(date.GetString(),CultureInfo.GetCultureInfo("en-US"),DateTimeStyles.None,out var parsed))g.ReleaseDate=parsed;
   string cache=Path.Combine(root,"artwork");Directory.CreateDirectory(cache);
   string dest=Path.Combine(cache,g.Id+"-steam.jpg");
   string portrait=$"https://shared.fastly.steamstatic.com/store_item_assets/steam/apps/{id}/library_600x900.jpg";
   bool downloaded=await Download(portrait,dest,ct);
   if(!downloaded && data.TryGetProperty("header_image",out var header)) downloaded=await Download(header.GetString()!,dest,ct);
   if(downloaded)g.CoverPath=dest;
   g.MetadataStatus=downloaded?"Steam · cached":"Steam title · local artwork";
  } catch(OperationCanceledException){g.MetadataStatus="Lookup timed out · local artwork";}
  catch(Exception e) when(e is HttpRequestException or JsonException or IOException or KeyNotFoundException or InvalidOperationException){g.MetadataStatus="Offline / unavailable · local artwork";}
 }
 async Task<JsonDocument> GetJson(string url,CancellationToken ct){using var r=await http.GetAsync(url,ct);r.EnsureSuccessStatusCode();return JsonDocument.Parse(await r.Content.ReadAsStringAsync(ct));}
 async Task<bool> Download(string url,string path,CancellationToken ct){
  if(!Uri.TryCreate(url,UriKind.Absolute,out var uri)||uri.Scheme!="https"||!(uri.Host.EndsWith(".steamstatic.com",StringComparison.OrdinalIgnoreCase)||uri.Host.EndsWith(".steamusercontent.com",StringComparison.OrdinalIgnoreCase)))return false;
  using var response=await http.GetAsync(uri,HttpCompletionOption.ResponseHeadersRead,ct);
  if(!response.IsSuccessStatusCode || response.Content.Headers.ContentLength>8_000_000 || !(response.Content.Headers.ContentType?.MediaType?.StartsWith("image/")??false))return false;
  using var stream=await response.Content.ReadAsStreamAsync(ct);using var output=new MemoryStream();byte[] buffer=new byte[16384];int n;
  while((n=await stream.ReadAsync(buffer,ct))>0){if(output.Length+n>8_000_000)return false;await output.WriteAsync(buffer.AsMemory(0,n),ct);}
  await File.WriteAllBytesAsync(path,output.ToArray(),ct);return true;
 }
 public void Dispose()=>http.Dispose();
}
