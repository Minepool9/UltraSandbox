using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Reflection;
using UnityEngine.InputSystem;

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
                if (stream == null)
                {
                    Debug.LogError("UI bundle resource not found.");
                    return;
                }

                byte[] bundleData = new byte[stream.Length];
                stream.Read(bundleData, 0, bundleData.Length);

                // Load the AssetBundle from memory
                AssetBundle uiBundle = AssetBundle.LoadFromMemory(bundleData);

                if (uiBundle == null)
                {
                    Debug.LogError("Failed to load UI bundle.");
                    return;
                }

                // Load necessary assets from the bundle
                GameObject prefab = uiBundle.LoadAsset<GameObject>("CustomCanvas");

                // You might want to check if prefab is not null before instantiating
                if (prefab == null)
                {
                    Debug.LogError("Failed to load prefab from UI bundle.");
                    return;
                }

                customCanvas = Instantiate(prefab, customMenu.transform);
                customCanvas.SetActive(false);

                // Find the button component from the customCanvas children
                Transform buttonTransform = customCanvas.transform.Find("Button");

                if (buttonTransform == null)
                {
                    Debug.LogWarning("Child named 'Button' not found in CustomCanvas.");
                    return;
                }

                Button button = buttonTransform.GetComponent<Button>();

                if (button == null)
                {
                    Debug.LogWarning("Button component not found on child named 'Button'.");
                    return;
                }

                // Add a listener to the button's click event
                button.onClick.AddListener(() =>
                {
                    CloseMenu();
                    Debug.Log("Close button pressed.");
                });

                // Unload the AssetBundle to free up memory
                uiBundle.Unload(false);
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

        public void changeMenuState(InputAction.CallbackContext obj)
        {
            Debug.Log("M key pressed");
            if (!isMenuOpen)
            {
                OpenMenu();
                return;
            }
            CloseMenu();
        }

        public void OpenMenu()
        {
            isMenuOpen = true;
            customCanvas.SetActive(true);
            PlayAnimation("openanimation");
            UnlockCursor();
            LockCameraRotation();
        }

        public void CloseMenu()
        {
            isMenuOpen = false;
            customCanvas.SetActive(false);
            PlayAnimation("closeanimation");
            LockCursor();
            UnlockCameraRotation();
        }

        void PlayAnimation(string animationName)
        {
            Animator animator = customCanvas.GetComponent<Animator>();

            if (animator == null)
            {
                Debug.LogWarning("Animator component not found on CustomCanvas.");
                return;
            }

            animator.Play(animationName);
        }

        void LockCameraRotation()
        {
            Camera[] playerCameras = GetComponentsInChildren<Camera>();
            foreach (Camera camera in playerCameras)
            {
                MouseLook mouseLook = camera.GetComponent<MouseLook>();
                if (CheckForCamera(camera, mouseLook)) mouseLook.enabled = false;
            }
        }

        void UnlockCameraRotation()
        {
            Camera[] playerCameras = GetComponentsInChildren<Camera>();
            foreach (Camera camera in playerCameras)
            {
                // Unlock camera rotation (Assuming MouseLook is a script handling camera rotation)
                MouseLook mouseLook = camera.GetComponent<MouseLook>();
                if (CheckForCamera(camera, mouseLook)) mouseLook.enabled = true;
            }
        }
        bool CheckForCamera(Camera camera, MouseLook mouseLook)
        {
            if (camera == null)
            {
                Debug.LogWarning("Camera not found.");
                return false;
            }

            if (mouseLook == null)
            {
                Debug.LogWarning("MouseLook script not found on camera.");
                return false;
            }

            return true;
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