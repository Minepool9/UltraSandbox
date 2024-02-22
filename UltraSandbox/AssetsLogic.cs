using Secondultrakillmod;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;

namespace UltraSandbox
{
    internal class AssetsLogic : MonoBehaviour
    {
        // Dictionary to store loaded GameObjects from each asset bundle
        internal Dictionary<string, List<GameObject>> loadedObjectsDict = new Dictionary<string, List<GameObject>>();

        // List to keep track of placed objects
        internal List<GameObject> placedObjects = new List<GameObject>();

        // Index of the currently selected object
        internal int currentObjectIndex = 0;

        // Name of the currently selected asset bundle
        internal string currentAssetBundleName;

        // List to store names of all loaded asset bundles
        internal List<string> assetBundleNames = new List<string>();

        // Store loaded objects
        internal GameObject[] loadedObjects;

        internal void TryStart()
        {
            // Check bundle path
            string pluginDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string parentDirectory = Path.GetDirectoryName(pluginDirectory);
            string bundlesDirectory = Path.Combine(parentDirectory, "HotLoadedBundles");

            if (!Directory.Exists(bundlesDirectory))
            {
                Debug.Log("Folder 'HotLoadedBundles' not found. Please check if you have the folder with the mod or an asset bundle.");
                return;
            }
            // Load all asset bundles asynchronously
            StartCoroutine(LoadAllAssetBundles(bundlesDirectory));
        }

        // Coroutine to load all asset bundles
        IEnumerator LoadAllAssetBundles(string dir)
        {
            // Load each asset bundle in the directory
            foreach (string bundlePath in Directory.GetFiles(dir, "*.bundle"))
            {
                yield return LoadAssetBundle(bundlePath);
            }

            // Set the first loaded asset bundle as the current one
            if (loadedObjectsDict.Count > 0)
            {
                currentAssetBundleName = loadedObjectsDict.Keys.OrderBy(k => k).First();
                currentObjectIndex = 0; // Reset object index
                LoadObjectsFromAssetBundle(currentAssetBundleName);
            }
            else
            {
                Debug.Log("No asset bundles found in the HotLoadedBundles folder.");
            }
        }

        // Coroutine to load an individual asset bundle
        IEnumerator LoadAssetBundle(string bundlePath)
        {
            var assetBundleRequest = AssetBundle.LoadFromFileAsync(bundlePath);
            yield return assetBundleRequest;

            AssetBundle assetBundle = assetBundleRequest.assetBundle;
            string bundleName = Path.GetFileNameWithoutExtension(bundlePath);

            List<GameObject> prefabList = new List<GameObject>();
            foreach (string name in assetBundle.GetAllAssetNames())
            {
                if (name.EndsWith(".prefab"))
                {
                    var assetLoadRequest = assetBundle.LoadAssetAsync<GameObject>(name);
                    yield return assetLoadRequest;
                    prefabList.Add(assetLoadRequest.asset as GameObject);
                }
            }

            loadedObjectsDict.Add(bundleName, prefabList);
            assetBundleNames.Add(bundleName); // Add bundle name to the list
            assetBundle.Unload(false);
        }

        // Load objects from the specified asset bundle
        internal void LoadObjectsFromAssetBundle(string bundleName)
        {
            if (!loadedObjectsDict.TryGetValue(bundleName, out List<GameObject> prefabList))
            {
                Debug.Log("Asset bundle not found: " + bundleName);
                return;
            }
            loadedObjects = prefabList.ToArray();
            InstantiateCurrentObject();
        }

        // Instantiate the currently selected object
        internal void InstantiateCurrentObject()
        {
            if (loadedObjects != null && loadedObjects.Length > 0 && currentObjectIndex >= 0 && currentObjectIndex < loadedObjects.Length)
            {
                MonoSingleton<HudMessageReceiver>.Instance.SendHudMessage("<color=green>Currently Selected Object: </color>" + loadedObjects[currentObjectIndex].name, "", "", 0, false);
            }
        }

        // Function to scroll through the list of objects
        internal void ScrollObjectList(InputAction.CallbackContext obj)
        {
            if (!AssetBundleLoader.instance.CheckForMissingBundle()) return;
            currentObjectIndex += 1;
            if (currentObjectIndex < 0)
                currentObjectIndex = loadedObjects.Length - 1;
            if (currentObjectIndex >= loadedObjects.Length)
                currentObjectIndex = 0;

            InstantiateCurrentObject();
        }
    }
}
