using UnityEngine;
using System.Collections.Generic;
using WiseTwin.UI;

namespace WiseTwin
{
    /// <summary>
    /// Composant pour rendre un objet 3D interactif et déclencher l'affichage de contenu
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
        [SerializeField] private ContentType contentType = ContentType.Question;
        [SerializeField] private string specificContentKey = ""; // Laisser vide pour prendre le premier disponible

        [Header("Debug")]
        [SerializeField] private bool debugMode = false;

        // Références
        private ObjectMetadataMapper metadataMapper;
        private Renderer objectRenderer;
        private Color originalColor;
        private bool isHovered = false;
        private bool isInteractionEnabled = true;

        // Cache des métadonnées
        private Dictionary<string, object> cachedObjectData;

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
            // Vérifier que ContentDisplayManager est disponible
            if (ContentDisplayManager.Instance == null && debugMode)
            {
                Debug.LogWarning($"[InteractableObject] ContentDisplayManager not found in scene!");
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

            // Ne pas activer l'hover si une UI est affichée
            if (ContentDisplayManager.Instance != null && ContentDisplayManager.Instance.IsDisplaying) return;

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

            // Ne pas interagir si une UI est déjà affichée
            if (ContentDisplayManager.Instance != null && ContentDisplayManager.Instance.IsDisplaying) return;

            string objectId = metadataMapper.MetadataId;
            if (debugMode) Debug.Log($"[InteractableObject] Interaction with {objectId} - Type: {contentType}");

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

            // Chercher le contenu selon le type
            string contentKey = !string.IsNullOrEmpty(specificContentKey) ? specificContentKey : GetFirstContentKeyOfType(cachedObjectData);

            if (!string.IsNullOrEmpty(contentKey) && cachedObjectData.ContainsKey(contentKey))
            {
                var contentData = cachedObjectData[contentKey] as Dictionary<string, object>;
                if (contentData != null && ContentDisplayManager.Instance != null)
                {
                    // Désactiver temporairement l'hover sur cet objet
                    if (isHovered)
                    {
                        RemoveHoverEffect();
                        isHovered = false;
                    }

                    ContentDisplayManager.Instance.DisplayContent(objectId, contentType, contentData);
                }
            }
            else
            {
                if (debugMode) Debug.LogWarning($"[InteractableObject] No content of type {contentType} found for {objectId}");
            }
        }

        /// <summary>
        /// Trouve la première clé de contenu correspondant au type configuré
        /// </summary>
        string GetFirstContentKeyOfType(Dictionary<string, object> objectData)
        {
            // Patterns de clés selon le type
            string[] patterns = contentType switch
            {
                ContentType.Question => new[] { "question", "quiz", "qcm" },
                ContentType.Procedure => new[] { "procedure", "steps", "process" },
                ContentType.Media => new[] { "media", "video", "image", "audio" },
                ContentType.Dialogue => new[] { "dialogue", "conversation", "chat" },
                ContentType.Instruction => new[] { "instruction", "info", "guide" },
                _ => new[] { "content" }
            };

            // Chercher une clé qui correspond aux patterns
            foreach (var key in objectData.Keys)
            {
                string lowerKey = key.ToLower();
                foreach (var pattern in patterns)
                {
                    if (lowerKey.Contains(pattern))
                    {
                        return key;
                    }
                }
            }

            // Si aucun pattern ne correspond, retourner la première clé qui semble être du contenu
            foreach (var key in objectData.Keys)
            {
                var value = objectData[key];
                if (value is Dictionary<string, object>)
                {
                    return key;
                }
            }

            return "";
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

        void OnEnable()
        {
            // S'abonner aux événements du ContentDisplayManager
            if (ContentDisplayManager.Instance != null)
            {
                ContentDisplayManager.Instance.OnContentClosed += HandleContentClosed;
            }
        }

        void OnDisable()
        {
            // Se désabonner des événements
            if (ContentDisplayManager.Instance != null)
            {
                ContentDisplayManager.Instance.OnContentClosed -= HandleContentClosed;
            }
        }

        void HandleContentClosed(ContentType type, string objectId)
        {
            // Réactiver les interactions après fermeture d'une UI
            isInteractionEnabled = true;
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