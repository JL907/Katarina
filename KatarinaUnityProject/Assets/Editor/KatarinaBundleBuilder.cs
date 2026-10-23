using System.IO;
using UnityEditor;
using UnityEngine;

public static class KatarinaBundleBuilder
{
    private const string BundleName = "katarina";
    private const string OutputFolder = "Library/KatarinaAssetBundles";
    private const string ModFolder = "../KatarinaMod";

    [MenuItem("Katarina/Build Katarina Asset Bundle")]
    public static void BuildAndCopy()
    {
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string modDirectory = Path.GetFullPath(Path.Combine(projectRoot, ModFolder));
        if (!Directory.Exists(modDirectory))
        {
            EditorUtility.DisplayDialog("Build Katarina Asset Bundle", "Couldn't find the mod folder:\n" + modDirectory, "OK");
            return;
        }

        AssetDatabase.SaveAssets();
        string outputDirectory = Path.Combine(projectRoot, OutputFolder);
        Directory.CreateDirectory(outputDirectory);

        AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles(
            outputDirectory,
            BuildAssetBundleOptions.ChunkBasedCompression,
            BuildTarget.StandaloneWindows64);

        string builtBundle = Path.Combine(outputDirectory, BundleName);
        if (manifest == null || !File.Exists(builtBundle))
        {
            EditorUtility.DisplayDialog("Build Katarina Asset Bundle", "The asset bundle build failed. Check the Console for errors.", "OK");
            return;
        }

        string destination = Path.Combine(modDirectory, BundleName);
        File.Copy(builtBundle, destination, true);

        long size = new FileInfo(destination).Length;
        Debug.Log("Built '" + BundleName + "' (" + (size / 1024f / 1024f).ToString("0.0") + " MB) and copied it to " + destination + ". Rebuild KatarinaMod to embed it.");
    }
}
