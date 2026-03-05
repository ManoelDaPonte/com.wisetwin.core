using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

namespace WiseTwin
{
    /// <summary>
    /// Mouse-only movement controller using NavMeshAgent.
    /// Left-click on ground to move. Camera is handled by FirstPersonCharacter (cameraOnly mode).
    /// Replaces keyboard movement when ControlMode.MouseOnly is active.
    ///
    /// Requires a baked NavMesh in the scene.
    /// </summary>
    public class ClickToMoveCharacter : MonoBehaviour
    {
        [Header("Movement")]
        public float rotationSpeed = 10f;

        [Header("Click - Ground Detection")]
        public LayerMask groundLayerMask = ~0;

        [Header("Click Indicator")]
        [SerializeField] private Color indicatorColor = new Color(0.1f, 0.8f, 0.6f, 0.8f);
        [SerializeField] private float indicatorDuration = 1f;

        [Header("Debug")]
        public bool debugMode = false;

        // Input
        private InputAction clickAction;

        // State
        private NavMeshAgent agent;
        private bool controlsEnabled = true;
        private bool hasDestination = false;

        // References (set by ControlModeSettings from FPC)
        private Camera playerCamera;
        private Animator animator;

        // Click indicator
        private GameObject clickIndicator;
        private float indicatorTimer;

        // Animator hashes
        private readonly int hashMoveX = Animator.StringToHash("MoveX");
        private readonly int hashMoveY = Animator.StringToHash("MoveY");
        private readonly int hashIsMoving = Animator.StringToHash("IsMoving");
        private readonly int hashSprint = Animator.StringToHash("Sprint");

        // UI detection cache
        private UnityEngine.UIElements.UIDocument[] cachedUIDocuments;
        private float lastUICacheTime = 0f;

        void Awake()
        {
            clickAction = new InputAction("Click", binding: "<Mouse>/leftButton");
        }

        void OnEnable()
        {
            agent = GetComponent<NavMeshAgent>();

            // Get camera from FPC (should still be active in cameraOnly mode)
            var fpc = GetComponent<FirstPersonCharacter>();
            if (fpc)
            {
                playerCamera = fpc.playerCamera;
                animator = fpc.animator;
            }

            clickAction.Enable();
            clickAction.performed += OnClickPerformed;

            // Configure agent and ensure it's on the NavMesh
            if (agent)
            {
                agent.updateRotation = false;

                if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                {
                    agent.Warp(hit.position);
                    if (debugMode) Debug.Log($"[ClickToMove] Agent warped to NavMesh at {hit.position}");
                }
                else
                {
                    Debug.LogWarning($"[ClickToMove] No NavMesh found near {transform.position}. Ensure NavMesh is baked.");
                }
            }
            else
            {
                Debug.LogError("[ClickToMove] No NavMeshAgent component found!");
            }

            CreateClickIndicator();

            if (debugMode) Debug.Log("[ClickToMove] Enabled");
        }

        void OnDisable()
        {
            clickAction.performed -= OnClickPerformed;
            clickAction.Disable();

            if (clickIndicator) clickIndicator.SetActive(false);
        }

        void OnDestroy()
        {
            if (clickIndicator) Destroy(clickIndicator);
            clickAction?.Dispose();
        }

        void Update()
        {
            if (!controlsEnabled) return;

            HandleMovementRotation();
            UpdateAnimator();
            UpdateClickIndicator();
        }

        void HandleMovementRotation()
        {
            if (!agent || !agent.isOnNavMesh || !agent.hasPath) return;

            Vector3 velocity = agent.velocity;
            velocity.y = 0;
            if (velocity.sqrMagnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(velocity.normalized);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
            }

            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {
                hasDestination = false;
            }
        }

        void UpdateAnimator()
        {
            if (!animator || animator.runtimeAnimatorController == null) return;

            float speed = (agent && agent.isOnNavMesh && agent.velocity.magnitude > 0.05f)
                ? agent.velocity.magnitude / Mathf.Max(agent.speed, 0.1f)
                : 0f;
            bool isMoving = speed > 0.05f;

            animator.SetFloat(hashMoveY, speed, 0.1f, Time.deltaTime);
            animator.SetFloat(hashMoveX, 0f, 0.1f, Time.deltaTime);
            animator.SetBool(hashIsMoving, isMoving);
            animator.SetBool(hashSprint, false);
        }

        void OnClickPerformed(InputAction.CallbackContext ctx)
        {
            if (!controlsEnabled || !playerCamera) return;
            if (Mouse.current == null) return;
            if (IsClickingOnUI()) return;

            if (!agent)
            {
                if (debugMode) Debug.LogWarning("[ClickToMove] No NavMeshAgent");
                return;
            }

            if (!agent.isOnNavMesh)
            {
                Debug.LogWarning("[ClickToMove] Agent not on NavMesh, attempting warp...");
                if (NavMesh.SamplePosition(transform.position, out NavMeshHit warpHit, 5f, NavMesh.AllAreas))
                    agent.Warp(warpHit.position);
                else
                {
                    Debug.LogError("[ClickToMove] Cannot find NavMesh near player!");
                    return;
                }
            }

            Vector2 mousePos = Mouse.current.position.ReadValue();
            Ray ray = playerCamera.ScreenPointToRay(mousePos);

            if (debugMode) Debug.Log($"[ClickToMove] Click at screen {mousePos}");

            if (Physics.Raycast(ray, out RaycastHit hit, 100f, groundLayerMask, QueryTriggerInteraction.Ignore))
            {
                if (debugMode) Debug.Log($"[ClickToMove] Hit '{hit.collider.gameObject.name}' at {hit.point} (layer: {LayerMask.LayerToName(hit.collider.gameObject.layer)})");

                if (NavMesh.SamplePosition(hit.point, out NavMeshHit navHit, 2f, NavMesh.AllAreas))
                {
                    if (agent.SetDestination(navHit.position))
                    {
                        hasDestination = true;
                        ShowClickIndicator(navHit.position);
                        if (debugMode) Debug.Log($"[ClickToMove] Destination set to {navHit.position}");
                    }
                }
                else if (debugMode)
                {
                    Debug.Log($"[ClickToMove] No NavMesh near {hit.point}");
                }
            }
            else if (debugMode)
            {
                Debug.Log("[ClickToMove] Raycast hit nothing");
            }
        }

        #region Click Indicator

        void CreateClickIndicator()
        {
            if (clickIndicator) return;

            clickIndicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            clickIndicator.name = "ClickIndicator";
            clickIndicator.transform.localScale = new Vector3(0.5f, 0.02f, 0.5f);

            var col = clickIndicator.GetComponent<Collider>();
            if (col) Destroy(col);

            var rend = clickIndicator.GetComponent<Renderer>();
            if (rend)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit")
                    ?? Shader.Find("Standard");
                if (shader)
                {
                    rend.material = new Material(shader);
                    rend.material.color = indicatorColor;
                }
            }

            clickIndicator.SetActive(false);
        }

        void ShowClickIndicator(Vector3 position)
        {
            if (!clickIndicator) return;
            clickIndicator.transform.position = position + Vector3.up * 0.05f;
            clickIndicator.SetActive(true);
            indicatorTimer = indicatorDuration;
        }

        void UpdateClickIndicator()
        {
            if (!clickIndicator || !clickIndicator.activeSelf) return;

            indicatorTimer -= Time.deltaTime;
            if (indicatorTimer <= 0f || !hasDestination)
            {
                clickIndicator.SetActive(false);
            }
        }

        #endregion

        #region UI Detection

        bool IsClickingOnUI()
        {
            if (Mouse.current == null) return false;

            Vector2 mousePos = Mouse.current.position.ReadValue();
            Vector2 pointerPosition = new Vector2(mousePos.x, Screen.height - mousePos.y);

            if (cachedUIDocuments == null || Time.time - lastUICacheTime > 1f)
            {
                cachedUIDocuments = FindObjectsByType<UnityEngine.UIElements.UIDocument>(FindObjectsSortMode.None);
                lastUICacheTime = Time.time;
            }

            foreach (var uiDoc in cachedUIDocuments)
            {
                if (uiDoc != null && uiDoc.rootVisualElement != null && uiDoc.rootVisualElement.panel != null)
                {
                    var picked = uiDoc.rootVisualElement.panel.Pick(pointerPosition);
                    if (picked is UnityEngine.UIElements.Button)
                        return true;
                }
            }

            return false;
        }

        #endregion

        /// <summary>
        /// Enable or disable controls (called via PlayerControls)
        /// </summary>
        public void SetControlsEnabled(bool enabled)
        {
            controlsEnabled = enabled;

            if (!enabled)
            {
                if (agent && agent.isOnNavMesh) agent.ResetPath();
                hasDestination = false;

                if (animator && animator.runtimeAnimatorController != null)
                {
                    animator.SetFloat(hashMoveX, 0f);
                    animator.SetFloat(hashMoveY, 0f);
                    animator.SetBool(hashIsMoving, false);
                    animator.SetBool(hashSprint, false);
                }
            }
        }
    }
}
