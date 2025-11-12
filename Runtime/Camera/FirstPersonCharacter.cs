using UnityEngine;
using UnityEngine.InputSystem;

namespace WiseTwin
{
    [RequireComponent(typeof(CharacterController))]
    public class FirstPersonCharacter : MonoBehaviour
    {
        [Header("References")]
        public Transform cameraPivot;     // Vide à la hauteur des yeux
        public Camera playerCamera;       // Enfant de cameraPivot
        public Transform headBone;        // Optionnel (mixamorig:Head)
        public Animator animator;

        [Header("Movement")]
        public float moveSpeed = 3.5f;
        public float sprintMultiplier = 1.7f;
        public float gravity = -9.81f;
        public float groundSnap = -2f;

        [Header("Look")]
        public float lookSensitivity = 0.12f;
        public float minPitch = -80f;
        public float maxPitch = 80f;
        public bool rotateOnlyOnRightClick = true;

        [Header("Smoothing")]
        public float rotationLerp = 15f;

        [Header("Camera Distance (FP/TP)")]
        [Tooltip("Distance cible par défaut en 3e personne")]
        public float defaultThirdPersonDistance = 2.5f;
        [Tooltip("Distance min/max de zoom (0 = 1ere personne)")]
        public float minDistance = 0f;
        public float maxDistance = 4f;
        [Tooltip("Vitesse de zoom (molette)")]
        public float zoomSpeed = 2.5f;
        [Tooltip("Vitesse de lissage de la distance")]
        public float zoomLerp = 12f;

        [Header("Camera Offsets")]
        [Tooltip("Décalage local du pivot par rapport à l’os tête")]
        public Vector3 headLocalOffset = new Vector3(0f, 0.06f, -0.08f);
        [Tooltip("Décalage épaule (X=épaule, Y=léger bas, Z=avant/arrière local pivot)")]
        public Vector3 shoulderLocalOffset = new Vector3(0.35f, -0.1f, 0f);

        [Header("Camera Collision")]
        [Tooltip("Rayon de la sphère anti-clip")]
        public float cameraCollisionRadius = 0.2f;
        [Tooltip("Marge par rapport aux obstacles")]
        public float cameraCollisionPadding = 0.05f;
        [Tooltip("Couches considérées comme obstacles caméra")]
        public LayerMask cameraObstructionMask = ~0;

        [Header("First Person Body Hide (Optionnel)")]
        [Tooltip("Renders à cacher en 1e personne (mains/tête/mesh)")]
        public Renderer[] hideInFirstPerson;
        [Tooltip("Seuil sous lequel on cache les meshes (mètres)")]
        public float hideThreshold = 0.1f;

        [Header("Debug")]
        [Tooltip("Afficher les logs de debug dans la console")]
        public bool debugMode = false;

        // Input
        private InputAction moveAction, lookAction, sprintAction, lookHoldAction, actionButton, scrollAction, toggleViewAction;

        // State
        private CharacterController cc;
        private Vector2 moveInput, lookInput;
        private bool isSprinting, isLooking;
        private float yaw, pitch, verticalVelocity;
        private bool controlsEnabled = true; // Pour bloquer les inputs pendant les UI

        // Cache for UI detection (to avoid FindObjectsByType every frame)
        private UnityEngine.UIElements.UIDocument[] cachedUIDocuments;
        private float lastUICacheTime = 0f;
        private const float UI_CACHE_REFRESH_INTERVAL = 1f; // Refresh cache every 1 second

        // Animator hashes
        private readonly int hashMoveX = Animator.StringToHash("MoveX");
        private readonly int hashMoveY = Animator.StringToHash("MoveY");
        private readonly int hashIsMoving = Animator.StringToHash("IsMoving");
        private readonly int hashSprint = Animator.StringToHash("Sprint");
        private readonly int hashAction = Animator.StringToHash("Action");

        // Zoom state
        private float targetDistance;
        private float currentDistance;

        void Awake()
        {
            cc = GetComponent<CharacterController>();
            if (!animator) animator = GetComponentInChildren<Animator>();

            // Inputs
            moveAction = new InputAction("Move");
            moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w").With("Up", "<Keyboard>/z")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a").With("Left", "<Keyboard>/q")
                .With("Right", "<Keyboard>/d");

            lookAction = new InputAction("Look", binding: "<Mouse>/delta");
            sprintAction = new InputAction("Sprint", binding: "<Keyboard>/leftShift");
            lookHoldAction = new InputAction("LookHold", binding: "<Mouse>/rightButton");
            actionButton = new InputAction("ActionBtn", binding: "<Mouse>/leftButton");

            scrollAction = new InputAction("Scroll", binding: "<Mouse>/scroll"); // Y = molette
            toggleViewAction = new InputAction("ToggleView", binding: "<Keyboard>/v"); // Switch FP/TP
        }

        void OnEnable()
        {
            moveAction.Enable();
            lookAction.Enable();
            sprintAction.Enable();
            lookHoldAction.Enable();
            actionButton.Enable();
            scrollAction.Enable();
            toggleViewAction.Enable();

            actionButton.performed += OnActionPerformed;
            toggleViewAction.performed += OnToggleView;
        }

        void OnDisable()
        {
            actionButton.performed -= OnActionPerformed;
            toggleViewAction.performed -= OnToggleView;

            moveAction.Disable();
            lookAction.Disable();
            sprintAction.Disable();
            lookHoldAction.Disable();
            actionButton.Disable();
            scrollAction.Disable();
            toggleViewAction.Disable();
        }

        void Start()
        {
            yaw = transform.eulerAngles.y;

            if (playerCamera && cameraPivot && playerCamera.transform.parent != cameraPivot)
            {
                playerCamera.transform.SetParent(cameraPivot);
                playerCamera.transform.localPosition = Vector3.zero;
                playerCamera.transform.localRotation = Quaternion.identity;
            }

            // Distance initiale = 3e personne par défaut
            targetDistance = Mathf.Clamp(defaultThirdPersonDistance, minDistance, maxDistance);
            currentDistance = targetDistance;
        }

        void Update()
        {
            ReadInput();
            HandleLook();
            HandleMove();
            UpdateAnimator();
            UpdateZoomTargetFromScroll();
            UpdateHideInFirstPerson();
        }

        void LateUpdate()
        {
            // Suivre la tête puis positionner la caméra (après maj Animator)
            FollowHeadBoneOrPivot();
            PositionCameraWithCollision();
        }

        void ReadInput()
        {
            if (!controlsEnabled)
            {
                // Bloquer tous les inputs et remettre le curseur visible
                moveInput = Vector2.zero;
                lookInput = Vector2.zero;
                isSprinting = false;
                isLooking = false;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                return;
            }

            moveInput = moveAction.ReadValue<Vector2>();
            lookInput = lookAction.ReadValue<Vector2>();
            isSprinting = sprintAction.IsPressed();

            isLooking = rotateOnlyOnRightClick ? lookHoldAction.IsPressed() : true;
            if (isLooking)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        void HandleLook()
        {
            if (!playerCamera) return;

            if (isLooking)
            {
                yaw += lookInput.x * lookSensitivity;
                pitch -= lookInput.y * lookSensitivity;
                pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
            }

            // Rotation yaw du corps
            Quaternion yRot = Quaternion.Euler(0f, yaw, 0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, yRot, rotationLerp * Time.deltaTime);

            // Pitch sur le pivot caméra
            if (cameraPivot)
                cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        void HandleMove()
        {
            Vector3 fwd = transform.forward; fwd.y = 0f; fwd.Normalize();
            Vector3 right = transform.right; right.y = 0f; right.Normalize();

            Vector3 moveDir = fwd * moveInput.y + right * moveInput.x;
            float speed = isSprinting ? moveSpeed * sprintMultiplier : moveSpeed;

            if (cc.isGrounded && verticalVelocity < 0f) verticalVelocity = groundSnap;
            verticalVelocity += gravity * Time.deltaTime;

            Vector3 velocity = moveDir.normalized * speed;
            velocity.y = verticalVelocity;

            cc.Move(velocity * Time.deltaTime);
        }

        void UpdateAnimator()
        {
            if (!animator) return;
            float targetX = moveInput.x;
            float targetY = moveInput.y * (isSprinting ? 1.2f : 1f);

            animator.SetFloat(hashMoveX, targetX, 0.1f, Time.deltaTime);
            animator.SetFloat(hashMoveY, targetY, 0.1f, Time.deltaTime);
            animator.SetBool(hashIsMoving, moveInput.sqrMagnitude > 0.01f);
            animator.SetBool(hashSprint, isSprinting);
        }

        void OnActionPerformed(InputAction.CallbackContext ctx)
        {
            // Ne pas jouer l'animation si les contrôles sont bloqués (UI active)
            if (!controlsEnabled) return;

            // Check if we're clicking on any UI Toolkit element
            if (IsClickingOnUI())
            {
                return; // Don't play animation when clicking UI
            }

            if (animator) animator.SetTrigger(hashAction);
        }

        /// <summary>
        /// Check if the mouse pointer is over any interactive UI element (Button, etc.)
        /// Returns false for non-interactive UI like modals/backgrounds so player can still click 3D objects
        /// </summary>
        bool IsClickingOnUI()
        {
            // Get mouse position
            if (UnityEngine.InputSystem.Mouse.current == null) return false;

            Vector2 mousePos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();

            // UI Toolkit uses coordinates with origin at TOP-LEFT
            // Input System uses coordinates with origin at BOTTOM-LEFT
            // So we need to flip Y coordinate
            Vector2 pointerPosition = new Vector2(mousePos.x, Screen.height - mousePos.y);

            // Refresh UI cache if needed (only every second, not every click)
            if (cachedUIDocuments == null || Time.time - lastUICacheTime > UI_CACHE_REFRESH_INTERVAL)
            {
                cachedUIDocuments = FindObjectsByType<UnityEngine.UIElements.UIDocument>(FindObjectsSortMode.None);
                lastUICacheTime = Time.time;
                if (debugMode) Debug.Log($"[FirstPersonCharacter] Cached {cachedUIDocuments.Length} UIDocuments");
            }

            // Check cached UIDocuments
            foreach (var uiDoc in cachedUIDocuments)
            {
                if (uiDoc != null && uiDoc.rootVisualElement != null && uiDoc.rootVisualElement.panel != null)
                {
                    // Try to pick an element at the pointer position
                    var pickedElement = uiDoc.rootVisualElement.panel.Pick(pointerPosition);

                    if (pickedElement != null)
                    {
                        if (debugMode) Debug.Log($"[FirstPersonCharacter] Picked element: {pickedElement.GetType().Name} (name: {pickedElement.name})");

                        // Only block animation if we're clicking on an INTERACTIVE element (Button)
                        // This allows clicking 3D objects even when modals are displayed
                        if (pickedElement is UnityEngine.UIElements.Button)
                        {
                            if (debugMode) Debug.Log("[FirstPersonCharacter] Blocking animation - clicked on Button!");
                            return true; // We're clicking on an interactive UI button
                        }
                    }
                }
            }

            return false; // Not clicking on any interactive UI
        }

        void OnToggleView(InputAction.CallbackContext ctx)
        {
            // Switch rapide FP/TP
            if (targetDistance <= hideThreshold + 0.001f)
                targetDistance = defaultThirdPersonDistance;
            else
                targetDistance = 0f;

            targetDistance = Mathf.Clamp(targetDistance, minDistance, maxDistance);
        }

        void UpdateZoomTargetFromScroll()
        {
            // Molette : +Y = vers toi sur beaucoup de souris -> on choisit un sens agréable
            Vector2 scroll = scrollAction.ReadValue<Vector2>();
            if (Mathf.Abs(scroll.y) > 0.01f)
            {
                // Scale léger pour ne pas aller trop vite
                float delta = scroll.y * 0.1f * zoomSpeed;
                targetDistance = Mathf.Clamp(targetDistance - delta, minDistance, maxDistance);
            }

            // Lissage vers la distance cible, avec anti-clip appliqué dans PositionCameraWithCollision()
            currentDistance = Mathf.Lerp(currentDistance, targetDistance, zoomLerp * Time.deltaTime);
        }

        void FollowHeadBoneOrPivot()
        {
            if (!playerCamera || !cameraPivot) return;

            if (headBone)
            {
                cameraPivot.position = headBone.TransformPoint(headLocalOffset);
            }
            else
            {
                // Si pas d’os tête assigné, garde une hauteur constante (place ton pivot à ~1.65m dans l’éditeur)
                // Rien à faire ici si le pivot est déjà bien placé dans la hiérarchie.
            }
        }

        void PositionCameraWithCollision()
        {
            if (!playerCamera || !cameraPivot) return;

            // Point de départ (épaule) en espace monde
            Vector3 shoulderWorld = cameraPivot.TransformPoint(shoulderLocalOffset);
            Vector3 backDir = -cameraPivot.forward; // reculer derrière le regard

            float desiredDist = Mathf.Max(0f, currentDistance);
            float hitDist = desiredDist;

            // SphereCast pour éviter de traverser les murs en 3e personne
            if (desiredDist > 0.001f)
            {
                if (Physics.SphereCast(shoulderWorld, cameraCollisionRadius, backDir, out RaycastHit hit, desiredDist + cameraCollisionPadding, cameraObstructionMask, QueryTriggerInteraction.Ignore))
                {
                    hitDist = Mathf.Max(0f, hit.distance - cameraCollisionPadding);
                }
            }
            else
            {
                hitDist = 0f;
            }

            Vector3 camPos = shoulderWorld + backDir * hitDist;
            playerCamera.transform.position = camPos;
            // La rotation vient déjà du pivot (pitch), et du corps (yaw)
            playerCamera.transform.rotation = cameraPivot.rotation;
        }

        void UpdateHideInFirstPerson()
        {
            if (hideInFirstPerson == null || hideInFirstPerson.Length == 0) return;
            bool hide = currentDistance <= hideThreshold + 0.001f;
            for (int i = 0; i < hideInFirstPerson.Length; i++)
            {
                if (hideInFirstPerson[i]) hideInFirstPerson[i].enabled = !hide ? true : false;
            }
        }

        /// <summary>
        /// Active ou désactive les contrôles du personnage (pour bloquer pendant les UI)
        /// </summary>
        public void SetControlsEnabled(bool enabled)
        {
            controlsEnabled = enabled;

            if (!enabled)
            {
                // Réinitialiser les inputs
                moveInput = Vector2.zero;
                lookInput = Vector2.zero;
                isSprinting = false;
                isLooking = false;

                // Remettre le curseur visible
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;

                // Arrêter les animations de mouvement
                if (animator)
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
