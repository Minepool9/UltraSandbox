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
        private Dictionary<string, List<GameObject>> loadedObjectsDict = new Dictionary<string, List<GameObject>>(); // Dictionary to store loaded GameObjects from each asset bundle
        private List<GameObject> placedObjects = new List<GameObject>(); // Keep track of placed objects
        private int currentObjectIndex = 0; // Index of the currently selected object
        private string currentAssetBundleName; // Name of the currently selected asset bundle
        private List<string> assetBundleNames = new List<string>(); // List to store names of all loaded asset bundles
        private GameObject[] loadedObjects; // Store loaded objects
        
        [Configgable("", "Build button")]
        private static ConfigKeybind Keybind = new ConfigKeybind(KeyCode.X);
        
        [Configgable("", "Switch asset bundle")]
        private static ConfigKeybind Keybind1 = new ConfigKeybind(KeyCode.N);

        [Configgable("", "Scroll through object list")]
        private static ConfigKeybind Keybind3 = new ConfigKeybind(KeyCode.C);

        public static ConfigBuilder ConfigBuilder { get; private set; }
        
        void Awake()
        {
            ConfigBuilder = new ConfigBuilder("doomahreal.ultrakill.Assetbundleloader", "Assetbundleloader");
            ConfigBuilder.Build();

            StartCoroutine(LoadAllAssetBundles());
        }

        IEnumerator LoadAllAssetBundles()
        {
            string bundlesDirectory = GetBundlesDirectory();
            if (bundlesDirectory == null)
                yield break;

            foreach (string bundlePath in Directory.GetFiles(bundlesDirectory, "*.bundle"))
            {
                yield return LoadAssetBundle(bundlePath);
            }

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

        bool CheckForMissingBundle()
        {
            if (loadedObjects == null || loadedObjects.Length == 0)
            {
                Debug.Log("<color=red>Error: Asset bundle not loaded. Please check if you have the folder with the mod or an asset bundle.</color>");
                return false;
            }
            return true;
        }

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

        void ScrollObjectList(int direction)
        {
            currentObjectIndex = (currentObjectIndex + direction + loadedObjects.Length) % loadedObjects.Length;
            SendHudMessage();
        }

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

        void SendHudMessage()
        {
            if (loadedObjects != null && loadedObjects.Length > 0 && currentObjectIndex >= 0 && currentObjectIndex < loadedObjects.Length)
            {
                string selectedObjectName = loadedObjects[currentObjectIndex].name;
                MonoSingleton<HudMessageReceiver>.Instance.SendHudMessage("Current object is: " + selectedObjectName, "", "", 0, false);
            }
        }

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
