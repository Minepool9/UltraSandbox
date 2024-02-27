using UnityEngine;
using UnityEditor;
using System.IO;

public class CreateAssetBundles : EditorWindow
{
    [MenuItem("Window/Create Asset Bundles")]
    static void Init()
    {
        CreateAssetBundles window = (CreateAssetBundles)EditorWindow.GetWindow(typeof(CreateAssetBundles));
        window.Show();
    }

    void OnGUI()
    {
        GUILayout.Label("Create Asset Bundles", EditorStyles.boldLabel);

        if (GUILayout.Button("Create Bundles"))
        {
            CreateBundles();
        }
    }

    static void CreateBundles()
    {
        string bundleDirectory = "Exportedbundles"; // Change this to match your folder name
        string outputPath = Path.Combine(Application.dataPath, "..", bundleDirectory);

        if (!Directory.Exists(outputPath))
        {
            Directory.CreateDirectory(outputPath);
        }

        // Delete any existing bundle files with the same name and extension
        DeleteExistingBundles(outputPath);

        BuildPipeline.BuildAssetBundles(outputPath, BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows);

        // Add .bundle extension to asset bundle files
        AddBundleExtension(outputPath);

        // Delete ExportedBundles.bundle and ExportedBundles.manifest.bundle files if they exist
        DeleteBundleFile(outputPath, "ExportedBundles.bundle");
        DeleteBundleFile(outputPath, "ExportedBundles.manifest.bundle");

        Debug.Log("Asset bundles created at: " + outputPath);
    }

    static void DeleteExistingBundles(string directory)
    {
        string[] bundleFiles = Directory.GetFiles(directory, "*.bundle");

        foreach (string filePath in bundleFiles)
        {
            File.Delete(filePath);
        }
    }

    static void AddBundleExtension(string directory)
    {
        string[] bundleFiles = Directory.GetFiles(directory, "*");

        foreach (string filePath in bundleFiles)
        {
            if (!filePath.EndsWith(".bundle"))
            {
                string newFilePath = filePath + ".bundle";
                File.Move(filePath, newFilePath);
            }
        }
    }

    static void DeleteBundleFile(string directory, string fileName)
    {
        string filePath = Path.Combine(directory, fileName);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }
}
