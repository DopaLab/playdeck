namespace Playdeck.Core;
public sealed record DailyPlay(DateTime Date,double Seconds);
public sealed record RankedPlay(Game Game,double Seconds,int Launches);
public sealed class ActivityReport {
 public required DailyPlay[] Days{get;init;}
 public required RankedPlay[] Ranking{get;init;}
 public required Session[] Recent{get;init;}
 public double Seconds=>Days.Sum(d=>d.Seconds);
 public double PreviousSeconds{get;init;}
 public int Launches{get;init;}
 public int ActiveDays=>Days.Count(d=>d.Seconds>0);
 public int GamesPlayed=>Ranking.Count(g=>g.Seconds>0);
 public double AverageSession{get;init;}
 public double LongestSession{get;init;}
 public double FocusSeconds{get;init;}
 public double FocusMeasuredSeconds{get;init;}
 public double AllSeconds{get;init;}
 public int Interrupted{get;init;}
 public int Unmeasured{get;init;}
 public static ActivityReport Build(IEnumerable<Game> games,IEnumerable<Session> sessions,int count,DateTime today){
  count=Math.Clamp(count,7,90);today=today.Date;var library=games.Where(g=>!g.Removed&&!g.IsTool).ToDictionary(g=>g.Id);var data=sessions.Where(s=>library.ContainsKey(s.GameId)).ToArray();var start=today.AddDays(1-count);var previous=start.AddDays(-count);
  var totals=new Dictionary<DateTime,double>();var byGame=new Dictionary<string,double>();
  foreach(var session in data){
   var days=session.DailySeconds.Count>0?session.DailySeconds.Select(p=>(Date:DateTime.TryParseExact(p.Key,"yyyy-MM-dd",System.Globalization.CultureInfo.InvariantCulture,System.Globalization.DateTimeStyles.None,out var date)?date:DateTime.MinValue,Seconds:p.Value)):new[]{(Date:session.Started.LocalDateTime.Date,Seconds:session.Seconds)};
   foreach(var d in days){if(!double.IsFinite(d.Seconds)||d.Seconds<=0||d.Date<previous||d.Date>today)continue;totals[d.Date]=totals.GetValueOrDefault(d.Date)+d.Seconds;if(d.Date>=start)byGame[session.GameId]=byGame.GetValueOrDefault(session.GameId)+d.Seconds;}
  }
  var launches=data.Where(s=>s.Started.LocalDateTime.Date>=start&&s.Started.LocalDateTime.Date<=today).ToArray();var played=launches.Where(s=>s.Seconds>0&&double.IsFinite(s.Seconds)).ToArray();var focus=data.Where(s=>s.HasFocusData&&s.Seconds>0&&double.IsFinite(s.Seconds)).ToArray();
  return new(){Days=Enumerable.Range(0,count).Select(i=>new DailyPlay(start.AddDays(i),totals.GetValueOrDefault(start.AddDays(i)))).ToArray(),PreviousSeconds=totals.Where(p=>p.Key>=previous&&p.Key<start).Sum(p=>p.Value),Launches=launches.Length,Ranking=byGame.Select(p=>new RankedPlay(library[p.Key],p.Value,launches.Count(s=>s.GameId==p.Key))).OrderByDescending(p=>p.Seconds).ToArray(),Recent=launches.OrderByDescending(s=>s.Started).Take(12).ToArray(),AverageSession=played.Length==0?0:played.Average(s=>s.Seconds),LongestSession=played.Length==0?0:played.Max(s=>s.Seconds),FocusSeconds=focus.Sum(s=>Math.Clamp(s.ForegroundSeconds,0,s.Seconds)),FocusMeasuredSeconds=focus.Sum(s=>s.Seconds),AllSeconds=data.Where(s=>s.Seconds>0&&double.IsFinite(s.Seconds)).Sum(s=>s.Seconds),Interrupted=launches.Count(s=>s.Status=="Interrupted"),Unmeasured=launches.Count(s=>s.Status is "Not detected" or "Launch only")};
 }
}
