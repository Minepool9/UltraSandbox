using UnityEngine;

public class MouseLook : MonoBehaviour
{
    public float sensitivity = 100f;
    public Transform playerBody;

    private float xRotation = 0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;

        // Attach this script to all camera children of the "Player" GameObject
        Transform playerTransform = GameObject.FindWithTag("Player").transform;
        foreach (Transform child in playerTransform)
        {
            bool tmp = true;
            if (!child.CompareTag("MainCamera")) tmp = false;

            // Check if the child already has a MouseLook component attached
            if (child.GetComponent<MouseLook>() != null) tmp = false;

            if (tmp)
            {
                // Attach MouseLook component to the camera
                MouseLook mouseLook = child.gameObject.AddComponent<MouseLook>();
                mouseLook.playerBody = playerBody;
            }
        }
    }

    void Update()
    {
        float mouseX = Input.GetAxis("Mouse X") * sensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * sensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        playerBody.Rotate(Vector3.up * mouseX);
    }
}