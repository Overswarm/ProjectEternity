using UnityEngine;

namespace Eternity
{
    /// <summary>Free RTS camera: WASD/arrows pan, wheel zoom, Q/E rotate.</summary>
    public class CameraRig : MonoBehaviour
    {
        public Camera Cam { get; private set; }

        Vector3 pivot;
        float yaw = 45f;
        const float Pitch = 55f;
        float dist = 42f;

        public void Init(float worldSize)
        {
            pivot = new Vector3(worldSize * 0.5f, 0f, worldSize * 0.5f);

            var camGo = new GameObject("MainCamera");
            camGo.tag = "MainCamera";
            Cam = camGo.AddComponent<Camera>();
            Cam.clearFlags = CameraClearFlags.SolidColor;
            Cam.backgroundColor = new Color(0.04f, 0.05f, 0.09f);
            Cam.farClipPlane = 500f;
            camGo.transform.SetParent(transform);
            Apply();
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            float panSpeed = dist * 0.9f * dt;

            Vector3 fwd = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
            Vector3 right = Quaternion.Euler(0f, yaw, 0f) * Vector3.right;

            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) pivot += fwd * panSpeed;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) pivot -= fwd * panSpeed;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) pivot += right * panSpeed;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) pivot -= right * panSpeed;
            if (Input.GetKey(KeyCode.Q)) yaw -= 70f * dt;
            if (Input.GetKey(KeyCode.E)) yaw += 70f * dt;

            if (Input.GetMouseButton(2))
            {
                pivot -= right * Input.GetAxis("Mouse X") * dist * 0.05f;
                pivot -= fwd * Input.GetAxis("Mouse Y") * dist * 0.05f;
            }

            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.001f && !GameUI.PointerOverUI)
                dist = Mathf.Clamp(dist * (1f - scroll * 1.6f), 10f, 90f);

            float half = Tuning.WorldSize;
            pivot.x = Mathf.Clamp(pivot.x, 0f, half);
            pivot.z = Mathf.Clamp(pivot.z, 0f, half);

            Apply();
        }

        void Apply()
        {
            if (Cam == null) return;
            var rot = Quaternion.Euler(Pitch, yaw, 0f);
            Cam.transform.position = pivot - rot * Vector3.forward * dist;
            Cam.transform.rotation = rot;
        }

        public void JumpTo(float x, float z)
        {
            pivot = new Vector3(x, 0f, z);
            Apply();
        }
    }
}
