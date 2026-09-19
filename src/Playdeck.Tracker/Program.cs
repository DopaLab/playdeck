using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using Playdeck.Core;

internal static class Program {
 [STAThread] static int Main(string[] args) {
  if(args.Length!=1)return 2;
  LaunchRequest? request=null;
  try {
   request=JsonSerializer.Deserialize<LaunchRequest>(File.ReadAllText(args[0]),Store.Json)??throw new InvalidDataException();
   using var single=new Mutex(true,"Local\\Playdeck.Track."+request.Game.Id,out bool owned);
   if(!owned){Store.Atomic(request.ReplyPath,new {ok=false,error="This game is already being tracked."});return 3;}
   var g=request.Game;
   if(!File.Exists(g.LaunchPath))throw new FileNotFoundException("The launch file is missing. Edit the game or mark it archived.");
   if(g.IsTool){int toolPid=ToolLaunch.Start(g);Store.Atomic(request.ReplyPath,new{ok=true,toolPid});try{File.Delete(args[0]);}catch(IOException){}return 0;}
   var before=Snapshot();
   var psi=new ProcessStartInfo(g.LaunchPath){UseShellExecute=true};
   if(Path.GetExtension(g.LaunchPath).Equals(".exe",StringComparison.OrdinalIgnoreCase))psi.Arguments=g.Arguments;
   if(Directory.Exists(g.WorkingDirectory))psi.WorkingDirectory=g.WorkingDirectory;
   // The tracker observes process identity; it does not need to retain the shell launch handle.
   Process.Start(psi)?.Dispose();
   // Lower only the observer after launch; the game keeps its normal launch priority.
   try{using var observer=Process.GetCurrentProcess();observer.PriorityClass=ProcessPriorityClass.BelowNormal;}catch{}
   var session=new Session{GameId=g.Id,TrackerPid=Environment.ProcessId,TrackerBornUtcTicks=Process.GetCurrentProcess().StartTime.ToUniversalTime().Ticks};
   string sessionPath=Path.Combine(request.DataRoot,"sessions",session.Id+".json");
   Store.Atomic(sessionPath,session);
   Store.Atomic(request.ReplyPath,new {ok=true});
   try {File.Delete(args[0]);}catch(IOException){}
   if(string.IsNullOrWhiteSpace(g.TrackPath)){
    session.Status="Launch only";session.Updated=DateTimeOffset.UtcNow;Store.Atomic(sessionPath,session);return 0;
   }
   var ids=new Dictionary<int,long>();var lineage=new Dictionary<int,long>();bool wasActive=false,wasForeground=false;
   // Track exact executable identity, then descendants. Never count a storefront client's lifetime.
   var clock=Stopwatch.StartNew();long last=clock.ElapsedMilliseconds;bool started=false;int empty=0;int ticks=0;
   while(true){
    // Fast path: process identity checks only. Refresh the tree at checkpoints or handoffs.
    bool needTree=ticks<8 || ticks%5==0 || ids.Count==0 || ids.Any(kv=>!IdentityAlive(kv.Key,kv.Value));
    if(needTree){
    var processes=Snapshot();
    foreach(var p in processes.Values) {
     bool candidate=ids.ContainsKey(p.Id) || (lineage.TryGetValue(p.Parent,out _) && !before.ContainsKey(p.Id) && InGameDirectory(p.Id,g.TrackPath));
     if(!candidate && !before.ContainsKey(p.Id) && string.Equals(p.Exe,Path.GetFileName(g.TrackPath),StringComparison.OrdinalIgnoreCase)) candidate=SamePath(p.Id,g.TrackPath);
     if(Names.Helper(p.Exe) || p.Exe.ToLowerInvariant() is "steam.exe" or "epicgameslauncher.exe" or "galaxyclient.exe") continue;
     if(!candidate)continue;
     if(lineage.TryGetValue(p.Parent,out long parentIdentity)&&processes.ContainsKey(p.Parent)&&!IdentityAlive(p.Parent,parentIdentity))continue;
     try {using var live=Process.GetProcessById(p.Id);long birth=live.StartTime.ToUniversalTime().Ticks;
      if(ids.TryGetValue(p.Id,out long known)&&birth!=known)continue;
      if(birth<session.Started.UtcTicks-TimeSpan.FromSeconds(2).Ticks)continue;
      if(lineage.TryGetValue(p.Parent,out long parentBirth)&&birth<parentBirth)continue;
      ids[p.Id]=birth;lineage[p.Id]=birth;
     }catch{}
    }
    foreach(var id in ids.Keys.ToArray())if(!processes.ContainsKey(id)||!IdentityAlive(id,ids[id]))ids.Remove(id);
    }else{
     foreach(var id in ids.Keys.ToArray())if(!IdentityAlive(id,ids[id]))ids.Remove(id);
    }
    bool active=ids.Count>0;
    long now=clock.ElapsedMilliseconds;
    if(wasActive)SessionAccounting.Accrue(session,DateTimeOffset.UtcNow,(now-last)/1000.0,wasForeground);
    GetWindowThreadProcessId(GetForegroundWindow(),out uint foregroundPid);
    wasActive=active;wasForeground=ids.ContainsKey((int)foregroundPid);
    if(active){started=true;empty=0;session.Status="Playing";}
    else if(started){empty++; if(empty>=3){session.Status="Completed";session.Updated=DateTimeOffset.UtcNow;Store.Atomic(sessionPath,session);break;}}
    else if(clock.Elapsed>TimeSpan.FromSeconds(120)){session.Status="Not detected";session.Updated=DateTimeOffset.UtcNow;Store.Atomic(sessionPath,session);break;}
    last=now;
    if(ticks++%15==0){session.Updated=DateTimeOffset.UtcNow;Store.Atomic(sessionPath,session);}
    Thread.Sleep(2000);
   }
   return 0;
  } catch(Exception ex){if(request!=null)try{Store.Atomic(request.ReplyPath,new {ok=false,error=ex.Message});}catch{}return 1;}
 }
 static bool IdentityAlive(int id,long born){try{using var p=Process.GetProcessById(id);return !p.HasExited && p.StartTime.ToUniversalTime().Ticks==born;}catch{return false;}}
 static bool InGameDirectory(int pid,string target){
  string? directory=Path.GetDirectoryName(target);if(string.IsNullOrWhiteSpace(directory))return false;
  IntPtr h=OpenProcess(0x1000,false,pid);if(h==IntPtr.Zero)return false;
  try{var name=new System.Text.StringBuilder(32768);int count=name.Capacity;return QueryFullProcessImageName(h,0,name,ref count)&&Discovery.Within(name.ToString(),directory);}finally{CloseHandle(h);}
 }
 static bool SamePath(int pid,string target){
  IntPtr h=OpenProcess(0x1000,false,pid);if(h==IntPtr.Zero)return false;
  try{var b=new System.Text.StringBuilder(32768);int n=b.Capacity;return QueryFullProcessImageName(h,0,b,ref n)&&string.Equals(b.ToString(),target,StringComparison.OrdinalIgnoreCase);}finally{CloseHandle(h);}
 }
 record Proc(int Id,int Parent,string Exe);
 static Dictionary<int,Proc> Snapshot(){
  var result=new Dictionary<int,Proc>();IntPtr h=CreateToolhelp32Snapshot(2,0);if(h==new IntPtr(-1))return result;
  try{var e=new Entry{size=(uint)Marshal.SizeOf<Entry>()};if(Process32First(h,ref e))do{result[(int)e.pid]=new((int)e.pid,(int)e.parent,e.exe);}while(Process32Next(h,ref e));}finally{CloseHandle(h);}return result;
 }
 [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)] struct Entry {public uint size,usage,pid;public UIntPtr heap;public uint module,threads,parent;public int priority;public uint flags;[MarshalAs(UnmanagedType.ByValTStr,SizeConst=260)]public string exe;}
 [DllImport("user32.dll")]static extern IntPtr GetForegroundWindow();
 [DllImport("user32.dll")]static extern uint GetWindowThreadProcessId(IntPtr window,out uint pid);
 [DllImport("kernel32.dll")]static extern IntPtr CreateToolhelp32Snapshot(uint flags,uint pid);
 [DllImport("kernel32.dll",CharSet=CharSet.Unicode)]static extern bool Process32First(IntPtr h,ref Entry e);
 [DllImport("kernel32.dll",CharSet=CharSet.Unicode)]static extern bool Process32Next(IntPtr h,ref Entry e);
 [DllImport("kernel32.dll")]static extern bool CloseHandle(IntPtr h);
 [DllImport("kernel32.dll")]static extern IntPtr OpenProcess(uint access,bool inherit,int pid);
 [DllImport("kernel32.dll",CharSet=CharSet.Unicode)]static extern bool QueryFullProcessImageName(IntPtr h,uint flags,System.Text.StringBuilder name,ref int size);
}
