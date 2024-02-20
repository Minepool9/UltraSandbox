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
        }

        void Start()
        {
            StartCoroutine(LoadAssetBundles());
        }

        IEnumerator LoadAssetBundles()
        {
            // Get the directory where the plugin DLL is located
            string pluginDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Debug.Log("Plugin directory: " + pluginDirectory);

            // Go back one directory level (assuming the plugin is inside a "plugins" folder)
            string parentDirectory = Path.GetDirectoryName(pluginDirectory);
            Debug.Log("Parent directory: " + parentDirectory);

            // Construct the path to the HotLoadedBundles folder
            string bundlesDirectory = Path.Combine(parentDirectory, "HotLoadedBundles");
            Debug.Log("Bundles directory: " + bundlesDirectory);

            // Check if the HotLoadedBundles folder exists
            if (!Directory.Exists(bundlesDirectory))
            {
                // Send a HUD message indicating missing folder
                Debug.Log("Folder 'HotLoadedBundles' not found. Please check if you have the folder with the mod or an asset bundle.");
                yield break; // Exit coroutine early
            }

            // Load all asset bundles in the HotLoadedBundles folder
            string[] bundlePaths = Directory.GetFiles(bundlesDirectory, "*.bundle");
            foreach (string bundlePath in bundlePaths)
            {
                // Load the asset bundle
                var assetBundleRequest = AssetBundle.LoadFromFileAsync(bundlePath);
                yield return assetBundleRequest;

                // Get the loaded asset bundle
                AssetBundle assetBundle = assetBundleRequest.assetBundle;

                // Get the name of the asset bundle
                string bundleName = Path.GetFileNameWithoutExtension(bundlePath);

                // Load the asset names from the bundle
                string[] assetNames = assetBundle.GetAllAssetNames();

                // Filter only the prefab asset names
                List<GameObject> prefabList = new List<GameObject>();
                foreach (string name in assetNames)
                {
                    if (name.EndsWith(".prefab"))
                    {
                        var assetLoadRequest = assetBundle.LoadAssetAsync<GameObject>(name);
                        yield return assetLoadRequest;
                        GameObject prefab = assetLoadRequest.asset as GameObject;
                        prefabList.Add(prefab);
                    }
                }

                // Add the loaded GameObjects to the dictionary
                loadedObjectsDict.Add(bundleName, prefabList);

                // Unload the asset bundle
                assetBundle.Unload(false);
            }

            // Load the first asset bundle in the dictionary
            if (loadedObjectsDict.Count > 0)
            {
                currentAssetBundleName = loadedObjectsDict.Keys.First();
                currentObjectIndex = 0; // Reset object index
                LoadObjectsFromAssetBundle(currentAssetBundleName);
            }
            else
            {
                Debug.Log("No asset bundles found in the HotLoadedBundles folder.");
            }
        }

        void LoadObjectsFromAssetBundle(string bundleName)
        {
            if (loadedObjectsDict.TryGetValue(bundleName, out List<GameObject> prefabList))
            {
                // Get the list of loaded GameObjects
                loadedObjects = prefabList.ToArray();

                // Instantiate the first GameObject at a default position
                InstantiateCurrentObject();
            }
            else
            {
                Debug.Log("Asset bundle not found: " + bundleName);
            }
        }

        void Update()
        {
            // Check if Build button key is pressed
            if (Input.GetKeyDown(Keybind.Value))
            {
                if (CheckForMissingBundle())
                {
                    ShootRaycast();
                }
            }

            // Check if Switch asset bundle key is pressed
            if (Input.GetKeyDown(Keybind1.Value))
            {
                SwitchAssetBundle();
            }

            // Check if Scroll key is pressed to scroll through object list
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
                // Send a HUD message indicating missing asset bundle
                MonoSingleton<HudMessageReceiver>.Instance.SendHudMessage("<color=red>Error: Asset bundle not loaded. Please check if you have the folder with the mod or an asset bundle.</color>", "", "", 0, false);
                return false;
            }
            return true;
        }

        void ShootRaycast()
        {
            if (loadedObjects != null && loadedObjects.Length > 0 && currentObjectIndex >= 0 && currentObjectIndex < loadedObjects.Length)
            {
                // Raycast from the camera's position forward
                Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit, float.MaxValue, LayerMaskDefaults.Get(LMD.Environment)))
                {
                    Debug.Log("Raycast hit object: " + hit.collider.gameObject.name);
                    Bounds bounds = loadedObjects[currentObjectIndex].GetComponent<Renderer>().bounds;
                    Vector3 spawnPosition = hit.point + hit.normal * bounds.extents.y;
                    GameObject spawnedObject = Instantiate(loadedObjects[currentObjectIndex], spawnPosition, Quaternion.identity);
                    spawnedObject.layer = 8;

                    // Add the Sandbox.SandboxProp component to the spawned object
                    spawnedObject.AddComponent<Sandbox.SandboxProp>();
					
                    // Add Rigidbody component to the spawned object
                    Rigidbody rb = spawnedObject.AddComponent<Rigidbody>();
                    // Set the Rigidbody's useGravity property to true
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
            // Increment or decrement the current object index based on the direction
            currentObjectIndex = (currentObjectIndex + direction + loadedObjects.Length) % loadedObjects.Length;

            // Send a HUD message with the name of the currently selected object
            SendHudMessage();
        }

        void InstantiateCurrentObject()
        {
            if (loadedObjects != null && loadedObjects.Length > 0 && currentObjectIndex >= 0 && currentObjectIndex < loadedObjects.Length)
            {
                // Instantiate the selected GameObject at a default position
                GameObject instantiatedObject = Instantiate(loadedObjects[currentObjectIndex], Vector3.zero, Quaternion.identity);
                
                // Add a Rigidbody component to the instantiated object
                Rigidbody rb = instantiatedObject.AddComponent<Rigidbody>();
                // Set the Rigidbody's useGravity property to true
                rb.useGravity = true;

                // Add the instantiated object to the list of placed objects
                placedObjects.Add(instantiatedObject);
            }
        }

        void SendHudMessage()
        {
            if (loadedObjects != null && loadedObjects.Length > 0 && currentObjectIndex >= 0 && currentObjectIndex < loadedObjects.Length)
            {
                // Get the name of the currently selected object
                string selectedObjectName = loadedObjects[currentObjectIndex].name;

                // Send the HUD message with the name of the currently selected object
                MonoSingleton<HudMessageReceiver>.Instance.SendHudMessage("Current object is: " + selectedObjectName, "", "", 0, false);
            }
        }

		void SwitchAssetBundle()
		{
			// Get the directory where the plugin DLL is located
			string pluginDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			Debug.Log("Plugin directory: " + pluginDirectory);

			// Go back one directory level (assuming the plugin is inside a "plugins" folder)
			string parentDirectory = Path.GetDirectoryName(pluginDirectory);
			Debug.Log("Parent directory: " + parentDirectory);

			// Construct the path to the HotLoadedBundles folder
			string bundlesDirectory = Path.Combine(parentDirectory, "HotLoadedBundles");
			Debug.Log("Bundles directory: " + bundlesDirectory);

			// Get all asset bundle files in the HotLoadedBundles folder
			string[] bundleFiles = Directory.GetFiles(bundlesDirectory, "*.bundle");

			// Filter out the currently loaded asset bundle
			string currentBundleName = currentAssetBundleName + ".bundle";
			List<string> availableBundles = new List<string>();
			foreach (string bundleFile in bundleFiles)
			{
				if (Path.GetFileName(bundleFile) != currentBundleName)
				{
					availableBundles.Add(bundleFile);
				}
			}

			// Check if there are any available asset bundles to switch to
			if (availableBundles.Count > 0)
			{
				// Randomly select an asset bundle from the available ones
				int randomIndex = Random.Range(0, availableBundles.Count);
				string randomBundlePath = availableBundles[randomIndex];

				// Extract the name of the selected bundle
				string randomBundleName = Path.GetFileNameWithoutExtension(randomBundlePath);

				// Load the selected asset bundle
				StartCoroutine(LoadAssetBundles()); // Corrected method call
			}
			else
			{
				Debug.Log("No other asset bundles available to switch to.");
			}
		}
    }
}
