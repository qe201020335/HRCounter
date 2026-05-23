using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BGLib.Polyglot;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace HRCounter.Patches;

[HarmonyPatch(typeof(LocalizationAsyncInstaller), nameof(LocalizationAsyncInstaller.LoadResourcesBeforeInstall))]
internal static class LocalizationPatch
{
    private static Harmony? _harmony;

    public static void Patch()
    {
        _harmony ??= Harmony.CreateAndPatchAll(typeof(LocalizationPatch), "com.github.qe201020335.HRCounter.LocalizationPatch");
    }

    private static TextAsset? _asset;

    [HarmonyPrepare]
    [UsedImplicitly]
    private static bool Prepare()
    {
        if (_asset != null) return true;

        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("HRCounter.Resources.Localization.csv");
        if (stream == null) // really should not happen
        {
            Plugin.Logger.Error("Failed to load localization csv text from resource!");
            return false;
        }

        using var reader = new StreamReader(stream);

        var content = reader.ReadToEnd();
        _asset = new TextAsset(content);
        return true;
    }

    [HarmonyPrefix]
    [UsedImplicitly]
    private static void Prefix(IList<TextAsset> assets)
    {
        assets.Add(_asset!);
    }
}
