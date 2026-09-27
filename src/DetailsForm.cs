using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;
namespace CodexQuotaLite
{
 public sealed class DetailsForm : Form
 {
  private static Color GlassTint { get { return Theme.IsDark?Color.FromArgb(190,12,18,29):Color.FromArgb(136,246,248,251); } }
  private static Color GlassMuted { get { return Theme.IsDark?Theme.Muted:Color.FromArgb(26,37,54); } }
  private static Color GlassLink { get { return Theme.IsDark?Theme.Blue:Color.FromArgb(12,40,100); } }
  private readonly AppSettings settings;
  [DllImport("dwmapi.dll", PreserveSig=true)]
  private static extern int DwmSetWindowAttribute(IntPtr handle,int attribute,ref int value,int size);
  private float scale, preferredScale;
  private Bitmap glassBackground;
  private string plan;
  private readonly Panel cards = new Panel();
  private readonly Label status = new GlassLabel(), updated = new GlassLabel();
  private readonly LinkLabel github = new GlassLinkLabel();
  private readonly Label resetNotice = new GlassLabel();
  private readonly LinkLabel resetSource = new GlassLinkLabel();
  private ResetFeed resetFeed;
  private readonly Label version = new GlassLabel();
  private readonly Button refresh = new GlassButton(), close = new GlassButton(), languageChoice = new GlassButton(), locationChoice = new GlassButton();
  private readonly UiDarkChoice windowChoice = new UiDarkChoice();
  private readonly UiDarkChoice themeChoice = new UiDarkChoice();
  private readonly Label themeLabel = new GlassLabel();
  private readonly List<QuotaWindow> windows = new List<QuotaWindow>();
  private bool binding, quitting, lastStale, lastBusy, editingLocation;
  public bool EditingLocation { get { return editingLocation; } }
  private WidgetForm anchor;
  private QuotaSnapshot lastSnapshot;
  private string lastSelectedId, lastMessage, lastSettingsMessage;
  public event EventHandler RefreshRequested;
  public event EventHandler SettingsChanged;
  public string SelectedLanguage { get { return UiText.Language; } }
  public string SelectedWindowId { get { return windowChoice.SelectedIndex >= 0 && windowChoice.SelectedIndex < windows.Count ? windows[windowChoice.SelectedIndex].Id : null; } }
  public string SelectedThemeMode { get { return themeChoice.SelectedIndex==1?"dark":themeChoice.SelectedIndex==2?"auto":"light"; } }
  private float CardsHeight { get { return Math.Max(70, windows.Count * 74 + Math.Max(0, windows.Count - 1) * 8); } }
  private float ChoiceY { get { return 84 + CardsHeight + 48; } }
  private float ThemeY { get { return ChoiceY + 36; } }
  private float FooterY { get { return ThemeY + 46; } }
  private float LogicalHeight { get { return FooterY + 44; } }
  public DetailsForm(AppSettings settings)
  {
   this.settings=settings??new AppSettings();
   UiText.Language = settings == null ? "zh" : settings.Language;
   FormBorderStyle=FormBorderStyle.None;StartPosition=FormStartPosition.Manual;ShowInTaskbar=false;
   AutoScaleMode=AutoScaleMode.None;BackColor=Theme.Background;DoubleBuffered=true;KeyPreview=true;
   cards.AutoScroll=false;cards.BackColor=Color.Transparent;
   resetNotice.ForeColor=GlassMuted;
   resetSource.LinkColor=GlassLink;resetSource.ActiveLinkColor=Theme.Aqua;resetSource.VisitedLinkColor=resetSource.LinkColor;
   resetSource.UseCompatibleTextRendering=false;resetSource.TextAlign=ContentAlignment.MiddleRight;
   resetNotice.UseCompatibleTextRendering=false;resetNotice.TextAlign=ContentAlignment.MiddleLeft;
   resetSource.LinkBehavior=LinkBehavior.HoverUnderline;
   resetSource.LinkClicked+=delegate{
    string url=resetFeed!=null&&resetFeed.Latest!=null?resetFeed.Latest.Url:"https://codex-resets.com/";
    try{System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url){UseShellExecute=true});}
    catch(System.ComponentModel.Win32Exception){resetNotice.Text=UiText.T("无法打开浏览器","Cannot open browser");}
    catch(InvalidOperationException){resetNotice.Text=UiText.T("无法打开浏览器","Cannot open browser");}
   };
   github.Text=UiText.T("GITHUB主页","GITHUB");github.LinkColor=GlassLink;github.ActiveLinkColor=Theme.Aqua;
   github.VisitedLinkColor=github.LinkColor;github.LinkBehavior=LinkBehavior.HoverUnderline;
   github.TextAlign=ContentAlignment.MiddleCenter;github.Cursor=Cursors.Hand;
   github.UseCompatibleTextRendering=false;
   github.LinkClicked+=delegate{
    try{System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("https://github.com/Amygdala42/CodexUsage"){UseShellExecute=true});}
    catch(System.ComponentModel.Win32Exception){ShowLinkError();}
    catch(InvalidOperationException){ShowLinkError();}
   };
   version.Text="v"+typeof(DetailsForm).Assembly.GetName().Version.ToString(3);
   version.ForeColor=GlassMuted;version.TextAlign=ContentAlignment.MiddleCenter;
   version.UseCompatibleTextRendering=false;
   SetupButton(close);close.Text="×";close.Click+=delegate{Hide();};
   SetupButton(refresh);refresh.Click+=delegate{if(RefreshRequested!=null)RefreshRequested(this,EventArgs.Empty);};
   SetupButton(languageChoice);languageChoice.Click+=delegate{
    UiText.Language=UiText.English?"zh":"en";
    SetState(lastSnapshot,SelectedWindowId??lastSelectedId,lastStale,lastBusy,lastMessage,lastSettingsMessage);
    if(SettingsChanged!=null)SettingsChanged(this,EventArgs.Empty);
   };
   SetupButton(locationChoice);locationChoice.Click+=delegate{EditLocation();};
   themeLabel.ForeColor=GlassMuted;themeLabel.TextAlign=ContentAlignment.MiddleLeft;
   themeChoice.BackColor=Theme.Card;themeChoice.ForeColor=Theme.Text;themeChoice.GlassSurface=true;
   themeChoice.Items.AddRange(new object[]{"浅色","深色","日出日落自动"});
   themeChoice.SelectedIndex=this.settings.ThemeMode=="dark"?1:this.settings.ThemeMode=="auto"?2:0;
   status.ForeColor=GlassMuted;updated.ForeColor=GlassMuted;status.AutoEllipsis=false;
   status.TextAlign=ContentAlignment.MiddleLeft;updated.TextAlign=ContentAlignment.MiddleLeft;
   windowChoice.BackColor=Theme.Card;windowChoice.ForeColor=Theme.Text;windowChoice.GlassSurface=true;
   Controls.AddRange(new Control[]{cards,close,refresh,status,updated,languageChoice,windowChoice,themeLabel,themeChoice,locationChoice,github,version,resetNotice,resetSource});
   windowChoice.SelectedIndexChanged+=delegate{if(!binding&&SettingsChanged!=null)SettingsChanged(this,EventArgs.Empty);};
   themeChoice.SelectedIndexChanged+=delegate{
    if(binding)return;
    if(themeChoice.SelectedIndex==2&&(!this.settings.Latitude.HasValue||!this.settings.Longitude.HasValue))
    {
     if(!EditLocation()){binding=true;themeChoice.SelectedIndex=this.settings.ThemeMode=="dark"?1:0;binding=false;return;}
    }
    if(SettingsChanged!=null)SettingsChanged(this,EventArgs.Empty);
   };
   ApplyScale(100);SetState(null,null,false,false,null,null);
  }
  protected override CreateParams CreateParams
  {
   get{CreateParams parameters=base.CreateParams;parameters.ClassStyle&=~0x00020000;return parameters;}
  }
  protected override void OnHandleCreated(EventArgs e)
  {
   base.OnHandleCreated(e);
   try
   {
    int noNonClientRendering=1;
    DwmSetWindowAttribute(Handle,2,ref noNonClientRendering,sizeof(int));
    int noSystemBorder=unchecked((int)0xFFFFFFFE);
    DwmSetWindowAttribute(Handle,34,ref noSystemBorder,sizeof(int));
   }
   catch(DllNotFoundException){}
   catch(EntryPointNotFoundException){}
  }
  private bool EditLocation()
  {
   editingLocation=true;
   try
   {
    using(LocationDialog dialog=new LocationDialog(settings.Latitude,settings.Longitude))
    {
     if(dialog.ShowDialog(this)!=DialogResult.OK)return false;
     settings.Latitude=dialog.Latitude;settings.Longitude=dialog.Longitude;
    }
   }
   finally{editingLocation=false;}
   UpdateLocationDescription();
   if(SettingsChanged!=null)SettingsChanged(this,EventArgs.Empty);
   return true;
  }
  private void UpdateLocationDescription()
  {
   locationChoice.AccessibleDescription=settings.Latitude.HasValue&&settings.Longitude.HasValue?
    String.Format(System.Globalization.CultureInfo.InvariantCulture,"{0:0.####}°, {1:0.####}°",settings.Latitude.Value,settings.Longitude.Value):
    UiText.T("尚未设置位置","Location not set");
  }
  public void ApplyTheme()
  {
   BackColor=Theme.Background;
   resetNotice.ForeColor=GlassMuted;version.ForeColor=GlassMuted;updated.ForeColor=GlassMuted;themeLabel.ForeColor=GlassMuted;
   github.LinkColor=GlassLink;github.ActiveLinkColor=Theme.Aqua;github.VisitedLinkColor=GlassLink;
   resetSource.LinkColor=GlassLink;resetSource.ActiveLinkColor=Theme.Aqua;resetSource.VisitedLinkColor=GlassLink;
   foreach(Button button in new Button[]{close,refresh,languageChoice,locationChoice})SetupButton(button);
   foreach(Control card in cards.Controls){card.BackColor=Theme.Background;card.Invalidate();}
   windowChoice.BackColor=Theme.Card;windowChoice.ForeColor=Theme.Text;windowChoice.CloseDropDown();
   themeChoice.BackColor=Theme.Card;themeChoice.ForeColor=Theme.Text;themeChoice.CloseDropDown();
   Invalidate(true);
  }
  private static void SetupButton(Button button){button.FlatStyle=FlatStyle.Flat;button.FlatAppearance.BorderColor=Theme.Border;button.FlatAppearance.MouseOverBackColor=Theme.Border;button.BackColor=Theme.Card;button.ForeColor=Theme.Text;button.Cursor=Cursors.Hand;}
  private void CaptureGlassBackground()
  {
   Bitmap old=glassBackground;glassBackground=null;if(old!=null)old.Dispose();
   if(SystemInformation.HighContrast||Width<=0||Height<=0)return;
   Bitmap captured=null,small=null,blurred=null;
   try
   {
    captured=new Bitmap(Width,Height);
    using(Graphics g=Graphics.FromImage(captured))g.CopyFromScreen(Location,Point.Empty,Size,CopyPixelOperation.SourceCopy);
    int divisor=Math.Max(3,(int)Math.Round(4*scale));
    small=new Bitmap(Math.Max(1,Width/divisor),Math.Max(1,Height/divisor),PixelFormat.Format32bppArgb);
    using(Graphics g=Graphics.FromImage(small)){g.InterpolationMode=InterpolationMode.HighQualityBilinear;g.DrawImage(captured,new Rectangle(0,0,small.Width,small.Height));}
    Bitmap softened=SoftenBackground(small);small.Dispose();small=softened;
    blurred=new Bitmap(Width,Height,PixelFormat.Format32bppArgb);
    using(Graphics g=Graphics.FromImage(blurred)){g.InterpolationMode=InterpolationMode.HighQualityBilinear;g.DrawImage(small,new Rectangle(0,0,Width,Height));}
    SoftenGlassEdges(blurred);
    glassBackground=blurred;blurred=null;
   }
   catch(Win32Exception){}
   catch(ExternalException){}
   finally{if(captured!=null)captured.Dispose();if(small!=null)small.Dispose();if(blurred!=null)blurred.Dispose();}
   Invalidate(true);
  }
  private static Bitmap SoftenBackground(Bitmap source)
  {
   int width=source.Width,height=source.Height,rowBytes=width*4;
   byte[] pixels=new byte[rowBytes*height],horizontal=new byte[pixels.Length],vertical=new byte[pixels.Length];
   Rectangle bounds=new Rectangle(0,0,width,height);
   BitmapData input=source.LockBits(bounds,ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
   try{for(int y=0;y<height;y++)Marshal.Copy(IntPtr.Add(input.Scan0,y*input.Stride),pixels,y*rowBytes,rowBytes);}
   finally{source.UnlockBits(input);}
   int[] weights={1,6,15,20,15,6,1};
   for(int y=0;y<height;y++)for(int x=0;x<width;x++)
   {
    int index=(y*width+x)*4;
    for(int channel=0;channel<3;channel++)
    {
     int sum=0;
     for(int tap=-3;tap<=3;tap++){int sample=Math.Max(0,Math.Min(width-1,x+tap));sum+=pixels[(y*width+sample)*4+channel]*weights[tap+3];}
     horizontal[index+channel]=(byte)((sum+32)/64);
    }
    horizontal[index+3]=255;
   }
   for(int y=0;y<height;y++)for(int x=0;x<width;x++)
   {
    int index=(y*width+x)*4;
    for(int channel=0;channel<3;channel++)
    {
     int sum=0;
     for(int tap=-3;tap<=3;tap++){int sample=Math.Max(0,Math.Min(height-1,y+tap));sum+=horizontal[(sample*width+x)*4+channel]*weights[tap+3];}
     vertical[index+channel]=(byte)((sum+32)/64);
    }
    vertical[index+3]=255;
   }
   Bitmap result=new Bitmap(width,height,PixelFormat.Format32bppArgb);
   BitmapData output=result.LockBits(bounds,ImageLockMode.WriteOnly,PixelFormat.Format32bppArgb);
   try{for(int y=0;y<height;y++)Marshal.Copy(vertical,y*rowBytes,IntPtr.Add(output.Scan0,y*output.Stride),rowBytes);}
   finally{result.UnlockBits(output);}
   return result;
  }
  private void SoftenGlassEdges(Bitmap image)
  {
   // Fade the sampled desktop toward its median color at the window boundary.
   // Extending a nearby pixel can carry a bright desktop object to the edge.
   int width=image.Width,height=image.Height,rowBytes=width*4;
   int edge=Math.Max(1,Math.Min((int)Math.Round(42*scale),Math.Min(width,height)/3));
   int[] redSamples=new int[25],greenSamples=new int[25],blueSamples=new int[25];
   int sampleIndex=0;
   for(int iy=1;iy<=5;iy++)for(int ix=1;ix<=5;ix++)
   {
    Color sample=image.GetPixel(width*ix/6,height*iy/6);
    redSamples[sampleIndex]=sample.R;
    greenSamples[sampleIndex]=sample.G;
    blueSamples[sampleIndex]=sample.B;
    sampleIndex++;
   }
   Array.Sort(redSamples);Array.Sort(greenSamples);Array.Sort(blueSamples);
   int stableRed=redSamples[12],stableGreen=greenSamples[12],stableBlue=blueSamples[12];
   byte[] source=new byte[rowBytes*height],output=new byte[source.Length];
   Rectangle bounds=new Rectangle(0,0,image.Width,image.Height);
   BitmapData data=image.LockBits(bounds,ImageLockMode.ReadWrite,PixelFormat.Format32bppArgb);
   try
   {
    for(int y=0;y<height;y++)Marshal.Copy(IntPtr.Add(data.Scan0,y*data.Stride),source,y*rowBytes,rowBytes);
    Array.Copy(source,output,source.Length);
    for(int y=0;y<height;y++)
    {
     int vertical=Math.Min(y,height-1-y);
     for(int x=0;x<width;x++)
     {
      int distance=Math.Min(vertical,Math.Min(x,width-1-x));
      if(distance>=edge)continue;
      float t=1f-distance/(float)edge;
      float weight=t*t*(3f-2f*t);
      int offset=(y*width+x)*4;
      output[offset]=(byte)Math.Round(source[offset]*(1f-weight)+stableBlue*weight);
      output[offset+1]=(byte)Math.Round(source[offset+1]*(1f-weight)+stableGreen*weight);
      output[offset+2]=(byte)Math.Round(source[offset+2]*(1f-weight)+stableRed*weight);
     }
    }
    for(int y=0;y<height;y++)Marshal.Copy(output,y*rowBytes,IntPtr.Add(data.Scan0,y*data.Stride),rowBytes);
   }
   finally{image.UnlockBits(data);}
  }
  protected override void OnPaintBackground(PaintEventArgs e)
  {
   base.OnPaintBackground(e);
   if(glassBackground==null)return;
   if(glassBackground.Size==ClientSize)e.Graphics.DrawImageUnscaled(glassBackground,0,0);
   else e.Graphics.DrawImage(glassBackground,ClientRectangle);
   using(Brush tint=new SolidBrush(GlassTint))e.Graphics.FillRectangle(tint,ClientRectangle);
  }
  internal bool PaintGlass(Graphics g,Control control)
  {
   if(glassBackground==null)return false;
   Point offset=PointToClient(control.PointToScreen(Point.Empty));
   if(glassBackground.Size==ClientSize)g.DrawImageUnscaled(glassBackground,-offset.X,-offset.Y);
   else g.DrawImage(glassBackground,new Rectangle(-offset.X,-offset.Y,ClientSize.Width,ClientSize.Height));
   using(Brush tint=new SolidBrush(GlassTint))g.FillRectangle(tint,control.ClientRectangle);
   return true;
  }
  internal bool HasGlassBackground { get { return glassBackground!=null; } }
  internal static void DrawGlassRim(Graphics g,GraphicsPath path,float height,bool active,float strength=1f)
  {
   RectangleF bounds=path.GetBounds();
   Color shine=Theme.IsDark?Color.FromArgb(225,229,235):Color.White;
   Color shade=Theme.IsDark?Color.FromArgb(126,137,150):Color.FromArgb(82,93,106);
   using(LinearGradientBrush edge=new LinearGradientBrush(new PointF(0,bounds.Top),new PointF(0,Math.Max(bounds.Top+1,bounds.Bottom)),shine,shade))
   {
    edge.InterpolationColors=new ColorBlend{
     Colors=new Color[]{Color.FromArgb((int)Math.Round((active?92:75)*strength),shine),Color.FromArgb((int)Math.Round((active?52:43)*strength),shine),Color.FromArgb((int)Math.Round((active?34:27)*strength),shade)},
     Positions=new float[]{0f,.32f,1f}};
    using(Pen pen=new Pen(edge,.65f)){pen.LineJoin=LineJoin.Round;g.DrawPath(pen,path);}
   }
  }
  private void ShowLinkError(){MessageBox.Show(this,UiText.T("无法打开浏览器。项目主页：https://github.com/Amygdala42/CodexUsage","Could not open your browser. Project page: https://github.com/Amygdala42/CodexUsage"),UiText.AppName,MessageBoxButtons.OK,MessageBoxIcon.Information);}
  public void ApplyScale(int ignoredLegacyPercent){using(Graphics g=CreateGraphics())preferredScale=g.DpiX/96f;FitToWorkingArea(Screen.FromRectangle(Bounds).WorkingArea);}
  private void FitToWorkingArea(Rectangle area)
  {
   windowChoice.CloseDropDown();themeChoice.CloseDropDown();
   scale=Math.Min(preferredScale,Math.Min(Math.Max(1,area.Width-12)/360f,Math.Max(1,area.Height-12)/LogicalHeight));
   ClientSize=new Size(Math.Max(1,(int)Math.Floor(360*scale)),Math.Max(1,(int)Math.Floor(LogicalHeight*scale)));
   foreach(Control control in Controls){Font previous=control.Font;Font next=new Font("Microsoft YaHei UI",(control==github||control==version?12:control==status||control==updated?10:11)*scale,FontStyle.Regular,GraphicsUnit.Pixel);if(next.Equals(previous))next.Dispose();else{control.Font=next;if(previous!=Font&&previous!=SystemFonts.DefaultFont)previous.Dispose();}}
   windowChoice.ItemHeight=(int)(21*scale);themeChoice.ItemHeight=(int)(21*scale);LayoutControls();Invalidate();
  }
  protected override void OnSizeChanged(EventArgs e)
  {
   base.OnSizeChanged(e);if(scale<=0||Width<=0||Height<=0)return;
   using(GraphicsPath path=Theme.Round(new RectangleF(0,0,Width,Height),18*scale)){Region old=Region;Region=new Region(path);if(old!=null)old.Dispose();}
   LayoutControls();Invalidate();
  }
  private void Box(Control control,float x,float y,float width,float height){control.Bounds=Rectangle.Round(new RectangleF(x*scale,y*scale,width*scale,height*scale));}
  private void LayoutControls()
  {
   Box(close,309,17,31,29);Box(cards,20,84,320,CardsHeight);
   Box(version,176,18,43,28);Box(github,222,18,82,28);
   Box(resetNotice,20,84+CardsHeight+10,262,28);
   Box(resetSource,290,84+CardsHeight+10,50,28);
   Box(languageChoice,20,ChoiceY,76,28);Box(windowChoice,108,ChoiceY,232,28);
   Box(themeLabel,20,ThemeY,76,28);Box(themeChoice,108,ThemeY,148,28);Box(locationChoice,264,ThemeY,76,28);
   Box(status,20,FooterY,124,28);Box(updated,148,FooterY,112,28);Box(refresh,268,FooterY,72,28);
   for(int i=0;i<cards.Controls.Count;i++){UiQuotaCard card=(UiQuotaCard)cards.Controls[i];card.ScaleFactor=scale;card.Bounds=Rectangle.Round(new RectangleF(0,i*82*scale,320*scale,74*scale));}
  }
  public void SetState(QuotaSnapshot snapshot,string selectedId,bool stale,bool busy,string message,string settingsMessage)
  {
   UpdateResetNotice();
   lastSnapshot=snapshot;lastSelectedId=selectedId;lastStale=stale;lastBusy=busy;lastMessage=message;lastSettingsMessage=settingsMessage;
   plan=snapshot==null||String.IsNullOrWhiteSpace(snapshot.PlanLabel)?UiText.T("套餐待获取","Plan unavailable"):UiText.Plan(snapshot.PlanLabel);
   Text=UiText.T("CodexUsage · 详情","CodexUsage");AccessibleName=UiText.T("CodexUsage详情和显示窗口选择","CodexUsage details and widget selection");
   github.Text=UiText.T("GITHUB主页","GITHUB");github.AccessibleName=UiText.T("打开 GitHub 项目主页","Open the GitHub project page");
   version.AccessibleName=UiText.T("版本 ","Version ")+version.Text;
   cards.AccessibleName=UiText.T("全部额度窗口","All usage windows");close.AccessibleName=UiText.T("关闭详情","Close details");
   languageChoice.Text=UiText.T("English","中文");languageChoice.AccessibleName=UiText.T("切换为英文","Switch to Chinese");
   themeLabel.Text=UiText.T("主题","Theme");themeChoice.AccessibleName=UiText.T("显示主题","Display theme");
   locationChoice.Text=UiText.T("位置","Location");locationChoice.AccessibleName=UiText.T("设置日出日落计算位置","Set location for sunrise and sunset");
   if(themeChoice.Items.Count==3){themeChoice.Items[0]=UiText.T("浅色","Light");themeChoice.Items[1]=UiText.T("深色","Dark");themeChoice.Items[2]=UiText.T("日出日落自动","Sunrise/sunset");}
   UpdateLocationDescription();
   windowChoice.AccessibleName=UiText.T("浮条展示的额度窗口","Usage window shown in the widget");
   status.AccessibleName=UiText.T("刷新状态","Refresh status");updated.AccessibleName=UiText.T("上次更新时间","Last successful update");refresh.AccessibleName=UiText.T("立即刷新套餐和额度","Refresh plan and usage");
   binding=true;
   string oldIds=String.Join("|",windows.ConvertAll(delegate(QuotaWindow w){return w.Id;}).ToArray());
   List<QuotaWindow> next=snapshot==null||snapshot.Windows==null?new List<QuotaWindow>():snapshot.Windows;
   string newIds=String.Join("|",next.ConvertAll(delegate(QuotaWindow w){return w.Id;}).ToArray());
   windows.Clear();windows.AddRange(next);
   if(oldIds!=newIds||cards.Controls.Count!=windows.Count){while(cards.Controls.Count>0){Control child=cards.Controls[0];cards.Controls.Remove(child);child.Dispose();}windowChoice.Items.Clear();foreach(QuotaWindow window in windows){cards.Controls.Add(new UiQuotaCard());windowChoice.Items.Add(UiText.WindowLabel(window.Label));}}
   for(int i=0;i<windows.Count;i++){string label=UiText.WindowLabel(windows[i].Label);if(!String.Equals(Convert.ToString(windowChoice.Items[i]),label,StringComparison.Ordinal))windowChoice.Items[i]=label;}
   windowChoice.SelectedIndex=windows.FindIndex(delegate(QuotaWindow w){return w.Id==selectedId;});windowChoice.Enabled=windows.Count>0;
   for(int i=0;i<windows.Count;i++){((UiQuotaCard)cards.Controls[i]).SetState(windows[i],stale,windows[i].Id==selectedId);}
   binding=false;refresh.Enabled=!busy;refresh.Text=busy?UiText.T("刷新中","Loading"):UiText.T("立即刷新","Refresh");
   status.Text=UiText.T("额度每5分钟自动刷新","Every 5 min");
   updated.Text=snapshot==null?UiText.T("尚未更新","Not updated"):UiText.T("更新于 ","Updated ")+snapshot.FetchedAtUtc.ToLocalTime().ToString("HH:mm:ss");
   string text=busy?UiText.T("正在读取 Codex 账号套餐与额度…","Reading your Codex plan and usage…"):!String.IsNullOrEmpty(message)?UiText.Error(message):snapshot==null?UiText.T("等待获取额度。请先在 Codex 中登录。","Waiting for usage. Sign in to Codex first."):stale?UiText.T("上次结果已过期，请刷新后查看。","The previous result is out of date. Please refresh."):String.Empty;
   if(!String.IsNullOrEmpty(settingsMessage))text=UiText.Error(settingsMessage)+" "+text;
   bool problem=!String.IsNullOrEmpty(message)||!String.IsNullOrEmpty(settingsMessage)||stale;
   status.Text=busy?UiText.T("正在刷新…","Refreshing…"):problem?UiText.T("刷新异常","Refresh issue"):snapshot==null?UiText.T("等待获取额度","Waiting for usage"):UiText.T("额度每5分钟自动刷新","Every 5 min");
   status.ForeColor=problem?Theme.Warning:GlassMuted;status.AccessibleDescription=text;
   cards.AccessibleDescription=UiText.T("额度窗口数量：","Usage windows: ")+windows.Count;
   if(Visible&&anchor!=null&&!anchor.IsDisposed)RepositionAnchored();
   else{FitToWorkingArea(Screen.FromRectangle(Bounds).WorkingArea);Bounds=Theme.Clamp(Bounds,Screen.FromRectangle(Bounds).WorkingArea);}Invalidate();
  }
  internal void SetResetFeed(ResetFeed feed){resetFeed=feed;UpdateResetNotice();}
  private void UpdateResetNotice()
  {
   resetNotice.Text=resetFeed!=null&&resetFeed.Latest!=null?resetFeed.Latest.Caption(UiText.English,resetFeed.Cached):
    resetFeed!=null&&resetFeed.Failed?UiText.T("重置公告暂不可用","Reset news unavailable"):
    UiText.T("暂无重置公告","No reset news");
   resetSource.Text=UiText.T("来源","Source");
   resetSource.AccessibleName=UiText.T("查看重置公告来源","View reset announcement source");
   resetNotice.AccessibleDescription=UiText.T("来源 codex-resets.com，公共重置公告，不代表个人账号到账时间。","Source: codex-resets.com. Public announcement, not confirmation of an account reset.");
  }
  public void ShowAnchored(WidgetForm widget){anchor=widget;RepositionAnchored();if(!Visible)CaptureGlassBackground();Show();Activate();}
  private void RepositionAnchored(){Rectangle area=Screen.FromControl(anchor).WorkingArea;FitToWorkingArea(area);int gap=(int)(10*scale),x=anchor.Right-Width,y=anchor.Top-Height-gap;if(y<area.Top)y=anchor.Bottom+gap;Bounds=Theme.Clamp(new Rectangle(x,y,Width,Height),area);}
  internal bool ContainsPointer(Point point){return Visible&&(Bounds.Contains(point)||windowChoice.DropDownContains(point)||themeChoice.DropDownContains(point));}
  protected override void OnPaint(PaintEventArgs e)
  {
   base.OnPaint(e);Graphics g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;
   if(glassBackground==null)Theme.Rounded(g,new RectangleF(.5f,.5f,Width-1,Height-1),18*scale,Theme.Background,Theme.Border);
   else using(GraphicsPath path=Theme.Round(new RectangleF(.5f,.5f,Width-1,Height-1),18*scale))DrawGlassRim(g,path,Height,false,.55f);
   Theme.Write(g,UiText.AppName,21,18,153,28,22,Theme.Text,true,scale);Theme.Write(g,UiText.T("账号套餐","Account plan"),22,52,83,17,10,GlassMuted,false,scale);
   Theme.Rounded(g,new RectangleF(108*scale,51*scale,180*scale,21*scale),7*scale,glassBackground==null?Theme.Card:Theme.IsDark?Color.FromArgb(105,30,46,59):Color.FromArgb(82,255,255,255),glassBackground==null?(Color?)null:Theme.IsDark?Color.FromArgb(75,154,199,222):Color.FromArgb(95,255,255,255));Theme.Write(g,plan,117,51,163,21,11,Theme.Aqua,true,scale);
   if(cards.Controls.Count==0)Theme.Write(g,UiText.T("额度信息将在读取成功后显示","Usage appears after a successful refresh"),30,94,300,42,11,Theme.Muted,false,scale);
  }
  public void Shutdown(){quitting=true;Close();}
  protected override void OnFormClosed(FormClosedEventArgs e){if(glassBackground!=null){glassBackground.Dispose();glassBackground=null;}base.OnFormClosed(e);}
  protected override void OnVisibleChanged(EventArgs e){if(!Visible){windowChoice.CloseDropDown();themeChoice.CloseDropDown();}base.OnVisibleChanged(e);}
  protected override void OnFormClosing(FormClosingEventArgs e){if(!quitting&&e.CloseReason==CloseReason.UserClosing){e.Cancel=true;Hide();}base.OnFormClosing(e);}
  protected override void OnKeyDown(KeyEventArgs e){base.OnKeyDown(e);if(e.KeyCode==Keys.Escape){Hide();e.Handled=true;}}
 }
 internal sealed class UiQuotaCard : Control
 {
  private QuotaWindow window;private bool stale,selected;internal float ScaleFactor=1;
  internal UiQuotaCard(){DoubleBuffered=true;BackColor=Theme.Background;}
  protected override void OnPaintBackground(PaintEventArgs e)
  {
   DetailsForm form=FindForm() as DetailsForm;
   if(form==null||!form.PaintGlass(e.Graphics,this))base.OnPaintBackground(e);
  }
  protected override void OnSizeChanged(EventArgs e)
  {
   base.OnSizeChanged(e);
   if(Width<=0||Height<=0)return;
   using(GraphicsPath path=Theme.Round(new RectangleF(0,0,Width,Height),11*ScaleFactor)){Region old=Region;Region=new Region(path);if(old!=null)old.Dispose();}
  }
  internal void SetState(QuotaWindow value,bool expired,bool active){window=value;stale=expired;selected=active;DateTimeOffset now=DateTimeOffset.UtcNow;AccessibleName=UiText.WindowLabel(value.Label)+(value.IsResetPending(now)?UiText.T("，已到重置时间，待更新",", reset reached; awaiting update"):UiText.T("，剩余额度 ",", remaining ")+Theme.Percent(value.RemainingPercent)+", "+Theme.ResetText(value,now));if(stale)AccessibleName+=UiText.T("，上次结果已过期","; previous result is out of date");Invalidate();}
  private void DrawGlassSurface(Graphics g,float s)
  {
   using(GraphicsPath path=Theme.Round(new RectangleF(.5f,.5f,Width-1,Height-1),11*s))
   using(LinearGradientBrush fill=new LinearGradientBrush(
    new PointF(0,0),new PointF(0,Math.Max(1,Height)),
     Theme.IsDark?Color.FromArgb(130,31,45,63):Color.FromArgb(127,255,255,255),Theme.IsDark?Color.FromArgb(93,15,31,48):Color.FromArgb(70,230,243,255)))
   {
    g.FillPath(fill,path);
    DetailsForm.DrawGlassRim(g,path,Height,selected);
   }
  }
  protected override void OnPaint(PaintEventArgs e)
  {
   if(window==null)return;Graphics g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;float s=ScaleFactor,width=Width/s;DateTimeOffset now=DateTimeOffset.UtcNow;bool pending=window.IsResetPending(now);Color color=stale||pending?Theme.Muted:Theme.Aqua;
   DrawGlassSurface(g,s);
   Theme.Write(g,UiText.WindowLabel(window.Label),12,8,width-105,22,12,Theme.Text,true,s);Theme.Write(g,pending?UiText.T("待更新","Pending"):Theme.Percent(window.RemainingPercent),width-87,7,75,23,pending?13:19,color,true,s);
   RectangleF track=new RectangleF(12*s,37*s,(width-24)*s,5*s);Theme.Rounded(g,track,2.5f*s,Theme.Border,null);
   if(!pending&&window.RemainingPercent.HasValue&&window.RemainingPercent.Value>0){track.Width*=(float)(window.RemainingPercent.Value/100);Theme.Rounded(g,track,2.5f*s,color,null);}
   string countdown=stale?UiText.T("上次结果 · 已过期","Previous result · Out of date"):pending?UiText.T("已重置 · 待更新","Reset reached · Pending"):Theme.ResetText(window,now);
    Theme.Write(g,countdown,12,49,width-150,17,9.5f,stale||pending?Theme.Warning:Theme.IsDark?Theme.Blue:Color.FromArgb(16,66,145),false,s);
   string reset=window.ResetsAtUtc.HasValue?window.ResetsAtUtc.Value.ToLocalTime().ToString("MM-dd HH:mm"):UiText.T("重置时间未知","Reset unknown");
    using(Font font=new Font("Microsoft YaHei UI",9.5f*s,FontStyle.Regular,GraphicsUnit.Pixel))TextRenderer.DrawText(g,reset,font,Rectangle.Round(new RectangleF((width-136)*s,49*s,124*s,17*s)),Theme.IsDark?Theme.Muted:Color.FromArgb(45,58,75),TextFormatFlags.Right|TextFormatFlags.VerticalCenter|TextFormatFlags.SingleLine|TextFormatFlags.NoPadding);
  }
 }
 internal sealed class GlassLabel : Label
 {
  protected override void OnPaintBackground(PaintEventArgs e){DetailsForm form=FindForm() as DetailsForm;if(form==null||!form.PaintGlass(e.Graphics,this))base.OnPaintBackground(e);}
 }
 internal sealed class GlassLinkLabel : LinkLabel
 {
  protected override void OnPaintBackground(PaintEventArgs e){DetailsForm form=FindForm() as DetailsForm;if(form==null||!form.PaintGlass(e.Graphics,this))base.OnPaintBackground(e);}
 }
 internal sealed class GlassButton : Button
 {
  internal GlassButton(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);}
  protected override void OnPaint(PaintEventArgs e)
  {
   DetailsForm form=FindForm() as DetailsForm;
   if(form==null||!form.PaintGlass(e.Graphics,this))e.Graphics.Clear(Theme.Card);
   e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
   bool hover=ClientRectangle.Contains(PointToClient(Cursor.Position));
   RectangleF bounds=new RectangleF(.5f,.5f,Width-1,Height-1);
   float radius=Math.Max(4,Height*.22f);
    Theme.Rounded(e.Graphics,bounds,radius,Theme.IsDark?Color.FromArgb(Enabled?(hover?125:80):60,35,52,70):Color.FromArgb(Enabled?(hover?94:62):50,255,255,255),form!=null&&form.HasGlassBackground?(Color?)null:Theme.Border);
   if(form!=null&&form.HasGlassBackground)using(GraphicsPath path=Theme.Round(bounds,radius))DetailsForm.DrawGlassRim(e.Graphics,path,Height,Focused);
   TextRenderer.DrawText(e.Graphics,Text,Font,ClientRectangle,Enabled?Theme.Text:Theme.Muted,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.SingleLine|TextFormatFlags.EndEllipsis|TextFormatFlags.NoPadding);
  }
  protected override void OnMouseEnter(EventArgs e){base.OnMouseEnter(e);Invalidate();}
  protected override void OnMouseLeave(EventArgs e){base.OnMouseLeave(e);Invalidate();}
  protected override void OnGotFocus(EventArgs e){base.OnGotFocus(e);Invalidate();}
  protected override void OnLostFocus(EventArgs e){base.OnLostFocus(e);Invalidate();}
 }
}


