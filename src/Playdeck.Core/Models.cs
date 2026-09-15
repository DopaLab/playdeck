using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Text;

namespace Playdeck.Core;
public sealed class Game {
 public string Id {get;set;} = Guid.NewGuid().ToString("N");
 public string Name {get;set;} = "Untitled game";
 public string LaunchPath {get;set;} = "";
 public string TrackPath {get;set;} = "";
 public string Arguments {get;set;} = "";
 public string WorkingDirectory {get;set;} = "";
 public string IconPath {get;set;} = "";
 public int IconIndex {get;set;}
 public string CoverPath {get;set;} = "";
 public string Source {get;set;} = "Local";
 public int SteamId {get;set;}
 public DateTimeOffset Added {get;set;} = DateTimeOffset.UtcNow;
 public DateTime? ReleaseDate {get;set;}
 public bool Archived {get;set;}
 public bool Favorite {get;set;}
 public int PinOrder {get;set;} = int.MaxValue;
 public bool Removed {get;set;}
 public DateTimeOffset? TrashedAt {get;set;}
 public bool PinBeforeTrash {get;set;}
 public string InstallRoot {get;set;} = "";
 public bool MetadataAttempted {get;set;}
 public string MetadataStatus {get;set;} = "Local artwork";
 public string Notes {get;set;} = "";
 public VersionRecord VersionInfo {get;set;} = new();
}
public sealed class Library {
 public int Version {get;set;} = 4;
 public string UserName {get;set;} = "Player one";
 public string AvatarPath {get;set;} = "";
 public List<Game> Games {get;set;} = [];
 public List<string> Folders {get;set;} = [];
 public List<string> ExcludedFolders {get;set;} = [];
 public bool OnePerFolder {get;set;} = true;
 public HashSet<string> Ignored {get;set;} = new(StringComparer.OrdinalIgnoreCase);
 public bool Online {get;set;} = true;
 public bool CompactCards {get;set;}
 public int SortIndex {get;set;}
}
public sealed class Session {
 public string Id {get;set;} = Guid.NewGuid().ToString("N");
 public string GameId {get;set;} = "";
 public DateTimeOffset Started {get;set;} = DateTimeOffset.UtcNow;
 public DateTimeOffset Updated {get;set;} = DateTimeOffset.UtcNow;
 public double Seconds {get;set;}
 public string Status {get;set;} = "Waiting";
 public int TrackerPid {get;set;}
 public long TrackerBornUtcTicks {get;set;}
 public double ForegroundSeconds {get;set;}
 public bool HasFocusData {get;set;}
 public Dictionary<string,double> DailySeconds {get;set;}=[];
}
public sealed class LaunchRequest {
 public Game Game {get;set;} = new();
 public string DataRoot {get;set;} = "";
 public string ReplyPath {get;set;} = "";
}
public static class Store {
 public static readonly JsonSerializerOptions Json = new() {WriteIndented = true, PropertyNameCaseInsensitive = true};
 public static string DefaultRoot => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Playdeck");
 public static void Atomic<T>(string path,T value) {
  Directory.CreateDirectory(Path.GetDirectoryName(path)!);
  string temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
  try { File.WriteAllText(temp,JsonSerializer.Serialize(value,Json)); File.Move(temp,path,true); }
  finally { if(File.Exists(temp)) File.Delete(temp); }
 }
 public static Library Load(string root) {
  string path = Path.Combine(root,"library.json");
  if(!File.Exists(path)) return new();
  try { return JsonSerializer.Deserialize<Library>(File.ReadAllText(path),Json) ?? throw new InvalidDataException("Empty library."); }
  catch(Exception e) when(e is JsonException or InvalidDataException) {
   throw new InvalidDataException("Library could not be read. Your file has been preserved. Restore library.json.bak from the data folder.",e);
  }
 }
 public static void Save(string root,Library library) {
  Directory.CreateDirectory(root);
  string path=Path.Combine(root,"library.json");
  if(File.Exists(path)) File.Copy(path,path+".bak",true);
  Atomic(path,library);
 }
 public static List<Session> Sessions(string root) {
  string dir=Path.Combine(root,"sessions"); if(!Directory.Exists(dir)) return [];
  var result=new List<Session>();
  foreach(var f in Directory.EnumerateFiles(dir,"*.json")) try {
   var s=JsonSerializer.Deserialize<Session>(File.ReadAllText(f),Json); if(s==null) continue;
   if(s.Status is "Playing" or "Waiting") {
    bool alive=false; try {using var p=Process.GetProcessById(s.TrackerPid); alive=!p.HasExited && (s.TrackerBornUtcTicks>0?p.StartTime.ToUniversalTime().Ticks==s.TrackerBornUtcTicks:p.StartTime.ToUniversalTime()>=s.Started.UtcDateTime.AddSeconds(-10));} catch { }
    if(!alive) s.Status="Interrupted";
   }
   result.Add(s);
  } catch(IOException) {} catch(JsonException) {}
  return result;
 }
 public static string Key(string path) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(path.ToLowerInvariant())))[..20];
}
public static partial class Names {
 public static string Clean(string text) {
  text=Regex.Replace(text,@"(?i)(\s*[-–]?\s*shortcut|\s*\(\d+\)|\s*[-–]\s*(?:fitgirl repack|dodi repack))$", "");
  text=text.Replace('_',' '); return Regex.Replace(text,@"\s+"," ").Trim();
 }
 public static string Normalize(string text) => Regex.Replace(Clean(text).ToLowerInvariant(),@"[^\p{L}\p{Nd}]","");
 public static bool Helper(string path) => Regex.IsMatch(Path.GetFileNameWithoutExtension(path),@"(?i)(^|[-_ .])(unins|uninstall|crash|reporter|redist|setup|installer|dxsetup|vcredist|benchmark|server|anticheat|easyanticheat|unitycrash|cefsubprocess|notification|playdeck)");
}
public static class LibraryActions {
 public static void Pin(Library library,Game game,bool pinned){var others=library.Games.Where(g=>g.Favorite&&g.Id!=game.Id).OrderBy(g=>g.PinOrder).ThenBy(g=>g.Added).ToList();for(int i=0;i<others.Count;i++)others[i].PinOrder=i;game.Favorite=pinned;if(pinned)game.PinOrder=others.Count;}
 public static void MovePin(Library library,string moving,string before,bool after=false){
  var pins=library.Games.Where(g=>g.Favorite&&!g.Removed&&!g.Archived).OrderBy(g=>g.PinOrder).ThenBy(g=>g.Added).ToList();
  var game=pins.FirstOrDefault(g=>g.Id==moving);var target=pins.FirstOrDefault(g=>g.Id==before);if(game==null||target==null||game==target)return;
  pins.Remove(game);pins.Insert(pins.IndexOf(target)+(after?1:0),game);for(int i=0;i<pins.Count;i++)pins[i].PinOrder=i;
 }
 public static void Remove(Library library,Game game){if(game.Removed)return;game.PinBeforeTrash=game.Favorite;game.Removed=true;game.TrashedAt=DateTimeOffset.UtcNow;game.Favorite=false;library.Ignored.Add(game.LaunchPath);}
 public static void Restore(Library library,Game game){game.Removed=false;game.TrashedAt=null;game.Favorite=game.PinBeforeTrash;library.Ignored.Remove(game.LaunchPath);}
 public static void StartFresh(Library library){foreach(var game in library.Games.Where(g=>!g.Removed).ToArray())Remove(library,game);library.Folders.Clear();library.ExcludedFolders.Clear();}
 public static int ExpireTrash(Library library,DateTimeOffset now){return library.Games.RemoveAll(g=>g.Removed&&g.TrashedAt.HasValue&&now-g.TrashedAt.Value>=TimeSpan.FromDays(7));}
 public static bool Matches(Game game,string query)=>query.Split(new[]{',',';'},StringSplitOptions.RemoveEmptyEntries|StringSplitOptions.TrimEntries).Any(word=>(game.Name+" "+game.LaunchPath).Contains(word,StringComparison.OrdinalIgnoreCase));
}
