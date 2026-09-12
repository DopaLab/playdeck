using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
namespace Playdeck;
public sealed partial class MainWindow {
 FrameworkElement LibraryToolbar(){
 bool narrow=ActualWidth>0&&ActualWidth<1120;
 var grid=new Grid{Margin=new Thickness(0,0,0,20)};grid.RowDefinitions.Add(new(){Height=new GridLength(48)});grid.RowDefinitions.Add(new(){Height=GridLength.Auto});grid.ColumnDefinitions.Add(new(){Width=new GridLength(1,GridUnitType.Star)});grid.ColumnDefinitions.Add(new(){Width=new GridLength(210)});grid.ColumnDefinitions.Add(new(){Width=narrow?new GridLength(0):GridLength.Auto});
 (search.Parent as Panel)?.Children.Remove(search);search.Width=double.NaN;search.HorizontalAlignment=HorizontalAlignment.Stretch;search.Margin=new Thickness(0);search.Height=48;var box=new Grid{Width=300,MinWidth=200,MaxWidth=320,Margin=new Thickness(0,0,20,0),HorizontalAlignment=HorizontalAlignment.Left};box.Children.Add(search);if(search.Text.Length==0)box.Children.Add(new TextBlock{Text="⌕  Search your games…",Foreground=Brush("#BDBDBD"),VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(16,0,0,0),IsHitTestVisible=false});grid.Children.Add(box);
 (sort.Parent as Panel)?.Children.Remove(sort);if(view!="Pinned"){sort.Height=48;sort.Margin=new Thickness(0);Grid.SetColumn(sort,1);grid.Children.Add(sort);}else{var hint=Text("DRAG TO REORDER",12,"#E1FF46",FontWeights.Bold);hint.VerticalAlignment=VerticalAlignment.Center;hint.HorizontalAlignment=HorizontalAlignment.Center;Grid.SetColumn(hint,1);grid.Children.Add(hint);}
 var actions=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right,Margin=narrow?new Thickness(0,14,0,0):new Thickness(18,0,0,0)};Grid.SetColumn(actions,narrow?0:2);Grid.SetColumnSpan(actions,narrow?3:1);Grid.SetRow(actions,narrow?1:0);grid.Children.Add(actions);
 var density=Btn(compact?"Comfort":"Compact",()=>{compact=!compact;library.CompactCards=compact;Save();Render();});density.MinWidth=96;density.ToolTip="Change card size";density.Margin=new Thickness(0,0,12,0);actions.Children.Add(density);
 var select=Btn(selected.Count>0?"Trash "+selected.Count:selectionMode?"Done":"Select",()=>{if(selected.Count>0){TrashGames(library.Games.Where(g=>selected.Contains(g.Id)).ToArray());return;}selectionMode=!selectionMode;selected.Clear();Render();});select.MinWidth=86;select.Margin=new Thickness(0,0,12,0);actions.Children.Add(select);var add=Btn("＋ Add game",AddGames,true);add.MinWidth=132;add.Margin=new Thickness(0);actions.Children.Add(add);return grid;
 }
}
