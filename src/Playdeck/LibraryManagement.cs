using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Playdeck.Core;
namespace Playdeck;
public sealed partial class MainWindow {
 void AddEditorActions(Game g,StackPanel body,Window dialog){
  var actions=new WrapPanel{Margin=new Thickness(0,12,0,10)};actions.Children.Add(Btn("Game version",()=>VersionDialog(g)));actions.Children.Add(Btn("Choose Steam match",()=>ChooseSteamMatch(g)));actions.Children.Add(Btn("Paste cover  ·  Ctrl+V",()=>PasteCover(g,dialog),true));
  Button? pinButton=null;pinButton=Btn(g.Favorite?"Unpin":"Pin",()=>{LibraryActions.Pin(library,g,!g.Favorite);foreach(var box in body.Children.OfType<CheckBox>().Where(x=>x.Content is string label&&label.StartsWith("Pin to")))box.IsChecked=g.Favorite;Save();Render();pinButton!.Content=g.Favorite?"Unpin":"Pin";});actions.Children.Add(pinButton);
  if(g.Favorite){actions.Children.Add(Btn("Move pin to front",()=>{g.PinOrder=library.Games.Where(x=>x.Favorite&&x.PinOrder<int.MaxValue).Select(x=>x.PinOrder).DefaultIfEmpty(0).Min()-1;Save();Render();}));}
  actions.Children.Add(Btn("Move to Trash",()=>{TrashGames(new[]{g});dialog.Close();}));body.Children.Insert(1,actions);
  body.Children.Add(Text("Paste a copied image or a copied local image file. Text URLs are not fetched. Trash keeps the card recoverable for 7 days. Your game files are untouched.",11,"#97A689"));
  dialog.PreviewKeyDown+=(_,e)=>{if(e.Key==System.Windows.Input.Key.V&&(System.Windows.Input.Keyboard.Modifiers&System.Windows.Input.ModifierKeys.Control)!=0&&(!(System.Windows.Input.Keyboard.FocusedElement is TextBox)||ClipboardHasImage())){PasteCover(g,dialog);e.Handled=true;}};
 }
 static bool ClipboardHasImage(){try{return Clipboard.ContainsImage()||Clipboard.ContainsData("PNG")||Clipboard.ContainsFileDropList();}catch{return false;}}
 void PasteCover(Game g,Window owner){try{BitmapSource? image=null;if(Clipboard.ContainsData("PNG")&&Clipboard.GetData("PNG") is Stream png){using(png){image=BitmapFrame.Create(png,BitmapCreateOptions.PreservePixelFormat,BitmapCacheOption.OnLoad);image.Freeze();}}else if(Clipboard.ContainsImage())image=Clipboard.GetImage();else if(Clipboard.ContainsFileDropList()){var files=Clipboard.GetFileDropList();if(files.Count>0&&files[0]!=null)image=LoadImage(files[0]!,1400);}if(image==null){MessageBox.Show(owner,"Copy an image (or a local image file), then choose Paste cover.","No clipboard image");return;}SaveCover(g,image);Save();Render();}catch(Exception ex){MessageBox.Show(owner,"The clipboard image could not be read. "+ex.Message,"Paste cover");}}
 void SaveCover(Game g,BitmapSource image){
  if(image.PixelWidth>20000||image.PixelHeight>20000||(long)image.PixelWidth*image.PixelHeight>80_000_000)throw new InvalidOperationException("Image is too large. Use a smaller image.");
  double scale=Math.Min(1,1400.0/Math.Max(image.PixelWidth,image.PixelHeight));if(scale<1)image=new TransformedBitmap(image,new ScaleTransform(scale,scale));
  string directory=System.IO.Path.Combine(root,"artwork");Directory.CreateDirectory(directory);string path=System.IO.Path.Combine(directory,g.Id+"-custom-"+Guid.NewGuid().ToString("N")+".png");var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(image));using(var stream=File.Create(path))encoder.Save(stream);g.CoverPath=path;g.MetadataAttempted=true;g.MetadataStatus="Custom clipboard artwork";
 }
}
