using System;
using UnityEngine;

namespace FishFramework
{
    public enum Languages
    {
        ChineseSimplified,
        ChineseTraditional,
        English,
        Spanish,
        Portuguese,
        Russian,
        German,
        French,
        Turkish,
        Indonesian,
        Italian,
        Japanese,
        Korean,
        Ukrainian,
    }

    public enum ChannelPlatform
    {
        None,
        FishSDK,
    }

    public enum ServerURLType
    {
        INTRA_URL = 1, // 内网
        OUTER_URL = 2, // 外网
    }


    [CreateAssetMenu(menuName = "GameSettings", fileName = "GameSettings", order = 0)]
    public class GameSettings : ScriptableObject
    {
        [Tooltip("游戏的日志模式")] public bool logMode;

        [Tooltip("是否开启GM")] public bool gmMode;

        [Tooltip("游戏的运行帧频，默认60帧")] public int game60;

        [Tooltip("打包语言选择")] public Languages language = Languages.ChineseSimplified;

        [Tooltip("打包服务器地址选择")] public ServerURLType urlType = ServerURLType.INTRA_URL;

        [Tooltip("打包平台选择")] public ChannelPlatform platform = ChannelPlatform.None;

        [Tooltip("游戏大版本")] public int appVersion = 1;

        [Header("资源版本号")]
        public int major;

        public int minor;

        public int build;

#if UNITY_EDITOR
        public void AddVersion()
        {
            build += 1;
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssets();
        }
#endif

        public string GetVersion()
        {
            var ver = new Version(major, minor, build);
            return ver.ToString();
        }

        public string GetUploadHotupURL()
        {
            return "";
        }
    }
}
