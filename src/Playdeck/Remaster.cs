using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Playdeck.Core;
namespace Playdeck;
public sealed partial class MainWindow {
 bool selectionMode; readonly HashSet<string> selected=[];
 void TrashGames(IEnumerable<Game> games){foreach(var game in games.ToArray())LibraryActions.Remove(library,game);selected.Clear();Save();Render();status.Text="Moved to Trash · Recover within 7 days. Your game files are untouched.";}
 StackPanel Section(string title,string description){var body=new StackPanel();body.Children.Add(Text(title,24,"#F0EDE5",FontWeights.Bold));if(description.Length>0){var t=Text(description,14,"#B8B4B8");t.Margin=new Thickness(0,8,0,16);body.Children.Add(t);}content.Children.Add(new Border{Background=Brush("#242427"),CornerRadius=new CornerRadius(16),Padding=new Thickness(24),Margin=new Thickness(0,0,0,18),Child=body});return body;}
 void Trash(){var body=Section("A second chance","Cards stay here for 7 days. Restoring keeps artwork, pins and activity. Game files are never deleted.");var games=library.Games.Where(g=>g.Removed).ToArray();if(games.Length==0)body.Children.Add(Text("Your trash is empty.",18));else body.Children.Add(Btn("Restore all",()=>{foreach(var g in games)LibraryActions.Restore(library,g);Save();Render();}));if(games.Length>0)body.Children.Add(Btn("Empty Trash…",EmptyTrash));foreach(var g in games){var row=new DockPanel{Margin=new Thickness(0,16,0,0)};var restore=Btn("Restore",()=>{LibraryActions.Restore(library,g);Save();Render();});DockPanel.SetDock(restore,Dock.Right);row.Children.Add(restore);var image=new Image{Source=LoadImage(g.CoverPath,80)??city,Width=42,Height=54,Margin=new Thickness(0,0,16,0)};DockPanel.SetDock(image,Dock.Left);row.Children.Add(image);var label=Text(g.Name+"\n"+Math.Max(0,7-(int)(DateTimeOffset.UtcNow-(g.TrashedAt??DateTimeOffset.UtcNow)).TotalDays)+" days left",14);label.VerticalAlignment=VerticalAlignment.Center;row.Children.Add(label);body.Children.Add(row);}status.Text="Trash · recoverable for 7 days";}
 void Settings(){
 IntegrationSettings();
 var versionPanel=Section("VERSION WATCH","Track manual versions, executable metadata and Steam builds. Online evidence is cached; checks never update a game.");versionPanel.Children.Add(Btn("Open version watch",VersionHub,true));
 var tools=Section("GAMES & TOOLS","Tools open without play tracking and leave Playdeck open. You can also mark a tool in its card editor.");tools.Children.Add(Btn("Manage tools",ManageTools));
 var profile=Section("PLAYER PROFILE","Customize the player card in the top-left corner.");profile.Children.Add(Btn("Edit profile",EditProfile));
 var art=Section("Artwork on autopilot","Titles and Steam IDs are used for online matching. Results are cached locally; unmatched games show their icon on your default cover. Paste your own image in the card editor.");var online=new CheckBox{Content="Fetch titles, release dates and artwork",IsChecked=library.Online};online.Click+=(_,_)=>{library.Online=online.IsChecked==true;Save();if(library.Online)_=Enrich();};art.Children.Add(online);
 var history=Section("Keep the story","Archived games retain cached icons, covers and activity even when their installation disappears. The tracking helper runs only during games launched through Playdeck.");history.Children.Add(Btn("Export sessions CSV",Export));
 var reset=Section("Make a fresh start","Move every card, including archived games, to Trash and start again. Restore cards within 7 days. Installed games are never touched.");reset.Children.Add(Btn("Reset library…",()=>{var body=new StackPanel();body.Children.Add(Text("Move all cards to Trash?",24));body.Children.Add(Text("This clears the current library. Your installed games are untouched. You can restore cards from Trash for 7 days.",14));var dialog=Dialog("Start fresh",body,510);var buttons=new WrapPanel{Margin=new Thickness(0,24,0,0)};buttons.Children.Add(Btn("Move all to Trash",()=>{LibraryActions.StartFresh(library);selected.Clear();Save();dialog.Close();Render();},true));buttons.Children.Add(Btn("Cancel",()=>dialog.Close()));body.Children.Add(buttons);dialog.ShowDialog();}));status.Text="Local data · "+root;
 }
}
