using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
namespace Playdeck.Core;

public sealed record PublishedRelease(string Version,string Title,string Url,DateTimeOffset PublishedAt);
public sealed class ReleaseCache {
 public string Key{get;set;}="";
 public DateTimeOffset? CheckedAt{get;set;}
 public DateTimeOffset RetryAfter{get;set;}
 public List<PublishedRelease> Items{get;set;}=[];
 public string Error{get;set;}="";
}

// Publisher announcements are evidence of numbered releases, not a universal version database.
// All HTTP is serialized, bounded and cached, including empty results and failures.
public sealed class ReleaseVersions(HttpClient http) {
 readonly SemaphoreSlim gate=new(1,1);
 DateTimeOffset nextRequest;
 static readonly Regex number=new(@"(?<![\w.])v?(\d+(?:\.\d+){1,3})(?![\w.])",RegexOptions.IgnoreCase|RegexOptions.CultureInvariant,TimeSpan.FromMilliseconds(100));
 static readonly Regex excluded=new(@"\b(development|devlog|community|sale|discount|giveaway|livestream|contest|merch|survey|recap|beta|alpha|preview|experimental|test(?:ing)?|upcoming|coming|soon|planned|announc(?:e|ed|ement)|roadmap|tomorrow|next week|playtest|demo|soundtrack|DLC|Nintendo|Switch|Xbox|PlayStation|PS[345]|Android|iOS|Linux|macOS)\b",RegexOptions.IgnoreCase|RegexOptions.CultureInvariant,TimeSpan.FromMilliseconds(100));
 static readonly Regex releaseWords=new(@"\b(patch|hotfix|update|version|release|released)\b",RegexOptions.IgnoreCase|RegexOptions.CultureInvariant,TimeSpan.FromMilliseconds(100));
 public static string Repository(string value){
  value=value.Trim().TrimEnd('/');
  if(value.StartsWith("https://github.com/",StringComparison.OrdinalIgnoreCase))value=value[19..];
  return Regex.IsMatch(value,@"\A[A-Za-z0-9][A-Za-z0-9_.-]{0,99}/[A-Za-z0-9][A-Za-z0-9_.-]{0,99}\z")?value.ToLowerInvariant():"";
 }
 public static string SourceKey(Game game){var v=game.VersionInfo;string repo=Repository(v.GitHubRepository);if(v.GitHubRepository.Length>0)return repo.Length>0?"github:"+repo:"";int id=v.ReleaseAppId>0?v.ReleaseAppId:game.SteamId;return id>0?"steam-news:"+id:"";}
 public static bool SafeLink(string url)=>Uri.TryCreate(url,UriKind.Absolute,out var u)&&u.Scheme=="https"&&u.UserInfo.Length==0&&u.IsDefaultPort&&(u.Host is "steamcommunity.com" or "store.steampowered.com" or "steamstore-a.akamaihd.net" or "github.com");
 public static string ExtractVersion(string title){
  if(title.Length>600||excluded.IsMatch(title))return "";
  var matches=number.Matches(title).Select(m=>m.Groups[1].Value).Distinct().ToArray();
  if(matches.Length!=1)return "";
  // A date, sale or sequel number cannot be a release just because it has dots.
  if(!releaseWords.IsMatch(title)&&!Regex.IsMatch(title,@"^\s*v?\d+(?:\.\d+){1,3}\s*(?:$|[-:–])",RegexOptions.IgnoreCase))return "";
  string value=matches[0];if(int.TryParse(value.Split('.')[0],out int year)&&year is >=1990 and <=2100)return "";
  return value;
 }
 static string Text(JsonElement e,string key)=>e.TryGetProperty(key,out var p)&&p.ValueKind==JsonValueKind.String?p.GetString()??"":"";
 public static ReleaseCache Parse(string json,string key,DateTimeOffset now){
  if(json.Length>2*1024*1024)throw new InvalidDataException("Release response is too large.");
  using var doc=JsonDocument.Parse(json,new JsonDocumentOptions{MaxDepth=32});
  var result=new ReleaseCache{Key=key,CheckedAt=now,RetryAfter=now.AddHours(24)};
  if(key.StartsWith("steam-news:",StringComparison.Ordinal)){
   if(!doc.RootElement.TryGetProperty("appnews",out var app)||!app.TryGetProperty("appid",out var id)||id.ToString()!=key[11..]||!app.TryGetProperty("newsitems",out var items)||items.ValueKind!=JsonValueKind.Array)throw new InvalidDataException("News response belongs to another game or has no feed.");
   foreach(var item in items.EnumerateArray().Take(100)){
    string title=Text(item,"title"),url=Text(item,"url");if(Text(item,"feedname")!="steam_community_announcements"||title.Length>600||!SafeLink(url)||excluded.IsMatch(title))continue;
    if(!item.TryGetProperty("date",out var date)||!date.TryGetInt64(out long seconds)||seconds<0||seconds>now.ToUnixTimeSeconds())continue;
    string version=ExtractVersion(title);if(version.Length==0&&!releaseWords.IsMatch(title))continue;
    result.Items.Add(new(version,title,url,DateTimeOffset.FromUnixTimeSeconds(seconds)));
   }
  }else if(key.StartsWith("github:",StringComparison.Ordinal)){
   if(doc.RootElement.ValueKind!=JsonValueKind.Array)throw new InvalidDataException("Release provider returned no release list.");
   foreach(var item in doc.RootElement.EnumerateArray().Take(30)){
    if(!item.TryGetProperty("draft",out var draft)||draft.ValueKind!=JsonValueKind.False||!item.TryGetProperty("prerelease",out var pre)||pre.ValueKind!=JsonValueKind.False)continue;
    string title=Text(item,"name"),tag=Text(item,"tag_name"),url=Text(item,"html_url");if(!SafeLink(url)||!url.StartsWith("https://github.com/"+key[7..]+"/releases/",StringComparison.OrdinalIgnoreCase)||!DateTimeOffset.TryParse(Text(item,"published_at"),out var date)||date>now)continue;
    string version=Regex.IsMatch(tag,@"\Av?\d+(?:\.\d+){1,3}\z",RegexOptions.IgnoreCase)?tag.TrimStart('v','V'):"";
    result.Items.Add(new(version,title.Length>0?title:tag,url,date));
   }
  }else throw new InvalidDataException("Choose a supported release source.");
  result.Items=result.Items.OrderByDescending(x=>x.PublishedAt).Take(30).ToList();return result;
 }
 public static void Apply(VersionRecord v,string key,ReleaseCache cache,bool compare=true){
  v.ReleaseSourceKey=key;v.ReleaseCheckedAt=cache.CheckedAt;v.NextReleaseCheckAt=cache.RetryAfter;v.ReleaseError=cache.Error;
  var numbered=cache.Items.FirstOrDefault(x=>x.Version.Length>0);var recent=cache.Items.FirstOrDefault();
  v.LatestVersion=numbered?.Version??"";v.NewerUnnumberedRelease=recent!=null&&(numbered==null||recent.PublishedAt>numbered.PublishedAt);
  var shown=v.NewerUnnumberedRelease?recent:numbered;
  v.ReleaseTitle=shown?.Title??"";v.ReleaseUrl=shown?.Url??"";v.ReleasePublishedAt=shown?.PublishedAt;
  if(compare)Compare(v);
 }
 public static void Compare(VersionRecord v){
  v.NeedsUpdate=false;v.MatchesLatest=false;
  if(v.ManualLatest.Length>0){
   int? c=v.LocalAvailable&&v.Installed.Length>0?GameVersions.CompareLabels(v.Installed,v.ManualLatest):null;
   v.NeedsUpdate=c>0;v.MatchesLatest=c==0;v.Status=c>0?"Newer version noted":c==0?"Matches your latest version":c<0?"Installed version is newer":"Enter compatible version labels";return;
  }
  if(v.ReleaseSourceKey.Length==0){v.Status="Choose a release source";return;}
  if(!v.ReleaseCheckedAt.HasValue){v.Status=v.ReleaseError.Length>0?"Release lookup unavailable":"Latest release not checked";return;}
  if(v.LatestVersion.Length==0){v.Status="Publisher has no readable version label";return;}
  if(!v.LocalAvailable||v.Installed.Length==0){v.Status="Latest found · enter your installed version";return;}
  if(v.Mode!="Manual"&&!v.InstalledConfirmed){v.Status="Verify the installed version in your game";return;}
  if(v.ConfirmedReleaseSource!=v.ReleaseSourceKey){v.Status="Confirm this release source matches your PC edition";return;}
  // Engine/product numbering and release numbering often have different shapes.
  var a=number.Match(v.Installed);var b=number.Match(v.LatestVersion);
  if(!a.Success||!b.Success||a.Groups[1].Value.Count(c=>c=='.')!=b.Groups[1].Value.Count(c=>c=='.')){v.Status="Different numbering · check release notes";return;}
  int? result=GameVersions.CompareLabels(v.Installed,v.LatestVersion);
  if(v.NewerUnnumberedRelease){v.Status="Newer announcement · version needs review";return;}
  v.NeedsUpdate=result>0;v.MatchesLatest=result==0;
  v.Status=result>0?"Newer release found":result==0?"Matches latest numbered release":result<0?"Installed version is newer than this source":"Different numbering · check release notes";
  if(v.ReleaseError.Length>0)v.Status+=" · cached";
 }
 public async Task<VersionRecord> Check(Game game,VersionRecord v,string root,bool online,CancellationToken stop,bool compare=true){
  string key=SourceKey(game);
  if(key.Length==0){Clear(v);if(compare)Compare(v);v.NextReleaseCheckAt=DateTimeOffset.UtcNow.AddHours(24);return v;}
  await gate.WaitAsync(stop);
  try{
   string file=Path.Combine(root,"release-cache",Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key)))+".json");
   var cache=new ReleaseCache{Key=key};
   try{if(File.Exists(file)&&new FileInfo(file).Length<=2*1024*1024){var saved=JsonSerializer.Deserialize<ReleaseCache>(File.ReadAllText(file),Store.Json);if(saved?.Key==key&&saved.Items!=null)cache=saved;}}
   catch(Exception e)when(e is IOException or UnauthorizedAccessException or JsonException){}
   // A moved portable library can still use evidence stored in its game record.
   if(!cache.CheckedAt.HasValue&&v.ReleaseSourceKey==key&&v.ReleaseCheckedAt.HasValue){cache.CheckedAt=v.ReleaseCheckedAt;cache.RetryAfter=v.NextReleaseCheckAt??DateTimeOffset.MinValue;if(v.ReleasePublishedAt.HasValue){if(v.NewerUnnumberedRelease&&v.LatestVersion.Length>0)cache.Items.Add(new(v.LatestVersion,"Previously found numbered release",v.ReleaseUrl,DateTimeOffset.MinValue));cache.Items.Insert(0,new(v.NewerUnnumberedRelease?"":v.LatestVersion,v.ReleaseTitle,v.ReleaseUrl,v.ReleasePublishedAt.Value));}}
   var now=DateTimeOffset.UtcNow;
   if(online&&cache.RetryAfter<=now){
    try{
     var delay=nextRequest-DateTimeOffset.UtcNow;if(delay>TimeSpan.Zero)await Task.Delay(delay,stop);nextRequest=DateTimeOffset.UtcNow.AddSeconds(1);
     string url=key.StartsWith("github:",StringComparison.Ordinal)?"https://api.github.com/repos/"+key[7..]+"/releases?per_page=30":"https://api.steampowered.com/ISteamNews/GetNewsForApp/v2/?appid="+key[11..]+"&count=100&maxlength=1&feeds=steam_community_announcements";
     using var response=await http.GetAsync(url,stop);
     if(!response.IsSuccessStatusCode){cache.RetryAfter=now.AddHours(24);var retry=response.Headers.RetryAfter;var until=retry?.Date??now+(retry?.Delta??TimeSpan.Zero);if(until>cache.RetryAfter)cache.RetryAfter=until;response.EnsureSuccessStatusCode();}
     cache=Parse(await response.Content.ReadAsStringAsync(stop),key,DateTimeOffset.UtcNow);
    }catch(OperationCanceledException)when(stop.IsCancellationRequested){throw;}
    catch(Exception e)when(e is HttpRequestException or TaskCanceledException or JsonException or InvalidDataException or InvalidOperationException){cache.Error="Could not reach or read the release provider. Saved evidence is retained; retry after "+now.AddHours(24).ToLocalTime().ToString("g")+".";if(cache.RetryAfter<now.AddHours(24))cache.RetryAfter=now.AddHours(24);}
    stop.ThrowIfCancellationRequested();Store.Atomic(file,cache);
   }
   Apply(v,key,cache,compare);if(!online)v.ReleaseError="Offline · showing saved release evidence.";
   v.Detail+="\nRelease source: "+key+". Publisher labels are independent of where this game was installed. Confirm the PC edition and numbering before comparing.";
   return v;
  }finally{gate.Release();}
 }
 public static void Clear(VersionRecord v){v.ReleaseSourceKey="";v.LatestVersion="";v.ReleaseTitle="";v.ReleaseUrl="";v.ReleasePublishedAt=null;v.ReleaseCheckedAt=null;v.NextReleaseCheckAt=null;v.ReleaseError="";v.NewerUnnumberedRelease=false;v.NeedsUpdate=false;v.MatchesLatest=false;}
}
