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

        [Header("Visual Feedback")]
        [SerializeField] private bool highlightOnHover = true;
        [SerializeField] private HoverMode hoverMode = HoverMode.MultiplyColor;
        [SerializeField] private Color hoverColor = new Color(1.2f, 1.2f, 1.2f, 1f);
        [SerializeField] private float emissionIntensity = 0.3f;
        [SerializeField] private bool showCursorChange = true;

        public enum HoverMode
        {
            MultiplyColor,      // Multiplie la couleur actuelle
            OverrideColor,      // Remplace la couleur
            EmissionBoost,      // Augmente l'émission
            MaterialSwap        // Change de matériau (nécessite un matériau d'hover)
        }

        [Header("Material Swap (if using MaterialSwap mode)")]
        [SerializeField] private Material hoverMaterial;

        [Header("Content Type")]
        [SerializeField] private ContentType contentType = ContentType.Question;
        [SerializeField] private string specificContentKey = ""; // Laisser vide pour prendre le premier disponible

        [Header("Debug")]
        [SerializeField] private bool debugMode = false;

        // Références
        private ObjectMetadataMapper metadataMapper;
        private Renderer objectRenderer;
        private Material originalMaterial;
        private Material instanceMaterial;
        private Color originalColor;
        private Color originalEmissionColor;
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
                // Stocker le matériau original
                originalMaterial = objectRenderer.sharedMaterial;

                // Créer une instance du matériau pour éviter de modifier l'asset
                instanceMaterial = new Material(originalMaterial);
                objectRenderer.material = instanceMaterial;

                // Stocker les couleurs originales
                if (instanceMaterial.HasProperty("_Color"))
                {
                    originalColor = instanceMaterial.GetColor("_Color");
                }
                else if (instanceMaterial.HasProperty("_BaseColor"))
                {
                    originalColor = instanceMaterial.GetColor("_BaseColor");
                }
                else
                {
                    originalColor = Color.white;
                }

                if (instanceMaterial.HasProperty("_EmissionColor"))
                {
                    originalEmissionColor = instanceMaterial.GetColor("_EmissionColor");
                }
            }

            // S'assurer qu'il y a un collider
            Collider col = GetComponent<Collider>();
            if (col == null)
            {
                col = gameObject.AddComponent<BoxCollider>();
                if (debugMode) Debug.Log($"[InteractableObject] Added BoxCollider to {gameObject.name}");
            }
        }

        void Start()
        {
            // Vérifier que ContentDisplayManager est disponible
            if (ContentDisplayManager.Instance == null && debugMode)
            {
                Debug.LogWarning($"[InteractableObject] ContentDisplayManager not found in scene!");
            }

            // Vérifier si le système de raycast est présent
            var raycastSystem = FindFirstObjectByType<InteractableObjectRaycast>();
            if (raycastSystem == null)
            {
                // Créer automatiquement le système de raycast s'il n'existe pas
                GameObject raycastGO = new GameObject("InteractableObjectRaycast");
                raycastGO.AddComponent<InteractableObjectRaycast>();
                if (debugMode) Debug.Log("[InteractableObject] Created InteractableObjectRaycast system");
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


        void OnMouseEnter()
        {
            if (!highlightOnHover || !isInteractionEnabled) return;

            // Ne pas activer l'hover si une UI est affichée
            if (ContentDisplayManager.Instance != null && ContentDisplayManager.Instance.IsDisplaying)
            {
                return;
            }

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
            if (objectRenderer == null || instanceMaterial == null) return;

            switch (hoverMode)
            {
                case HoverMode.MultiplyColor:
                    // Multiplier la couleur
                    if (instanceMaterial.HasProperty("_Color"))
                    {
                        instanceMaterial.SetColor("_Color", originalColor * hoverColor);
                    }
                    if (instanceMaterial.HasProperty("_BaseColor"))
                    {
                        instanceMaterial.SetColor("_BaseColor", originalColor * hoverColor);
                    }
                    break;

                case HoverMode.OverrideColor:
                    // Remplacer la couleur
                    if (instanceMaterial.HasProperty("_Color"))
                    {
                        instanceMaterial.SetColor("_Color", hoverColor);
                    }
                    if (instanceMaterial.HasProperty("_BaseColor"))
                    {
                        instanceMaterial.SetColor("_BaseColor", hoverColor);
                    }
                    break;

                case HoverMode.EmissionBoost:
                    // Augmenter l'émission
                    if (instanceMaterial.HasProperty("_EmissionColor"))
                    {
                        instanceMaterial.EnableKeyword("_EMISSION");
                        Color boostedEmission = originalEmissionColor + hoverColor * emissionIntensity;
                        instanceMaterial.SetColor("_EmissionColor", boostedEmission);
                    }
                    break;

                case HoverMode.MaterialSwap:
                    // Changer de matériau
                    if (hoverMaterial != null)
                    {
                        objectRenderer.material = hoverMaterial;
                    }
                    break;
            }

        }

        void RemoveHoverEffect()
        {
            if (objectRenderer == null) return;

            switch (hoverMode)
            {
                case HoverMode.MultiplyColor:
                case HoverMode.OverrideColor:
                    // Restaurer les couleurs
                    if (instanceMaterial != null)
                    {
                        if (instanceMaterial.HasProperty("_Color"))
                        {
                            instanceMaterial.SetColor("_Color", originalColor);
                        }
                        if (instanceMaterial.HasProperty("_BaseColor"))
                        {
                            instanceMaterial.SetColor("_BaseColor", originalColor);
                        }
                    }
                    break;

                case HoverMode.EmissionBoost:
                    // Restaurer l'émission
                    if (instanceMaterial != null && instanceMaterial.HasProperty("_EmissionColor"))
                    {
                        instanceMaterial.SetColor("_EmissionColor", originalEmissionColor);
                    }
                    break;

                case HoverMode.MaterialSwap:
                    // Restaurer le matériau original
                    if (instanceMaterial != null)
                    {
                        objectRenderer.material = instanceMaterial;
                    }
                    break;
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

            if (debugMode) Debug.Log($"[InteractableObject] Looking for key: {contentKey} in object data with keys: {string.Join(", ", cachedObjectData.Keys)}");

            if (!string.IsNullOrEmpty(contentKey) && cachedObjectData.ContainsKey(contentKey))
            {
                // Debug pour voir le type exact
                var rawContent = cachedObjectData[contentKey];
                if (debugMode)
                {
                    Debug.Log($"[InteractableObject] Raw content type for '{contentKey}': {rawContent?.GetType()?.FullName ?? "null"}");
                    if (rawContent != null)
                    {
                        string jsonDebug = Newtonsoft.Json.JsonConvert.SerializeObject(rawContent, Newtonsoft.Json.Formatting.Indented);
                        Debug.Log($"[InteractableObject] Content data: {jsonDebug}");
                    }
                }

                // Essayer de convertir en Dictionary
                Dictionary<string, object> contentData = null;

                // Si c'est déjà un Dictionary
                if (rawContent is Dictionary<string, object> dict)
                {
                    contentData = dict;
                }
                // Si c'est un JObject de Newtonsoft
                else if (rawContent != null)
                {
                    try
                    {
                        // Convertir le JObject en Dictionary
                        string json = Newtonsoft.Json.JsonConvert.SerializeObject(rawContent);
                        contentData = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
                    }
                    catch (System.Exception e)
                    {
                        if (debugMode) Debug.LogError($"[InteractableObject] Failed to convert content: {e.Message}");
                    }
                }

                if (contentData != null)
                {
                    if (ContentDisplayManager.Instance != null)
                    {
                        // Désactiver temporairement l'hover sur cet objet
                        if (isHovered)
                        {
                            RemoveHoverEffect();
                            isHovered = false;
                        }

                        if (debugMode) Debug.Log($"[InteractableObject] Displaying content for {objectId} with type {contentType}");
                        ContentDisplayManager.Instance.DisplayContent(objectId, contentType, contentData);
                    }
                    else
                    {
                        Debug.LogError("[InteractableObject] ContentDisplayManager.Instance is null! Add ContentDisplayManager to your scene.");
                    }
                }
                else
                {
                    if (debugMode) Debug.LogWarning($"[InteractableObject] Content data for key '{contentKey}' is not a Dictionary");
                }
            }
            else
            {
                if (debugMode) Debug.LogWarning($"[InteractableObject] No content of type {contentType} found for {objectId}. Keys available: {string.Join(", ", cachedObjectData.Keys)}");
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
                // Vérifier si c'est un Dictionary ou un JObject (Newtonsoft)
                if (value is Dictionary<string, object> ||
                    (value != null && value.GetType().FullName.Contains("JObject")))
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

            // Détruire l'instance du matériau pour éviter les fuites mémoire
            if (instanceMaterial != null)
            {
                Destroy(instanceMaterial);
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