using UnityEngine;
using UnityEngine.InputSystem;
using WiseTwin.UI;

namespace WiseTwin
{
    /// <summary>
    /// Composant temporaire ajouté aux objets en surbrillance pendant une procédure
    /// Permet de valider l'étape en cliquant directement sur l'objet
    /// </summary>
    public class ProcedureStepClickHandler : MonoBehaviour
    {
        private ProcedureDisplayer procedureDisplayer;
        private int stepIndex;
        private GameObject associatedObject; // L'objet GameObject lié à ce handler
        private bool isActive = false;
        private Renderer objectRenderer;
        private Color originalEmissionColor;
        private bool hasOriginalEmission;

        // Pour gérer le feedback visuel au survol
        private bool isHovered = false;
        private Color hoverColor = new Color(0.1f, 1f, 0.3f); // Vert vif
        private Color originalColor;

        // Distance maximale de clic
        [Header("Click Distance Settings")]
        [Tooltip("Distance maximale pour pouvoir cliquer (en mètres Unity)")]
        public float maxClickDistance = 5f;

        [Tooltip("Utiliser une distance relative à la taille de l'objet (s'adapte au scale)")]
        public bool useRelativeDistance = true;

        [Tooltip("Si useRelativeDistance = true, distance = facteur × taille de l'objet")]
        public float relativeDistanceFactor = 3f;

        private bool isInRange = false;

        public void Initialize(ProcedureDisplayer displayer, int index, GameObject obj)
        {
            procedureDisplayer = displayer;
            stepIndex = index;
            associatedObject = obj;
            isActive = true;

            // Récupérer le renderer pour le feedback visuel
            objectRenderer = GetComponent<Renderer>();
            if (objectRenderer != null && objectRenderer.material != null)
            {
                // Sauvegarder la couleur originale du matériau
                if (objectRenderer.material.HasProperty("_Color"))
                {
                    originalColor = objectRenderer.material.GetColor("_Color");
                }

                // Sauvegarder l'émission originale
                hasOriginalEmission = objectRenderer.material.IsKeywordEnabled("_EMISSION");
                if (hasOriginalEmission && objectRenderer.material.HasProperty("_EmissionColor"))
                {
                    originalEmissionColor = objectRenderer.material.GetColor("_EmissionColor");
                }
            }
        }

        void Update()
        {
            if (!isActive || procedureDisplayer == null) return;

            // Vérifier si on survole l'objet
            CheckHover();

            // Détecter le clic avec le nouveau Input System uniquement
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && isHovered)
            {
                OnObjectClicked();
            }
        }

        void CheckHover()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null) return;

            // Créer le rayon avec le nouveau Input System
            if (Mouse.current == null) return;

            Vector2 mousePos = Mouse.current.position.ReadValue();
            Ray ray = mainCamera.ScreenPointToRay(new Vector3(mousePos.x, mousePos.y, 0));

            // Calculer la distance maximale effective
            float effectiveMaxDistance = maxClickDistance;
            if (useRelativeDistance)
            {
                // Distance basée sur la taille de l'objet (s'adapte au scale)
                Bounds bounds = GetObjectBounds();
                float objectSize = bounds.size.magnitude;
                effectiveMaxDistance = objectSize * relativeDistanceFactor;
            }

            // Vérifier la distance entre la caméra et l'objet
            float distanceToObject = Vector3.Distance(mainCamera.transform.position, transform.position);
            isInRange = distanceToObject <= effectiveMaxDistance;

            // Effectuer le raycast avec la distance maximale
            RaycastHit hit;
            bool wasHovered = isHovered;

            // Vérifier si on touche cet objet spécifique ET si on est assez proche
            if (Physics.Raycast(ray, out hit, effectiveMaxDistance * 1.5f)) // 1.5x pour éviter les coupures brusques
            {
                bool hitThisObject = (hit.transform == transform || hit.transform.IsChildOf(transform));
                isHovered = hitThisObject && isInRange;
            }
            else
            {
                isHovered = false;
            }

            // Appliquer le feedback visuel si l'état de survol a changé
            if (wasHovered != isHovered)
            {
                ApplyHoverFeedback(isHovered);
            }
        }

        Bounds GetObjectBounds()
        {
            // Calculer les bounds de l'objet (incluant tous les renderers enfants)
            Bounds bounds = new Bounds(transform.position, Vector3.one);
            bool hasBounds = false;

            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                if (hasBounds)
                {
                    bounds.Encapsulate(renderer.bounds);
                }
                else
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
            }

            // Fallback si pas de renderer
            if (!hasBounds)
            {
                Collider col = GetComponent<Collider>();
                if (col != null)
                {
                    bounds = col.bounds;
                }
            }

            return bounds;
        }

        void ApplyHoverFeedback(bool hovering)
        {
            if (objectRenderer == null || objectRenderer.material == null) return;

            if (hovering)
            {
                // Changer la couleur en vert
                if (objectRenderer.material.HasProperty("_Color"))
                {
                    objectRenderer.material.SetColor("_Color", hoverColor);
                }

                // Intensifier l'émission en vert
                if (objectRenderer.material.HasProperty("_EmissionColor"))
                {
                    objectRenderer.material.SetColor("_EmissionColor", hoverColor * 2f);
                }
            }
            else
            {
                // Restaurer la couleur originale
                if (objectRenderer.material.HasProperty("_Color"))
                {
                    objectRenderer.material.SetColor("_Color", originalColor);
                }

                // Restaurer l'émission jaune de base (gérée par ProcedureDisplayer)
                if (hasOriginalEmission && objectRenderer.material.HasProperty("_EmissionColor"))
                {
                    objectRenderer.material.SetColor("_EmissionColor", originalEmissionColor);
                }
            }
        }

        void OnObjectClicked()
        {
            if (!isActive) return;

            // Ne logger que si le debug mode est activé dans ContentDisplayManager
            if (UI.ContentDisplayManager.Instance?.DebugMode ?? false)
            {
                Debug.Log($"[ProcedureStepClickHandler] Object {gameObject.name} clicked for step {stepIndex + 1}");
            }

            // Valider l'étape dans le ProcedureDisplayer en passant l'objet cliqué
            if (procedureDisplayer != null && associatedObject != null)
            {
                // Feedback visuel rapide
                if (objectRenderer != null && objectRenderer.material != null)
                {
                    Color originalColor = objectRenderer.material.color;
                    objectRenderer.material.color = Color.white;
                    StartCoroutine(RestoreColorAfterDelay(originalColor, 0.1f));
                }

                // Passer l'objet associé au displayer pour vérification
                procedureDisplayer.ValidateCurrentStep(associatedObject);

                // Ne désactiver ce handler que si c'était la bonne réponse
                // Si mauvaise réponse, ValidateCurrentStep retournera sans passer à l'étape suivante
                // et le handler restera actif pour permettre de réessayer
            }
        }

        System.Collections.IEnumerator RestoreColorAfterDelay(Color originalColor, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (objectRenderer != null && objectRenderer.material != null)
            {
                objectRenderer.material.color = originalColor;
            }
        }


        void OnDestroy()
        {
            // Restaurer la couleur originale si possible
            if (objectRenderer != null && objectRenderer.material != null)
            {
                if (objectRenderer.material.HasProperty("_Color") && originalColor != default(Color))
                {
                    objectRenderer.material.SetColor("_Color", originalColor);
                }
            }
        }

        void OnDisable()
        {
            isActive = false;
            isHovered = false;

            // Restaurer l'état visuel
            if (objectRenderer != null && objectRenderer.material != null)
            {
                if (objectRenderer.material.HasProperty("_Color") && originalColor != default(Color))
                {
                    objectRenderer.material.SetColor("_Color", originalColor);
                }
            }
        }
    }
}