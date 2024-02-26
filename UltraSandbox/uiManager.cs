using UnityEngine;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using System.Collections;
using System.IO;
using System.Reflection;

namespace Secondultrakillmod
{
    public class UIManager : MonoBehaviour
    {
        private static UIManager instance;
        private GameObject customMenu; // Unused field
        private GameObject customCanvas;
        private GameObject customScroll;
        private GameObject objectButtonPrefab; // New field to store Objectbutton prefab
        public bool isMenuOpen = false;
        private bool uiBundleLoaded = false;

        void Awake()
        {
            instance = this;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            StartCoroutine(InitializeUIAfterDelay());
        }

        IEnumerator InitializeUIAfterDelay()
        {
            yield return new WaitForSeconds(1); // Adjust the delay as needed
            LoadUIBundle();
            while (!uiBundleLoaded)
                yield return null; // Wait until the UI bundle is fully loaded
            InstantiateCustomCanvas();
            InstantiateCustomScroll();
            PopulateObjectButtons();
            InitializeMenuState();
        }

        void LoadUIBundle()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            using (Stream stream = assembly.GetManifestResourceStream("Assetbundleloader.ui.bundle"))
            {
                if (stream != null)
                {
                    byte[] bundleData = new byte[stream.Length];
                    stream.Read(bundleData, 0, bundleData.Length);

                    AssetBundle uiBundle = AssetBundle.LoadFromMemory(bundleData);

                    if (uiBundle != null)
                    {
                        GameObject prefab = uiBundle.LoadAsset<GameObject>("CustomCanvas");

                        if (prefab != null)
                        {
                            customCanvas = Instantiate(prefab);
                            customCanvas.SetActive(false);
                        }
                        else
                        {
                            Debug.LogError("Failed to load prefab from UI bundle.");
                        }

                        // Load CustomScroll here
                        prefab = uiBundle.LoadAsset<GameObject>("CustomScroll");
                        if (prefab != null)
                        {
                            customScroll = Instantiate(prefab);
                            customScroll.SetActive(false);
                        }
                        else
                        {
                            Debug.LogError("Failed to load CustomScroll prefab from UI bundle.");
                        }

                        // Load Objectbutton prefab
                        prefab = uiBundle.LoadAsset<GameObject>("Objectbutton");
                        if (prefab != null)
                        {
                            objectButtonPrefab = prefab;
                        }
                        else
                        {
                            Debug.LogError("Failed to load Objectbutton prefab from UI bundle.");
                        }

                        uiBundle.Unload(false);
                        uiBundleLoaded = true; // Set the flag to true when UI bundle is loaded
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
            customCanvas = Instantiate(Resources.Load<GameObject>("CustomCanvas"));
            customCanvas.SetActive(false);
        }

        void InstantiateCustomScroll()
        {
            customScroll = Instantiate(Resources.Load<GameObject>("CustomScroll"));
            customScroll.SetActive(false);
        }

		void PopulateObjectButtons()
		{
			StartCoroutine(WaitForUIElements());
		}

		IEnumerator WaitForUIElements()
		{
			while (customCanvas == null || customScroll == null)
			{
				yield return null;
			}

			Transform contentTransform = customScroll.transform.Find("Scroll View/Viewport/Content");
			if (contentTransform != null)
			{
				GameObject content = contentTransform.gameObject;
				GameObject objectButton = Instantiate(objectButtonPrefab, content.transform);
        
			}
			else
			{
				Debug.LogError("Content not found under Scroll View/Viewport/Content.");
			}
		}
		
        void InitializeMenuState()
        {
            if (customCanvas != null)
                customCanvas.SetActive(true);
            if (customScroll != null)
                customScroll.SetActive(false);
        }

        public void OpenMenu()
        {
            if (!isMenuOpen && customCanvas != null)
            {
                isMenuOpen = true;
                customCanvas.SetActive(true);
                if (customScroll != null)
                    customScroll.SetActive(true);
                PlayAnimation("openanimation");
                UnlockCursor();
                DisableCamera();
            }
        }

        public void CloseMenu()
        {
            if (isMenuOpen && customCanvas != null)
            {
                isMenuOpen = false;
                customCanvas.SetActive(false);
                if (customScroll != null)
                    customScroll.SetActive(false);
                PlayAnimation("closeanimation");
                LockCursor();
                EnableCamera();
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
                Debug.LogError("Animator component not found on CustomCanvas.");
            }
        }

        void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        void UnlockCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        void DisableCamera()
        {
            CameraController.Instance.enabled = false;
        }

        void EnableCamera()
        {
            CameraController.Instance.enabled = true;
        }
    }
}
