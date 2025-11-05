using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Navigation;
using TouchpadSideScroll.Models;
using TouchpadSideScroll.ViewModels;

namespace TouchpadSideScroll
{
    /// <summary>
    /// 主視窗
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainViewModel? _viewModel;
        private HwndSource? _hwndSource;
        private Hardcodet.Wpf.TaskbarNotification.TaskbarIcon? _notifyIcon;

        public MainWindow()
        {
            InitializeComponent();

            // 從資源中取得 NotifyIcon
            _notifyIcon = (Hardcodet.Wpf.TaskbarNotification.TaskbarIcon?)FindResource("NotifyIcon");
        }

        /// <summary>
        /// 視窗載入完成
        /// </summary>
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            try
            {
                // 取得 ViewModel
                _viewModel = App.GetService<MainViewModel>();
                DataContext = _viewModel;

                // 加入視窗訊息鉤子以接收 WM_INPUT
                var windowHandle = new WindowInteropHelper(this).Handle;
                _hwndSource = HwndSource.FromHwnd(windowHandle);
                _hwndSource?.AddHook(WndProc);

                // 初始化 ViewModel
                _viewModel.Initialize(windowHandle);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"視窗初始化失敗：{ex.Message}",
                    "錯誤",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 視窗訊息處理
        /// </summary>
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_INPUT = 0x00FF;

            if (msg == WM_INPUT)
            {
                _viewModel?.ProcessRawInput(lParam);
            }

            return IntPtr.Zero;
        }

        /// <summary>
        /// 視窗狀態變更
        /// </summary>
        private void Window_StateChanged(object? sender, EventArgs e)
        {
            if (_viewModel?.Settings.MinimizeToTray == true)
            {
                if (WindowState == WindowState.Minimized)
                {
                    // 最小化到系統匣
                    Hide();
                    _notifyIcon?.ShowBalloonTip(
                        "Touchpad 側邊捲動",
                        "應用程式已最小化到系統匣",
                        Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info);
                }
            }
        }

        /// <summary>
        /// 視窗關閉中
        /// </summary>
        private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_viewModel?.Settings.MinimizeToTray == true)
            {
                // 取消關閉，改為最小化到系統匣
                e.Cancel = true;
                WindowState = WindowState.Minimized;
            }
        }

        /// <summary>
        /// 系統匣圖示點選
        /// </summary>
        private void NotifyIcon_TrayLeftMouseDown(object sender, RoutedEventArgs e)
        {
            ShowWindow();
        }

        /// <summary>
        /// 顯示視窗
        /// </summary>
        private void ShowWindow_Click(object sender, RoutedEventArgs e)
        {
            ShowWindow();
        }

        /// <summary>
        /// 顯示視窗
        /// </summary>
        private void ShowWindow()
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
        }

        /// <summary>
        /// 結束應用程式
        /// </summary>
        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            // 強制關閉
            if (_viewModel != null)
            {
                _viewModel.Settings.MinimizeToTray = false;
            }
            Application.Current.Shutdown();
        }

        /// <summary>
        /// 超連結導覽
        /// </summary>
        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = e.Uri.AbsoluteUri,
                    UseShellExecute = true
                });
                e.Handled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"無法開啟連結：{ex.Message}",
                    "錯誤",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 視窗關閉時清理資源
        /// </summary>
        protected override void OnClosed(EventArgs e)
        {
            _hwndSource?.RemoveHook(WndProc);
            _notifyIcon?.Dispose();
            base.OnClosed(e);
        }
    }

    #region Value Converters

    /// <summary>
    /// Bool 轉 Brush 轉換器
    /// </summary>
    public class BoolToBrushConverter : IValueConverter
    {
        public Brush? TrueBrush { get; set; }
        public Brush? FalseBrush { get; set; }

        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? TrueBrush : FalseBrush;
            }
            return FalseBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Enum 轉 Bool 轉換器（用於 RadioButton）
    /// </summary>
    public class EnumToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return false;

            return value.ToString() == parameter.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue && boolValue && parameter != null)
            {
                return Enum.Parse(targetType, parameter.ToString()!);
            }
            return Binding.DoNothing;
        }
    }

    #endregion
}
