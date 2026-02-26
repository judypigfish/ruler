using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ruler
{
    /// <summary>
    /// RatioWindow.xaml 的互動邏輯
    /// </summary>
    public partial class RatioWindow : Window
    {
        public double PxLength { get; private set; }
        public double LogicLength { get; private set; }
        public string? LogicUnit { get; private set; }

        public RatioWindow()
        {
            InitializeComponent();
        }

        private void BtnOK_Click(object sender, RoutedEventArgs e)
        {
            // 驗證：確保輸入的是數字，且 px >= 1
            if (double.TryParse(txtPx.Text, out double px) && px >= 1 &&
                double.TryParse(txtLogic.Text, out double logic))
            {
                // 把 TextBox 裡的文字，存進公開變數裡
                PxLength = px;
                LogicLength = logic;
                LogicUnit = txtUnit.Text;

                this.DialogResult = true; // 關閉視窗，並告訴主程式「使用者按了確定」
            }
            else
            {
                MessageBox.Show("請輸入正確的數值！像素長度必須大於等於 1。", "輸入錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 當按下「取消」按鈕時
        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false; // 直接關閉視窗
        }

        // 🌟 記錄旋轉角度與狀態
        private double currentAngle = 0;
        private bool isRotating = false;
        private double startMouseAngle = 0;
        private double startWindowAngle = 0;

    }
}
