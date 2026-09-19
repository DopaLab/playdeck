using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Playdeck.Core;
namespace Playdeck;
public sealed partial class MainWindow {
 static readonly DrawingImage EditorDots=CreateEditorDots();
 static DrawingImage CreateEditorDots(){var drawing=new DrawingGroup();using(var dc=drawing.Open()){for(int i=0;i<3;i++)dc.DrawEllipse(Brushes.White,null,new Point(2+i*7,2),1.5,1.5);}drawing.Freeze();var image=new DrawingImage(drawing);image.Freeze();return image;}
 static BitmapImage Asset(string name,int decode){var image=new BitmapImage();image.BeginInit();image.UriSource=new Uri("pack://application:,,,/Assets/"+name);image.DecodePixelWidth=decode;image.CacheOption=BitmapCacheOption.OnLoad;image.EndInit();image.Freeze();return image;}
 readonly BitmapImage city=Asset("fallback.png",480);
 readonly BitmapImage brandIcon=Asset("icon.png",96);
 void Render(){
  if(!IsInitialized)return;bool searchFocused=search.IsKeyboardFocused;int caret=search.CaretIndex;content.Children.Clear();foreach(var item in navigation){item.Value.Background=Brush(item.Key==view?"#E1FF46":"#00000000");item.Value.Foreground=Brush(item.Key==view?"#11150B":"#ECEEE5");}
  content.Background=view is "Folders & settings" or "Activity"?Brush("#181818"):Brush("#00000000");if(view=="Folders & settings"){Settings();return;}if(view=="Activity"){Activity();return;}if(view=="Trash"){Trash();return;}
  var tool=LibraryToolbar();
  var games=library.Games.Where(g=>!g.Removed&&g.Archived==(view=="Archived")&&(view!="Pinned"||g.Favorite)&&g.Name.Contains(search.Text,StringComparison.CurrentCultureIgnoreCase));
  games=view=="Pinned"?games.OrderBy(g=>g.PinOrder).ThenBy(g=>g.Added):sort.SelectedIndex switch{1=>games.OrderByDescending(g=>History(g).Sum(s=>s.Seconds)),2=>games.OrderByDescending(g=>History(g).Count()),3=>games.OrderByDescending(g=>g.Added),4=>games.OrderByDescending(g=>g.ReleaseDate),5=>games.OrderBy(g=>g.Name),7=>games.OrderByDescending(UpdateDates.SortDate).ThenBy(g=>g.Name),6=>games.OrderByDescending(g=>g.VersionInfo.NeedsUpdate).ThenBy(g=>g.Name),_=>games.OrderByDescending(g=>History(g).Select(s=>(DateTimeOffset?)s.Started).Max())};
  var list=games.ToList();var count=Text($"{list.Count:00}  /  {(demo?"DEMO":view.ToUpperInvariant())}",11,"#B9C1AF",FontWeights.Bold);count.Margin=new Thickness(17,0,0,0);count.VerticalAlignment=VerticalAlignment.Center;content.Children.Add(tool);
  if(list.Count==0){var empty=new StackPanel{Margin=new Thickness(40,70,40,70)};empty.Children.Add(new Image{Source=brandIcon,Width=90,Height=90,HorizontalAlignment=HorizontalAlignment.Left});empty.Children.Add(Text(view=="Pinned"?"Your instant-play shelf.":"Make room for a good game.",30,"#F3F2E8",FontWeights.Black));empty.Children.Add(Text(view=="Pinned"?"Pin games from the ··· editor. Drag them into your own order here.":search.Text.Length>0?"No matches. Try another title.":"Right-click a game in Explorer and choose Add to Playdeck.",15,"#B9C1AF"));var b=Btn("＋ Add games",AddMenu,true);b.HorizontalAlignment=HorizontalAlignment.Left;b.Margin=new Thickness(0,22,0,0);empty.Children.Add(b);content.Children.Add(empty);}
  else{var cards=new CardPanel{CardWidth=compact?176:218,Items=list,HeightFactory=CardHeight,Factory=(g,width)=>Card(g,width)};content.Children.Add(cards);cards.SetViewport(Math.Max(0,libraryScroll.VerticalOffset-tool.DesiredSize.Height),libraryScroll.ViewportHeight>0?libraryScroll.ViewportHeight:Height);}
  UpdateLayout();
  if(selectionMode)status.Text="Select cards, then choose Trash. Hold Delete and click any card for quick removal.";else if(!enriching)status.Text=$"●  YOUR LIBRARY     {Hours(sessions.Sum(s=>s.Seconds))} played    /    {sessions.Count} launches     ·     Click a card to play.  ··· to edit.";
  if(searchFocused){search.Focus();search.CaretIndex=Math.Min(caret,search.Text.Length);}
 }
 double CardHeight(double width)=>Math.Min(CardPanel.HeightFor(width),Math.Max(247,Height-(ActualWidth<1120?360:230)));
 FrameworkElement Card(Game g,double? forcedWidth=null){
  double width=forcedWidth??(compact?176:218);double height=CardHeight(width);double artHeight=height-77;
  var card=new Border{Width=width,Height=height,CornerRadius=new CornerRadius(18),BorderBrush=Brush("#080908"),BorderThickness=new Thickness(3),Background=Brush("#303033"),Cursor=System.Windows.Input.Cursors.Hand,Focusable=true,AllowDrop=view=="Pinned"};
  var all=new Grid();card.Child=all;var cover=LoadImage(g.CoverPath,350);var localIcon=LoadImage(CachedIcon(g),40);
  var surface=new GameSurface(g.Name,g.IsTool?"TOOL  /  Not tracked":$"{Hours(History(g).Sum(s=>s.Seconds))}  /  {History(g).Count()} launches",cover??city,cover==null?LoadImage(CachedIcon(g),128)??brandIcon:null,localIcon,g.Favorite,compact,g.VersionInfo,g,library.CardInfoMode);all.Children.Add(surface);
  surface.OpenVersion=()=>OpenGameUpdate(g);

  if(cover==null){var art=new Grid{Height=artHeight,VerticalAlignment=VerticalAlignment.Top};var match=Btn("Find cover",()=>ChooseSteamMatch(g));match.FontSize=13;match.MinHeight=28;match.Height=28;match.Padding=new Thickness(10,2,10,2);match.Margin=new Thickness(10,0,0,10);match.HorizontalAlignment=HorizontalAlignment.Left;match.VerticalAlignment=VerticalAlignment.Bottom;art.Children.Add(match);all.Children.Add(art);}
  if(selected.Contains(g.Id))card.BorderBrush=Brush("#FF765E");
  var edit=Btn("",()=>Edit(g));var dots=new Image{Source=EditorDots,Width=17,Height=4,HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center};edit.Content=dots;edit.MinHeight=28;edit.Width=28;edit.Height=28;edit.Padding=new Thickness(0);edit.FontSize=19;edit.Background=Brush("#E5151712");edit.Margin=new Thickness(0,10,10,0);edit.HorizontalAlignment=HorizontalAlignment.Right;edit.VerticalAlignment=VerticalAlignment.Top;edit.ToolTip="Edit game";all.Children.Add(edit);
  card.GotKeyboardFocus+=(_,_)=>card.BorderBrush=Brush("#F0EDE5");card.LostKeyboardFocus+=(_,_)=>card.BorderBrush=Brush("#080908");card.MouseEnter+=(_,_)=>{card.BorderBrush=Brush("#F0EDE5");surface.ShowPlay=true;};card.MouseLeave+=(_,_)=>{card.BorderBrush=Brush(selected.Contains(g.Id)?"#FF765E":"#080908");surface.ShowPlay=false;};
  Point? origin=null;bool dragged=false,versionPressed=false;
  card.PreviewMouseLeftButtonDown+=(_,e)=>{if(InsideButton(e.OriginalSource as DependencyObject))return;origin=e.GetPosition(card);dragged=false;versionPressed=!selectionMode&&!System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.Delete)&&surface.IsVersionHit(e.GetPosition(surface));};
  card.PreviewMouseMove+=(_,e)=>{if(versionPressed||selectionMode||view!="Pinned"||origin==null||e.LeftButton!=System.Windows.Input.MouseButtonState.Pressed)return;var point=e.GetPosition(card);if(Math.Abs(point.X-origin.Value.X)>SystemParameters.MinimumHorizontalDragDistance||Math.Abs(point.Y-origin.Value.Y)>SystemParameters.MinimumVerticalDragDistance){dragged=true;origin=null;DragDrop.DoDragDrop(card,new DataObject("Playdeck.Pin",g.Id),DragDropEffects.Move);}};
  card.MouseLeftButtonUp+=(_,e)=>{if(dragged||origin==null||InsideButton(e.OriginalSource as DependencyObject)){origin=null;return;}origin=null;e.Handled=true;if(versionPressed){versionPressed=false;if(surface.IsVersionHit(e.GetPosition(surface)))OpenGameUpdate(g);return;}ActivateCard(g);};
  card.KeyDown+=(_,e)=>{if(InsideButton(e.OriginalSource as DependencyObject))return;if(e.Key==System.Windows.Input.Key.V){e.Handled=true;OpenGameUpdate(g);return;}if(e.Key==System.Windows.Input.Key.Delete){e.Handled=true;TrashGames(new[]{g});return;}if(e.Key==System.Windows.Input.Key.Enter||e.Key==System.Windows.Input.Key.Space){e.Handled=true;ActivateCard(g);}};
  card.DragLeave+=(_,_)=>card.BorderBrush=Brush("#080908");card.DragOver+=(_,e)=>{card.BorderBrush=Brush("#F0EDE5");e.Effects=e.Data.GetDataPresent("Playdeck.Pin")?DragDropEffects.Move:DragDropEffects.None;e.Handled=true;};card.Drop+=(_,e)=>{if(e.Data.GetData("Playdeck.Pin") is string id){LibraryActions.MovePin(library,id,g.Id,e.GetPosition(card).Y>card.ActualHeight/2);Save();Render();}e.Handled=true;};
  card.ToolTip=$"{g.Name} · Click to {(g.Archived?"restore":"play")}\n{g.MetadataStatus}\n{(library.CardInfoMode=="Version"?g.VersionInfo.Summary:UpdateDates.Badge(g).Text)}\nClick the chip or press V for details.";System.Windows.Automation.AutomationProperties.SetName(card,"Play "+g.Name);return card;
 }
 static bool InsideButton(DependencyObject? source){while(source!=null){if(source is Button)return true;source=source is Visual?VisualTreeHelper.GetParent(source):LogicalTreeHelper.GetParent(source);}return false;}
 void ActivateCard(Game g){if(System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.Delete)){TrashGames(new[]{g});return;}if(selectionMode){if(!selected.Add(g.Id))selected.Remove(g.Id);Render();return;}if(launchProbe!=null){launchProbe(g);return;}if(g.Archived){g.Archived=false;Save();Render();}else _=Launch(g);}
}





