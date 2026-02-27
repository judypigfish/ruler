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

        // 🌟 核心：使用 TransformGroup 完全接管旋轉與位移，無視 WPF 佈局限制
        private RotateTransform currentRotation;
        private TranslateTransform currentTranslation;

        // 拖曳狀態變數
        private bool isMoving = false;
        private bool isRotating = false;
        private Point dragStartMouse;
        private Point dragStartTranslation;
        private double dragStartRulerWidth;
        private double dragStartRulerHeight;
        private double dragStartAngle;
        private Point logicalCenter;
        private double ratioPx = 1.0, ratioLogic = 1.0;
        private string ratioUnit = "px";

        // 🌟 防飄移神器：直接向系統要絕對座標
        [DllImport("user32.dll")] internal static extern bool GetCursorPos(out POINT pt);
        [StructLayout(LayoutKind.Sequential)] internal struct POINT { public int X; public int Y; }

        public MainWindow()
        {
            try
            {
                InitializeComponent();

                // 全螢幕透明畫布
                this.Topmost = false;
                this.WindowStyle = WindowStyle.None;
                this.AllowsTransparency = true;
                this.Background = Brushes.Transparent;

                this.Left = SystemParameters.VirtualScreenLeft;
                this.Top = SystemParameters.VirtualScreenTop;
                this.Width = SystemParameters.VirtualScreenWidth;
                this.Height = SystemParameters.VirtualScreenHeight;

                RulerBody.Background = new SolidColorBrush(Color.FromArgb((byte)(255 * 0.7), 255, 255, 255));

                // 解除所有佈局綁定
                RulerBody.HorizontalAlignment = HorizontalAlignment.Left;
                RulerBody.VerticalAlignment = VerticalAlignment.Top;
                RulerBody.Margin = new Thickness(0);
                RulerBody.Width = 600;
                RulerBody.Height = 150;

                // 🌟 初始化變換矩陣，覆蓋 XAML 裡的設定
                TransformGroup tg = new TransformGroup();
                currentRotation = windowRotation;
                currentTranslation = currentTranslation1;

                // 計算初始畫面正中央
                double startX = (SystemParameters.PrimaryScreenWidth - 600) / 2 - SystemParameters.VirtualScreenLeft;
                double startY = (SystemParameters.PrimaryScreenHeight - 150) / 2 - SystemParameters.VirtualScreenTop;
                currentTranslation.X = startX;
                currentTranslation.Y = startY;

                DrawRuler();
        }
            catch (Exception ex)
            {
                // 🌟 如果程式打不開，這行會告訴你為什麼
                MessageBox.Show($"程式啟動發生錯誤！\n\n訊息: {ex.Message}\n\n堆疊: {ex.StackTrace}", "啟動失敗");
                Application.Current.Shutdown();
            }
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (RulerBody == null || currentRotation == null || isRotating || isMoving) return;
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
        private void MenuAbout_Click(object sender, RoutedEventArgs e) => MessageBox.Show("螢幕半透明尺 v1.1", "關於");
        private void MenuExit_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();
        private void Logo_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) { e.Handled = true; try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = "https://store.line.me/emojishop/product/688098f87177072ad3367082/zh-Hant", UseShellExecute = true }); } catch { } }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            double s = (Keyboard.Modifiers == ModifierKeys.Control) ? 1.0 : 5.0;
            switch (e.Key)
            {
                case Key.Up: currentTranslation.Y -= s; break;
                case Key.Down: currentTranslation.Y += s; break;
                case Key.Left: currentTranslation.X -= s; break;
                case Key.Right: currentTranslation.X += s; break;
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
            RatioWindow rw = new RatioWindow { Owner = this };
            if (rw.ShowDialog() == true)
            {
                ratioPx = rw.PxLength; ratioLogic = rw.LogicLength; ratioUnit = rw.LogicUnit ?? "";
                MenuRatioToggle.IsChecked = true;
                MenuRatioToggle_Click(MenuRatioToggle, new RoutedEventArgs());
            }
        }

        // ==========================================
        // 尺身拖曳與 Ctrl 旋轉邏輯
        // ==========================================
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                // 先記錄所有狀態
                dragStartMouse = e.GetPosition(this);
                dragStartAngle = currentRotation.Angle;
                logicalCenter = new Point(currentTranslation.X + RulerBody.Width / 2, currentTranslation.Y + RulerBody.Height / 2);

                // 再開啟控制開關並捕捉滑鼠
                isRotating = true;
                this.CaptureMouse();
            }
            else if (e.LeftButton == MouseButtonState.Pressed)
            {
                // 先記錄起始座標與目前平移量
                dragStartMouse = e.GetPosition(this);
                dragStartTranslation = new Point(currentTranslation.X, currentTranslation.Y);

                // 再開啟移動開關並捕捉滑鼠
                isMoving = true;
                this.CaptureMouse();
            }
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            Point currentMouse = e.GetPosition(this);

            if (isMoving && e.LeftButton == MouseButtonState.Pressed)
            {
                double dx = currentMouse.X - dragStartMouse.X;
                double dy = currentMouse.Y - dragStartMouse.Y;
                // 🌟 核心：目前位置 = 起始平移量 + 滑鼠總位移
                currentTranslation.X = dragStartTranslation.X + dx;
                currentTranslation.Y = dragStartTranslation.Y + dy;
            }
            else if (isRotating && e.LeftButton == MouseButtonState.Pressed)
            {
                double startAngleRad = Math.Atan2(dragStartMouse.Y - logicalCenter.Y, dragStartMouse.X - logicalCenter.X);
                double currentAngleRad = Math.Atan2(currentMouse.Y - logicalCenter.Y, currentMouse.X - logicalCenter.X);
                double delta = (currentAngleRad - startAngleRad) * 180 / Math.PI;
                currentRotation.Angle = (dragStartAngle + delta + 360) % 360; // 🌟 使用變數
            }
            UpdateTexts();
            DrawRuler();
        }

        private void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (isMoving || isRotating)
            {
                isMoving = false;
                isRotating = false;
                this.ReleaseMouseCapture();
                DrawRuler(); // 停止後重畫，確保效能絲滑
            }
        }

        // ==========================================
        // Thumb 把手控制邏輯
        // ==========================================
        private void Thumb_DragStarted(object sender, DragStartedEventArgs e)
        {
            // 全部改用相對於全螢幕視窗的座標
            dragStartMouse = Mouse.GetPosition(this);
            dragStartTranslation = new Point(currentTranslation.X, currentTranslation.Y);
            dragStartRulerWidth = RulerBody.Width;
            dragStartRulerHeight = RulerBody.Height;
            dragStartAngle = currentRotation.Angle;
            logicalCenter = new Point(currentTranslation.X, currentTranslation.Y);
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

            currentRotation.Angle = newAngle;
            e.Handled = true;
        }

        private void ThumbResize_DragDelta(object sender, DragDeltaEventArgs e)
        {
            Thumb? thumb = sender as Thumb;
            if (thumb == null) return;

            Point currentMouse = Mouse.GetPosition(this);
            double deltaX = currentMouse.X - dragStartMouse.X;
            double deltaY = currentMouse.Y - dragStartMouse.Y;

            // 將滑鼠位移轉為尺的本地座標系方向
            double radLocal = -dragStartAngle * Math.PI / 180.0;
            double localDeltaX = deltaX * Math.Cos(radLocal) - deltaY * Math.Sin(radLocal);
            double localDeltaY = deltaX * Math.Sin(radLocal) + deltaY * Math.Cos(radLocal);

            double newW = Math.Max(100, dragStartRulerWidth + (thumb.Name.Contains("Right") ? localDeltaX : 0));
            double newH = Math.Max(50, dragStartRulerHeight + (thumb.Name.Contains("Bottom") ? localDeltaY : 0));

            double dW = newW - dragStartRulerWidth;
            double dH = newH - dragStartRulerHeight;

            RulerBody.Width = newW;
            RulerBody.Height = newH;

            // 🌟 補償公式修正
            double radAbs = currentRotation.Angle * Math.PI / 180.0;
            double offsetX = (dW / 2) * (Math.Cos(radAbs) - 1) - (dH / 2) * Math.Sin(radAbs);
            double offsetY = (dW / 2) * Math.Sin(radAbs) + (dH / 2) * (Math.Cos(radAbs) - 1);

            // 套用在正確的變數上
            currentTranslation.X = dragStartTranslation.X + offsetX;
            currentTranslation.Y = dragStartTranslation.Y + offsetY;

            UpdateTexts();
        }

        private void Thumb_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            DrawRuler();
            e.Handled = true;
        }

        private void MenuReset_Click(object sender, RoutedEventArgs e)
        {
            // 1. 恢復尺身大小
            RulerBody.Width = 600;
            RulerBody.Height = 150;

            // 2. 恢復變換矩陣 (旋轉歸零，移動到畫面中央附近)
            currentRotation.Angle = 0;
            currentTranslation.X = (SystemParameters.PrimaryScreenWidth - 600) / 2;
            currentTranslation.Y = (SystemParameters.PrimaryScreenHeight - 150) / 2;

            // 3. 恢復預設顏色與透明度 (30%)
            RulerBody.Background = new SolidColorBrush(Color.FromArgb((byte)(255 * 0.7), 255, 255, 255));

            // 4. 重設選單勾選狀態
            MenuPosTop.IsChecked = true;
            MenuPosBottom.IsChecked = MenuPosLeft.IsChecked = MenuPosRight.IsChecked = false;
            posTop = true; posBottom = posLeft = posRight = false;
    
            // 5. 更新文字顯示並重畫刻度
            UpdateTexts();
            DrawRuler(); 
}
    }
}