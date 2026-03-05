using UnityEngine;
using UnityEngine.InputSystem;

namespace WiseTwin
{
    /// <summary>
    /// Place on a 3D object in the scene to trigger a character swap when clicked.
    /// Compatible with procedure steps — the same object can be a procedure target.
    /// </summary>
    public class CharacterSwapTrigger : MonoBehaviour
    {
        [Header("Character Swap")]
        [Tooltip("Name of the target model (must match a CharacterModelEntry.modelName)")]
        public string targetModelName;

        private bool isHovered = false;

        private bool EnableDebugLogs => WiseTwinManager.Instance != null && WiseTwinManager.Instance.EnableDebugLogs;

        void Update()
        {
            if (string.IsNullOrEmpty(targetModelName)) return;
            if (CharacterSwapper.Instance == null) return;

            CheckHover();

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && isHovered)
            {
                OnObjectClicked();
            }
        }

        void CheckHover()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null || Mouse.current == null)
            {
                isHovered = false;
                return;
            }

            Vector2 mousePos = Mouse.current.position.ReadValue();
            Ray ray = mainCamera.ScreenPointToRay(new Vector3(mousePos.x, mousePos.y, 0));

            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
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
            if (CharacterSwapper.Instance == null) return;

            // Don't swap if already on this model
            if (string.Equals(CharacterSwapper.Instance.CurrentModelName, targetModelName, System.StringComparison.OrdinalIgnoreCase))
            {
                if (EnableDebugLogs)
                    Debug.Log($"[CharacterSwapTrigger] Already using model '{targetModelName}'");
                return;
            }

            if (EnableDebugLogs)
                Debug.Log($"[CharacterSwapTrigger] Swapping to '{targetModelName}' via {gameObject.name}");

            CharacterSwapper.Instance.SwapTo(targetModelName);
        }
    }
}
