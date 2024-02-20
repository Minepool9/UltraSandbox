using BepInEx;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Linq; // Added for LINQ
using Configgy;

namespace Secondultrakillmod
{
    [BepInPlugin("doomahreal.ultrakill.Assetbundleloader", "Assetbundleloader", "1.0.0")]
    [BepInDependency("Hydraxous.ULTRAKILL.Configgy", BepInDependency.DependencyFlags.HardDependency)] 
    public class Assetbundleloader : BaseUnityPlugin
    {
        // Dictionary to store loaded GameObjects from each asset bundle
        private Dictionary<string, List<GameObject>> loadedObjectsDict = new Dictionary<string, List<GameObject>>();

        // List to keep track of placed objects
        private List<GameObject> placedObjects = new List<GameObject>();

        // Index of the currently selected object
        private int currentObjectIndex = 0;

        // Name of the currently selected asset bundle
        private string currentAssetBundleName;

        // List to store names of all loaded asset bundles
        private List<string> assetBundleNames = new List<string>();

        // Store loaded objects
        private GameObject[] loadedObjects;

        // Keybind for the "Build" button
        [Configgable("", "Build button")]
        private static ConfigKeybind Keybind = new ConfigKeybind(KeyCode.X);
        
        // Keybind for switching asset bundles
        [Configgable("", "Switch asset bundle")]
        private static ConfigKeybind Keybind1 = new ConfigKeybind(KeyCode.N);

        // Keybind for scrolling through the object list
        [Configgable("", "Scroll through object list")]
        private static ConfigKeybind Keybind3 = new ConfigKeybind(KeyCode.C);

        // Config builder instance
        public static ConfigBuilder ConfigBuilder { get; private set; }
        
        // Awake method called when the plugin is loaded
        void Awake()
        {
            // Initialize config builder
            ConfigBuilder = new ConfigBuilder("doomahreal.ultrakill.Assetbundleloader", "Assetbundleloader");
            ConfigBuilder.Build();

            // Load all asset bundles asynchronously
            StartCoroutine(LoadAllAssetBundles());
        }

        // Coroutine to load all asset bundles
        IEnumerator LoadAllAssetBundles()
        {
            // Get the directory containing the asset bundles
            string bundlesDirectory = GetBundlesDirectory();
            if (bundlesDirectory == null)
                yield break;

            // Load each asset bundle in the directory
            foreach (string bundlePath in Directory.GetFiles(bundlesDirectory, "*.bundle"))
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

        // Get the directory containing the asset bundles
        string GetBundlesDirectory()
        {
            string pluginDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string parentDirectory = Path.GetDirectoryName(pluginDirectory);
            string bundlesDirectory = Path.Combine(parentDirectory, "HotLoadedBundles");

            if (!Directory.Exists(bundlesDirectory))
            {
                Debug.Log("Folder 'HotLoadedBundles' not found. Please check if you have the folder with the mod or an asset bundle.");
                return null;
            }

            return bundlesDirectory;
        }

        // Load objects from the specified asset bundle
        void LoadObjectsFromAssetBundle(string bundleName)
        {
            if (loadedObjectsDict.TryGetValue(bundleName, out List<GameObject> prefabList))
            {
                loadedObjects = prefabList.ToArray();
                InstantiateCurrentObject();
            }
            else
            {
                Debug.Log("Asset bundle not found: " + bundleName);
            }
        }

        // Update method called once per frame
        void Update()
        {
            if (Input.GetKeyDown(Keybind.Value))
            {
                if (CheckForMissingBundle())
                {
                    ShootRaycast();
                }
            }

            if (Input.GetKeyDown(Keybind1.Value))
            {
                SwitchAssetBundle();
            }

            if (Input.GetKeyDown(Keybind3.Value))
            {
                if (CheckForMissingBundle())
                {
                    ScrollObjectList(1);
                }
            }
        }

        // Check if the current asset bundle is missing
        bool CheckForMissingBundle()
        {
            if (loadedObjects == null || loadedObjects.Length == 0)
            {
                MonoSingleton<HudMessageReceiver>.Instance.SendHudMessage("<colour=red>Error:You dont either have a assetbundle in HotLoadedBundles or you dont even have assetbundles, stupid.</colour>", "", "", 0, false);
                return false;
            }
            return true;
        }

        // Shoot a raycast to spawn objects
        void ShootRaycast()
        {
            if (loadedObjects != null && loadedObjects.Length > 0 && currentObjectIndex >= 0 && currentObjectIndex < loadedObjects.Length)
            {
                Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit, float.MaxValue, LayerMaskDefaults.Get(LMD.Environment)))
                {
                    Debug.Log("Raycast hit object: " + hit.collider.gameObject.name);
                    Bounds bounds = loadedObjects[currentObjectIndex].GetComponent<Renderer>().bounds;
                    Vector3 spawnPosition = hit.point + hit.normal * bounds.extents.y;
                    GameObject spawnedObject = Instantiate(loadedObjects[currentObjectIndex], spawnPosition, Quaternion.identity);
                    spawnedObject.layer = 8;
                    spawnedObject.AddComponent<Sandbox.SandboxProp>();
                    Rigidbody rb = spawnedObject.AddComponent<Rigidbody>();
                    rb.useGravity = true;
                    placedObjects.Add(spawnedObject);
                }
                else
                {
                    Debug.Log("Raycast didn't hit anything.");
                }
            }
        }

        // Scroll through the object list
        void ScrollObjectList(int direction)
        {
            currentObjectIndex = (currentObjectIndex + direction + loadedObjects.Length) % loadedObjects.Length;
            SendHudMessage();
        }

        // Instantiate the currently selected object
        void InstantiateCurrentObject()
        {
            if (loadedObjects != null && loadedObjects.Length > 0 && currentObjectIndex >= 0 && currentObjectIndex < loadedObjects.Length)
            {
                GameObject instantiatedObject = Instantiate(loadedObjects[currentObjectIndex], Vector3.zero, Quaternion.identity);
                Rigidbody rb = instantiatedObject.AddComponent<Rigidbody>();
                rb.useGravity = true;
                placedObjects.Add(instantiatedObject);
            }
        }

        // Send HUD message with the name of the currently selected object
        void SendHudMessage()
        {
            if (loadedObjects != null && loadedObjects.Length > 0 && currentObjectIndex >= 0 && currentObjectIndex < loadedObjects.Length)
            {
                string selectedObjectName = loadedObjects[currentObjectIndex].name;
                MonoSingleton<HudMessageReceiver>.Instance.SendHudMessage("Current object is: " + selectedObjectName, "", "", 0, false);
            }
        }

        // Switch to the next asset bundle
        void SwitchAssetBundle()
        {
            if (assetBundleNames.Count > 1)
            {
                int currentIndex = assetBundleNames.IndexOf(currentAssetBundleName);
                int nextIndex = (currentIndex + 1) % assetBundleNames.Count;
                currentAssetBundleName = assetBundleNames[nextIndex];
                currentObjectIndex = 0; // Reset object index
                LoadObjectsFromAssetBundle(currentAssetBundleName);
            }
            else
            {
                Debug.Log("No other asset bundles available to switch to.");
            }
        }
    }
}
