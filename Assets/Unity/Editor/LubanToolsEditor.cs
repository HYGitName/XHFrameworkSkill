using UnityEngine;
using UnityEditor;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

public class LubanToolsEditor
{
    private const string LubanDataPath = "Luban/MiniTemplate/Datas";
    private const string LubanGenBatPath = "Luban/MiniTemplate/gen.bat";
    private const string LubanGenShPath = "Luban/MiniTemplate/gen.sh";

    [MenuItem("Tools/Luban/打开配置表文件夹")]
    public static void OpenLubanDataFolder()
    {
        string projectPath = Directory.GetParent(Application.dataPath).FullName;
        string fullPath = Path.Combine(projectPath, LubanDataPath);

        if (Directory.Exists(fullPath))
        {
            EditorUtility.RevealInFinder(fullPath);
            UnityEngine.Debug.Log($"已打开文件夹: {fullPath}");
        }
        else
        {
            EditorUtility.DisplayDialog("错误", $"文件夹不存在: {fullPath}", "确定");
        }
    }

    [MenuItem("Tools/Luban/执行生成脚本")]
    public static void ExecuteGenBat()
    {
        string projectPath = Directory.GetParent(Application.dataPath).FullName;

        if (Application.platform == RuntimePlatform.OSXEditor)
        {
            string shPath = Path.Combine(projectPath, LubanGenShPath);
            string workingDirectory = Path.GetDirectoryName(shPath);

            if (!File.Exists(shPath))
            {
                EditorUtility.DisplayDialog("错误", $"文件不存在: {shPath}", "确定");
                return;
            }

            try
            {
                // 使用 bash 显式解释脚本，避免未 chmod +x 时出现 Permission denied
                // gen.sh 仅在交互终端下 read；非 TTY（Unity 子进程）不会阻塞
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = "-c \"bash gen.sh\"",
                    WorkingDirectory = workingDirectory,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using (Process process = Process.Start(startInfo))
                {
                    if (process == null)
                    {
                        EditorUtility.DisplayDialog("错误", "无法启动进程", "确定");
                        return;
                    }

                    UnityEngine.Debug.Log($"正在执行: {shPath}");
                    Task<string> stdoutTask = process.StandardOutput.ReadToEndAsync();
                    Task<string> stderrTask = process.StandardError.ReadToEndAsync();
                    process.WaitForExit();
                    Task.WaitAll(stdoutTask, stderrTask);
                    string stdout = stdoutTask.Result;
                    string stderr = stderrTask.Result;

                    if (!string.IsNullOrEmpty(stdout))
                    {
                        UnityEngine.Debug.Log(stdout);
                    }

                    if (process.ExitCode == 0)
                    {
                        UnityEngine.Debug.Log("Luban生成脚本执行成功！");
                        AssetDatabase.Refresh();
                    }
                    else
                    {
                        UnityEngine.Debug.LogWarning($"Luban生成脚本执行完成，退出代码: {process.ExitCode}");
                        if (!string.IsNullOrEmpty(stderr))
                        {
                            UnityEngine.Debug.LogError(stderr);
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("错误", $"执行失败: {e.Message}", "确定");
                UnityEngine.Debug.LogError($"执行gen.sh失败: {e.Message}");
            }

            return;
        }

        string batPath = Path.Combine(projectPath, LubanGenBatPath);
        string batWorkingDirectory = Path.GetDirectoryName(batPath);

        if (File.Exists(batPath))
        {
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = batPath,
                    WorkingDirectory = batWorkingDirectory,
                    UseShellExecute = true,
                    CreateNoWindow = false
                };

                Process process = Process.Start(startInfo);
                UnityEngine.Debug.Log($"正在执行: {batPath}");

                // 可选：等待进程完成
                if (process != null)
                {
                    process.WaitForExit();
                    if (process.ExitCode == 0)
                    {
                        UnityEngine.Debug.Log("Luban生成脚本执行成功！");
                        AssetDatabase.Refresh();
                    }
                    else
                    {
                        UnityEngine.Debug.LogWarning($"Luban生成脚本执行完成，退出代码: {process.ExitCode}");
                    }
                }
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("错误", $"执行失败: {e.Message}", "确定");
                UnityEngine.Debug.LogError($"执行gen.bat失败: {e.Message}");
            }
        }
        else
        {
            EditorUtility.DisplayDialog("错误", $"文件不存在: {batPath}", "确定");
        }
    }
}
