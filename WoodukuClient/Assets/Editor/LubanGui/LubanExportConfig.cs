using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Luban.Editor
{
    /// <summary>
    /// Luban 4.x 导出配置。Inspector 布局对齐 AATileExploreLike 的 Odin 版。
    /// 路径相对 Unity 工程根目录（WoodukuClient）。
    /// </summary>
    [CreateAssetMenu(fileName = "Luban", menuName = "Luban/ExportConfig")]
    public class LubanExportConfig : ScriptableObject
    {
        #region 生命周期

        [LabelText("生成前")]
        [BeforeGenSelector]
        [BoxGroup("生命周期")]
        public string before_gen;

        [LabelText("生成后")]
        [AfterGenSelector]
        [BoxGroup("生命周期")]
        public string after_gen;

        #endregion

        #region 必要参数

        [Required]
        [LabelText("Luban.dll")]
        [FilePath(Extensions = "dll")]
        [Tooltip("相对 WoodukuClient，例如 ../Tools/Luban/Luban.dll")]
        [BoxGroup("必要参数")]
        public string which_dll = "../Tools/Luban/Luban.dll";

        [Required]
        [LabelText("luban.conf")]
        [FilePath]
        [Tooltip("相对 WoodukuClient，例如 ../WoodukuProfile/luban.conf")]
        [BoxGroup("必要参数")]
        public string conf = "../WoodukuProfile/luban.conf";

        [Required]
        [LabelText("Target (-t)")]
        [Tooltip("对应 luban.conf 的 targets：client / server / all")]
        [ValueDropdown(nameof(TargetDropdown))]
        [BoxGroup("必要参数")]
        public string service = "client";

        [Required]
        [LabelText("代码类型 (-c)")]
        [ValueDropdown(nameof(CodeTargetDropdown))]
        [BoxGroup("必要参数")]
        public string code_target = "cs-simple-json";

        [Required]
        [LabelText("数据类型 (-d)")]
        [ValueDropdown(nameof(DataTargetDropdown))]
        [BoxGroup("必要参数")]
        public string data_target = "json";

        private static IEnumerable<string> TargetDropdown => new[] { "client", "server", "all" };

        private static IEnumerable<string> CodeTargetDropdown => new[]
        {
            "cs-simple-json",
            "cs-bin",
            "cs-newtonsoft-json",
            "cs-dotnet-json"
        };

        private static IEnumerable<string> DataTargetDropdown => new[]
        {
            "json",
            "bin",
            "json-monolithic",
            "lua",
            "yaml"
        };

        #endregion

        #region 输出配置

        [LabelText("配置文件夹")]
        [FolderPath]
        [FoldoutGroup("输出配置")]
        public string output_data_dir = "Assets/GameRes/Config";

        [LabelText("代码文件夹")]
        [FolderPath]
        [FoldoutGroup("输出配置")]
        public string output_code_dir = "Assets/Scripts/GameLogic/Cfg";

        [LabelText("额外 -x 参数")]
        [Tooltip("每项形如 key=value，会拼成 -x key=value")]
        [FoldoutGroup("输出配置")]
        public List<string> extra_xargs = new List<string>();

        #endregion

        #region 其他

        [LabelText("Module 命名风格")]
        [FoldoutGroup("其他配置")]
        public NamingConvertion naming_convertion_module;

        [LabelText("Bean 命名风格")]
        [FoldoutGroup("其他配置")]
        public NamingConvertion naming_convertion_bean_member;

        [LabelText("Enum 命名风格")]
        [FoldoutGroup("其他配置")]
        public NamingConvertion naming_convertion_enum_member;

        [TextArea(5, 15)]
        [LabelText("预览命令")]
        [ShowInInspector]
        [NonSerialized]
        public string preview_command;

        #endregion

        [Button("生成", ButtonSizes.Large)]
        public void Gen()
        {
            if (!Validate(out string error))
            {
                Debug.LogError($"[Luban] {error}");
                return;
            }

            Preview();
            GenUtils.Gen(_GetCommand(), before_gen, after_gen);
        }

        [Button("预览")]
        public void Preview()
        {
            preview_command = $"{GenUtils.Dotnet} {_GetCommand()}";
        }

        public bool Validate(out string error)
        {
            string dll = GenUtils.ResolvePath(which_dll);
            if (!File.Exists(dll))
            {
                error = $"找不到 Luban.dll: {dll}";
                return false;
            }

            string confPath = GenUtils.ResolvePath(conf);
            if (!File.Exists(confPath))
            {
                error = $"找不到 luban.conf: {confPath}";
                return false;
            }

            if (string.IsNullOrWhiteSpace(service))
            {
                error = "Target 不能为空";
                return false;
            }

            error = null;
            return true;
        }

        private string _GetCommand()
        {
            var sb = new StringBuilder();
            sb.Append(Quote(GenUtils.ResolvePath(which_dll)));
            sb.Append(" -t ").Append(service);

            if (!string.IsNullOrWhiteSpace(code_target))
            {
                sb.Append(" -c ").Append(code_target);
            }

            if (!string.IsNullOrWhiteSpace(data_target))
            {
                sb.Append(" -d ").Append(data_target);
            }

            sb.Append(" --conf ").Append(Quote(GenUtils.ResolvePath(conf)));

            if (!string.IsNullOrWhiteSpace(output_code_dir))
            {
                sb.Append(" -x outputCodeDir=").Append(NormalizeArgPath(output_code_dir));
            }

            if (!string.IsNullOrWhiteSpace(output_data_dir))
            {
                sb.Append(" -x outputDataDir=").Append(NormalizeArgPath(output_data_dir));
            }

            AppendNaming(sb, "namingConvention.module", naming_convertion_module);
            AppendNaming(sb, "namingConvention.beanMember", naming_convertion_bean_member);
            AppendNaming(sb, "namingConvention.enumMember", naming_convertion_enum_member);

            if (extra_xargs != null)
            {
                foreach (string x in extra_xargs)
                {
                    if (string.IsNullOrWhiteSpace(x))
                    {
                        continue;
                    }

                    string arg = x.Trim();
                    sb.Append(arg.StartsWith("-x ", StringComparison.Ordinal) ? $" {arg}" : $" -x {arg}");
                }
            }

            return sb.ToString();
        }

        private static void AppendNaming(StringBuilder sb, string key, NamingConvertion value)
        {
            if (value == NamingConvertion.None)
            {
                return;
            }

            sb.Append(" -x ").Append(key).Append('=').Append(value);
        }

        private static string NormalizeArgPath(string path)
        {
            return path.Replace('\\', '/').TrimEnd('/');
        }

        private static string Quote(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return "\"\"";
            }

            return path.Contains(" ") ? $"\"{path}\"" : path;
        }
    }
}
