using System;
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

        // 拖曳狀態變數
        private bool isMoving = false;
        private bool isRotating = false;
        private Point dragStartMouse;
        private Thickness dragStartMargin;
        private double dragStartRulerWidth;
        private double dragStartRulerHeight;
        private double dragStartAngle;
        private Point logicalCenter;
        private double ratioPx = 1.0, ratioLogic = 1.0;
        private string ratioUnit = "px";

        public MainWindow()
        {
            InitializeComponent();

            // 🌟 天才解法：將視窗化為完全透明的「虛擬全螢幕畫布」
            this.Topmost = false;
            this.WindowStyle = WindowStyle.None;
            this.AllowsTransparency = true;
            this.Background = Brushes.Transparent; // WPF 特性：透明區域會自動讓滑鼠穿透到底下程式！

            this.Left = SystemParameters.VirtualScreenLeft;
            this.Top = SystemParameters.VirtualScreenTop;
            this.Width = SystemParameters.VirtualScreenWidth;
            this.Height = SystemParameters.VirtualScreenHeight;

            RulerBody.Background = new SolidColorBrush(Color.FromArgb((byte)(255 * 0.7), 255, 255, 255));

            // 解除尺的置中綁定，改用絕對 Margin 定位，並初始放在主螢幕正中央
            RulerBody.HorizontalAlignment = HorizontalAlignment.Left;
            RulerBody.VerticalAlignment = VerticalAlignment.Top;

            // 設定初始寬高以避免 NaN
            RulerBody.Width = 600;
            RulerBody.Height = 150;

            double startX = (SystemParameters.PrimaryScreenWidth - RulerBody.Width) / 2 - SystemParameters.VirtualScreenLeft;
            double startY = (SystemParameters.PrimaryScreenHeight - RulerBody.Height) / 2 - SystemParameters.VirtualScreenTop;
            RulerBody.Margin = new Thickness(startX, startY, 0, 0);

            DrawRuler();
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (RulerBody == null || windowRotation == null || isRotating || isMoving) return;
            UpdateTexts();
            DrawRuler();
        }

        private void DrawRuler()
        {
            if (RulerCanvas == null || RulerBody == null) return;
            RulerCanvas.Children.Clear();

            double width = RulerBody.Width;
            double height = RulerBody.Height;
            if (double.IsNaN(width) || double.IsNaN(height)) return;

            double unitScale = currentUnit == "mm" ? (96.0 / 25.4) : 1.0;
            if (posTop || posBottom)
            {
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

        // --- 選單事件保持不變 ---
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
        private void Logo_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) { e.Handled = true; try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = "https://store.line.me/emojishop/product/688098f87177072ad3367082/zh-Hant", UseShellExecute = true }); } catch { } }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            double s = (Keyboard.Modifiers == ModifierKeys.Control) ? 1.0 : 5.0;
            // 由於改用全螢幕，現在微調移動是改 RulerBody 的 Margin
            switch (e.Key)
            {
                case Key.Up: RulerBody.Margin = new Thickness(RulerBody.Margin.Left, RulerBody.Margin.Top - s, 0, 0); break;
                case Key.Down: RulerBody.Margin = new Thickness(RulerBody.Margin.Left, RulerBody.Margin.Top + s, 0, 0); break;
                case Key.Left: RulerBody.Margin = new Thickness(RulerBody.Margin.Left - s, RulerBody.Margin.Top, 0, 0); break;
                case Key.Right: RulerBody.Margin = new Thickness(RulerBody.Margin.Left + s, RulerBody.Margin.Top, 0, 0); break;
            }
        }

        private void UpdateTexts()
        {
            if (RulerBody == null) return;
            double sc = currentUnit == "mm" ? (96.0 / 25.4) : 1.0;
            double cW = RulerBody.Width / sc, cH = RulerBody.Height / sc;
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
            // 此處保留你的 RatioWindow 呼叫 (如果你有這個 Class 的話)
            // RatioWindow rw = new RatioWindow { Owner = this };
            // if (rw.ShowDialog() == true) { ratioPx = rw.PxLength; ratioLogic = rw.LogicLength; ratioUnit = rw.LogicUnit ?? ""; MenuRatioToggle.IsChecked = true; MenuRatioToggle_Click(MenuRatioToggle, new RoutedEventArgs()); }
        }

        // ==========================================
        // 尺身拖曳與 Ctrl 旋轉邏輯 (全螢幕絕對平移法)
        // ==========================================
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                isRotating = true;
                this.CaptureMouse();
                dragStartMouse = e.GetPosition(this);
                dragStartAngle = windowRotation.Angle;
                logicalCenter = new Point(RulerBody.Margin.Left + RulerBody.Width / 2, RulerBody.Margin.Top + RulerBody.Height / 2);
            }
            else if (e.LeftButton == MouseButtonState.Pressed)
            {
                // 取代舊的 DragMove()，我們現在是在全螢幕中移動尺的 Margin！
                isMoving = true;
                this.CaptureMouse();
                dragStartMouse = e.GetPosition(this);
                dragStartMargin = RulerBody.Margin;
            }
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if (isMoving && e.LeftButton == MouseButtonState.Pressed)
            {
                Point currentMouse = e.GetPosition(this);
                double dx = currentMouse.X - dragStartMouse.X;
                double dy = currentMouse.Y - dragStartMouse.Y;
                RulerBody.Margin = new Thickness(dragStartMargin.Left + dx, dragStartMargin.Top + dy, 0, 0);
            }
            else if (isRotating && e.LeftButton == MouseButtonState.Pressed)
            {
                Point currentMouse = e.GetPosition(this);
                double startAngleRad = Math.Atan2(dragStartMouse.Y - logicalCenter.Y, dragStartMouse.X - logicalCenter.X);
                double currentAngleRad = Math.Atan2(currentMouse.Y - logicalCenter.Y, currentMouse.X - logicalCenter.X);
                double delta = (currentAngleRad - startAngleRad) * 180 / Math.PI;
                double ang = (dragStartAngle + delta + 360) % 360;

                if (ang < 3 || ang > 357) ang = 0; else if (ang > 87 && ang < 93) ang = 90; else if (ang > 177 && ang < 183) ang = 180; else if (ang > 267 && ang < 273) ang = 270;
                if (windowRotation != null) windowRotation.Angle = ang;
            }
        }

        private void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (isMoving || isRotating)
            {
                isMoving = false;
                isRotating = false;
                this.ReleaseMouseCapture();
                UpdateTexts();
                DrawRuler();
            }
        }

        // ==========================================
        // Thumb 把手控制邏輯 (全螢幕無敵版)
        // ==========================================
        private void Thumb_DragStarted(object sender, DragStartedEventArgs e)
        {
            dragStartMouse = Mouse.GetPosition(this);
            dragStartMargin = RulerBody.Margin;
            dragStartRulerWidth = RulerBody.Width;
            dragStartRulerHeight = RulerBody.Height;
            dragStartAngle = windowRotation.Angle;
            logicalCenter = new Point(RulerBody.Margin.Left + RulerBody.Width / 2, RulerBody.Margin.Top + RulerBody.Height / 2);
            e.Handled = true;
        }

        private void ThumbRotate_DragDelta(object sender, DragDeltaEventArgs e)
        {
            Point currentMouse = Mouse.GetPosition(this);
            double startAngleRad = Math.Atan2(dragStartMouse.Y - logicalCenter.Y, dragStartMouse.X - logicalCenter.X);
            double currentAngleRad = Math.Atan2(currentMouse.Y - logicalCenter.Y, currentMouse.X - logicalCenter.X);
            double deltaAngle = (currentAngleRad - startAngleRad) * 180 / Math.PI;
            double newAngle = (dragStartAngle + deltaAngle + 360) % 360;

            if (newAngle < 3 || newAngle > 357) newAngle = 0; else if (newAngle > 87 && newAngle < 93) newAngle = 90; else if (newAngle > 177 && newAngle < 183) newAngle = 180; else if (newAngle > 267 && newAngle < 273) newAngle = 270;

            windowRotation.Angle = newAngle;
            e.Handled = true;
        }

        private void ThumbResize_DragDelta(object sender, DragDeltaEventArgs e)
        {
            Thumb thumb = sender as Thumb;
            if (thumb == null) return;

            Point currentMouse = Mouse.GetPosition(this);
            double deltaX = currentMouse.X - dragStartMouse.X;
            double deltaY = currentMouse.Y - dragStartMouse.Y;

            // 反向旋轉解算，讓尺能照著拉伸方向生長
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

            // 🌟 將視覺錨點釘死在螢幕上的無敵公式
            double radAbs = dragStartAngle * Math.PI / 180.0;
            double shiftX = (dW / 2) * Math.Cos(radAbs) - (dH / 2) * Math.Sin(radAbs);
            double shiftY = (dW / 2) * Math.Sin(radAbs) + (dH / 2) * Math.Cos(radAbs);

            double newMarginLeft = dragStartMargin.Left - (dW / 2) + shiftX;
            double newMarginTop = dragStartMargin.Top - (dH / 2) + shiftY;
            RulerBody.Margin = new Thickness(newMarginLeft, newMarginTop, 0, 0);

            UpdateTexts();
            // 🌟 解決卡頓關鍵：把 DrawRuler() 從這裡拿掉，拖曳期間不重新產生刻度！
            e.Handled = true;
        }

        private void Thumb_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            // 🌟 拖曳結束時，再把完美的刻度畫上去
            DrawRuler();
            e.Handled = true;
        }
    }
}