using UnityEngine;
using UnityEngine.InputSystem;

namespace WiseTwin
{
    /// <summary>
    /// Component added to 3D objects that trigger video playback when clicked
    /// Uses raycast detection similar to ProcedureStepClickHandler
    /// </summary>
    public class VideoClickHandler : MonoBehaviour
    {
        private VideoTriggerData triggerData;
        private bool isActive = false;
        private bool isHovered = false;

        // Debug
        private bool EnableDebugLogs => WiseTwinManager.Instance != null && WiseTwinManager.Instance.EnableDebugLogs;

        void DebugLog(string message)
        {
            if (EnableDebugLogs)
                Debug.Log($"[VideoClickHandler] {message}");
        }

        public void Initialize(VideoTriggerData data)
        {
            triggerData = data;
            isActive = true;
        }

        void Update()
        {
            if (!isActive || triggerData == null) return;

            // Check hover state
            CheckHover();

            // Detect click with new Input System
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && isHovered)
            {
                OnObjectClicked();
            }
        }

        void CheckHover()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null) return;

            if (Mouse.current == null) return;

            Vector2 mousePos = Mouse.current.position.ReadValue();
            Ray ray = mainCamera.ScreenPointToRay(new Vector3(mousePos.x, mousePos.y, 0));

            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, Mathf.Infinity))
            {
                isHovered = (hit.transform == transform || hit.transform.IsChildOf(transform));
            }
            else
            {
                isHovered = false;
            }
        }

        void OnObjectClicked()
        {
            if (!isActive || triggerData == null) return;

            string videoUrl = triggerData.GetVideoUrl();
            if (string.IsNullOrEmpty(videoUrl))
            {
                DebugLog($"No video URL available for {gameObject.name}");
                return;
            }

            DebugLog($"Playing video for {gameObject.name}: {videoUrl}");

            // Show video displayer
            VideoDisplayer.Instance?.ShowVideo(videoUrl);
        }

        void OnDisable()
        {
            isActive = false;
            isHovered = false;
        }

        void OnDestroy()
        {
            isActive = false;
        }
    }
}
