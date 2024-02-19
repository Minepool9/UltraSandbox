using UnityEngine;
using UnityEditor;
using System.IO;

public class CreateAssetBundles : EditorWindow
{
    [MenuItem("Custom/Create Asset Bundles")]
    static void CreateBundles()
    {
        string bundleDirectory = "Exportedbundles"; // Change this to match your folder name
        string outputPath = Path.Combine(Application.dataPath, "..", bundleDirectory);

        if (!Directory.Exists(outputPath))
        {
            Directory.CreateDirectory(outputPath);
        }

        BuildPipeline.BuildAssetBundles(outputPath, BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows);
        Debug.Log("Asset bundles created at: " + outputPath);
    }
}
