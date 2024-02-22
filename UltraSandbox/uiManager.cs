using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.IO;
using System.Reflection;

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
            DontDestroyOnLoad(customMenu); // Don't destroy the parent menu object

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

                            // Find the button component from the customCanvas children
                            Transform buttonTransform = customCanvas.transform.Find("Button");
                            if (buttonTransform != null)
                            {
                                Button button = buttonTransform.GetComponent<Button>();
                                if (button != null)
                                {
                                    // Add a listener to the button's click event
                                    button.onClick.AddListener(() =>
                                    {
                                        CloseMenu();
                                        Debug.Log("Close button pressed.");
                                    });
                                }
                                else
                                {
                                    Debug.LogWarning("Button component not found on child named 'Button'.");
                                }
                            }
                            else
                            {
                                Debug.LogWarning("Child named 'Button' not found in CustomCanvas.");
                            }
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
                UnlockCursor();
                LockCameraRotation();
            }
        }

        public void CloseMenu()
        {
            if (isMenuOpen)
            {
                isMenuOpen = false;
                customCanvas.SetActive(false);
                PlayAnimation("closeanimation");
                LockCursor();
                UnlockCameraRotation();
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

        void LockCameraRotation()
        {
            Camera[] playerCameras = GetComponentsInChildren<Camera>();
            foreach (Camera camera in playerCameras)
            {
                if (camera != null)
                {
                    // Lock camera rotation (Assuming MouseLook is a script handling camera rotation)
                    MouseLook mouseLook = camera.GetComponent<MouseLook>();
                    if (mouseLook != null)
                    {
                        mouseLook.enabled = false;
                    }
                    else
                    {
                        Debug.LogWarning("MouseLook script not found on camera.");
                    }
                }
                else
                {
                    Debug.LogWarning("Camera not found.");
                }
            }
        }

        void UnlockCameraRotation()
        {
            Camera[] playerCameras = GetComponentsInChildren<Camera>();
            foreach (Camera camera in playerCameras)
            {
                if (camera != null)
                {
                    // Unlock camera rotation (Assuming MouseLook is a script handling camera rotation)
                    MouseLook mouseLook = camera.GetComponent<MouseLook>();
                    if (mouseLook != null)
                    {
                        mouseLook.enabled = true;
                    }
                    else
                    {
                        Debug.LogWarning("MouseLook script not found on camera.");
                    }
                }
                else
                {
                    Debug.LogWarning("Camera not found.");
                }
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
    }
}

