using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Playdeck.Core;
namespace Playdeck;
public sealed partial class MainWindow {
 void ManageTools(){var body=new StackPanel();body.Children.Add(Text("GAMES & TOOLS",27,"#76DDD3"));body.Children.Add(Text("Mark overlays, mod managers and other gaming utilities as tools. They open normally, leave Playdeck open and create no play sessions. Existing history is retained but excluded from game analytics while marked as a tool.",14,"#B7B1C0"));var list=new ListBox{ItemsSource=library.Games.Where(g=>!g.Removed).OrderBy(g=>g.Name).ToArray(),DisplayMemberPath="Name",Height=280,Margin=new Thickness(0,20,0,18),Background=Brush("#242427"),Foreground=Brush("#EDEAE7"),FontSize=17};body.Children.Add(list);var mark=new CheckBox{Content="Mark as tool · no tracking, keep launcher open",IsEnabled=false,Margin=new Thickness(0,0,0,18)};body.Children.Add(mark);list.SelectionChanged+=(_,_)=>{mark.IsEnabled=list.SelectedItem is Game;mark.IsChecked=(list.SelectedItem as Game)?.IsTool==true;};mark.Click+=(_,_)=>{if(list.SelectedItem is Game g){g.IsTool=mark.IsChecked==true;Save();Render();}};var dialog=Dialog("Games and tools",body,650);body.Children.Add(Btn("Done",()=>dialog.Close()));dialog.ShowDialog();}
}
