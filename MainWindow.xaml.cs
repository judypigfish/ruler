using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
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

            // 🌟 關鍵修復：視窗必須比尺大一點，否則外圍的 Thumb 會被裁切掉看不見！
            this.Width = 660;  // 600 + 60 緩衝
            this.Height = 210; // 150 + 60 緩衝

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

        // ==========================================
        // 輔助方法：計算旋轉後的完美包覆框尺寸
        // ==========================================


        private Size GetBoundingBox(double w, double h, double angle)
        {
            double rad = angle * Math.PI / 180.0;
            double cos = Math.Abs(Math.Cos(rad));
            double sin = Math.Abs(Math.Sin(rad));
            // 🌟 緩衝加大到 100，確保把手跟陰影不管在什麼角度都絕對安全
            return new Size((w * cos) + (h * sin) + 100, (w * sin) + (h * cos) + 100);
        }

        // ==========================================
        // 取得支援 DPI 縮放的絕對滑鼠座標 
        // ==========================================
        private Point GetMousePositionDIP()
        {
            GetCursorPos(out POINT pt);
            PresentationSource source = PresentationSource.FromVisual(this);
            if (source != null && source.CompositionTarget != null)
            {
                return source.CompositionTarget.TransformFromDevice.Transform(new Point(pt.X, pt.Y));
            }
            return new Point(pt.X, pt.Y);
        }

        // ==========================================
        // 拖曳狀態變數
        // ==========================================
        private Point dragStartMouseAbsolute;
        private double dragStartRulerWidth;
        private double dragStartRulerHeight;
        private double dragStartAngle;
        private Point dragStartWindowCenterAbsolute;
        private Point currentWindowCenterAbsolute; // 🌟 隨時紀錄最新的中心點

        // ==========================================
        // 1. 拖曳開始：使用你的 Ctrl 對角線絕招！
        // ==========================================
        private void Thumb_DragStarted(object sender, DragStartedEventArgs e)
        {
            // 防呆機制：解決剛開程式的 NaN 問題
            if (double.IsNaN(this.Left) || double.IsNaN(this.Top))
            {
                try
                {
                    Point p = this.PointToScreen(new Point(0, 0));
                    PresentationSource source = PresentationSource.FromVisual(this);
                    if (source != null) p = source.CompositionTarget.TransformFromDevice.Transform(p);
                    this.Left = p.X;
                    this.Top = p.Y;
                }
                catch { this.Left = 0; this.Top = 0; }
            }

            if (double.IsNaN(RulerBody.Width)) RulerBody.Width = RulerBody.ActualWidth > 0 ? RulerBody.ActualWidth : 600;
            if (double.IsNaN(RulerBody.Height)) RulerBody.Height = RulerBody.ActualHeight > 0 ? RulerBody.ActualHeight : 150;

            dragStartRulerWidth = RulerBody.Width;
            dragStartRulerHeight = RulerBody.Height;
            dragStartAngle = windowRotation.Angle;

            double currentW = double.IsNaN(this.Width) ? this.ActualWidth : this.Width;
            double currentH = double.IsNaN(this.Height) ? this.ActualHeight : this.Height;

            // 紀錄起點中心
            dragStartWindowCenterAbsolute = new Point(this.Left + currentW / 2, this.Top + currentH / 2);
            currentWindowCenterAbsolute = dragStartWindowCenterAbsolute;
            dragStartMouseAbsolute = GetMousePositionDIP();

            // 🌟 核心：算出對角線長度，把視窗瞬間撐成「永遠切不到」的巨大正方形
            double diag = Math.Sqrt(Math.Pow(dragStartRulerWidth, 2) + Math.Pow(dragStartRulerHeight, 2));
            double safeSize = diag + 100; // 加上 100 緩衝確保把手不消失

            this.Width = safeSize;
            this.Height = safeSize;
            this.Left = dragStartWindowCenterAbsolute.X - (safeSize / 2);
            this.Top = dragStartWindowCenterAbsolute.Y - (safeSize / 2);

            RulerBody.Margin = new Thickness(0);
            e.Handled = true;
        }

        // ==========================================
        // 2A. 旋轉中：視窗已經是正方形，只需旋轉內容，完全不需改視窗尺寸！
        // ==========================================
        private void ThumbRotate_DragDelta(object sender, DragDeltaEventArgs e)
        {
            Point currentMouse = GetMousePositionDIP();

            double startAngleRad = Math.Atan2(dragStartMouseAbsolute.Y - dragStartWindowCenterAbsolute.Y, dragStartMouseAbsolute.X - dragStartWindowCenterAbsolute.X);
            double currentAngleRad = Math.Atan2(currentMouse.Y - dragStartWindowCenterAbsolute.Y, currentMouse.X - dragStartWindowCenterAbsolute.X);

            double deltaAngle = (currentAngleRad - startAngleRad) * 180 / Math.PI;
            double newAngle = (dragStartAngle + deltaAngle + 360) % 360;

            if (newAngle < 3 || newAngle > 357) newAngle = 0;
            else if (newAngle > 87 && newAngle < 93) newAngle = 90;
            else if (newAngle > 177 && newAngle < 183) newAngle = 180;
            else if (newAngle > 267 && newAngle < 273) newAngle = 270;

            // 🌟 因為我們在 DragStarted 已經把視窗變成對角線長度的正方形了
            // 這裡「不要」修改 this.Width 和 this.Height，視窗不動，保證絕不閃爍、絕不切邊！
            windowRotation.Angle = newAngle;

            e.Handled = true;
        }

        // ==========================================
        // 2B. 縮放中
        // ==========================================
        private void ThumbResize_DragDelta(object sender, DragDeltaEventArgs e)
        {
            Thumb thumb = sender as Thumb;
            if (thumb == null) return;

            Point currentMouse = GetMousePositionDIP();
            double deltaX = currentMouse.X - dragStartMouseAbsolute.X;
            double deltaY = currentMouse.Y - dragStartMouseAbsolute.Y;

            double radLocal = -dragStartAngle * Math.PI / 180.0;
            double localDeltaX = deltaX * Math.Cos(radLocal) - deltaY * Math.Sin(radLocal);
            double localDeltaY = deltaX * Math.Sin(radLocal) + deltaY * Math.Cos(radLocal);

            double newW = dragStartRulerWidth;
            double newH = dragStartRulerHeight;

            if (thumb.Name.Contains("Right")) newW += localDeltaX;
            if (thumb.Name.Contains("Bottom")) newH += localDeltaY;

            if (newW < 100) newW = 100;
            if (newH < 50) newH = 50;

            double dW = newW - dragStartRulerWidth;
            double dH = newH - dragStartRulerHeight;

            RulerBody.Width = newW;
            RulerBody.Height = newH;

            double radAbs = dragStartAngle * Math.PI / 180.0;
            double shiftCenterX = (dW / 2) * Math.Cos(radAbs) - (dH / 2) * Math.Sin(radAbs);
            double shiftCenterY = (dW / 2) * Math.Sin(radAbs) + (dH / 2) * Math.Cos(radAbs);

            // 更新絕對中心點
            currentWindowCenterAbsolute = new Point(
                dragStartWindowCenterAbsolute.X + shiftCenterX,
                dragStartWindowCenterAbsolute.Y + shiftCenterY
            );

            // 🌟 縮放時同樣維持對角線正方形的邏輯，避免越拉越大超出邊界
            double diag = Math.Sqrt(Math.Pow(newW, 2) + Math.Pow(newH, 2));
            double safeSize = diag + 100;

            this.Width = safeSize;
            this.Height = safeSize;
            this.Left = currentWindowCenterAbsolute.X - (safeSize / 2);
            this.Top = currentWindowCenterAbsolute.Y - (safeSize / 2);

            UpdateTexts();
            DrawRuler();
            e.Handled = true;
        }

        // ==========================================
        // 3. 拖曳結束：把正方形縮回「剛好包覆」的大小，還給桌面點擊空間
        // ==========================================
        private void Thumb_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            // 🌟 瞬間收縮：計算出最緊密的長方形包覆框
            Size box = GetBoundingBox(RulerBody.Width, RulerBody.Height, windowRotation.Angle);

            this.Width = box.Width;
            this.Height = box.Height;
            this.Left = currentWindowCenterAbsolute.X - (box.Width / 2);
            this.Top = currentWindowCenterAbsolute.Y - (box.Height / 2);

            e.Handled = true;
        }
    }
}