using UnityEngine;
using System.Collections.Generic;

namespace WiseTwin
{
    /// <summary>
    /// Composant pour rendre un objet 3D interactif et déclencher l'affichage de questions
    /// Travaille en conjonction avec ObjectMetadataMapper pour récupérer l'ID
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class InteractableObject : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private bool useMouseClick = true;

        [Header("Visual Feedback")]
        [SerializeField] private bool highlightOnHover = true;
        [SerializeField] private Color hoverColor = new Color(1.2f, 1.2f, 1.2f, 1f);
        [SerializeField] private bool showCursorChange = true;

        [Header("Content Type")]
        [SerializeField] private ContentInteractionType interactionType = ContentInteractionType.Question;
        [SerializeField] private string specificContentKey = ""; // Laisser vide pour prendre le premier disponible

        [Header("Debug")]
        [SerializeField] private bool debugMode = false;

        // Références
        private ObjectMetadataMapper metadataMapper;
        private QuestionController questionController;
        private Renderer objectRenderer;
        private Color originalColor;
        private bool isHovered = false;
        private bool isInteractionEnabled = true;

        // Cache des métadonnées
        private Dictionary<string, object> cachedObjectData;

        public enum ContentInteractionType
        {
            Question,
            Procedure,
            Dialogue,
            Media,
            Custom
        }

        void Awake()
        {
            // Récupérer les composants
            metadataMapper = GetComponent<ObjectMetadataMapper>();
            if (metadataMapper == null)
            {
                metadataMapper = gameObject.AddComponent<ObjectMetadataMapper>();
                if (debugMode) Debug.Log($"[InteractableObject] Added ObjectMetadataMapper to {gameObject.name}");
            }

            objectRenderer = GetComponent<Renderer>();
            if (objectRenderer != null && objectRenderer.material != null)
            {
                originalColor = objectRenderer.material.color;
            }

            // S'assurer qu'il y a un collider
            Collider col = GetComponent<Collider>();
            if (col == null)
            {
                col = gameObject.AddComponent<BoxCollider>();
                if (debugMode) Debug.Log($"[InteractableObject] Added BoxCollider to {gameObject.name}");
            }

            // Le collider doit être en mode Trigger ou non selon la configuration
            if (useMouseClick && !col.isTrigger)
            {
                // Pour OnMouseDown, le collider ne doit pas être trigger
                col.isTrigger = false;
            }
        }

        void Start()
        {
            // Trouver le QuestionController
            questionController = FindFirstObjectByType<QuestionController>();
            if (questionController == null && debugMode)
            {
                Debug.LogWarning($"[InteractableObject] QuestionController not found in scene!");
            }

            // Précharger les données si possible
            PreloadMetadata();
        }

        void PreloadMetadata()
        {
            if (WiseTwinManager.Instance != null && WiseTwinManager.Instance.IsMetadataLoaded)
            {
                string objectId = metadataMapper.MetadataId;
                cachedObjectData = WiseTwinManager.Instance.GetDataForObject(objectId);

                if (debugMode && cachedObjectData != null)
                {
                    Debug.Log($"[InteractableObject] Preloaded metadata for {objectId}");
                }
            }
        }

        void OnMouseDown()
        {
            if (!useMouseClick || !isInteractionEnabled) return;

            HandleInteraction();
        }

        void OnMouseEnter()
        {
            if (!highlightOnHover || !isInteractionEnabled) return;

            isHovered = true;
            ApplyHoverEffect();

            if (showCursorChange)
            {
                // Note: Dans Unity, changer le curseur nécessite un setup supplémentaire
                // Cursor.SetCursor(customCursor, hotSpot, cursorMode);
            }
        }

        void OnMouseExit()
        {
            if (!highlightOnHover) return;

            isHovered = false;
            RemoveHoverEffect();
        }

        void ApplyHoverEffect()
        {
            if (objectRenderer != null && objectRenderer.material != null)
            {
                objectRenderer.material.color = originalColor * hoverColor.r;
            }
        }

        void RemoveHoverEffect()
        {
            if (objectRenderer != null && objectRenderer.material != null)
            {
                objectRenderer.material.color = originalColor;
            }
        }

        public void HandleInteraction()
        {
            if (!isInteractionEnabled) return;

            string objectId = metadataMapper.MetadataId;
            if (debugMode) Debug.Log($"[InteractableObject] Interaction with {objectId}");

            // Récupérer les données de l'objet
            if (cachedObjectData == null)
            {
                cachedObjectData = WiseTwinManager.Instance?.GetDataForObject(objectId);
            }

            if (cachedObjectData == null)
            {
                if (debugMode) Debug.LogWarning($"[InteractableObject] No metadata found for {objectId}");
                return;
            }

            // Traiter selon le type d'interaction
            switch (interactionType)
            {
                case ContentInteractionType.Question:
                    HandleQuestionInteraction(objectId, cachedObjectData);
                    break;

                case ContentInteractionType.Procedure:
                    HandleProcedureInteraction(objectId, cachedObjectData);
                    break;

                case ContentInteractionType.Dialogue:
                    HandleDialogueInteraction(objectId, cachedObjectData);
                    break;

                case ContentInteractionType.Media:
                    HandleMediaInteraction(objectId, cachedObjectData);
                    break;

                case ContentInteractionType.Custom:
                    HandleCustomInteraction(objectId, cachedObjectData);
                    break;
            }
        }

        void HandleQuestionInteraction(string objectId, Dictionary<string, object> objectData)
        {
            // Chercher une question dans les données
            string questionKey = !string.IsNullOrEmpty(specificContentKey) ? specificContentKey : "question_1";

            if (objectData.ContainsKey(questionKey))
            {
                var questionData = objectData[questionKey] as Dictionary<string, object>;

                if (questionData != null)
                {
                    if (debugMode) Debug.Log($"[InteractableObject] Found question data for {objectId}.{questionKey}");

                    // Envoyer au QuestionController
                    if (questionController != null)
                    {
                        questionController.ShowQuestion(objectId, questionKey, questionData);
                    }
                    else
                    {
                        // Fallback: essayer d'utiliser directement WiseTwinUIManager
                        ShowQuestionFallback(questionData);
                    }
                }
            }
            else
            {
                if (debugMode) Debug.LogWarning($"[InteractableObject] No question '{questionKey}' found for {objectId}");
            }
        }

        void ShowQuestionFallback(Dictionary<string, object> questionData)
        {
            // Utiliser LocalizationManager pour obtenir la langue
            string lang = LocalizationManager.Instance != null ? LocalizationManager.Instance.CurrentLanguage : "en";

            // Extraire les données selon la langue
            string questionText = ExtractLocalizedText(questionData, "text", lang);

            if (!string.IsNullOrEmpty(questionText))
            {
                // Essayer d'afficher via WiseTwinUIManager
                if (WiseTwinUIManager.Instance != null)
                {
                    // Pour l'instant, afficher juste le texte
                    // TODO: Adapter ShowQuestion pour accepter les données complètes
                    WiseTwinUIManager.Instance.ShowNotification(questionText, NotificationType.Info);
                }
            }
        }

        string ExtractLocalizedText(Dictionary<string, object> data, string key, string language)
        {
            if (data.ContainsKey(key))
            {
                var textData = data[key];

                // Si c'est déjà une string, la retourner
                if (textData is string simpleText)
                {
                    return simpleText;
                }

                // Si c'est un dictionnaire de langues
                if (textData is Dictionary<string, object> localizedText)
                {
                    if (localizedText.ContainsKey(language))
                    {
                        return localizedText[language]?.ToString();
                    }
                    // Fallback to English
                    if (localizedText.ContainsKey("en"))
                    {
                        return localizedText["en"]?.ToString();
                    }
                }
            }

            return "";
        }

        void HandleProcedureInteraction(string objectId, Dictionary<string, object> objectData)
        {
            if (debugMode) Debug.Log($"[InteractableObject] Procedure interaction not yet implemented for {objectId}");
            // TODO: Implémenter l'affichage des procédures
        }

        void HandleDialogueInteraction(string objectId, Dictionary<string, object> objectData)
        {
            if (debugMode) Debug.Log($"[InteractableObject] Dialogue interaction not yet implemented for {objectId}");
            // TODO: Implémenter l'affichage des dialogues
        }

        void HandleMediaInteraction(string objectId, Dictionary<string, object> objectData)
        {
            if (debugMode) Debug.Log($"[InteractableObject] Media interaction not yet implemented for {objectId}");
            // TODO: Implémenter l'affichage des médias
        }

        void HandleCustomInteraction(string objectId, Dictionary<string, object> objectData)
        {
            if (debugMode) Debug.Log($"[InteractableObject] Custom interaction for {objectId}");
            // Les classes dérivées peuvent override cette méthode
            OnCustomInteraction(objectId, objectData);
        }

        /// <summary>
        /// Méthode virtuelle pour les interactions personnalisées
        /// </summary>
        protected virtual void OnCustomInteraction(string objectId, Dictionary<string, object> objectData)
        {
            // À override dans les classes dérivées
        }

        /// <summary>
        /// Active/désactive l'interaction
        /// </summary>
        public void SetInteractionEnabled(bool enabled)
        {
            isInteractionEnabled = enabled;

            if (!enabled && isHovered)
            {
                RemoveHoverEffect();
                isHovered = false;
            }
        }

        /// <summary>
        /// Force le rechargement des métadonnées
        /// </summary>
        public void RefreshMetadata()
        {
            cachedObjectData = null;
            PreloadMetadata();
        }

        void OnDestroy()
        {
            // Nettoyer les effets visuels
            if (isHovered)
            {
                RemoveHoverEffect();
            }
        }

        /// <summary>
        /// Pour les tests dans l'éditeur
        /// </summary>
        [ContextMenu("Test Interaction")]
        public void TestInteraction()
        {
            HandleInteraction();
        }
    }
}