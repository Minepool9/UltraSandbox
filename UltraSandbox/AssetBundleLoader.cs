using BepInEx;
using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using Configgy;
using UltraSandbox;
using System.Timers;

namespace Secondultrakillmod
{
    [BepInPlugin("doomahreal.AssetBundleLoader", "AssetBundleLoader", "1.0.0")]
    [BepInDependency("Hydraxous.ULTRAKILL.Configgy", BepInDependency.DependencyFlags.HardDependency)]
    public class AssetBundleLoader : BaseUnityPlugin
    {
        private readonly Harmony harmony = new Harmony("doomahreal.AssetBundleLoader");

        public static AssetBundleLoader instance;

        private UIManager uiManager; // Instance of UIManager class

        private AssetsLogic al = new AssetsLogic();

        // Config builder instance
        private ConfigBuilder config;

        private static Timer timer = new Timer(1000);

        // Awake method called when the plugin is loaded
        void Awake()
        {
            // Initialize config builder
            config = new ConfigBuilder("doomahreal.AssetBundleLoader", "AssetBundleLoader");
            config.Build();
            if (instance == null) instance = this;

            al.TryStart();

            AttachInterrupts();

            timer.Elapsed += UpdateByConfigs;
            timer.Start();

            uiManager = gameObject.AddComponent<UIManager>(); // Instantiate UIManager

            // Patch the ExperimentalArmRotationPatch class
            harmony.PatchAll(typeof(ExperimentalArmRotationPatch));
        }

        internal InputAction buildInterrupt;
        internal InputAction switchInterrupt;
        internal InputAction scrollInterrupt;
        internal InputAction menuInterrupt;

        // attach all the interrupts to their actions
        void AttachInterrupts()
        {
            buildInterrupt = new InputAction(null, InputActionType.Value, "<Keyboard>/" + Configs.buildB.Value, null, null, null);
            buildInterrupt.performed += ShootRaycast;
            buildInterrupt.Enable();

            switchInterrupt = new InputAction(null, InputActionType.Value, "<Keyboard>/" + Configs.switchB.Value, null, null, null);
            switchInterrupt.performed += SwitchAssetBundle;
            switchInterrupt.Enable();

            scrollInterrupt = new InputAction(null, InputActionType.Value, "<Keyboard>/" + Configs.scrollB.Value, null, null, null);
            scrollInterrupt.performed += al.ScrollObjectList;
            scrollInterrupt.Enable();

            menuInterrupt = new InputAction(null, InputActionType.Value, "<Keyboard>/" + Configs.menuB.Value, null, null, null);
            scrollInterrupt.performed += uiManager.changeMenuState;
            scrollInterrupt.Enable();
        }

        void UpdateByConfigs(object sender, ElapsedEventArgs e)
        {
            buildInterrupt.ChangeBinding("<Keyboard>/" + Configs.buildB.Value);
            switchInterrupt.ChangeBinding("<Keyboard>/" + Configs.switchB.Value);
            scrollInterrupt.ChangeBinding("<Keyboard>/" + Configs.scrollB.Value);
            menuInterrupt.ChangeBinding("<Keyboard>/" + Configs.menuB.Value);
        }

        // Update method called once per frame
        void Update()
        {
            // Call the new function
            ShootRaycastWithRotation();
        }

        // Check if the current asset bundle is missing
        internal bool CheckForMissingBundle()
        {
            if (al.loadedObjects == null || al.loadedObjects.Length == 0)
            {
                MonoSingleton<HudMessageReceiver>.Instance.SendHudMessage("<color=red>Error:You dont either have a assetbundle in HotLoadedBundles or you dont even have assetbundles, stupid.</color>", "", "", 0, false);
                return false;
            }
            return true;
        }

        // Shoot a raycast to spawn objects
        void ShootRaycast(InputAction.CallbackContext obj)
        {
            if (!CheckForMissingBundle()) return;

            // for more readeability use early returns
            if (al.loadedObjects == null) return;
            if (al.loadedObjects.Length <= 0) return;
            if (al.currentObjectIndex < 0) return;
            if (al.currentObjectIndex >= al.loadedObjects.Length) return;

            Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
            RaycastHit hit;

            if (!Physics.Raycast(ray, out hit, float.MaxValue, LayerMaskDefaults.Get(LMD.Environment)))
            {
                Debug.Log("Raycast didn't hit anything.");
                return;
            }

            Debug.Log("Raycast hit object: " + hit.collider.gameObject.name);
            Bounds bounds = al.loadedObjects[al.currentObjectIndex].GetComponent<Renderer>().bounds;
            Vector3 spawnPosition = hit.point + hit.normal * bounds.extents.y;
            GameObject spawnedObject = Instantiate(al.loadedObjects[al.currentObjectIndex], spawnPosition, Quaternion.identity);
            spawnedObject.layer = 8;
            spawnedObject.AddComponent<Sandbox.SandboxProp>();
            Rigidbody rb = spawnedObject.AddComponent<Rigidbody>();
            rb.useGravity = true;
            al.placedObjects.Add(spawnedObject);
        }

        // Switch to the next asset bundle in the list
        void SwitchAssetBundle(InputAction.CallbackContext obj)
        {
            if (al.assetBundleNames.Count <= 0) return;

            int currentIndex = al.assetBundleNames.IndexOf(al.currentAssetBundleName);
            currentIndex = (currentIndex + 1) % al.assetBundleNames.Count;
            al.currentAssetBundleName = al.assetBundleNames[currentIndex];
            al.currentObjectIndex = 0; // Reset object index
            al.LoadObjectsFromAssetBundle(al.currentAssetBundleName);
        }

        // New function for shooting raycast with rotation
        void ShootRaycastWithRotation()
        {
            // Check if the Spawner Arm is active and the player is holding the left mouse button and the R button
            GameObject spawnerArm = GameObject.Find("Player/Main Camera/Guns/Spawner Arm(Clone) - MoveHand");
            if (spawnerArm == null || !Input.GetMouseButton(0) || !Input.GetKey(KeyCode.R)) return;

            // Shoot a raycast
            Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
            RaycastHit hit;

            if (!Physics.Raycast(ray, out hit, float.MaxValue, LayerMaskDefaults.Get(LMD.Environment))) return;

            // If the raycast hits an object with SandboxProp component
            if (hit.collider.gameObject.GetComponent<Sandbox.SandboxProp>() != null)
            {
                // Rotate the object based on mouse movement
                StartCoroutine(RotateObject(hit.collider.gameObject));
            }
        }

        // Coroutine to rotate the object based on mouse movement
        IEnumerator RotateObject(GameObject objToRotate)
        {
            // Record initial mouse position
            Vector3 initialMousePosition = Input.mousePosition;

            while (true)
            {
                // Calculate rotation based on mouse movement
                Vector3 mouseDelta = (Input.mousePosition - initialMousePosition) * 0.1f;
                objToRotate.transform.Rotate(Vector3.up, mouseDelta.x, Space.World);
                objToRotate.transform.Rotate(Vector3.right, -mouseDelta.y, Space.World);

                // Update initial mouse position
                initialMousePosition = Input.mousePosition;

                // Check if conditions have changed
                if (!Input.GetKey(KeyCode.R) || !Input.GetMouseButton(0) || GameObject.Find("Player/Main Camera/Guns/Spawner Arm(Clone) - MoveHand") == null)
                {
                    yield break;
                }

                yield return null;
            }
        }
    }
}