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
        private float hoverScale = 1.05f;
        private Vector3 originalScale;

        public void Initialize(ProcedureDisplayer displayer, int index, GameObject obj)
        {
            procedureDisplayer = displayer;
            stepIndex = index;
            associatedObject = obj;
            isActive = true;
            originalScale = transform.localScale;

            // Récupérer le renderer pour le feedback visuel
            objectRenderer = GetComponent<Renderer>();
            if (objectRenderer != null && objectRenderer.material != null)
            {
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

            // Effectuer le raycast
            RaycastHit hit;
            bool wasHovered = isHovered;

            // Vérifier si on touche cet objet spécifique
            if (Physics.Raycast(ray, out hit, Mathf.Infinity))
            {
                isHovered = (hit.transform == transform || hit.transform.IsChildOf(transform));
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

        void ApplyHoverFeedback(bool hovering)
        {
            if (hovering)
            {
                // Augmenter légèrement l'échelle
                transform.localScale = originalScale * hoverScale;

                // Intensifier l'émission
                if (objectRenderer != null && objectRenderer.material != null &&
                    objectRenderer.material.HasProperty("_EmissionColor"))
                {
                    Color currentEmission = objectRenderer.material.GetColor("_EmissionColor");
                    objectRenderer.material.SetColor("_EmissionColor", currentEmission * 1.3f);
                }
            }
            else
            {
                // Restaurer l'échelle normale
                transform.localScale = originalScale;

                // Restaurer l'émission normale (mais garder la surbrillance de base)
                if (objectRenderer != null && objectRenderer.material != null &&
                    objectRenderer.material.HasProperty("_EmissionColor"))
                {
                    // L'émission de base est gérée par ProcedureDisplayer, on ne fait que retirer le boost
                    Color currentEmission = objectRenderer.material.GetColor("_EmissionColor");
                    objectRenderer.material.SetColor("_EmissionColor", currentEmission / 1.3f);
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
            // S'assurer que l'échelle est restaurée
            if (originalScale != Vector3.zero)
            {
                transform.localScale = originalScale;
            }
        }

        void OnDisable()
        {
            isActive = false;
            isHovered = false;

            // Restaurer l'état visuel
            if (originalScale != Vector3.zero)
            {
                transform.localScale = originalScale;
            }
        }
    }
}