using System;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using TouchpadSideScroll.Models;
using TouchpadSideScroll.Native;
using static TouchpadSideScroll.Native.NativeMethods;

namespace TouchpadSideScroll.Core
{
    /// <summary>
    /// 捲動轉換器 - 將觸控板移動轉換為滾輪事件
    /// </summary>
    public class ScrollConverter
    {
        private readonly ILogger<ScrollConverter> _logger;
        private int _accumulatedDeltaY;
        private int _accumulatedDeltaX;
        private DateTime _lastScrollTime;
        private const int MinScrollThreshold = 5; // 最小捲動閾值（觸控板單位）

        public ScrollConverter(ILogger<ScrollConverter> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 處理捲動區移動並注入滾輪事件
        /// </summary>
        public void ProcessScroll(ScrollZoneEventArgs args, TouchpadSettings settings)
        {
            try
            {
                // 累積移動距離
                int deltaY = settings.InvertScrollDirection ? -args.DeltaY : args.DeltaY;
                int deltaX = args.DeltaX;

                _accumulatedDeltaY += deltaY;

                if (settings.EnableHorizontalScroll)
                {
                    _accumulatedDeltaX += deltaX;
                }

                // 計算觸控板座標到滾輪單位的轉換比例
                // 假設觸控板高度約為 6000 單位，移動 50 單位 = 1 個滾輪增量 (120 單位)
                double scaleFactor = CalculateScaleFactor(args.TouchpadInfo, settings);

                // 檢查是否累積足夠的移動距離來觸發捲動
                int scrollUnitsY = 0;
                int scrollUnitsX = 0;

                if (Math.Abs(_accumulatedDeltaY) >= MinScrollThreshold)
                {
                    // 計算滾輪單位數
                    double scrollAmount = _accumulatedDeltaY * scaleFactor * settings.ScrollSpeed * settings.ScrollSensitivity;
                    scrollUnitsY = (int)(scrollAmount / WHEEL_DELTA);

                    if (scrollUnitsY != 0)
                    {
                        // 重置累積值（保留餘數）
                        _accumulatedDeltaY -= (int)(scrollUnitsY * WHEEL_DELTA / (scaleFactor * settings.ScrollSpeed * settings.ScrollSensitivity));
                    }
                }

                if (settings.EnableHorizontalScroll && Math.Abs(_accumulatedDeltaX) >= MinScrollThreshold)
                {
                    double scrollAmount = _accumulatedDeltaX * scaleFactor * settings.ScrollSpeed * settings.ScrollSensitivity;
                    scrollUnitsX = (int)(scrollAmount / WHEEL_DELTA);

                    if (scrollUnitsX != 0)
                    {
                        _accumulatedDeltaX -= (int)(scrollUnitsX * WHEEL_DELTA / (scaleFactor * settings.ScrollSpeed * settings.ScrollSensitivity));
                    }
                }

                // 注入滾輪事件
                if (scrollUnitsY != 0 || scrollUnitsX != 0)
                {
                    InjectScrollEvent(scrollUnitsY, scrollUnitsX, settings);
                    _lastScrollTime = DateTime.Now;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "處理捲動失敗");
            }
        }

        /// <summary>
        /// 計算縮放係數
        /// </summary>
        private double CalculateScaleFactor(TouchpadInfo touchpadInfo, TouchpadSettings settings)
        {
            // 根據觸控板高度計算基礎縮放係數
            // 假設標準觸控板高度為 6000 單位，移動 40 單位產生 1 個滾輪增量
            const double standardHeight = 6000.0;
            const double standardPixelsPerWheelDelta = 40.0;

            double touchpadHeight = touchpadInfo.Height;
            double scaleFactor = standardPixelsPerWheelDelta / standardHeight;

            // 根據實際觸控板大小調整
            if (touchpadHeight > 0)
            {
                scaleFactor *= standardHeight / touchpadHeight;
            }

            return scaleFactor;
        }

        /// <summary>
        /// 注入滾輪事件
        /// </summary>
        private void InjectScrollEvent(int scrollUnitsY, int scrollUnitsX, TouchpadSettings settings)
        {
            try
            {
                // 垂直捲動
                if (scrollUnitsY != 0)
                {
                    var input = new INPUT
                    {
                        Type = INPUT_MOUSE,
                        Mouse = new MOUSEINPUT
                        {
                            X = 0,
                            Y = 0,
                            MouseData = (uint)(scrollUnitsY * WHEEL_DELTA),
                            Flags = MOUSEEVENTF_WHEEL,
                            Time = 0,
                            ExtraInfo = IntPtr.Zero
                        }
                    };

                    uint result = SendInput(1, new[] { input }, Marshal.SizeOf<INPUT>());

                    if (result == 0)
                    {
                        var error = Marshal.GetLastWin32Error();
                        _logger.LogWarning("注入垂直滾輪事件失敗，錯誤碼：{ErrorCode}", error);
                    }
                    else if (settings.DebugMode)
                    {
                        _logger.LogDebug("注入垂直滾輪：{Units} 單位", scrollUnitsY);
                    }
                }

                // 水平捲動
                if (scrollUnitsX != 0 && settings.EnableHorizontalScroll)
                {
                    var input = new INPUT
                    {
                        Type = INPUT_MOUSE,
                        Mouse = new MOUSEINPUT
                        {
                            X = 0,
                            Y = 0,
                            MouseData = (uint)(scrollUnitsX * WHEEL_DELTA),
                            Flags = MOUSEEVENTF_HWHEEL,
                            Time = 0,
                            ExtraInfo = IntPtr.Zero
                        }
                    };

                    uint result = SendInput(1, new[] { input }, Marshal.SizeOf<INPUT>());

                    if (result == 0)
                    {
                        var error = Marshal.GetLastWin32Error();
                        _logger.LogWarning("注入水平滾輪事件失敗，錯誤碼：{ErrorCode}", error);
                    }
                    else if (settings.DebugMode)
                    {
                        _logger.LogDebug("注入水平滾輪：{Units} 單位", scrollUnitsX);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "注入滾輪事件失敗");
            }
        }

        /// <summary>
        /// 重置累積的捲動距離
        /// </summary>
        public void Reset()
        {
            _accumulatedDeltaY = 0;
            _accumulatedDeltaX = 0;
        }
    }
}
