using System;
using System.Threading;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using TouchpadAdvancedTool.Core;
using TouchpadAdvancedTool.Services;
using TouchpadAdvancedTool.ViewModels;

namespace TouchpadAdvancedTool
{
    /// <summary>
    /// 應用程式主類別
    /// </summary>
    public partial class App : Application
    {
        private static Mutex? _mutex;
        private IServiceProvider? _serviceProvider;
        private bool _startMinimized;

        /// <summary>
        /// 應用程式啟動
        /// </summary>
        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                // 檢查命令列參數
                _startMinimized = e.Args.Length > 0 &&
                    (e.Args[0] == "--minimized" || e.Args[0] == "--silent" || e.Args[0] == "-m");

                // 檢查是否已有執行個體在執行（單一執行個體）
                const string mutexName = "TouchpadAdvancedTool_SingleInstance_Mutex";
                _mutex = new Mutex(true, mutexName, out bool createdNew);

                if (!createdNew)
                {
                    // 如果是靜默啟動，不顯示訊息直接退出
                    if (!_startMinimized)
                    {
                        MessageBox.Show(
                            "Touchpad Advanced Tool 已在執行中。",
                            "Touchpad Advanced Tool",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                    Shutdown();
                    return;
                }

                // 設定 Serilog
                Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Debug()
                    .WriteTo.File(
                        path: System.IO.Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                            "TouchpadAdvancedTool",
                            "logs",
                            "log-.txt"),
                        rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: 7)
                    .CreateLogger();

                Log.Information("應用程式啟動");

                // 設定依賴注入
                var services = new ServiceCollection();
                ConfigureServices(services);
                _serviceProvider = services.BuildServiceProvider();

                // 處理未捕獲的例外狀況
                DispatcherUnhandledException += (s, args) =>
                {
                    Log.Error(args.Exception, "未處理的例外狀況");
                    MessageBox.Show(
                        $"發生錯誤：{args.Exception.Message}\n\n詳細資訊：{args.Exception.StackTrace}",
                        "錯誤",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    args.Handled = true;
                };

                base.OnStartup(e);

                Log.Information("正在建立主視窗");
                // 顯示主視窗
                var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();

                if (_startMinimized)
                {
                    // 靜默啟動：啟動到系統匣，不顯示主視窗
                    Log.Information("靜默啟動模式：啟動到系統匣");
                    // 必須先 Show() 再設定 WindowState，否則會出現問題
                    mainWindow.Show();
                    mainWindow.WindowState = WindowState.Minimized;
                    mainWindow.Hide();
                    Log.Information("應用程式已靜默啟動到系統匣");
                }
                else
                {
                    Log.Information("正在顯示主視窗");
                    mainWindow.Show();
                    Log.Information("主視窗已顯示");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"應用程式啟動失敗：{ex.Message}\n\n詳細資訊：\n{ex.StackTrace}",
                    "嚴重錯誤",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                try
                {
                    Log.Fatal(ex, "應用程式啟動失敗");
                }
                catch { }

                Shutdown();
            }
        }

        /// <summary>
        /// 設定服務
        /// </summary>
        private void ConfigureServices(ServiceCollection services)
        {
            // 日誌服務
            services.AddLogging(builder =>
            {
                builder.AddSerilog(dispose: true);
            });

            // 核心服務
            services.AddSingleton<RawInputManager>();
            services.AddSingleton<MouseHookManager>();
            services.AddSingleton<TouchpadTracker>();
            services.AddSingleton<ScrollConverter>();
            services.AddSingleton<StartupManager>();
            services.AddSingleton<SettingsManager>();

            // ViewModels
            services.AddSingleton<MainViewModel>();

            // Views
            services.AddSingleton<MainWindow>();
        }

        /// <summary>
        /// 應用程式關閉
        /// </summary>
        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                var hook = _serviceProvider?.GetService<MouseHookManager>();
                hook?.UninstallHook();

                var raw = _serviceProvider?.GetService<RawInputManager>();
                raw?.Unregister();

                if (_serviceProvider is IDisposable d)
                {
                    d.Dispose();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "應用程式關閉時清理失敗");
            }

            Log.Information("應用程式關閉");
            Log.CloseAndFlush();

            _mutex?.ReleaseMutex();
            _mutex?.Dispose();

            base.OnExit(e);
        }

        /// <summary>
        /// 取得服務
        /// </summary>
        public static T GetService<T>() where T : class
        {
            var app = (App)Current;
            return app._serviceProvider?.GetService<T>()
                ?? throw new InvalidOperationException($"服務 {typeof(T).Name} 未註冊");
        }
    }
}
