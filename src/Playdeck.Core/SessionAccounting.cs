namespace Playdeck.Core;
public static class SessionAccounting {
 // Sampled process lifetime, not an assertion that the player is actively providing input.
 // Reject suspend/resume gaps instead of counting hours spent asleep.
 public static void Accrue(Session session,DateTimeOffset end,double seconds,bool foreground){
  if(!double.IsFinite(seconds)||seconds<=0||seconds>=10)return;
  session.Seconds+=seconds;if(foreground)session.ForegroundSeconds+=seconds;session.HasFocusData=true;
  var cursor=end.ToLocalTime().AddSeconds(-seconds);var finish=end.ToLocalTime();
  while(cursor<finish){var next=new DateTimeOffset(cursor.Date.AddDays(1),cursor.Offset);var stop=next<finish?next:finish;var key=cursor.ToString("yyyy-MM-dd",System.Globalization.CultureInfo.InvariantCulture);session.DailySeconds[key]=session.DailySeconds.GetValueOrDefault(key)+(stop-cursor).TotalSeconds;cursor=stop;}
 }
 public static double OnDay(Session session,DateTime date)=>session.DailySeconds.Count>0?session.DailySeconds.GetValueOrDefault(date.ToString("yyyy-MM-dd",System.Globalization.CultureInfo.InvariantCulture)):(session.Started.LocalDateTime.Date==date.Date?Math.Max(0,session.Seconds):0);
}
