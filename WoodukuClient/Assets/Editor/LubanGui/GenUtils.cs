using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using UnityEditor;
using Debug = UnityEngine.Debug;

namespace Luban.Editor
{
    internal static class GenUtils
    {
        internal static readonly string Dotnet =
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "dotnet.exe" : "dotnet";

        public static string ProjectRoot =>
            Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, ".."));

        public static string ResolvePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            if (Path.IsPathRooted(path))
            {
                return Path.GetFullPath(path);
            }

            return Path.GetFullPath(Path.Combine(ProjectRoot, path));
        }

        public static void Gen(string arguments, string before, string after)
        {
            Debug.Log($"[Luban] {Dotnet} {arguments}");

            IBeforeGen beforeGen = CreateHook<IBeforeGen>(before);
            IAfterGen afterGen = CreateHook<IAfterGen>(after);

            beforeGen?.Process();

            var info = new ProcessStartInfo
            {
                FileName = Dotnet,
                Arguments = arguments,
                CreateNoWindow = true,
                UseShellExecute = false,
                WorkingDirectory = ProjectRoot,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            using var process = Process.Start(info);
            if (process == null)
            {
                Debug.LogError($"[Luban] 无法启动: {Dotnet}");
                return;
            }

            string stdout = process.StandardOutput.ReadToEnd();
            string stderr = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (!string.IsNullOrWhiteSpace(stdout))
            {
                Debug.Log(stdout);
            }

            if (!string.IsNullOrWhiteSpace(stderr))
            {
                Debug.LogWarning(stderr);
            }

            if (process.ExitCode != 0)
            {
                Debug.LogError($"[Luban] 生成失败，退出码 {process.ExitCode}");
                return;
            }

            Debug.Log("[Luban] 生成完成");
            afterGen?.Process();
            AssetDatabase.Refresh();
        }

        private static T CreateHook<T>(string typeName) where T : class
        {
            if (string.IsNullOrWhiteSpace(typeName))
            {
                return null;
            }

            Type type = Type.GetType(typeName);
            if (type == null)
            {
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    type = assembly.GetType(typeName);
                    if (type != null)
                    {
                        break;
                    }
                }
            }

            if (type == null)
            {
                Debug.LogWarning($"[Luban] 找不到生命周期类型: {typeName}");
                return null;
            }

            return Activator.CreateInstance(type) as T;
        }
    }
}
