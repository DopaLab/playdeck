using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
namespace Playdeck.Core;

public sealed record VersionEvidence(string Source,string Value,string Path,string Kind,int AppId=0) {
 public string Label=>Source+" · "+Value;
}
// Deliberately bounded, read-only evidence discovery. Never searches game contents recursively.
public static class VersionDiscovery {
 static readonly object cacheLock=new();
 static readonly Dictionary<string,(long Stamp,long Length,VdfNode Node)> manifests=new(StringComparer.OrdinalIgnoreCase);
 static long manifestBytes;
 public static VdfNode Manifest(string path){var info=new FileInfo(path);lock(cacheLock){if(manifests.TryGetValue(path,out var entry)&&entry.Stamp==info.LastWriteTimeUtc.Ticks&&entry.Length==info.Length)return entry.Node;string text=Read(path);var parsed=VdfNode.Parse(text);if(manifests.Remove(path,out var old))manifestBytes-=old.Length;if(manifests.Count>=512||manifestBytes+info.Length>4*1024*1024){manifests.Clear();manifestBytes=0;}manifests[path]=(info.LastWriteTimeUtc.Ticks,info.Length,parsed);manifestBytes+=info.Length;return parsed;}}
 public static string Executable(Game game)=>string.IsNullOrWhiteSpace(game.TrackPath)?game.LaunchPath:game.TrackPath;
 static readonly string[] names={"version.txt","version","build.txt","build.version","version.json","gameinfo.json","app.info"};
 public static string Read(string path){var info=new FileInfo(path);if(info.Length>256*1024)throw new InvalidDataException("Version file exceeds 256 KiB.");return File.ReadAllText(path);}
 static IEnumerable<string> Files(string directory,string pattern,int limit){try{if(!Directory.Exists(directory)||(File.GetAttributes(directory)&FileAttributes.ReparsePoint)!=0)return [];return Directory.EnumerateFiles(directory,pattern,SearchOption.TopDirectoryOnly).Take(limit).ToArray();}catch(Exception e)when(e is IOException or UnauthorizedAccessException){return [];}}
 static string? Label(string value){value=value.Trim().Trim('"');return value.Length is >0 and <=120&&!value.Any(char.IsControl)&&Regex.IsMatch(value,@"\d",RegexOptions.CultureInvariant)?value:null;}
 public static string? ReadLabel(string path){
  string text=Read(path);if(Path.GetExtension(path).ToLowerInvariant() is ".json" or ".info" or ".item"){
   using var doc=JsonDocument.Parse(text,new JsonDocumentOptions{MaxDepth=16});if(doc.RootElement.ValueKind!=JsonValueKind.Object)return null;
   // 'version' in goggame-*.info is the schema version, NOT the installed game version.
   string[] keys=Path.GetFileName(path).StartsWith("goggame-",StringComparison.OrdinalIgnoreCase)?["buildVersion","buildId"]:["AppVersionString","buildVersion","productVersion","gameVersion","version"];
   foreach(var key in keys)foreach(var p in doc.RootElement.EnumerateObject())if(p.Name.Equals(key,StringComparison.OrdinalIgnoreCase)&&p.Value.ValueKind is JsonValueKind.String or JsonValueKind.Number)return Label(p.Value.ToString());return null;
  }
  var lines=text.Split('\n').Select(l=>l.Trim()).Where(l=>l.Length>0&&!l.StartsWith('#')&&!l.StartsWith("//")).Take(32).ToArray();
  foreach(string line in lines){var match=Regex.Match(line,@"^(?:game[_ ]?version|product[_ ]?version|version|build(?:id|version)?)\s*[:=]\s*(.{1,120})$",RegexOptions.IgnoreCase|RegexOptions.CultureInvariant);if(match.Success)return Label(match.Groups[1].Value);}
  return lines.Length==1&&Regex.IsMatch(lines[0],@"^v?\d[\w.+-]{0,119}$",RegexOptions.CultureInvariant)?Label(lines[0]):null;
 }
 public static IReadOnlyList<VersionEvidence> Find(Game game,IEnumerable<string>? steamRoots=null,string? epicDirectory=null,CancellationToken stop=default){
  var found=new List<VersionEvidence>();string exe=Executable(game);var dirs=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
  try{var d=Directory.GetParent(exe);for(int n=0;d!=null&&n<4;n++,d=d.Parent){if(d.Parent==null||new[]{"common","steamapps","games","epic games","gog games","desktop","program files","program files (x86)"}.Contains(d.Name,StringComparer.OrdinalIgnoreCase))break;dirs.Add(d.FullName);}}catch{}
  if(game.InstallRoot.Length>0&&Discovery.Within(exe,game.InstallRoot)&&Directory.GetParent(game.InstallRoot)!=null)dirs.Add(game.InstallRoot);
  var roots=(steamRoots??GameVersions.SteamRoots()).Take(16).ToHashSet(StringComparer.OrdinalIgnoreCase);
  try{for(var d=Directory.GetParent(exe);d!=null;d=d.Parent)if(d.Name.Equals("steamapps",StringComparison.OrdinalIgnoreCase)&&d.Parent!=null)roots.Add(d.Parent.FullName);}catch{}
  int manifestBudget=512;
  foreach(var root in roots){foreach(string file in Files(Path.Combine(root,"steamapps"),"appmanifest_*.acf",Math.Max(0,manifestBudget))){stop.ThrowIfCancellationRequested();manifestBudget--;try{var app=Manifest(file).Node("AppState");if(app==null||!int.TryParse(app.Text("appid"),out int id)||id<=0)continue;string dir=app.Text("installdir");if(dir.Length==0||Path.IsPathRooted(dir)||dir.Contains(".."))continue;string folder=Path.Combine(root,"steamapps","common",dir);bool matches=Discovery.Within(exe,folder);if(!matches&&game.SteamId==id&&Path.GetExtension(game.LaunchPath).Equals(".url",StringComparison.OrdinalIgnoreCase))matches=Discovery.Read(game.LaunchPath)?.SteamId==id;if(!matches)continue;if(!ulong.TryParse(app.Text("buildid"),out ulong build)||build==0)continue;found.Add(new("Steam installation", "Build "+build,file,"Steam",id));dirs.Add(folder);}catch(Exception e)when(e is IOException or UnauthorizedAccessException or InvalidDataException){}}}
  foreach(string dir in dirs.Take(6)){stop.ThrowIfCancellationRequested();foreach(string file in names.Select(n=>Path.Combine(dir,n)).Concat(Files(dir,"goggame-*.info",8))){if(!File.Exists(file))continue;try{string? value=ReadLabel(file);if(value!=null)found.Add(new(Path.GetFileName(file),value,file,"File"));}catch(Exception e)when(e is IOException or UnauthorizedAccessException or JsonException or InvalidDataException){}}}
  epicDirectory??=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),"Epic","EpicGamesLauncher","Data","Manifests");
  foreach(string file in Files(epicDirectory,"*.item",256)){stop.ThrowIfCancellationRequested();try{using var doc=JsonDocument.Parse(Read(file),new JsonDocumentOptions{MaxDepth=16});var obj=doc.RootElement;if(!obj.TryGetProperty("InstallLocation",out var location)||location.ValueKind!=JsonValueKind.String||string.IsNullOrWhiteSpace(location.GetString())||!Discovery.Within(exe,location.GetString()!))continue;string? value=ReadLabel(file);if(value!=null)found.Add(new("Epic installation",value,file,"File"));}catch(Exception e)when(e is IOException or UnauthorizedAccessException or JsonException or InvalidDataException or InvalidOperationException){}}
  stop.ThrowIfCancellationRequested();try{if(File.Exists(exe)&&Path.GetExtension(exe).Equals(".exe",StringComparison.OrdinalIgnoreCase)){var info=FileVersionInfo.GetVersionInfo(exe);string? value=Label(info.ProductVersion??info.FileVersion??"");if(value!=null)found.Add(new("Executable · verify in the game",value,exe,"Executable"));}}catch(Exception e)when(e is IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception){}
  return found.DistinctBy(x=>x.Path,StringComparer.OrdinalIgnoreCase).Take(24).ToArray();
 }
}
