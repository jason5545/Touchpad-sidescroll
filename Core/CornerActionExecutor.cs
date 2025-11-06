using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using TouchpadAdvancedTool.Models;
using TouchpadAdvancedTool.Native;
using static TouchpadAdvancedTool.Native.NativeMethods;

namespace TouchpadAdvancedTool.Core;

/// <summary>
/// 角落動作執行器，負責執行各種角落觸擊動作
/// </summary>
public class CornerActionExecutor
{
    /// <summary>
    /// 執行角落動作
    /// </summary>
    public void ExecuteAction(CornerAction action)
    {
        try
        {
            switch (action)
            {
                case CornerAction.None:
                    // 無動作
                    break;

                case CornerAction.ShowDesktop:
                    ExecuteShowDesktop();
                    break;

                case CornerAction.TaskView:
                    ExecuteTaskView();
                    break;

                case CornerAction.ActionCenter:
                    ExecuteActionCenter();
                    break;

                case CornerAction.MediaPlayPause:
                    ExecuteMediaKey(VirtualKeyCode.MEDIA_PLAY_PAUSE);
                    break;

                case CornerAction.MediaNextTrack:
                    ExecuteMediaKey(VirtualKeyCode.MEDIA_NEXT_TRACK);
                    break;

                case CornerAction.MediaPreviousTrack:
                    ExecuteMediaKey(VirtualKeyCode.MEDIA_PREV_TRACK);
                    break;

                case CornerAction.VolumeMute:
                    ExecuteMediaKey(VirtualKeyCode.VOLUME_MUTE);
                    break;

                case CornerAction.ScreenSnip:
                    ExecuteScreenSnip();
                    break;

                case CornerAction.RightClick:
                    ExecuteRightClick();
                    break;

                case CornerAction.CustomCommand:
                    // TODO: 實現自訂指令功能
                    break;

                default:
                    Debug.WriteLine($"[CornerActionExecutor] 未知的動作類型: {action}");
                    break;
            }

            Debug.WriteLine($"[CornerActionExecutor] 已執行動作: {action}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[CornerActionExecutor] 執行動作失敗: {action}, 錯誤: {ex.Message}");
        }
    }

    /// <summary>
    /// 執行「顯示桌面」動作 (Win + D)
    /// </summary>
    private void ExecuteShowDesktop()
    {
        SendKeyCombo(VirtualKeyCode.LWIN, VirtualKeyCode.VK_D);
    }

    /// <summary>
    /// 執行「工作檢視」動作 (Win + Tab)
    /// </summary>
    private void ExecuteTaskView()
    {
        SendKeyCombo(VirtualKeyCode.LWIN, VirtualKeyCode.TAB);
    }

    /// <summary>
    /// 執行「動作中心」動作 (Win + A)
    /// </summary>
    private void ExecuteActionCenter()
    {
        SendKeyCombo(VirtualKeyCode.LWIN, VirtualKeyCode.VK_A);
    }

    /// <summary>
    /// 執行「螢幕擷取」動作 (Win + Shift + S)
    /// </summary>
    private void ExecuteScreenSnip()
    {
        SendKeyCombo(VirtualKeyCode.LWIN, VirtualKeyCode.SHIFT, VirtualKeyCode.VK_S);
    }

    /// <summary>
    /// 執行「滑鼠右鍵」動作
    /// </summary>
    private void ExecuteRightClick()
    {
        var inputs = new INPUT[2];

        // 按下右鍵
        inputs[0] = new INPUT
        {
            Type = INPUT_MOUSE,
            U = new InputUnion
            {
                mi = new MOUSEINPUT
                {
                    X = 0,
                    Y = 0,
                    MouseData = 0,
                    Flags = MOUSEEVENTF_RIGHTDOWN,
                    Time = 0,
                    ExtraInfo = IntPtr.Zero
                }
            }
        };

        // 釋放右鍵
        inputs[1] = new INPUT
        {
            Type = INPUT_MOUSE,
            U = new InputUnion
            {
                mi = new MOUSEINPUT
                {
                    X = 0,
                    Y = 0,
                    MouseData = 0,
                    Flags = MOUSEEVENTF_RIGHTUP,
                    Time = 0,
                    ExtraInfo = IntPtr.Zero
                }
            }
        };

        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
    }

    /// <summary>
    /// 執行媒體按鍵
    /// </summary>
    private void ExecuteMediaKey(VirtualKeyCode keyCode)
    {
        var inputs = new INPUT[2];

        // 按下
        inputs[0] = new INPUT
        {
            Type = INPUT_KEYBOARD,
            U = new InputUnion
            {
                ki = new KEYBDINPUT
                {
                    wVk = (ushort)keyCode,
                    wScan = 0,
                    dwFlags = 0,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };

        // 釋放
        inputs[1] = new INPUT
        {
            Type = INPUT_KEYBOARD,
            U = new InputUnion
            {
                ki = new KEYBDINPUT
                {
                    wVk = (ushort)keyCode,
                    wScan = 0,
                    dwFlags = KEYEVENTF_KEYUP,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };

        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
    }

    /// <summary>
    /// 傳送組合鍵（兩鍵）
    /// </summary>
    private void SendKeyCombo(VirtualKeyCode modifier, VirtualKeyCode key)
    {
        var inputs = new INPUT[4];

        // 按下修飾鍵
        inputs[0] = CreateKeyInput(modifier, false);
        // 按下主鍵
        inputs[1] = CreateKeyInput(key, false);
        // 釋放主鍵
        inputs[2] = CreateKeyInput(key, true);
        // 釋放修飾鍵
        inputs[3] = CreateKeyInput(modifier, true);

        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
    }

    /// <summary>
    /// 傳送組合鍵（三鍵）
    /// </summary>
    private void SendKeyCombo(VirtualKeyCode modifier1, VirtualKeyCode modifier2, VirtualKeyCode key)
    {
        var inputs = new INPUT[6];

        // 按下第一個修飾鍵
        inputs[0] = CreateKeyInput(modifier1, false);
        // 按下第二個修飾鍵
        inputs[1] = CreateKeyInput(modifier2, false);
        // 按下主鍵
        inputs[2] = CreateKeyInput(key, false);
        // 釋放主鍵
        inputs[3] = CreateKeyInput(key, true);
        // 釋放第二個修飾鍵
        inputs[4] = CreateKeyInput(modifier2, true);
        // 釋放第一個修飾鍵
        inputs[5] = CreateKeyInput(modifier1, true);

        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
    }

    /// <summary>
    /// 建立鍵盤輸入結構
    /// </summary>
    private INPUT CreateKeyInput(VirtualKeyCode keyCode, bool keyUp)
    {
        return new INPUT
        {
            Type = INPUT_KEYBOARD,
            U = new InputUnion
            {
                ki = new KEYBDINPUT
                {
                    wVk = (ushort)keyCode,
                    wScan = 0,
                    dwFlags = keyUp ? KEYEVENTF_KEYUP : 0,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };
    }
}

/// <summary>
/// 虛擬鍵碼（擴充現有的 NativeMethods）
/// </summary>
public enum VirtualKeyCode : ushort
{
    // 修飾鍵
    SHIFT = 0x10,
    LWIN = 0x5B,
    RWIN = 0x5C,

    // 字母鍵
    VK_A = 0x41,
    VK_D = 0x44,
    VK_S = 0x53,

    // 功能鍵
    TAB = 0x09,

    // 媒體鍵
    MEDIA_NEXT_TRACK = 0xB0,
    MEDIA_PREV_TRACK = 0xB1,
    MEDIA_STOP = 0xB2,
    MEDIA_PLAY_PAUSE = 0xB3,

    // 音量鍵
    VOLUME_MUTE = 0xAD,
    VOLUME_DOWN = 0xAE,
    VOLUME_UP = 0xAF
}
