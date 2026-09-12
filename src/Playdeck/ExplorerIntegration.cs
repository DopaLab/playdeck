using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
namespace Playdeck;
public static class ExplorerIntegration {
 const string Verb="Playdeck.AddGame";
 static readonly string[] Types={"exefile","lnkfile","InternetShortcut"};
 public static string Command(string exe)=>"\""+Path.GetFullPath(exe)+"\" --add \"%1\"";
 public static bool Installed {get{using var key=Registry.CurrentUser.OpenSubKey(@"Software\Classes\exefile\shell\"+Verb+@"\command");return string.Equals(key?.GetValue("") as string,Command(Environment.ProcessPath!),StringComparison.OrdinalIgnoreCase);}}
 public static void Install(){foreach(var type in Types){using var key=Registry.CurrentUser.CreateSubKey(@"Software\Classes\"+type+@"\shell\"+Verb);key.SetValue("","Add to Playdeck");key.SetValue("Icon","\""+Environment.ProcessPath+"\",0");key.SetValue("MultiSelectModel","Document");using var command=key.CreateSubKey("command");command.SetValue("",Command(Environment.ProcessPath!));}Notify();}
 public static void Remove(){foreach(var type in Types){string path=@"Software\Classes\"+type+@"\shell\"+Verb;bool owned;using(var key=Registry.CurrentUser.OpenSubKey(path+@"\command"))owned=string.Equals(key?.GetValue("") as string,Command(Environment.ProcessPath!),StringComparison.OrdinalIgnoreCase);if(owned)Registry.CurrentUser.DeleteSubKeyTree(path,false);}Notify();}
 [System.Runtime.InteropServices.DllImport("shell32.dll")]static extern void SHChangeNotify(uint e,uint flags,IntPtr item1,IntPtr item2);
 static void Notify()=>SHChangeNotify(0x08000000,0,IntPtr.Zero,IntPtr.Zero);
}
public static class ImportTransport {
 public static string PipeName(string root)=>"Playdeck.Import."+Playdeck.Core.Store.Key(Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar).ToUpperInvariant());
 public static async Task Send(string root,string[] files){using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(8));using var pipe=new NamedPipeClientStream(".",PipeName(root),PipeDirection.InOut,PipeOptions.Asynchronous|PipeOptions.CurrentUserOnly);await pipe.ConnectAsync(timeout.Token);byte[] data=Encoding.UTF8.GetBytes(JsonSerializer.Serialize(files));if(data.Length>131072)throw new IOException("Too many files in one request.");await pipe.WriteAsync(BitConverter.GetBytes(data.Length),timeout.Token);await pipe.WriteAsync(data,timeout.Token);await pipe.FlushAsync(timeout.Token);byte[] ack=new byte[1];await pipe.ReadExactlyAsync(ack,timeout.Token);if(ack[0]!=1)throw new IOException("Import was not accepted.");}
 public static async Task Listen(string root,Action<string[]> receive,CancellationToken stop){while(!stop.IsCancellationRequested){try{using var pipe=new NamedPipeServerStream(PipeName(root),PipeDirection.InOut,1,PipeTransmissionMode.Byte,PipeOptions.Asynchronous|PipeOptions.CurrentUserOnly);await pipe.WaitForConnectionAsync(stop);using var request=CancellationTokenSource.CreateLinkedTokenSource(stop);request.CancelAfter(TimeSpan.FromSeconds(5));byte[] size=new byte[4];await pipe.ReadExactlyAsync(size,request.Token);int length=BitConverter.ToInt32(size);if(length<2||length>131072)continue;byte[] data=new byte[length];await pipe.ReadExactlyAsync(data,request.Token);var files=JsonSerializer.Deserialize<string[]>(data)??[];receive(files);await pipe.WriteAsync(new byte[]{1},request.Token);}catch(OperationCanceledException){if(stop.IsCancellationRequested)return;}catch(IOException){}catch(JsonException){}}}
}
