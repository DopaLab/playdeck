using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using Playdeck.Core;
namespace Playdeck;
// Static card content is one retained drawing, not a tree of panels, borders and text controls.
public sealed class GameSurface:FrameworkElement {
 readonly string name,detail;readonly ImageSource cover;readonly ImageSource? centerIcon,localIcon;readonly bool pinned,compact;readonly VersionBadge badge;readonly string versionLabel;
 FormattedText? titleText,detailText,pinText,badgeText;double textWidth,textDpi;bool showPlay;
 static readonly Brush BadgeTexture=CreateBadgeTexture();
 static Brush CreateBadgeTexture(){var drawing=new DrawingGroup();using(var dc=drawing.Open()){dc.DrawRectangle(ChartInk.Brush("#19181B"),null,new Rect(0,0,4,4));dc.DrawRectangle(ChartInk.Brush("#302E33"),null,new Rect(0,0,2,2));dc.DrawRectangle(ChartInk.Brush("#302E33"),null,new Rect(2,2,2,2));}drawing.Freeze();var brush=new DrawingBrush(drawing){TileMode=TileMode.Tile,ViewportUnits=BrushMappingMode.Absolute,Viewport=new Rect(0,0,4,4)};brush.Freeze();return brush;}
 static readonly Typeface BadgeFont=new(new FontFamily(new Uri("pack://application:,,,/"),"./Assets/Fonts/#Barlow Condensed"),FontStyles.Normal,FontWeights.Bold,FontStretches.Normal);
 static readonly Brush Shade=new LinearGradientBrush(Colors.Transparent,Color.FromArgb(170,0,0,0),90);
 public GameSurface(string name,string detail,ImageSource cover,ImageSource? centerIcon,ImageSource? localIcon,bool pinned,bool compact,VersionRecord version){this.name=name;this.detail=detail;this.cover=cover;this.centerIcon=centerIcon;this.localIcon=localIcon;this.pinned=pinned;this.compact=compact;badge=VersionBadge.For(version);versionLabel=VersionBadge.CompactLabel(version);SnapsToDevicePixels=true;}
 double badgeWidth,badgeDpi;
 public Action? OpenVersion;
 public bool IsVersionHit(Point point)=>ActualWidth>64&&new Rect(11,pinned?39:11,VersionChipWidth(ActualWidth),20).Contains(point);
 protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer()=>new VersionPeer(this);
 sealed class VersionPeer(GameSurface owner):System.Windows.Automation.Peers.FrameworkElementAutomationPeer(owner),System.Windows.Automation.Provider.IInvokeProvider {
  protected override string GetNameCore()=>"Version details for "+owner.name+" · "+owner.badge.Text;
  protected override System.Windows.Automation.Peers.AutomationControlType GetAutomationControlTypeCore()=>System.Windows.Automation.Peers.AutomationControlType.Button;
  protected override Rect GetBoundingRectangleCore(){if(!owner.IsVisible)return Rect.Empty;var dpi=VisualTreeHelper.GetDpi(owner);return new Rect(owner.PointToScreen(new Point(11,owner.pinned?39:11)),new Size(owner.VersionChipWidth(owner.ActualWidth)*dpi.DpiScaleX,20*dpi.DpiScaleY));}
  public override object? GetPattern(System.Windows.Automation.Peers.PatternInterface pattern)=>pattern==System.Windows.Automation.Peers.PatternInterface.Invoke?this:base.GetPattern(pattern);
  public void Invoke()=>owner.Dispatcher.BeginInvoke(()=>owner.OpenVersion?.Invoke());
 }
 static readonly System.Collections.Generic.Dictionary<(string Label,string Color,double Width,double Dpi),FormattedText> badgeCache=new();
 public double VersionChipWidth(double width){double dpi=VisualTreeHelper.GetDpi(this).PixelsPerDip;double maximum=Math.Min(112,width-64);if(badgeText==null||badgeDpi!=dpi||badgeText.MaxTextWidth!=Math.Max(1,maximum-16)){badgeDpi=dpi;var key=(versionLabel,badge.Color,maximum,dpi);if(!badgeCache.TryGetValue(key,out badgeText)){if(badgeCache.Count>=256)badgeCache.Clear();badgeText=new FormattedText(versionLabel,CultureInfo.CurrentCulture,FlowDirection.LeftToRight,BadgeFont,11,ChartInk.Brush(badge.Color),dpi){MaxTextWidth=Math.Max(1,maximum-16),MaxLineCount=1,Trimming=TextTrimming.CharacterEllipsis};badgeCache[key]=badgeText;}badgeWidth=Math.Min(maximum,Math.Max(42,badgeText.Width+16));}return badgeWidth;}

 public bool ShowPlay{get=>showPlay;set{if(showPlay==value)return;showPlay=value;InvalidateVisual();}}
 static void FillImage(DrawingContext dc,ImageSource image,Rect box){double scale=Math.Max(box.Width/image.Width,box.Height/image.Height);double w=image.Width*scale,h=image.Height*scale;dc.PushClip(new RectangleGeometry(box));dc.DrawImage(image,new Rect(box.X+(box.Width-w)/2,box.Y+(box.Height-h)/2,w,h));dc.Pop();}
 protected override void OnRender(DrawingContext dc){double w=ActualWidth,h=ActualHeight,art=h-71;if(w<=0||art<=0)return;dc.PushClip(new RectangleGeometry(new Rect(0,0,w,h),15,15));FillImage(dc,cover,new Rect(0,0,w,art));
  if(centerIcon!=null){dc.DrawEllipse(ChartInk.Brush("#BF171717"),new Pen(ChartInk.Brush("#666666"),2),new Point(w/2,art/2),47,47);double scale=Math.Min(66/centerIcon.Width,66/centerIcon.Height);IconShape.Draw(dc,centerIcon,new Rect(w/2-centerIcon.Width*scale/2,art/2-centerIcon.Height*scale/2,centerIcon.Width*scale,centerIcon.Height*scale));}
  dc.DrawRectangle(Shade,null,new Rect(0,Math.Max(0,art-65),w,Math.Min(65,art)));
  double dpi=VisualTreeHelper.GetDpi(this).PixelsPerDip;double x=localIcon!=null?52:13;
  if(titleText==null||textWidth!=w||textDpi!=dpi){textWidth=w;textDpi=dpi;titleText=new FormattedText(name,CultureInfo.CurrentCulture,FlowDirection.LeftToRight,ChartInk.Display,compact?17:19,ChartInk.Brush("#F1F2E8"),dpi){MaxTextWidth=Math.Max(1,w-x-10),MaxLineCount=1,Trimming=TextTrimming.CharacterEllipsis};detailText=new FormattedText(detail,CultureInfo.CurrentCulture,FlowDirection.LeftToRight,ChartInk.Body,11,ChartInk.Brush("#B3BCAA"),dpi);pinText=new FormattedText("◆  PINNED",CultureInfo.CurrentCulture,FlowDirection.LeftToRight,ChartInk.Display,9,ChartInk.Brush("#D8FF39"),dpi);}
  VersionChipWidth(w);
  if(pinned){dc.DrawRoundedRectangle(ChartInk.Brush("#CC10120D"),null,new Rect(11,11,pinText!.Width+18,pinText.Height+8),11,11);dc.DrawText(pinText,new Point(20,15));}
  double badgeY=pinned?39:11;dc.DrawRoundedRectangle(BadgeTexture,new Pen(ChartInk.Brush(badge.Color),1),new Rect(11,badgeY,badgeWidth,20),10,10);dc.DrawText(badgeText!,new Point(11+(badgeWidth-badgeText!.Width)/2,badgeY+(20-badgeText.Height)/2));
  if(localIcon!=null)IconShape.Draw(dc,localIcon,new Rect(13,art+13,30,30));dc.DrawText(titleText,new Point(x,art+11));dc.DrawText(detailText!,new Point(x,art+35));
  if(showPlay){var point=new Point(w-33.5,art-33.5);dc.DrawEllipse(ChartInk.Brush("#FF765E"),null,point,21.5,21.5);var triangle=new StreamGeometry();using(var c=triangle.Open()){c.BeginFigure(new Point(point.X-5,point.Y-8),true,true);c.LineTo(new Point(point.X+8,point.Y),true,false);c.LineTo(new Point(point.X-5,point.Y+8),true,false);}dc.DrawGeometry(ChartInk.Brush("#111608"),null,triangle);}dc.Pop();
 }
}
