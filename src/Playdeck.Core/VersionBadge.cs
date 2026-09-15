namespace Playdeck.Core;
public sealed record VersionBadge(string Text,string Color,bool NeedsHelp) {
 public static string CompactLabel(VersionRecord v){if(v.Installed.Length==0)return "Version?";string label=v.Installed.StartsWith("Build ",StringComparison.Ordinal)?"b"+v.Installed[6..]:v.Installed;if(label.Length>0&&char.IsDigit(label[0])&&label.Contains('.'))label="v"+label;return label.Length>18?label[..17]+"…":label;}
 public static VersionBadge For(VersionRecord v,DateTimeOffset? now=null){
  var time=now??DateTimeOffset.UtcNow;
  bool fresh=v.CheckedAt.HasValue&&time-v.CheckedAt.Value<TimeSpan.FromHours(24)&&time>=v.CheckedAt.Value;
  bool comparison=v.Mode is "Manual" or "File" ? v.ManualLatest.Length>0&&v.ManualLatestAt.HasValue&&time-v.ManualLatestAt.Value<TimeSpan.FromHours(24)&&time>=v.ManualLatestAt.Value : v.RemoteCheckedAt.HasValue&&time-v.RemoteCheckedAt.Value<TimeSpan.FromHours(24)&&time>=v.RemoteCheckedAt.Value;
  if(v.LocalAvailable&&fresh&&comparison){if(v.NeedsUpdate)return new("UPDATE · "+v.Installed,"#F4D35D",false);if(v.MatchesLatest)return new("UP TO DATE · "+v.Installed,"#B6EC78",false);}
  return new(v.Installed.Length==0?"SET VERSION":!v.LocalAvailable?"LAST KNOWN · "+v.Installed:!fresh&&comparison?"CHECK DUE · "+v.Installed:"VERSION · "+v.Installed,"#C1BBCB",true);
 }
}
