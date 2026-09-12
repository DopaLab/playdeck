using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Playdeck.Core;
namespace Playdeck;
public sealed partial class MainWindow {
 static BitmapImage Asset(string name,int decode){var image=new BitmapImage();image.BeginInit();image.UriSource=new Uri("pack://application:,,,/Assets/"+name);image.DecodePixelWidth=decode;image.CacheOption=BitmapCacheOption.OnLoad;image.EndInit();image.Freeze();return image;}
 readonly BitmapImage city=Asset("fallback.png",480);
 readonly BitmapImage brandIcon=Asset("icon.png",96);
 void Render(){
  if(!IsInitialized)return;bool searchFocused=search.IsKeyboardFocused;int caret=search.CaretIndex;content.Children.Clear();foreach(var item in navigation){item.Value.Background=Brush(item.Key==view?"#E1FF46":"#00000000");item.Value.Foreground=Brush(item.Key==view?"#11150B":"#ECEEE5");}
  content.Background=view is "Folders & settings" or "Activity"?Brush("#181818"):Brush("#00000000");if(view=="Folders & settings"){Settings();return;}if(view=="Activity"){Activity();return;}if(view=="Trash"){Trash();return;}
  var tool=LibraryToolbar();
  var games=library.Games.Where(g=>!g.Removed&&g.Archived==(view=="Archived")&&(view!="Pinned"||g.Favorite)&&g.Name.Contains(search.Text,StringComparison.CurrentCultureIgnoreCase));
  games=view=="Pinned"?games.OrderBy(g=>g.PinOrder).ThenBy(g=>g.Added):sort.SelectedIndex switch{1=>games.OrderByDescending(g=>History(g).Sum(s=>s.Seconds)),2=>games.OrderByDescending(g=>History(g).Count()),3=>games.OrderByDescending(g=>g.Added),4=>games.OrderByDescending(g=>g.ReleaseDate),5=>games.OrderBy(g=>g.Name),_=>games.OrderByDescending(g=>History(g).Select(s=>(DateTimeOffset?)s.Started).Max())};
  var list=games.ToList();var count=Text($"{list.Count:00}  /  {(demo?"DEMO":view.ToUpperInvariant())}",11,"#B9C1AF",FontWeights.Bold);count.Margin=new Thickness(17,0,0,0);count.VerticalAlignment=VerticalAlignment.Center;content.Children.Add(tool);
  if(list.Count==0){var empty=new StackPanel{Margin=new Thickness(40,70,40,70)};empty.Children.Add(new Image{Source=brandIcon,Width=90,Height=90,HorizontalAlignment=HorizontalAlignment.Left});empty.Children.Add(Text(view=="Pinned"?"Your instant-play shelf.":"Make room for a good game.",30,"#F3F2E8",FontWeights.Black));empty.Children.Add(Text(view=="Pinned"?"Pin games from the ··· editor. Drag them into your own order here.":search.Text.Length>0?"No matches. Try another title.":"Right-click a game in Explorer and choose Add to Playdeck.",15,"#B9C1AF"));var b=Btn("＋ Add games",AddMenu,true);b.HorizontalAlignment=HorizontalAlignment.Left;b.Margin=new Thickness(0,22,0,0);empty.Children.Add(b);content.Children.Add(empty);}
  else{var cards=new CardPanel{CardWidth=compact?176:218,Items=list,HeightFactory=CardHeight,Factory=(g,width)=>Card(g,width)};content.Children.Add(cards);cards.SetViewport(Math.Max(0,libraryScroll.VerticalOffset-tool.DesiredSize.Height),libraryScroll.ViewportHeight>0?libraryScroll.ViewportHeight:Height);}
  UpdateLayout();
  if(selectionMode)status.Text="Select cards, then choose Trash. Hold Delete and click any card for quick removal.";else if(!enriching)status.Text=$"●  YOUR LIBRARY     {Hours(sessions.Sum(s=>s.Seconds))} played    /    {sessions.Count} launches     ·     Click a card to play.  ··· to edit.";
  if(searchFocused){search.Focus();search.CaretIndex=Math.Min(caret,search.Text.Length);}
 }
 double CardHeight(double width)=>Math.Min(CardPanel.HeightFor(width),Math.Max(247,Height-(ActualWidth<1120?360:230)));
 FrameworkElement Card(Game g,double? forcedWidth=null){
  double width=forcedWidth??(compact?176:218);int seed=Convert.ToInt32(Store.Key(g.Name)[..4],16);double height=CardHeight(width);double artHeight=height-77;
  var card=new Border{Width=width,Height=height,CornerRadius=new CornerRadius(18),BorderBrush=Brush("#080908"),BorderThickness=new Thickness(3),Background=Brush("#303033"),Cursor=System.Windows.Input.Cursors.Hand,Focusable=true,AllowDrop=view=="Pinned"};
  var all=new Grid{Clip=new RectangleGeometry(new Rect(0,0,width-6,height-6),18,18)};card.Child=all;all.RowDefinitions.Add(new(){Height=new GridLength(artHeight)});all.RowDefinitions.Add(new(){Height=new GridLength(1,GridUnitType.Star)});
  var art=new Grid{ClipToBounds=true};all.Children.Add(art);var cover=LoadImage(g.CoverPath,350);
  string[] tones={"#344C55","#554437","#43435A","#4B5433","#513B4B"};art.Background=Brush(tones[seed%tones.Length]);
  art.Children.Add(new Image{Source=cover??city,Stretch=Stretch.UniformToFill});
  if(cover==null){var gameIcon=LoadImage(CachedIcon(g),128)??brandIcon;var centerpiece=new Border{Width=94,Height=94,CornerRadius=new CornerRadius(47),Background=Brush("#BF171717"),BorderBrush=Brush("#666666"),BorderThickness=new Thickness(2),HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center,Child=new Image{Source=gameIcon,Width=66,Height=66,Stretch=Stretch.Uniform}};System.Windows.Automation.AutomationProperties.SetName(centerpiece,"Centered game icon");art.Children.Add(centerpiece);}
  art.Children.Add(new Border{Height=65,VerticalAlignment=VerticalAlignment.Bottom,Background=new LinearGradientBrush(Colors.Transparent,Color.FromArgb(170,0,0,0),90)});
  var tag=new Border{Background=Brush("#CC10120D"),CornerRadius=new CornerRadius(11),Padding=new Thickness(9,4,9,4),HorizontalAlignment=HorizontalAlignment.Left,VerticalAlignment=VerticalAlignment.Top,Margin=new Thickness(11)};tag.Child=Text(g.Favorite?"◆  PINNED":g.Source.ToUpperInvariant(),9,g.Favorite?"#D8FF39":"#F3F4E9",FontWeights.Black);if(g.Favorite)art.Children.Add(tag);
  if(cover==null){var match=Btn("Find cover",()=>ChooseSteamMatch(g));match.FontSize=13;match.MinHeight=28;match.Height=28;match.Padding=new Thickness(10,2,10,2);match.Margin=new Thickness(10,0,0,10);match.HorizontalAlignment=HorizontalAlignment.Left;match.VerticalAlignment=VerticalAlignment.Bottom;art.Children.Add(match);}
  var playGlyph=new Border{Width=43,Height=43,CornerRadius=new CornerRadius(22),Background=Brush("#FF765E"),HorizontalAlignment=HorizontalAlignment.Right,VerticalAlignment=VerticalAlignment.Bottom,Margin=new Thickness(0,0,12,12),Opacity=0};playGlyph.Child=Text("▶",18,"#111608",FontWeights.Black);((TextBlock)playGlyph.Child).HorizontalAlignment=HorizontalAlignment.Center;((TextBlock)playGlyph.Child).VerticalAlignment=VerticalAlignment.Center;art.Children.Add(playGlyph);
  var footer=new DockPanel{Margin=new Thickness(13,11,10,8)};Grid.SetRow(footer,1);all.Children.Add(footer);var localIcon=LoadImage(CachedIcon(g),40);if(localIcon!=null){var badge=new Image{Source=localIcon,Width=30,Height=30,Margin=new Thickness(0,2,9,0),VerticalAlignment=VerticalAlignment.Top};DockPanel.SetDock(badge,Dock.Left);footer.Children.Add(badge);}var foot=new StackPanel();footer.Children.Add(foot);var title=Text(g.Name,compact?17:19,"#F1F2E8",FontWeights.Black);title.TextWrapping=TextWrapping.NoWrap;title.TextTrimming=TextTrimming.CharacterEllipsis;foot.Children.Add(title);foot.Children.Add(Text($"{Hours(History(g).Sum(s=>s.Seconds))}  /  {History(g).Count()} launches",11,"#B3BCAA"));
  if(selected.Contains(g.Id))card.BorderBrush=Brush("#FF765E");
  var edit=Btn("",()=>Edit(g));var dots=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center};for(int i=0;i<3;i++)dots.Children.Add(new System.Windows.Shapes.Ellipse{Width=3,Height=3,Fill=Brush("#FFFFFF"),Margin=new Thickness(2,0,2,0)});edit.Content=dots;edit.MinHeight=28;edit.Width=28;edit.Height=28;edit.Padding=new Thickness(0);edit.FontSize=19;edit.Background=Brush("#E5151712");edit.Margin=new Thickness(0,10,10,0);edit.HorizontalAlignment=HorizontalAlignment.Right;edit.VerticalAlignment=VerticalAlignment.Top;edit.ToolTip="Edit game";all.Children.Add(edit);
  card.GotKeyboardFocus+=(_,_)=>card.BorderBrush=Brush("#F0EDE5");card.LostKeyboardFocus+=(_,_)=>card.BorderBrush=Brush("#080908");card.MouseEnter+=(_,_)=>{card.BorderBrush=Brush("#F0EDE5");playGlyph.Opacity=1;};card.MouseLeave+=(_,_)=>{card.BorderBrush=Brush(selected.Contains(g.Id)?"#FF765E":"#080908");playGlyph.Opacity=0;};
  Point? origin=null;bool dragged=false;
  card.PreviewMouseLeftButtonDown+=(_,e)=>{if(InsideButton(e.OriginalSource as DependencyObject))return;origin=e.GetPosition(card);dragged=false;};
  card.PreviewMouseMove+=(_,e)=>{if(selectionMode||view!="Pinned"||origin==null||e.LeftButton!=System.Windows.Input.MouseButtonState.Pressed)return;var point=e.GetPosition(card);if(Math.Abs(point.X-origin.Value.X)>SystemParameters.MinimumHorizontalDragDistance||Math.Abs(point.Y-origin.Value.Y)>SystemParameters.MinimumVerticalDragDistance){dragged=true;origin=null;DragDrop.DoDragDrop(card,new DataObject("Playdeck.Pin",g.Id),DragDropEffects.Move);}};
  card.MouseLeftButtonUp+=(_,e)=>{if(dragged||origin==null||InsideButton(e.OriginalSource as DependencyObject)){origin=null;return;}origin=null;e.Handled=true;ActivateCard(g);};
  card.KeyDown+=(_,e)=>{if(InsideButton(e.OriginalSource as DependencyObject))return;if(e.Key==System.Windows.Input.Key.Delete){e.Handled=true;TrashGames(new[]{g});return;}if(e.Key==System.Windows.Input.Key.Enter||e.Key==System.Windows.Input.Key.Space){e.Handled=true;ActivateCard(g);}};
  card.DragLeave+=(_,_)=>card.BorderBrush=Brush("#080908");card.DragOver+=(_,e)=>{card.BorderBrush=Brush("#F0EDE5");e.Effects=e.Data.GetDataPresent("Playdeck.Pin")?DragDropEffects.Move:DragDropEffects.None;e.Handled=true;};card.Drop+=(_,e)=>{if(e.Data.GetData("Playdeck.Pin") is string id){LibraryActions.MovePin(library,id,g.Id,e.GetPosition(card).Y>card.ActualHeight/2);Save();Render();}e.Handled=true;};
  card.ToolTip=$"{g.Name} · Click to {(g.Archived?"restore":"play")}\n{g.MetadataStatus}";System.Windows.Automation.AutomationProperties.SetName(card,"Play "+g.Name);return card;
 }
 static bool InsideButton(DependencyObject? source){while(source!=null){if(source is Button)return true;source=source is Visual?VisualTreeHelper.GetParent(source):LogicalTreeHelper.GetParent(source);}return false;}
 void ActivateCard(Game g){if(System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.Delete)){TrashGames(new[]{g});return;}if(selectionMode){if(!selected.Add(g.Id))selected.Remove(g.Id);Render();return;}if(launchProbe!=null){launchProbe(g);return;}if(g.Archived){g.Archived=false;Save();Render();}else _=Launch(g);}
}





