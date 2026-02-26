using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Runtime.InteropServices;
namespace ruler
{
    public partial class MainWindow : Window
    {
        private Brush rulerColor = Brushes.Black;
        private FontFamily font = new FontFamily("Noto Sans");
        private string currentUnit = "px";
        private bool posTop = true, posBottom = false, posLeft = false, posRight = false;
        private bool isRotating = false;
        private Point logicalCenter;
        private double startMouseAngle = 0, startWindowAngle = 0, ratioPx = 1.0, ratioLogic = 1.0;
        private string ratioUnit = "px";
        [DllImport("user32.dll")] internal static extern bool GetCursorPos(out POINT pt);
        [StructLayout(LayoutKind.Sequential)] internal struct POINT { public int X; public int Y; }
        public MainWindow()
        {
            InitializeComponent();
            this.Topmost = false;
            this.Background = Brushes.Transparent;
            RulerBody.Background = new SolidColorBrush(Color.FromArgb((byte)(255 * 0.7), 255, 255, 255));
            DrawRuler();
        }
        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (RulerBody == null || windowRotation == null || isRotating) return;
            UpdateTexts(); DrawRuler();
        }
        private void DrawRuler()
        {
            if (RulerCanvas == null || RulerBody == null) return;
            RulerCanvas.Children.Clear();
            // 🌟 關鍵修正：直接抓取 RulerBody 的實際渲染寬度 (ActualWidth)
            double width = RulerBody.ActualWidth;
            double height = RulerBody.ActualHeight;
            double unitScale = currentUnit == "mm" ? (96.0 / 25.4) : 1.0;
            if (posTop || posBottom)
            {
                // 修正循環：確保 x 跑滿整個寬度，且間隔正確
                for (double x = 0; x <= width; x += (currentUnit == "px" ? 2 : 1 * unitScale))
                {
                    int val = (int)Math.Round(x / unitScale);
                    int tickLen = (currentUnit == "mm" && val % 100 == 0) ? 22 : (val % (currentUnit == "px" ? 100 : 10) == 0) ? 18 : (val % (currentUnit == "px" ? 50 : 5) == 0) ? 15 : 10;
                    bool drawLabel = (val % (currentUnit == "px" ? 50 : 10) == 0 && val != 0);
                    if (posTop) DrawTickLine(x, 0, x, tickLen, val, drawLabel, "Top");
                    if (posBottom) DrawTickLine(x, height, x, height - tickLen, val, drawLabel, "Bottom");
                }
            }
            if (posLeft || posRight)
            {
                for (double y = 0; y <= height; y += (currentUnit == "px" ? 2 : 1 * unitScale))
                {
                    int val = (int)Math.Round(y / unitScale);
                    int tickLen = (val % (currentUnit == "px" ? 100 : 10) == 0) ? 18 : 10;
                    bool drawLabel = (val % (currentUnit == "px" ? 50 : 10) == 0 && val != 0);
                    if (posLeft) DrawTickLine(0, y, tickLen, y, val, drawLabel, "Left");
                    if (posRight) DrawTickLine(width, y, width - tickLen, y, val, drawLabel, "Right");
                }
            }
        }
        private void DrawTickLine(double x1, double y1, double x2, double y2, int val, bool drawLabel, string edge)
        {
            Line tick = new Line { X1 = x1, X2 = x2, Y1 = y1, Y2 = y2, Stroke = rulerColor, StrokeThickness = 0.8, SnapsToDevicePixels = true };
            RulerCanvas.Children.Add(tick);
            if (drawLabel)
            {
                TextBlock label = new TextBlock { Text = val.ToString(), Foreground = rulerColor, FontSize = 13, FontWeight = (val % 100 == 0) ? FontWeights.Bold : FontWeights.Normal, FontFamily = font };
                label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                double tw = label.DesiredSize.Width, th = label.DesiredSize.Height;
                if (edge == "Top") { Canvas.SetLeft(label, x1 - (tw / 2)); Canvas.SetTop(label, y2 + 2); }
                else if (edge == "Bottom") { Canvas.SetLeft(label, x1 - (tw / 2)); Canvas.SetTop(label, y2 - th - 2); }
                else if (edge == "Left") { Canvas.SetLeft(label, x2 + 4); Canvas.SetTop(label, y1 - (th / 2)); }
                else if (edge == "Right") { Canvas.SetLeft(label, x2 - tw - 4); Canvas.SetTop(label, y1 - (th / 2)); }
                RulerCanvas.Children.Add(label);
            }
        }
        private void MenuTopmost_Click(object sender, RoutedEventArgs e) => this.Topmost = ((MenuItem)sender).IsChecked;
        private void MenuUnit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem clicked)
            {
                if (clicked.Parent is MenuItem parent) foreach (var item in parent.Items) if (item is MenuItem mi) mi.IsChecked = false;
                clicked.IsChecked = true;
                currentUnit = (clicked.Header?.ToString() ?? "").Contains("mm") ? "mm" : "px";
                UpdateTexts(); DrawRuler();
            }
        }
        private void MenuPosition_Click(object sender, RoutedEventArgs e)
        {
            posLeft = MenuPosLeft.IsChecked; posTop = MenuPosTop.IsChecked;
            posRight = MenuPosRight.IsChecked; posBottom = MenuPosBottom.IsChecked; DrawRuler();
        }
        private void MenuOpacity_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem clicked)
            {
                if (clicked.Parent is MenuItem parent) foreach (var item in parent.Items) if (item is MenuItem mi) mi.IsChecked = false;
                clicked.IsChecked = true;
                if (double.TryParse((clicked.Header?.ToString() ?? "").Replace("%", ""), out double pct))
                    RulerBody.Background = new SolidColorBrush(Color.FromArgb((byte)(255 * ((100 - pct) / 100.0)), 255, 255, 255));
            }
        }
        private void MenuAbout_Click(object sender, RoutedEventArgs e) => MessageBox.Show("螢幕半透明尺 v1.0", "關於");
        private void MenuExit_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            double s = (Keyboard.Modifiers == ModifierKeys.Control) ? 1.0 : 5.0;
            switch (e.Key) { case Key.Up: this.Top -= s; break; case Key.Down: this.Top += s; break; case Key.Left: this.Left -= s; break; case Key.Right: this.Left += s; break; }
        }
        private void UpdateTexts()
        {
            if (RulerBody == null) return;
            double sc = currentUnit == "mm" ? (96.0 / 25.4) : 1.0;
            double cW = RulerBody.ActualWidth / sc, cH = RulerBody.ActualHeight / sc;
            widthSize.Text = $"{cW,4:F0}"; heightSize.Text = $"{cH,4:F0}"; unitW.Text = unitH.Text = $" {currentUnit}";
            if (MenuRatioToggle?.IsChecked == true)
            {
                logicSizeW.Text = $"{(cW / ratioPx) * ratioLogic:F2}"; logicSizeH.Text = $"{(cH / ratioPx) * ratioLogic:F2}";
                unitLW.Text = unitLH.Text = $" {ratioUnit}";
            }
        }
        private void MenuRatioToggle_Click(object sender, RoutedEventArgs e)
        {
            Visibility v = (MenuRatioToggle.IsChecked == true) ? Visibility.Visible : Visibility.Collapsed;
            logicTitleW1.Visibility = logicTitleW2.Visibility = logicTitleW3.Visibility = logicTitleW4.Visibility = logicSizeW.Visibility = unitLW.Visibility = v;
            logicTitleH1.Visibility = logicTitleH2.Visibility = logicTitleH3.Visibility = logicTitleH4.Visibility = logicSizeH.Visibility = unitLH.Visibility = v;
            UpdateTexts();
        }
        private void SetRatio_Click(object sender, RoutedEventArgs e)
        {
            RatioWindow rw = new RatioWindow { Owner = this };
            if (rw.ShowDialog() == true) { ratioPx = rw.PxLength; ratioLogic = rw.LogicLength; ratioUnit = rw.LogicUnit ?? ""; MenuRatioToggle.IsChecked = true; MenuRatioToggle_Click(MenuRatioToggle, new RoutedEventArgs()); }
        }
        private void Logo_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) { e.Handled = true; try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = "https://store.line.me/emojishop/product/688098f87177072ad3367082/zh-Hant", UseShellExecute = true }); } catch { } }
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                isRotating = true; this.CaptureMouse();
                logicalCenter = new Point(this.Left + this.ActualWidth / 2, this.Top + this.ActualHeight / 2);
                RulerBody.Width = RulerBody.ActualWidth; RulerBody.Height = RulerBody.ActualHeight;
                RulerBody.HorizontalAlignment = HorizontalAlignment.Center; RulerBody.VerticalAlignment = VerticalAlignment.Center;
                double diag = Math.Sqrt(Math.Pow(RulerBody.Width, 2) + Math.Pow(RulerBody.Height, 2));
                this.Width = this.Height = diag; this.Left = logicalCenter.X - (diag / 2); this.Top = logicalCenter.Y - (diag / 2);
                GetCursorPos(out POINT pt); startMouseAngle = Math.Atan2(pt.Y - logicalCenter.Y, pt.X - logicalCenter.X) * 180 / Math.PI; startWindowAngle = windowRotation.Angle;
            }
            else if (e.LeftButton == MouseButtonState.Pressed) DragMove();
        }
        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if (isRotating && e.LeftButton == MouseButtonState.Pressed)
            {
                GetCursorPos(out POINT pt);
                double delta = (Math.Atan2(pt.Y - logicalCenter.Y, pt.X - logicalCenter.X) * 180 / Math.PI) - startMouseAngle;
                double ang = (startWindowAngle + delta + 360) % 360;
                if (ang < 3 || ang > 357) ang = 0; else if (ang > 87 && ang < 93) ang = 90;
                if (windowRotation != null) windowRotation.Angle = ang;
            }
        }
        private void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (isRotating) { isRotating = false; this.ReleaseMouseCapture(); this.Dispatcher.BeginInvoke(new Action(() => { UpdateTexts(); DrawRuler(); }), System.Windows.Threading.DispatcherPriority.Render); }
        }
    }
}