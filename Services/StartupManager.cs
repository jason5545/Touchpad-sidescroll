using System;
using System.IO;
using System.Security.Principal;
using Microsoft.Extensions.Logging;
using Microsoft.Win32.TaskScheduler;

namespace TouchpadAdvancedTool.Services
{
    /// <summary>
    /// 啟動管理器 - 負責管理應用程式的開機自動啟動功能
    /// </summary>
    public class StartupManager
    {
        private readonly ILogger<StartupManager> _logger;
        private const string TaskName = "TouchpadAdvancedTool";

        public StartupManager(ILogger<StartupManager> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 啟用開機自動啟動（使用排程工作，於使用者登入時以最高權限執行）
        /// </summary>
        /// <param name="silentStart">是否靜默啟動（啟動到系統匣）</param>
        /// <returns>是否成功</returns>
        public bool EnableStartup(bool silentStart = true)
        {
            try
            {
                var exePath = GetExecutablePath();
                if (string.IsNullOrEmpty(exePath))
                {
                    _logger.LogError("無法取得執行檔路徑");
                    return false;
                }

                using var taskService = new TaskService();
                using var taskDefinition = taskService.NewTask();

                taskDefinition.RegistrationInfo.Description = "Touchpad Advanced Tool - 開機自動啟動";

                var currentUser = WindowsIdentity.GetCurrent().Name;

                // 以目前使用者最高權限執行
                taskDefinition.Principal.UserId = currentUser;
                taskDefinition.Principal.LogonType = TaskLogonType.InteractiveToken;
                taskDefinition.Principal.RunLevel = TaskRunLevel.Highest;

                // 觸發條件：目前使用者登入時
                taskDefinition.Triggers.Add(new LogonTrigger
                {
                    UserId = currentUser
                });

                var arguments = silentStart ? "--minimized" : string.Empty;
                var workingDirectory = Path.GetDirectoryName(exePath);

                taskDefinition.Actions.Add(new ExecAction(exePath, arguments, workingDirectory));

                // 建立或更新排程工作
                taskService.RootFolder.RegisterTaskDefinition(
                    TaskName,
                    taskDefinition,
                    TaskCreation.CreateOrUpdate,
                    null,
                    null,
                    TaskLogonType.InteractiveToken,
                    null);

                _logger.LogInformation(
                    "已啟用開機自動啟動（排程工作）：TaskName={TaskName}, Path={Path}, Args={Args}",
                    TaskName, exePath, arguments);

                return true;
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "啟用開機自動啟動失敗：權限不足（建立排程工作失敗）");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "啟用開機自動啟動失敗（建立排程工作失敗）");
                return false;
            }
        }

        /// <summary>
        /// 停用開機自動啟動（刪除排程工作）
        /// </summary>
        /// <returns>是否成功</returns>
        public bool DisableStartup()
        {
            try
            {
                using var taskService = new TaskService();
                var existingTask = taskService.GetTask(TaskName);

                if (existingTask == null)
                {
                    _logger.LogInformation(
                        "停用開機自動啟動：找不到排程工作，視為已停用。TaskName={TaskName}",
                        TaskName);
                    return true;
                }

                taskService.RootFolder.DeleteTask(TaskName, false);
                _logger.LogInformation("已停用開機自動啟動（已刪除排程工作）：TaskName={TaskName}", TaskName);
                return true;
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "停用開機自動啟動失敗：權限不足（刪除排程工作失敗）");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "停用開機自動啟動失敗（刪除排程工作失敗）");
                return false;
            }
        }

        /// <summary>
        /// 檢查是否已啟用開機自動啟動（是否存在排程工作）
        /// </summary>
        /// <returns>是否已啟用</returns>
        public bool IsStartupEnabled()
        {
            try
            {
                using var taskService = new TaskService();
                var task = taskService.GetTask(TaskName);
                return task != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "檢查開機自動啟動狀態失敗（排程工作）");
                return false;
            }
        }

        /// <summary>
        /// 取得當前註冊的啟動命令（從排程工作動作）
        /// </summary>
        /// <returns>啟動命令，如果未啟用則返回 null</returns>
        public string? GetStartupCommand()
        {
            try
            {
                using var taskService = new TaskService();
                var task = taskService.GetTask(TaskName);
                if (task == null)
                    return null;

                if (task.Definition.Actions.Count == 0)
                    return null;

                if (task.Definition.Actions[0] is not ExecAction execAction)
                    return null;

                if (string.IsNullOrEmpty(execAction.Path))
                    return null;

                var arguments = string.IsNullOrWhiteSpace(execAction.Arguments)
                    ? string.Empty
                    : " " + execAction.Arguments;

                return $"\"{execAction.Path}\"" + arguments;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得啟動命令失敗（排程工作）");
                return null;
            }
        }

        /// <summary>
        /// 驗證啟動項目是否指向正確的執行檔
        /// </summary>
        /// <returns>是否有效</returns>
        public bool ValidateStartupEntry()
        {
            try
            {
                var command = GetStartupCommand();
                if (string.IsNullOrEmpty(command))
                    return false;

                var currentExePath = GetExecutablePath();
                if (string.IsNullOrEmpty(currentExePath))
                    return false;

                // 檢查命令中是否包含當前執行檔路徑
                return command.Contains(currentExePath, StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "驗證啟動項目失敗（排程工作）");
                return false;
            }
        }

        /// <summary>
        /// 取得執行檔路徑
        /// </summary>
        /// <returns>執行檔路徑</returns>
        /// <summary>
        /// 取得執行檔路徑
        /// </summary>
        /// <returns>執行檔路徑</returns>
        private string? GetExecutablePath()
        {
            try
            {
                // 使用 Environment.ProcessPath 取得當前執行檔路徑 (.NET 6+)
                // 這比 Process.GetCurrentProcess().MainModule.FileName 更可靠且效能更好
                return Environment.ProcessPath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得執行檔路徑失敗");
                return null;
            }
        }
    }
}

