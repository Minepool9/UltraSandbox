using UnityEngine;
using BepInEx;
using HarmonyLib;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Linq; // Added for LINQ
using Configgy;

namespace Secondultrakillmod
{
    public class UIManager : MonoBehaviour
    {
        private static UIManager instance;
        private GameObject customMenu;
        private GameObject customCanvas;
		public bool isMenuOpen = false;

        // Add this property to provide access to isMenuOpen
        public bool IsMenuOpen
        {
            get { return isMenuOpen; }
            set { isMenuOpen = value; }
        }

        void Awake()
        {
            instance = this;
            customMenu = new GameObject("Custom Menu");
            DontDestroyOnLoad(customMenu);

            LoadUIBundle();
            InstantiateCustomCanvas();
            InitializeMenuState();
        }

        void LoadUIBundle()
        {
            // Get the assembly where the script is located
            Assembly assembly = Assembly.GetExecutingAssembly();

            // Load the UI bundle from embedded resources
            using (Stream stream = assembly.GetManifestResourceStream("Assetbundleloader.ui.bundle"))
            {
                if (stream != null)
                {
                    byte[] bundleData = new byte[stream.Length];
                    stream.Read(bundleData, 0, bundleData.Length);

                    // Load the AssetBundle from memory
                    AssetBundle uiBundle = AssetBundle.LoadFromMemory(bundleData);

                    if (uiBundle != null)
                    {
                        // Load necessary assets from the bundle
                        GameObject prefab = uiBundle.LoadAsset<GameObject>("CustomCanvas");

                        // You might want to check if prefab is not null before instantiating
                        if (prefab != null)
                        {
                            customCanvas = Instantiate(prefab, customMenu.transform);
                            customCanvas.SetActive(false);
                        }
                        else
                        {
                            Debug.LogError("Failed to load prefab from UI bundle.");
                        }

                        // Unload the AssetBundle to free up memory
                        uiBundle.Unload(false);
                    }
                    else
                    {
                        Debug.LogError("Failed to load UI bundle.");
                    }
                }
                else
                {
                    Debug.LogError("UI bundle resource not found.");
                }
            }
        }

        void InstantiateCustomCanvas()
        {
            customCanvas = Instantiate(Resources.Load<GameObject>("CustomCanvas"), customMenu.transform);
            customCanvas.SetActive(false);

            // Get the button component from the customCanvas
            Button closeButton = customCanvas.GetComponentInChildren<Button>();
            if (closeButton != null)
            {
                // Add a listener to the button's click event
                closeButton.onClick.AddListener(CloseMenu);
            }
            else
            {
                Debug.LogWarning("Button not found in CustomCanvas.");
            }
        }

        void InitializeMenuState()
        {
            customMenu.SetActive(true);
            customCanvas.SetActive(false);
        }

        public void OpenMenu()
        {
            if (!isMenuOpen)
            {
                isMenuOpen = true;
                customCanvas.SetActive(true);
                PlayAnimation("openanimation");
            }
        }

        public void CloseMenu()
        {
            if (isMenuOpen)
            {
                isMenuOpen = false;
                PlayAnimation("closeanimation");
                customCanvas.SetActive(false);
            }
        }

        void PlayAnimation(string animationName)
        {
            Animator animator = customCanvas.GetComponent<Animator>();
            if (animator != null)
            {
                animator.Play(animationName);
            }
            else
            {
                Debug.LogWarning("Animator component not found on CustomCanvas.");
            }
        }
    }
}
