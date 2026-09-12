using System;
using System.IO;
using System.Threading;
using System.Windows;
using System.Diagnostics;
using System.Linq;
namespace Playdeck;
public partial class App : Application {
 Mutex? mutex;readonly CancellationTokenSource ipcStop=new();
 protected override async void OnStartup(StartupEventArgs e){base.OnStartup(e);ShutdownMode=ShutdownMode.OnExplicitShutdown;
 string root=Playdeck.Core.Store.DefaultRoot;bool demo=false;string? capture=null,launch=null,ipcSmoke=null;var imports=new System.Collections.Generic.List<string>();
 for(int i=0;i<e.Args.Length;i++){if(e.Args[i]=="--data"&&i+1<e.Args.Length)root=e.Args[++i];else if(e.Args[i]=="--demo")demo=true;else if(e.Args[i]=="--capture"&&i+1<e.Args.Length)capture=e.Args[++i];else if(e.Args[i]=="--launch"&&i+1<e.Args.Length)launch=e.Args[++i];else if(e.Args[i]=="--ipc-smoke"&&i+1<e.Args.Length)ipcSmoke=e.Args[++i];else if(e.Args[i]=="--add"&&i+1<e.Args.Length)imports.Add(e.Args[++i]);}
 root=Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar);
 if(e.Args.Contains("--install-menu")){try{ExplorerIntegration.Install();Shutdown(0);}catch(Exception ex){MessageBox.Show(ex.Message,"Explorer integration");Shutdown(1);}return;}
 if(e.Args.Contains("--remove-menu")){try{ExplorerIntegration.Remove();Shutdown(0);}catch(Exception ex){MessageBox.Show(ex.Message,"Explorer integration");Shutdown(1);}return;}
 mutex=new Mutex(true,"Local\\"+ImportTransport.PipeName(root),out bool created);
 if(!created){try{await ImportTransport.Send(root,imports.ToArray());}catch(Exception ex){MessageBox.Show("Playdeck is already open but could not receive the import. Try again.\n"+ex.Message,"Playdeck");}Shutdown();return;}
 DispatcherUnhandledException+=(s,a)=>{a.Handled=true;if(capture!=null){File.WriteAllText(capture+".error.txt",a.Exception.ToString());Shutdown(1);}else MessageBox.Show(a.Exception.Message,"Playdeck",MessageBoxButton.OK,MessageBoxImage.Warning);};
 try{var window=new MainWindow(root,demo,capture,launch);MainWindow=window;ShutdownMode=ShutdownMode.OnMainWindowClose;_=ImportTransport.Listen(root,files=>Dispatcher.BeginInvoke(()=>{if(ipcSmoke!=null){File.WriteAllText(ipcSmoke,System.Text.Json.JsonSerializer.Serialize(files));Shutdown();return;}if(window.WindowState==WindowState.Minimized)window.WindowState=WindowState.Normal;window.Activate();window.QueueImports(files);}),ipcStop.Token);window.Loaded+=(_,_)=>window.QueueImports(imports.ToArray());window.Show();}catch(Exception ex){if(capture!=null)File.WriteAllText(capture+".error.txt",ex.ToString());else MessageBox.Show(ex.Message,"Playdeck could not open");Shutdown(1);}
 }
 protected override void OnExit(ExitEventArgs e){ipcStop.Cancel();mutex?.Dispose();base.OnExit(e);}
}
