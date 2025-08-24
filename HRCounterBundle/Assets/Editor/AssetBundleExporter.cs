using System.IO;
using UnityEditor;
using UnityEngine;

public class CreateAssetBundles
{
    [MenuItem("Tool/Build HRCounter Bundle")]
    static void BuildAllAssetBundles()
    {
        var resourcesPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "HRCounter", "Resources"));
        var targetPath = EditorUtility.SaveFilePanel("Export Font Asset Bundle", resourcesPath, "hrcounter", string.Empty);

        var assetBundleBuild = new AssetBundleBuild
        {
            assetBundleName = "hrcounter",
            assetNames = new[] { Path.Combine("Assets", "HRCounter.prefab") }
        };
        var manifest = BuildPipeline.BuildAssetBundles(Application.temporaryCachePath, new[] { assetBundleBuild }, BuildAssetBundleOptions.ForceRebuildAssetBundle,
            EditorUserBuildSettings.activeBuildTarget);

        if (manifest == null)
        {
            Debug.LogError("Failed to build AssetBundle!");
            return;
        }

        var fileName = manifest.GetAllAssetBundles()[0];
        File.Copy(Path.Combine(Application.temporaryCachePath, fileName), targetPath, true);
        Debug.Log($"AssetBundle exported to: {targetPath}");
    }
}
