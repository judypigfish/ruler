using System;
using System.Windows;
using System.Windows.Controls;
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

        // 移除旋轉狀態變數，回歸單純移動邏輯
        private double ratioPx = 1.0;
        private double ratioLogic = 1.0;
        private string ratioUnit = "px";

        public MainWindow()
        {
            InitializeComponent();

            this.Topmost = false;
            this.Background = Brushes.Transparent;
            // 尺本體半透明背景
            RulerBody.Background = new SolidColorBrush(Color.FromArgb((byte)(255 * 0.7), 255, 255, 255));

            widthSize.FontFamily = font;
            heightSize.FontFamily = font;

            DrawRuler();
        }

        // 🌟 恢復最乾淨的 SizeChanged：只負責更新文字與重畫刻度
        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (RulerBody == null) return;

            UpdateTexts();
            DrawRuler();
        }

        private void DrawRuler()
        {
            if (RulerCanvas == null || RulerBody == null) return;
            RulerCanvas.Children.Clear();

            // 抓取 RulerBody 當前的實際渲染大小
            double width = RulerBody.ActualWidth;
            double height = RulerBody.ActualHeight;

            double unitScale = currentUnit == "mm" ? (96.0 / 25.4) : 1.0;
            double maxW = width / unitScale;
            double maxH = height / unitScale;

            int step = currentUnit == "px" ? 2 : 1;
            int major = currentUnit == "px" ? 100 : 10;
            int mid = currentUnit == "px" ? 50 : 5;
            int minor = currentUnit == "px" ? 10 : 1;
            int labelStep = currentUnit == "px" ? 50 : 10;

            if (posTop || posBottom)
            {
                for (int i = 0; i < maxW; i += step)
                {
                    double x = i * unitScale;
                    int tickLen = (currentUnit == "mm" && i % 100 == 0) ? 22 :
                                  (i % major == 0) ? 18 : (i % mid == 0) ? 15 : (i % minor == 0) ? 10 : 5;
                    bool drawLabel = (i % labelStep == 0 && i != 0);

                    if (posTop) DrawTickLine(x, 0, x, tickLen, i, drawLabel, "Top");
                    if (posBottom) DrawTickLine(x, height, x, height - tickLen, i, drawLabel, "Bottom");
                }
            }

            if (posLeft || posRight)
            {
                for (int i = 0; i < maxH; i += step)
                {
                    double y = i * unitScale;
                    int tickLen = (currentUnit == "mm" && i % 100 == 0) ? 22 :
                                  (i % major == 0) ? 18 : (i % mid == 0) ? 15 : (i % minor == 0) ? 10 : 5;
                    bool drawLabel = (i % labelStep == 0 && i != 0);

                    if (posLeft) DrawTickLine(0, y, tickLen, y, i, drawLabel, "Left");
                    if (posRight) DrawTickLine(width, y, width - tickLen, y, i, drawLabel, "Right");
                }
            }
        }

        private void DrawTickLine(double x1, double y1, double x2, double y2, int val, bool drawLabel, string edge)
        {
            Line tick = new Line
            {
                X1 = x1,
                X2 = x2,
                Y1 = y1,
                Y2 = y2,
                Stroke = rulerColor,
                StrokeThickness = 0.8,
                SnapsToDevicePixels = true
            };
            RulerCanvas.Children.Add(tick);

            if (drawLabel)
            {
                TextBlock label = new TextBlock
                {
                    Text = val.ToString(),
                    Foreground = rulerColor,
                    FontSize = 13,
                    FontWeight = (val % 100 == 0) ? FontWeights.Bold : FontWeights.Normal,
                    FontFamily = font
                };
                label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                double tw = label.DesiredSize.Width;
                double th = label.DesiredSize.Height;

                if (edge == "Top") { Canvas.SetLeft(label, x1 - (tw / 2)); Canvas.SetTop(label, y2 + 2); }
                else if (edge == "Bottom") { Canvas.SetLeft(label, x1 - (tw / 2)); Canvas.SetTop(label, y2 - th - 2); }
                else if (edge == "Left") { Canvas.SetLeft(label, x2 + 4); Canvas.SetTop(label, y1 - (th / 2)); }
                else if (edge == "Right") { Canvas.SetLeft(label, x2 - tw - 4); Canvas.SetTop(label, y1 - (th / 2)); }

                RulerCanvas.Children.Add(label);
            }
        }

        // 🌟 恢復最單純的滑鼠事件
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // 只保留視窗拖曳功能
            if (e.ButtonState == MouseButtonState.Pressed) DragMove();
        }

        private void Window_MouseMove(object sender, MouseEventArgs e) { /* 已移除旋轉邏輯 */ }

        private void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) { /* 已移除旋轉邏輯 */ }

        // --- 以下右鍵選單與邏輯比率功能保持不變 ---
        private void MenuTopmost_Click(object sender, RoutedEventArgs e) => this.Topmost = ((MenuItem)sender).IsChecked;

        private void MenuUnit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem clicked)
            {
                if (clicked.Parent is MenuItem parent)
                {
                    foreach (var item in parent.Items) if (item is MenuItem mi) mi.IsChecked = false;
                }
                clicked.IsChecked = true;
                string headerText = clicked.Header?.ToString() ?? "";
                currentUnit = headerText.Contains("mm") ? "mm" : "px";
                UpdateTexts();
                DrawRuler();
            }
        }

        private void MenuPosition_Click(object sender, RoutedEventArgs e)
        {
            posLeft = MenuPosLeft.IsChecked;
            posTop = MenuPosTop.IsChecked;
            posRight = MenuPosRight.IsChecked;
            posBottom = MenuPosBottom.IsChecked;
            DrawRuler();
        }

        private void MenuOpacity_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem clicked)
            {
                if (clicked.Parent is MenuItem parent)
                {
                    foreach (var item in parent.Items) if (item is MenuItem mi) mi.IsChecked = false;
                }
                clicked.IsChecked = true;
                string headerText = clicked.Header?.ToString() ?? "";
                if (double.TryParse(headerText.Replace("%", ""), out double pct))
                {
                    byte alpha = (byte)(255 * ((100 - pct) / 100.0));
                    RulerBody.Background = new SolidColorBrush(Color.FromArgb(alpha, 255, 255, 255));
                }
            }
        }

        private void MenuAbout_Click(object sender, RoutedEventArgs e) => MessageBox.Show("螢幕半透明尺 v1.0", "關於");
        private void MenuExit_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            double step = (Keyboard.Modifiers == ModifierKeys.Control) ? 1.0 : 5.0;
            switch (e.Key)
            {
                case Key.Up: this.Top -= step; break;
                case Key.Down: this.Top += step; break;
                case Key.Left: this.Left -= step; break;
                case Key.Right: this.Left += step; break;
            }
        }

        private void UpdateTexts()
        {
            if (RulerBody == null) return;
            double scale = currentUnit == "mm" ? (96.0 / 25.4) : 1.0;
            double currentW = RulerBody.ActualWidth / scale;
            double currentH = RulerBody.ActualHeight / scale;

            widthSize.Text = $"{currentW,4:F0}";
            heightSize.Text = $"{currentH,4:F0}";
            unitW.Text = $" {currentUnit}";
            unitH.Text = $" {currentUnit}";

            if (MenuRatioToggle?.IsChecked == true)
            {
                logicSizeW.Text = $"{(currentW / ratioPx) * ratioLogic:F2}";
                logicSizeH.Text = $"{(currentH / ratioPx) * ratioLogic:F2}";
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

        private void Logo_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = "https://store.line.me/emojishop/product/688098f87177072ad3367082/zh-Hant", UseShellExecute = true }); }
            catch { }
        }

    }

}