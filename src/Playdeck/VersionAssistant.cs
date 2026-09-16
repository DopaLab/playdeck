using System;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Playdeck.Core;
namespace Playdeck;
public sealed partial class MainWindow {
 void VersionDialog(Game game){
  var body=new StackPanel();body.Children.Add(Text("RELEASE WATCH",28,"#76DDD3"));body.Children.Add(Text(game.Name,22));
  var intro=Text("Your local copy, compared with published releases. Where you installed it does not matter.",13,"#BAB5C4");intro.Margin=new Thickness(0,12,0,20);body.Children.Add(intro);
  var grid=new Grid();grid.ColumnDefinitions.Add(new());grid.ColumnDefinitions.Add(new(){Width=new GridLength(20)});grid.ColumnDefinitions.Add(new());
  var local=new StackPanel();local.Children.Add(Text("YOUR COPY",19,"#76DDD3",FontWeights.Black));
  var installed=new TextBox{Text=game.VersionInfo.InstalledBuild.Length==0?game.VersionInfo.Installed:"",Margin=new Thickness(0,12,0,12),ToolTip="Copy the version shown on the game's main menu or About screen."};local.Children.Add(installed);local.Children.Add(Text("Enter the version from the game, or choose a detected file below.",12,"#B7B1C0"));
  var remote=new StackPanel();remote.Children.Add(Text("PUBLISHED RELEASE",19,"#F4D35D",FontWeights.Black));var latest=Text("Looking for a release…",23);latest.Margin=new Thickness(0,12,0,12);remote.Children.Add(latest);var citation=Text("",12,"#B7B1C0");remote.Children.Add(citation);
  var a=new Border{Child=local,Padding=new Thickness(18),CornerRadius=new CornerRadius(16),Background=Brush("#29272F")};var b=new Border{Child=remote,Padding=new Thickness(18),CornerRadius=new CornerRadius(16),Background=Brush("#29272F")};Grid.SetColumn(b,2);grid.Children.Add(a);grid.Children.Add(b);body.Children.Add(grid);
  var summary=Text("",17,"#EAE5EF");summary.Margin=new Thickness(0,18,0,12);body.Children.Add(summary);
  var confirm=new CheckBox{Content="This source matches my PC edition and its version numbering",IsChecked=game.VersionInfo.ConfirmedReleaseSource==ReleaseVersions.SourceKey(game)&&ReleaseVersions.SourceKey(game).Length>0,Margin=new Thickness(0,4,0,12)};body.Children.Add(confirm);
  var automatic=new CheckBox{Content="Check daily while Playdeck is open · cached for at least 24 hours",IsChecked=game.VersionInfo.AutoCheck,Margin=new Thickness(0,0,0,16)};automatic.Click+=(_,_)=>{game.VersionInfo.AutoCheck=automatic.IsChecked==true;Save();};body.Children.Add(automatic);
  var detail=Text("",12,"#B7B1C0");detail.Margin=new Thickness(0,0,0,14);body.Children.Add(detail);var actions=new WrapPanel();body.Children.Add(actions);
  var evidenceBody=new StackPanel();var evidence=new ListBox{Height=110,Background=Brush("#222225"),Foreground=Brush("#ECEAE6"),BorderThickness=new Thickness(0),DisplayMemberPath="Label",FontSize=14,Margin=new Thickness(0,12,0,12)};evidenceBody.Children.Add(evidence);
  var evidenceHint=Text("Search small files near this game. Confirm any result against the version inside the game.",12,"#B7B1C0");evidenceHint.Margin=new Thickness(0,0,0,12);evidenceBody.Children.Add(evidenceHint);evidence.SelectionChanged+=(_,_)=>{if(evidence.SelectedItem is VersionEvidence e)evidenceHint.Text=e.Path+"\nConfirm this agrees with the version shown inside your game.";};
  var fileActions=new WrapPanel();evidenceBody.Children.Add(fileActions);body.Children.Add(new Expander{Header="Find my installed version",Content=evidenceBody,Foreground=Brush("#C5BECD"),Margin=new Thickness(0,8,0,10)});
  var dialog=Dialog("Release watch · "+game.Name,body,800);using var stop=CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);dialog.Closed+=(_,_)=>stop.Cancel();bool busy=false;
  void Refresh(){var v=game.VersionInfo;latest.Text=v.ManualLatest.Length>0?v.ManualLatest:v.LatestVersion.Length>0?v.LatestVersion:"No numbered release found";citation.Text=(v.ManualLatest.Length>0?"Your latest-version note":v.ReleaseTitle)+(v.ReleasePublishedAt.HasValue?"\n"+v.ReleasePublishedAt.Value.LocalDateTime.ToString("dd MMM yyyy"):"");summary.Text=v.Status;detail.Text=(v.ReleaseError.Length>0?v.ReleaseError+"\n":"")+(v.ReleaseCheckedAt.HasValue?"Source checked "+v.ReleaseCheckedAt.Value.LocalDateTime.ToString("dd MMM yyyy HH:mm")+". ":"")+(v.NextReleaseCheckAt.HasValue?"Next online check "+v.NextReleaseCheckAt.Value.LocalDateTime.ToString("dd MMM HH:mm")+". ":"")+"\n"+ReleaseHelp(v);}
  async Task Check(bool saveInput,bool selected=false){if(busy)return;if(installed.Text.Length>120){summary.Text="Use a version label of at most 120 characters.";return;}busy=true;actions.IsEnabled=false;fileActions.IsEnabled=false;try{
   string inputBefore=installed.Text;string key=VersionKey(game);var snapshot=JsonSerializer.Deserialize<Game>(JsonSerializer.Serialize(game,Store.Json),Store.Json)!;var v=snapshot.VersionInfo;
   if(selected){if(evidence.SelectedItem is not VersionEvidence e){summary.Text="Select a version file first.";return;}v.Mode=e.Kind=="Steam"?"Automatic":e.Kind;v.EvidencePath=e.Kind=="File"?e.Path:"";v.ManifestPath=e.Kind=="Steam"?e.Path:"";v.InstalledConfirmed=true;}
   else if(saveInput&&installed.Text.Trim().Length>0){if(v.Mode!="File"||installed.Text.Trim()!=v.Installed){v.Mode="Manual";v.ManualVersion=installed.Text.Trim();}v.InstalledConfirmed=true;}
   if(saveInput)v.ConfirmedReleaseSource=confirm.IsChecked==true?ReleaseVersions.SourceKey(snapshot):"";
   summary.Text="Reading local and published version evidence…";var info=await versions.Check(snapshot,root,library.Online,stop.Token);if(stop.IsCancellationRequested||VersionKey(game)!=key)return;game.VersionInfo=info;Save();Render();if(saveInput||selected||installed.Text==inputBefore)installed.Text=info.InstalledBuild.Length==0?info.Installed:"";Refresh();
  }catch(OperationCanceledException){}catch(Exception e){summary.Text="Check unavailable: "+e.Message;}finally{busy=false;actions.IsEnabled=true;fileActions.IsEnabled=true;}}
  async Task Scan(){if(busy)return;busy=true;fileActions.IsEnabled=false;try{var snapshot=JsonSerializer.Deserialize<Game>(JsonSerializer.Serialize(game,Store.Json),Store.Json)!;var found=await Task.Run(()=>VersionDiscovery.Find(snapshot,stop:stop.Token),stop.Token);if(stop.IsCancellationRequested)return;evidence.ItemsSource=found;evidenceHint.Text=found.Count==0?"No readable version file. Enter the version shown in the game above.":$"{found.Count} possible source(s). Select one and confirm it against your game.";if(found.Count>0)evidence.SelectedIndex=0;}catch(OperationCanceledException){}catch(Exception e){evidenceHint.Text=e.Message;}finally{busy=false;fileActions.IsEnabled=true;}}
  actions.Children.Add(Btn("Save & compare",async()=>await Check(true),true));actions.Children.Add(Btn("Release source",async()=>{ReleaseSourceDialog(game);confirm.IsChecked=game.VersionInfo.ConfirmedReleaseSource==ReleaseVersions.SourceKey(game)&&ReleaseVersions.SourceKey(game).Length>0;await Check(false);}));
  actions.Children.Add(Btn("Read release notes",()=>{if(ReleaseVersions.SafeLink(game.VersionInfo.ReleaseUrl)){try{Process.Start(new ProcessStartInfo(game.VersionInfo.ReleaseUrl){UseShellExecute=true});}catch(Exception e){summary.Text=e.Message;}}else summary.Text="No publisher link is available yet. Choose a source, then check.";}));
  actions.Children.Add(Btn("Check again",async()=>await Check(false)));actions.Children.Add(Btn("Advanced / manual latest",()=>{AdvancedVersionDialog(game);installed.Text=game.VersionInfo.InstalledBuild.Length==0?game.VersionInfo.Installed:"";Refresh();}));actions.Children.Add(Btn("Done",()=>dialog.Close()));
  fileActions.Children.Add(Btn("Find version files",async()=>await Scan()));fileActions.Children.Add(Btn("Use selected",async()=>await Check(true,true),true));Refresh();dialog.Loaded+=async(_,_)=>{if(dialogProbe==null)await Check(false);};dialog.ShowDialog();
 }
 static string ReleaseHelp(VersionRecord v){
  if(v.ManualLatest.Length>0)return "Comparing your latest-version note. Clear it in Advanced to use online releases.";
  if(v.ReleaseSourceKey.Length==0)return "Choose the game's publisher feed or its official GitHub repository. A Steam installation is never required.";
  if(v.NewerUnnumberedRelease)return "A newer update post has no readable version number. Open its notes; the older numbered release cannot establish that you are current.";
  if(v.LatestVersion.Length==0)return "No supported release label in the recent posts. Check the publisher's notes or record a latest version in Advanced.";
  if(v.Installed.Length==0)return "Latest found. Enter your installed version from the game's main menu to compare.";
  if(v.Mode!="Manual"&&!v.InstalledConfirmed)return "Executable metadata may describe the engine. Confirm the version inside the game, then Save & compare.";
  return "Green matches this source's latest numbered release. Yellow means a newer version. Other editions can update on different schedules.";
 }
 void ReleaseSourceDialog(Game game){
  var body=new StackPanel();body.Children.Add(Text("PICK A RELEASE SOURCE",26,"#76DDD3"));body.Children.Add(Text("Search publisher announcements. This changes only release identity, keeping your card's title, cover and installation.",13,"#BAB5C4"));
  var query=new TextBox{Text=game.Name,Margin=new Thickness(0,16,0,12)};body.Children.Add(query);var results=new ListBox{Height=190,DisplayMemberPath="Name",Margin=new Thickness(0,12,0,12)};var hint=Text("",12,"#BAB5C4");var actions=new WrapPanel();var dialog=Dialog("Release source",body,680);using var stop=CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);dialog.Closed+=(_,_)=>stop.Cancel();bool searching=false;
  body.Children.Add(Btn("Search publisher feeds",async()=>{if(searching)return;if(!library.Online){hint.Text="Enable online metadata in Settings to search.";return;}searching=true;actions.IsEnabled=false;try{results.ItemsSource=await metadata.Search(query.Text,root,stop.Token);hint.Text="Select the exact game. Confirm the PC edition before comparing.";}catch(OperationCanceledException){}catch(Exception e){hint.Text=e.Message;}finally{searching=false;actions.IsEnabled=true;}}));body.Children.Add(results);
  body.Children.Add(Text("OR OFFICIAL GITHUB REPOSITORY · OWNER/REPOSITORY",12,"#BAB5C4"));var repo=new TextBox{Text=game.VersionInfo.GitHubRepository,Margin=new Thickness(0,8,0,12)};body.Children.Add(repo);body.Children.Add(hint);body.Children.Add(actions);
  void SetSource(int id,string repository){game.VersionInfo.ReleaseAppId=id;game.VersionInfo.GitHubRepository=repository;game.VersionInfo.ConfirmedReleaseSource="";game.VersionInfo.ManualLatest="";game.VersionInfo.ManualLatestAt=null;ReleaseVersions.Clear(game.VersionInfo);Save();Render();dialog.Close();}
  actions.Children.Add(Btn("Use selected feed",()=>{if(results.SelectedItem is SteamMatch match)SetSource(match.Id,"");else hint.Text="Select a search result.";},true));actions.Children.Add(Btn("Use GitHub releases",()=>{string value=ReleaseVersions.Repository(repo.Text);if(value.Length==0){hint.Text="Enter owner/repository or its GitHub URL.";return;}SetSource(0,value);}));actions.Children.Add(Btn("Cancel",()=>dialog.Close()));dialog.ShowDialog();
 }
}
