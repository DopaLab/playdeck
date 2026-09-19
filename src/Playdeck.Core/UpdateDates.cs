using System.Globalization;
using System.Text.RegularExpressions;
namespace Playdeck.Core;
public static class UpdateDates {
 public static DateOnly? BuildDate(string label,DateOnly? today=null){
  var match=Regex.Match(label.Trim(),@"\A(?:v|build\s+)?(20\d{2})[.\-_](\d{1,2})[.\-_](\d{1,2})\z",RegexOptions.IgnoreCase|RegexOptions.CultureInvariant);
  if(!match.Success)return null;
  if(!DateOnly.TryParseExact($"{match.Groups[1].Value}-{int.Parse(match.Groups[2].Value):00}-{int.Parse(match.Groups[3].Value):00}","yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out var date))return null;
  return date<=(today??DateOnly.FromDateTime(DateTime.UtcNow))?date:null;
 }
 public static bool PossibleUpdate(VersionRecord v)=>v.CompareBuildDate&&v.ConfirmedReleaseSource==v.ReleaseSourceKey&&v.ReleaseSourceKey.Length>0&&BuildDate(v.ConfirmedBuildDate) is DateOnly date&&v.ReleasePublishedAt.HasValue&&DateOnly.FromDateTime(v.ReleasePublishedAt.Value.UtcDateTime)>date;
 public static DateTime? SortDate(Game game)=>game.IsTool?null:game.VersionInfo.ReleasePublishedAt?.UtcDateTime??game.ReleaseDate;
 public static string Label(Game game)=>game.VersionInfo.ReleasePublishedAt is DateTimeOffset date?"UPD "+date.UtcDateTime.ToString("dd MMM yy",CultureInfo.InvariantCulture):game.ReleaseDate is DateTime release?"REL "+release.ToString("dd MMM yy",CultureInfo.InvariantCulture):"Update ?";
 public static VersionBadge Badge(Game game){var v=game.VersionInfo;return new(v.ReleasePublishedAt.HasValue?"Last published update: "+v.ReleasePublishedAt.Value.UtcDateTime.ToString("dd MMM yyyy")+(PossibleUpdate(v)?" · published after your confirmed build date; review notes":"")+(v.ReleaseError.Length>0?" · cached evidence":""):game.ReleaseDate.HasValue?"Original release date; no update date found":"No published update date found",PossibleUpdate(v)?"#F4D35D":"#76DDD3",!v.ReleasePublishedAt.HasValue);}
}
