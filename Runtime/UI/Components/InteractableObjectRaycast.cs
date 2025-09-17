using UnityEngine;
using UnityEngine.InputSystem;
using WiseTwin.UI;

namespace WiseTwin
{
    /// <summary>
    /// Système de raycast pour détecter l'hover et les clics sur les InteractableObject
    /// Utilise le nouveau Input System de Unity 6
    /// Se crée automatiquement au démarrage si nécessaire
    /// </summary>
    public class InteractableObjectRaycast : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private LayerMask interactionMask = -1; // Tous les layers par défaut
        [SerializeField] private float raycastDistance = 100f;
        [SerializeField] private bool debugMode = false;

        private Camera mainCamera;
        private InteractableObject currentHoveredObject;
        private Mouse mouse;

        void Start()
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError("[InteractableObjectRaycast] No main camera found!");
                enabled = false;
                return;
            }

            // Récupérer la souris du nouveau Input System
            mouse = Mouse.current;
            if (mouse == null)
            {
                Debug.LogError("[InteractableObjectRaycast] No mouse detected! Input System required.");
                enabled = false;
                return;
            }

            if (debugMode) Debug.Log("[InteractableObjectRaycast] System initialized");
        }

        void Update()
        {
            if (mouse == null) return;

            // Ne pas faire de raycast si une UI est affichée
            if (ContentDisplayManager.Instance != null && ContentDisplayManager.Instance.IsDisplaying)
            {
                // Si on avait un objet en hover, le désactiver
                if (currentHoveredObject != null)
                {
                    ExitHover();
                }
                return;
            }

            // Récupérer la position de la souris avec le nouveau Input System
            Vector2 mousePosition = mouse.position.ReadValue();
            bool clickDetected = mouse.leftButton.wasPressedThisFrame;

            Ray ray = mainCamera.ScreenPointToRay(mousePosition);

            // Faire le raycast
            if (Physics.Raycast(ray, out RaycastHit hit, raycastDistance, interactionMask))
            {
                InteractableObject interactable = hit.collider.GetComponent<InteractableObject>();

                if (interactable != null)
                {
                    // Nouvel objet survolé
                    if (currentHoveredObject != interactable)
                    {
                        // Quitter l'ancien hover
                        if (currentHoveredObject != null)
                        {
                            ExitHover();
                        }

                        // Entrer dans le nouveau hover
                        EnterHover(interactable);
                    }

                    // Détecter le clic
                    if (clickDetected)
                    {
                        if (debugMode) Debug.Log($"[InteractableObjectRaycast] Click detected on {interactable.name}");
                        interactable.HandleInteraction();
                    }
                }
                else
                {
                    // On survole quelque chose mais ce n'est pas interactable
                    if (currentHoveredObject != null)
                    {
                        ExitHover();
                    }
                }
            }
            else
            {
                // Aucun hit, on ne survole rien
                if (currentHoveredObject != null)
                {
                    ExitHover();
                }
            }
        }

        void EnterHover(InteractableObject obj)
        {
            currentHoveredObject = obj;
            if (debugMode) Debug.Log($"[InteractableObjectRaycast] Enter hover on {obj.name}");

            // Appeler manuellement OnMouseEnter via SendMessage
            obj.SendMessage("OnMouseEnter", SendMessageOptions.DontRequireReceiver);
        }

        void ExitHover()
        {
            if (currentHoveredObject != null)
            {
                if (debugMode) Debug.Log($"[InteractableObjectRaycast] Exit hover on {currentHoveredObject.name}");

                // Appeler manuellement OnMouseExit
                currentHoveredObject.SendMessage("OnMouseExit", SendMessageOptions.DontRequireReceiver);
                currentHoveredObject = null;
            }
        }

        void OnDestroy()
        {
            // Nettoyer si on détruit le système
            if (currentHoveredObject != null)
            {
                ExitHover();
            }
        }

        void OnDisable()
        {
            // Nettoyer si on désactive le système
            if (currentHoveredObject != null)
            {
                ExitHover();
            }
        }
    }
}