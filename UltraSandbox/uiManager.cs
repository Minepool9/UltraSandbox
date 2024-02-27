using UnityEngine;
using BepInEx;
using UnityEngine.SceneManagement;
using System.Collections;
using System.IO;
using System.Reflection;

namespace Secondultrakillmod
{
    public class UIManager : MonoBehaviour
    {
        private static UIManager instance;
        private GameObject customCanvas;
        private GameObject customScroll;
        private GameObject objectButtonPrefab;
        public bool isMenuOpen = false;
        private bool uiBundleLoaded = false;
        private static GunControl gc => GunControl.Instance;

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
            yield return new WaitForSeconds(7); // Adjust the delay as needed
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
                if (stream == null)
                {
                    Debug.LogError("UI bundle resource not found.");
                    return;
                }

                byte[] bundleData = new byte[stream.Length];
                stream.Read(bundleData, 0, bundleData.Length);

                AssetBundle uiBundle = AssetBundle.LoadFromMemory(bundleData);

                if (uiBundle == null)
                {
                    Debug.LogError("Failed to load UI bundle.");
                    return;
                }

                customCanvas = InstantiatePrefabFromBundle(uiBundle, "CustomCanvas");
                customScroll = InstantiatePrefabFromBundle(uiBundle, "CustomScroll");
                objectButtonPrefab = uiBundle.LoadAsset<GameObject>("Objectbutton");

                uiBundle.Unload(false);
                uiBundleLoaded = true;
            }
        }

        GameObject InstantiatePrefabFromBundle(AssetBundle bundle, string prefabName)
        {
            GameObject prefab = bundle.LoadAsset<GameObject>(prefabName);
            if (prefab == null)
            {
                Debug.LogError($"Failed to load {prefabName} prefab from UI bundle.");
                return null;
            }
            GameObject instance = Instantiate(prefab);
            instance.SetActive(false);
            return instance;
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
            if (customScroll == null)
            {
                Debug.LogError("CustomScroll is not assigned.");
                return;
            }

            Transform viewport = customScroll.transform.Find("Scroll View/Viewport");
            if (viewport == null)
            {
                Debug.LogError("Viewport not found under Scroll View.");
                return;
            }

            Transform content = viewport.Find("Content");
            if (content == null)
            {
                Debug.LogError("Content not found under Viewport.");
                return;
            }

            foreach (Transform child in content)
            {
                Destroy(child.gameObject);
            }

            if (objectButtonPrefab == null)
            {
                Debug.LogError("Objectbutton prefab is not assigned.");
                return;
            }

            int numberOfButtons = 5;
            for (int i = 0; i < numberOfButtons; i++)
            {
                Instantiate(objectButtonPrefab, content.transform);
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
                customScroll?.SetActive(true);
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
                customScroll?.SetActive(false);
                PlayAnimation("closeanimation");
                LockCursor();
                EnableCamera();
            }
        }

        void PlayAnimation(string animationName)
        {
            Animator animator = customCanvas.GetComponent<Animator>();
            if (animator == null)
            {
                Debug.LogError("Animator component not found on CustomCanvas.");
                return;
            }
            animator.Play(animationName);
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
            gc.enabled = false;
        }

        void EnableCamera()
        {
            CameraController.Instance.enabled = true;
            gc.enabled = false;
        }
    }
}
