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
            return new Size((w * cos) + (h * sin) + 60, (w * sin) + (h * cos) + 60);
        }

        // ==========================================
        // 拖曳狀態變數
        // ==========================================
        private Point dragStartMouseRelative;
        private double dragStartRulerWidth;
        private double dragStartRulerHeight;
        private double dragStartAngle;
        private Point dragStartAbsoluteCenter;

        // ==========================================
        // 1. 拖曳開始：「先展開預留空間」
        // ==========================================
        private void Thumb_DragStarted(object sender, DragStartedEventArgs e)
        {
            // 🌟 【徹底破案】：如果視窗剛啟動，Left 和 Top 會是 NaN，必須強制轉換為絕對座標！
            if (double.IsNaN(this.Left) || double.IsNaN(this.Top))
            {
                try
                {
                    // 將視窗的原點 (0,0) 轉換為螢幕上的真實座標
                    Point p = this.PointToScreen(new Point(0, 0));
                    PresentationSource source = PresentationSource.FromVisual(this);
                    if (source != null)
                    {
                        p = source.CompositionTarget.TransformFromDevice.Transform(p);
                    }
                    this.Left = p.X;
                    this.Top = p.Y;
                }
                catch
                {
                    // 防呆機制：如果轉換失敗給個預設值
                    this.Left = 0;
                    this.Top = 0;
                }
            }

            // 確保尺有明確的寬高
            if (double.IsNaN(RulerBody.Width)) RulerBody.Width = RulerBody.ActualWidth;
            if (double.IsNaN(RulerBody.Height)) RulerBody.Height = RulerBody.ActualHeight;

            dragStartRulerWidth = RulerBody.Width;
            dragStartRulerHeight = RulerBody.Height;
            dragStartAngle = windowRotation.Angle;

            // 取得視窗真正的寬高
            double currentW = double.IsNaN(this.Width) ? this.ActualWidth : this.Width;
            double currentH = double.IsNaN(this.Height) ? this.ActualHeight : this.Height;

            // 紀錄絕對中心點 (因為消滅了 NaN，現在絕對安全！)
            dragStartAbsoluteCenter = new Point(this.Left + currentW / 2, this.Top + currentH / 2);

            // 把視窗瞬間撐大到 6000x6000，並把中心點對齊原本的尺
            this.Width = 6000;
            this.Height = 6000;
            this.Left = dragStartAbsoluteCenter.X - 3000;
            this.Top = dragStartAbsoluteCenter.Y - 3000;

            RulerBody.Margin = new Thickness(0); // 重置內部偏移

            // 強制 WPF 立即更新畫面版面
            this.UpdateLayout();

            // 紀錄滑鼠的座標
            dragStartMouseRelative = Mouse.GetPosition(this);
            e.Handled = true;
        }

        // ==========================================
        // 2A. 旋轉中：視窗不動，滑鼠零干擾
        // ==========================================
        private void ThumbRotate_DragDelta(object sender, DragDeltaEventArgs e)
        {
            Point currentMouse = Mouse.GetPosition(this);

            // 視窗是 6000x6000，所以中心點永遠是 3000, 3000
            double startAngleRad = Math.Atan2(dragStartMouseRelative.Y - 3000, dragStartMouseRelative.X - 3000);
            double currentAngleRad = Math.Atan2(currentMouse.Y - 3000, currentMouse.X - 3000);

            double deltaAngle = (currentAngleRad - startAngleRad) * 180 / Math.PI;
            double newAngle = (dragStartAngle + deltaAngle + 360) % 360;

            // 磁吸效果
            if (newAngle < 3 || newAngle > 357) newAngle = 0;
            else if (newAngle > 87 && newAngle < 93) newAngle = 90;
            else if (newAngle > 177 && newAngle < 183) newAngle = 180;
            else if (newAngle > 267 && newAngle < 273) newAngle = 270;

            windowRotation.Angle = newAngle;
            e.Handled = true;
        }

        // ==========================================
        // 2B. 縮放中：用 Margin 推移內容，視覺釘死原點
        // ==========================================
        private void ThumbResize_DragDelta(object sender, DragDeltaEventArgs e)
        {
            Thumb thumb = sender as Thumb;
            if (thumb == null) return;

            Point currentMouse = Mouse.GetPosition(this);
            double deltaX = currentMouse.X - dragStartMouseRelative.X;
            double deltaY = currentMouse.Y - dragStartMouseRelative.Y;

            // 將滑鼠位移量反向旋轉，換算成尺變長/變寬的正確數值
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

            // 🌟 核心數學：因為 Grid 是置中的，變大時會向四周均勻擴張。
            // 為了讓尺的「左上角/左下角」釘死在原地，我們用 Margin 把內容推回去。
            double radAbs = dragStartAngle * Math.PI / 180.0;
            double screenShiftX = (dW / 2) * Math.Cos(radAbs) - (dH / 2) * Math.Sin(radAbs);
            double screenShiftY = (dW / 2) * Math.Sin(radAbs) + (dH / 2) * Math.Cos(radAbs);

            // 套用兩倍的 Margin 偏移量 (因為置中對齊會抵銷一半)
            RulerBody.Margin = new Thickness(screenShiftX * 2, screenShiftY * 2, 0, 0);

            UpdateTexts();
            DrawRuler();
            e.Handled = true;
        }

        // ==========================================
        // 3. 拖曳結束：「再縮回來貼齊」(你的想法)
        // ==========================================
        private void Thumb_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            double dW = RulerBody.Width - dragStartRulerWidth;
            double dH = RulerBody.Height - dragStartRulerHeight;

            // 計算這段期間內，尺的「真實中心點」位移了多少
            double radAbs = windowRotation.Angle * Math.PI / 180.0;
            double screenShiftX = (dW / 2) * Math.Cos(radAbs) - (dH / 2) * Math.Sin(radAbs);
            double screenShiftY = (dW / 2) * Math.Sin(radAbs) + (dH / 2) * Math.Cos(radAbs);

            // 新的絕對中心點
            Point newAbsoluteCenter = new Point(
                dragStartAbsoluteCenter.X + screenShiftX,
                dragStartAbsoluteCenter.Y + screenShiftY
            );

            // 計算剛剛好包覆尺的視窗大小
            Size box = GetBoundingBox(RulerBody.Width, RulerBody.Height, windowRotation.Angle);

            // 🌟 瞬間收縮：移除 Margin 偏移，把視窗縮到最小，並對齊新的中心點
            RulerBody.Margin = new Thickness(0);
            this.Width = box.Width;
            this.Height = box.Height;
            this.Left = newAbsoluteCenter.X - box.Width / 2;
            this.Top = newAbsoluteCenter.Y - box.Height / 2;

            e.Handled = true;
        }
    }
}