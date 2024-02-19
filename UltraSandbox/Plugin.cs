using BepInEx;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using Configgy;

namespace Secondultrakillmod
{
    [BepInPlugin("doomahreal.ultrakill.Assetbundleloader", "Assetbundleloader", "1.0.0")]
    [BepInDependency("Hydraxous.ULTRAKILL.Configgy", BepInDependency.DependencyFlags.HardDependency)] 
    public class Assetbundleloader : BaseUnityPlugin
    {
        public string assetBundleName = "testassets"; // Name of the asset bundle
        public string[] assetNames; // Names of the assets within the bundle
        private GameObject[] loadedObjects; // Loaded GameObjects
        private List<GameObject> placedObjects = new List<GameObject>(); // Keep track of placed objects
        private int currentObjectIndex = 0; // Index of the currently selected object

        [Configgable("", "Build button")]
        private static ConfigKeybind Keybind = new ConfigKeybind(KeyCode.X);
        
        [Configgable("", "Undo")]
        private static ConfigKeybind Keybind2 = new ConfigKeybind(KeyCode.Z);
        
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
            StartCoroutine(LoadAssetBundle());
        }

        IEnumerator LoadAssetBundle()
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

            // Construct the path to the asset bundle inside the HotLoadedBundles folder
            string assetBundlePath = Path.Combine(bundlesDirectory, assetBundleName);
            Debug.Log("Asset bundle path: " + assetBundlePath);

            // Check if the asset bundle file exists
            if (!File.Exists(assetBundlePath))
            {
                // Send a HUD message indicating missing asset bundle
                Debug.Log("Asset bundle file not found. Please check if you have the folder with the mod or an asset bundle.");
                yield break; // Exit coroutine early
            }

            // Load the asset bundle
            var assetBundleRequest = AssetBundle.LoadFromFileAsync(assetBundlePath);
            yield return assetBundleRequest;

            // Get the loaded asset bundle
            AssetBundle assetBundle = assetBundleRequest.assetBundle;

            // Load the asset names from the bundle
            assetNames = assetBundle.GetAllAssetNames();

            // Filter only the prefab asset names
            List<string> prefabNames = new List<string>();
            foreach (string name in assetNames)
            {
                if (name.EndsWith(".prefab"))
                {
                    prefabNames.Add(name);
                }
            }

            // Load the GameObjects from the bundle
            loadedObjects = new GameObject[prefabNames.Count];
            for (int i = 0; i < prefabNames.Count; i++)
            {
                var assetLoadRequest = assetBundle.LoadAssetAsync<GameObject>(prefabNames[i]);
                yield return assetLoadRequest;
                loadedObjects[i] = assetLoadRequest.asset as GameObject;
            }

            // Unload the asset bundle
            assetBundle.Unload(false);

            // Instantiate the first GameObject at a default position
            InstantiateCurrentObject();
        }

        void Update()
        {
            // Check if Numpad 1 key is pressed
            if (Input.GetKeyDown(Keybind.Value))
            {
                if (CheckForMissingBundle())
                {
                    ShootRaycast();
                }
            }

            // Check if Z key is pressed to delete the last placed object
            if (Input.GetKeyDown(Keybind2.Value))
            {
                if (CheckForMissingBundle())
                {
                    DeleteLastPlacedObject();
                }
            }

            // Check if Numpad + key is pressed to scroll up the list
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

        void DeleteLastPlacedObject()
        {
            if (placedObjects.Count > 0)
            {
                GameObject lastPlacedObject = placedObjects[placedObjects.Count - 1];
                placedObjects.RemoveAt(placedObjects.Count - 1);
                Destroy(lastPlacedObject);

                // Send a HUD message indicating the last object was undone
                MonoSingleton<HudMessageReceiver>.Instance.SendHudMessage("Undid last object", "", "", 0, true);
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
                // Filter assetNames array to include only .prefab files
                List<string> prefabNames = new List<string>();
                foreach (string name in assetNames)
                {
                    if (name.EndsWith(".prefab"))
                    {
                        prefabNames.Add(name);
                    }
                }

                // Get the name of the currently selected .prefab object
                string selectedObjectName = Path.GetFileNameWithoutExtension(prefabNames[currentObjectIndex]);

                // Send the HUD message with the name of the currently selected object
                MonoSingleton<HudMessageReceiver>.Instance.SendHudMessage("Current object is: " + selectedObjectName, "", "", 0, false);
            }
        }
    }
}
