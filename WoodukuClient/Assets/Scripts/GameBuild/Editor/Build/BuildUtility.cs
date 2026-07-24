using System;
using System.Collections.Generic;
using FishFramework;
using UnityEditor;
using UnityEngine;
using YooAsset;
using YooAsset.Editor;

public static class BuildUtility
{
    public static string[] GetLevelsFromBuildSettings()
    {
        var levels = new List<string>();
        foreach (var scene in EditorBuildSettings.scenes)
        {
            if (scene.enabled)
            {
                levels.Add(scene.path);
            }
        }

        return levels.ToArray();
    }

    public static GameSettings LoadGameSettings()
    {
        return Resources.Load<GameSettings>("GameSettings");
    }

    /// <summary>
    /// 递增资源版本并用 YooAsset Scriptable 管线构建 DefaultPackage，复制到 StreamingAssets。
    /// </summary>
    public static void BuildAllBundles(Action completed = null)
    {
        var settings = LoadGameSettings();
        if (settings != null)
        {
            settings.AddVersion();
        }

        string packageName = ResourceModule.DefaultPackageName;
        string packageVersion = settings != null
            ? settings.GetVersion()
            : DateTime.Now.ToString("yyyy-MM-dd-HH-mm");

        var uniqueBundleName = BundleCollectorSettingData.Setting.UniqueBundleName;
        var packRuleResult = DefaultBundlePackRule.CreateShadersPackRuleResult();
        string builtinShadersBundleName = packRuleResult.GetBundleName(packageName, uniqueBundleName);

        var buildParameters = new ScriptableBuildParameters
        {
            BuildOutputRoot = BundleBuilderHelper.GetDefaultBuildOutputRoot(),
            BundledFileRoot = BundleBuilderHelper.GetStreamingAssetsRoot(),
            BuildPipeline = EBuildPipeline.ScriptableBuildPipeline.ToString(),
            BuildBundleType = (int)EBundleType.AssetBundle,
            BuildTarget = EditorUserBuildSettings.activeBuildTarget,
            PackageName = packageName,
            PackageVersion = packageVersion,
            EnableSharePackRule = true,
            VerifyBuildingResult = true,
            FileNameStyle = EFileNameStyle.HashName,
            BundledCopyOption = EBundledCopyOption.ClearAndCopyAll,
            BundledCopyParams = string.Empty,
            CompressOption = ECompressOption.LZ4,
            ClearBuildCacheFiles = false,
            UseAssetDependencyDB = true,
            BuiltinShadersBundleName = builtinShadersBundleName,
            WriteLinkXML = true,
        };

        var pipeline = new ScriptableBuildPipeline();
        var buildResult = pipeline.Run(buildParameters, true);
        if (buildResult.Success)
        {
            Debug.Log($"[BuildUtility] YooAsset build ok: {buildResult.OutputPackageDirectory}");
            AssetDatabase.Refresh();
        }
        else
        {
            Debug.LogError($"[BuildUtility] YooAsset build failed: {buildResult.ErrorInfo}");
        }

        completed?.Invoke();
    }
}
