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



        // 🌟 新增：狀態變數

        private string currentUnit = "px";

        private bool posTop = true, posBottom = false, posLeft = false, posRight = false;



        private bool isRotating = false;

        private Point logicalCenter; // 記錄旋轉中心點

        private double startMouseAngle = 0;

        private double startWindowAngle = 0;



        private double baseWidth = 600;

        private double baseHeight = 150;



        // 加入這段：呼叫 Windows 底層 API 來獲取「絕對螢幕物理座標」

        [System.Runtime.InteropServices.DllImport("user32.dll")]

        internal static extern bool GetCursorPos(out POINT pt);



        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]

        internal struct POINT { public int X; public int Y; }



        public MainWindow()

        {

            InitializeComponent();



            this.Topmost = false;

            // 🌟 修正 1：視窗外殼保持全透明，把半透明背景給「尺本體 (RulerBody)」

            this.Background = Brushes.Transparent;

            RulerBody.Background = new SolidColorBrush(Color.FromArgb((byte)(255 * 0.7), 255, 255, 255));



            widthSize.FontFamily = font;

            heightSize.FontFamily = font;



            DrawRuler();

        }





        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)

        {

            if (RulerBody == null || windowRotation == null || isRotating) return;



            // 🌟 如果尺是歪的，我們手動計算縮放

            if (windowRotation.Angle % 90 != 0)

            {

                // 1. 取得視窗增加的比例

                double scaleX = e.NewSize.Width / e.PreviousSize.Width;

                double scaleY = e.NewSize.Height / e.PreviousSize.Height;



                // 2. 碩士生等級的幾何投影：

                // 我們取寬高變化率的加權平均，這能讓拉動邊框時，尺的長度產生直覺的連動

                double angleRad = windowRotation.Angle * Math.PI / 180.0;

                double cos = Math.Abs(Math.Cos(angleRad));

                double sin = Math.Abs(Math.Sin(angleRad));



                // 這裡我們計算一個綜合縮放率，讓拉動水平邊框時長度變動更明顯

                double effectiveScale = (scaleX * cos) + (scaleY * sin);

                System.Diagnostics.Debug.WriteLine($"[拉伸檢查] 視窗寬增率: {scaleX:F2} | 尺新寬度: {RulerBody.Width:F0}");

                if (!double.IsNaN(effectiveScale) && !double.IsInfinity(effectiveScale))

                {

                    // 🌟 直接更新 RulerBody 的邏輯尺寸

                    RulerBody.Width *= effectiveScale;

                    RulerBody.Height *= effectiveScale;

                }

            }



            UpdateTexts();

            DrawRuler();

        }



        // ==========================================

        // 核心繪圖邏輯

        // ==========================================

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




        // ==========================================

        // 右鍵選單事件

        // ==========================================



        private void MenuTopmost_Click(object sender, RoutedEventArgs e)

        {

            this.Topmost = ((MenuItem)sender).IsChecked;

        }



        // 切換單位 (做到單選按鈕的效果)

        private void MenuUnit_Click(object sender, RoutedEventArgs e)

        {

            // 使用 is 進行安全轉型，如果不為 null 才會進入 if 裡面

            if (sender is MenuItem clicked)

            {

                // 安全地取得上一層選單

                if (clicked.Parent is MenuItem parent)

                {

                    foreach (var item in parent.Items)

                    {

                        if (item is MenuItem mi) mi.IsChecked = false;

                    }

                }



                clicked.IsChecked = true;



                // 安全地讀取 Header (加上 ? 處理可能為 null 的情況)

                string headerText = clicked.Header?.ToString() ?? "";

                currentUnit = headerText.Contains("mm") ? "mm" : "px";



                // 直接呼叫我們寫好的更新方法，取代 Window_SizeChanged(null, null)

                UpdateTexts();

                DrawRuler();

            }

        }



        // 切換位置 (允許多選)

        private void MenuPosition_Click(object sender, RoutedEventArgs e)

        {

            posLeft = MenuPosLeft.IsChecked;

            posTop = MenuPosTop.IsChecked;

            posRight = MenuPosRight.IsChecked;

            posBottom = MenuPosBottom.IsChecked;

            DrawRuler();

        }



        // 切換透明度

        private void MenuOpacity_Click(object sender, RoutedEventArgs e)

        {

            if (sender is MenuItem clicked)

            {

                if (clicked.Parent is MenuItem parent)

                {

                    foreach (var item in parent.Items)

                    {

                        if (item is MenuItem mi) mi.IsChecked = false;

                    }

                }



                clicked.IsChecked = true;



                string headerText = clicked.Header?.ToString() ?? "";

                string val = headerText.Replace("%", "");



                if (double.TryParse(val, out double pct))

                {

                    byte alpha = (byte)(255 * ((100 - pct) / 100.0));

                    RulerBody.Background = new SolidColorBrush(Color.FromArgb(alpha, 255, 255, 255));

                }

            }

        }



        private void MenuAbout_Click(object sender, RoutedEventArgs e)

        {

            MessageBox.Show("螢幕半透明尺 v1.0\n支援多向測量與單位切換", "關於", MessageBoxButton.OK, MessageBoxImage.Information);

        }



        private void MenuExit_Click(object sender, RoutedEventArgs e)

        {

            Application.Current.Shutdown();

        }



        // ======== 鍵盤微調視窗位置 ========

        private void Window_KeyDown(object sender, KeyEventArgs e)

        {

            // 判斷是否按住了 Ctrl 鍵

            // 如果有按住 Ctrl，移動距離為 1px；否則為預設的 5px

            double step = (Keyboard.Modifiers == ModifierKeys.Control) ? 1.0 : 5.0;



            switch (e.Key)

            {

                case Key.Up:

                    this.Top -= step; // 往上移動

                    e.Handled = true; // 告訴系統這個按鍵我們處理過了

                    break;



                case Key.Down:

                    this.Top += step; // 往下移動

                    e.Handled = true;

                    break;



                case Key.Left:

                    this.Left -= step; // 往左移動

                    e.Handled = true;

                    break;



                case Key.Right:

                    this.Left += step; // 往右移動

                    e.Handled = true;

                    break;

            }

        }



        private double ratioPx = 1.0;

        private double ratioLogic = 1.0;

        private string ratioUnit = "px";



        // ... 保留 MainWindow() 建構子 ...



        // 🌟 專門用來更新文字和計算邏輯比率的 Function

        private void UpdateTexts()

        {

            if (RulerBody == null) return;

            double scale = currentUnit == "mm" ? (96.0 / 25.4) : 1.0;



            // 🌟 同樣使用 RulerBody 的大小來顯示文字

            double currentW = RulerBody.ActualWidth / scale;

            double currentH = RulerBody.ActualHeight / scale;



            widthSize.Text = $"{currentW,4:F0}";

            heightSize.Text = $"{currentH,4:F0}";

            unitW.Text = $" {currentUnit}";

            unitH.Text = $" {currentUnit}";



            if (MenuRatioToggle != null && MenuRatioToggle.IsChecked == true)

            {

                double convertedW = (currentW / ratioPx) * ratioLogic;

                double convertedH = (currentH / ratioPx) * ratioLogic;

                logicSizeW.Text = $"{convertedW:F2}";

                logicSizeH.Text = $"{convertedH:F2}";

                unitLW.Text = $" {ratioUnit}";

                unitLH.Text = $" {ratioUnit}";

            }

        }



        // 🌟 開關「度量比率」功能 (切換 LW 和 LH 的顯示/隱藏)

        private void MenuRatioToggle_Click(object sender, RoutedEventArgs e)

        {

            Visibility v = (MenuRatioToggle.IsChecked == true) ? Visibility.Visible : Visibility.Collapsed;



            // 控制 LW 的顯示

            logicTitleW1.Visibility = v; logicTitleW2.Visibility = v; logicTitleW3.Visibility = v;

            logicTitleW4.Visibility = v; logicSizeW.Visibility = v; unitLW.Visibility = v;



            // 控制 LH 的顯示

            logicTitleH1.Visibility = v; logicTitleH2.Visibility = v; logicTitleH3.Visibility = v;

            logicTitleH4.Visibility = v; logicSizeH.Visibility = v; unitLH.Visibility = v;



            UpdateTexts();

        }



        // 🌟 彈出「設定度量比率」視窗

        private void SetRatio_Click(object sender, RoutedEventArgs e)

        {

            RatioWindow rw = new RatioWindow();

            rw.Owner = this; // 讓彈出視窗固定在主程式中央



            if (rw.ShowDialog() == true) // 如果使用者按了「確定」

            {

                // 更新設定值

                ratioPx = rw.PxLength;

                ratioLogic = rw.LogicLength;

                ratioUnit = rw.LogicUnit is null ? "" : rw.LogicUnit;



                // 自動把外層功能開啟，並顯示出來

                MenuRatioToggle.IsChecked = true;

                MenuRatioToggle_Click(MenuRatioToggle, new RoutedEventArgs());

            }

        }



        // ======== 點擊 Logo 開啟網頁 ========

        private void Logo_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)

        {

            // 🌟 1. 阻止事件往上傳遞！

            // 這樣系統才知道你是在點擊圖片，而不是要拖曳整個視窗

            e.Handled = true;



            try

            {

                // 🌟 2. 呼叫系統預設瀏覽器開啟網址

                // (這是 .NET 現代版本最標準、最安全的開網頁寫法)

                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo

                {

                    FileName = "https://store.line.me/emojishop/product/688098f87177072ad3367082/zh-Hant", // 👉 請把這裡換成你想要的網址！

                    UseShellExecute = true

                });

            }

            catch (Exception ex)

            {

                MessageBox.Show("無法開啟網頁: " + ex.Message, "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);

            }

        }



        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)

        {

            if (Keyboard.Modifiers == ModifierKeys.Control)

            {

                isRotating = true;

                this.CaptureMouse();



                // 救援 NaN 實體座標

                if (double.IsNaN(this.Left) || double.IsNaN(this.Top))

                {

                    var source = PresentationSource.FromVisual(this);

                    if (source != null)

                    {

                        var pos = source.CompositionTarget.TransformFromDevice.Transform(this.PointToScreen(new Point(0, 0)));

                        this.Left = pos.X;

                        this.Top = pos.Y;

                    }

                }



                // 先取得目前的絕對中心點

                logicalCenter = new Point(this.Left + this.ActualWidth / 2, this.Top + this.ActualHeight / 2);



                // 鎖死尺的實體長寬

                if (double.IsNaN(RulerBody.Width))

                {

                    RulerBody.Width = RulerBody.ActualWidth;

                    RulerBody.Height = RulerBody.ActualHeight;

                    RulerBody.HorizontalAlignment = HorizontalAlignment.Center;

                    RulerBody.VerticalAlignment = VerticalAlignment.Center;

                }



                // 🌟 天才最佳化：算出對角線長度 (360度旋轉需要的最大空間)

                double rw = RulerBody.Width;

                double rh = RulerBody.Height;

                double diagonal = Math.Sqrt((rw * rw) + (rh * rh));



                // 🌟 直接把透明視窗變成一個巨大的正方形，完美包容所有旋轉角度！

                this.Width = diagonal;

                this.Height = diagonal;

                this.Left = logicalCenter.X - (diagonal / 2);

                this.Top = logicalCenter.Y - (diagonal / 2);



                GetCursorPos(out POINT pt);

                startMouseAngle = Math.Atan2(pt.Y - logicalCenter.Y, pt.X - logicalCenter.X) * 180 / Math.PI;

                startWindowAngle = windowRotation.Angle;



                e.Handled = true;

            }

            else

            {

                if (e.ButtonState == MouseButtonState.Pressed) DragMove();

            }

        }



        // ======== 2. 滑鼠移動 (極致極簡，零負擔旋轉) ========

        private void Window_MouseMove(object sender, MouseEventArgs e)

        {

            if (isRotating && e.LeftButton == MouseButtonState.Pressed)

            {

                GetCursorPos(out POINT pt);

                double currentMouseAngle = Math.Atan2(pt.Y - logicalCenter.Y, pt.X - logicalCenter.X) * 180 / Math.PI;

                double deltaAngle = currentMouseAngle - startMouseAngle;



                if (deltaAngle > 180) deltaAngle -= 360;

                if (deltaAngle < -180) deltaAngle += 360;



                double newAngle = startWindowAngle + deltaAngle;

                if (newAngle >= 360) newAngle %= 360;

                if (newAngle < 0) newAngle += 360;



                // 磁吸貼齊

                if (newAngle < 3 || newAngle > 357) newAngle = 0;

                else if (newAngle > 87 && newAngle < 93) newAngle = 90;

                else if (newAngle > 177 && newAngle < 183) newAngle = 180;

                else if (newAngle > 267 && newAngle < 273) newAngle = 270;



                // 🌟 核心引擎：因為外殼已經夠大了，這裡只負責轉動角度，不再計算邊界！效能直接起飛 🚀

                if (windowRotation != null) windowRotation.Angle = newAngle;

            }

        }



        // ======== 3. 滑鼠放開 (縮回緊湊保鮮盒，避免擋住其他軟體) ========

        // ======== 3. 滑鼠放開 (計算精準邊界，防止垂直裁切) ========

        // ======== 3. 滑鼠放開 (加入強制刷新邏輯) ========

        private void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)

        {

            if (isRotating)

            {

                isRotating = false;

                this.ReleaseMouseCapture();



                // 恢復 Stretch 讓視窗邊框的拉力可以傳遞進去

                RulerBody.HorizontalAlignment = HorizontalAlignment.Center;

                RulerBody.VerticalAlignment = VerticalAlignment.Center;



                // 🌟 保持 RulerBody.Width/Height 為當下的數值，不要設為 NaN

                // 這樣在 SizeChanged 裡面的補償邏輯才有基數可以算



                this.Dispatcher.BeginInvoke(new Action(() => {

                    UpdateTexts();

                    DrawRuler();

                }), System.Windows.Threading.DispatcherPriority.Render);

            }

        }

    }

}

