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
        [Header("Visual Fedback")]
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

        [Header("Procedure Settings (Only for Procedure type)")]
        [SerializeField] private bool useDragDropSequence = false;
        [SerializeField] private bool enableYellowHighlight = true; // Option pour activer/désactiver le clignotement jaune
        [SerializeField] private bool keepProgressOnOtherClick = false; // Ne pas réinitialiser la progression en mode procédure
        [SerializeField] private List<GameObject> procedureSequence = new List<GameObject>();

        [Header("Reset Settings (Only for Procedure type)")]
        [SerializeField] private bool useResetScript = false;
        [SerializeField] private MonoBehaviour resetScript = null;

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

        // Progression state
        private bool isInProgressionMode = false;
        private bool isActiveInProgression = false;
        private ProgressionVisibilityMode currentVisibilityMode = ProgressionVisibilityMode.Visible;
        private float originalAlpha = 1f;

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

            // Pour les procédures avec drag & drop, créer les données dynamiquement
            if (contentType == ContentType.Procedure && useDragDropSequence && procedureSequence.Count > 0)
            {
                if (ContentDisplayManager.Instance != null)
                {
                    // Désactiver temporairement l'hover sur cet objet
                    if (isHovered)
                    {
                        RemoveHoverEffect();
                        isHovered = false;
                    }

                    // Créer les données de procédure dynamiquement
                    var procedureData = CreateDynamicProcedureData();

                    if (debugMode) Debug.Log($"[InteractableObject] Displaying drag & drop procedure with {procedureSequence.Count} steps");
                    // Passer l'option de highlight avec les données
                    procedureData["enableHighlight"] = enableYellowHighlight;
                    procedureData["keepProgressOnOtherClick"] = keepProgressOnOtherClick;
                    // Passer le script de reset si configuré
                    if (useResetScript && resetScript != null)
                    {
                        var resetInterface = resetScript as IProcedureReset;
                        if (resetInterface != null)
                        {
                            procedureData["resetScript"] = resetInterface;
                        }
                        else
                        {
                            Debug.LogWarning($"[InteractableObject] Reset script {resetScript.name} does not implement IProcedureReset interface");
                        }
                    }
                    ContentDisplayManager.Instance.DisplayContent(objectId, contentType, procedureData);
                }
                else
                {
                    Debug.LogError("[InteractableObject] ContentDisplayManager.Instance is null! Add ContentDisplayManager to your scene.");
                }
                return;
            }

            // Pour les questions, on doit passer TOUTES les données de l'objet pour supporter les questions multiples
            if (contentType == ContentType.Question)
            {
                // Vérifier qu'il y a au moins une question
                bool hasQuestions = false;
                foreach (var key in cachedObjectData.Keys)
                {
                    if (key.StartsWith("question"))
                    {
                        hasQuestions = true;
                        break;
                    }
                }

                if (hasQuestions)
                {
                    if (ContentDisplayManager.Instance != null)
                    {
                        // Désactiver temporairement l'hover sur cet objet
                        if (isHovered)
                        {
                            RemoveHoverEffect();
                            isHovered = false;
                        }

                        if (debugMode) Debug.Log($"[InteractableObject] Displaying questions for {objectId}");

                        // Passer toutes les données de l'objet pour que QuestionDisplayer puisse trouver toutes les questions
                        ContentDisplayManager.Instance.DisplayContent(objectId, contentType, cachedObjectData);
                    }
                    else
                    {
                        Debug.LogError("[InteractableObject] ContentDisplayManager.Instance is null! Add ContentDisplayManager to your scene.");
                    }
                }
                else
                {
                    if (debugMode) Debug.LogWarning($"[InteractableObject] No questions found for {objectId}. Keys available: {string.Join(", ", cachedObjectData.Keys)}");
                }
            }
            else
            {
                // Pour les autres types de contenu, on continue avec l'ancienne logique
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
                            // Ajouter l'option de highlight si c'est une procédure
                            if (contentType == ContentType.Procedure)
                            {
                                contentData["enableHighlight"] = enableYellowHighlight;
                                contentData["keepProgressOnOtherClick"] = keepProgressOnOtherClick;
                                // Passer le script de reset si configuré
                                if (useResetScript && resetScript != null)
                                {
                                    var resetInterface = resetScript as IProcedureReset;
                                    if (resetInterface != null)
                                    {
                                        contentData["resetScript"] = resetInterface;
                                    }
                                    else
                                    {
                                        Debug.LogWarning($"[InteractableObject] Reset script {resetScript.name} does not implement IProcedureReset interface");
                                    }
                                }
                            }
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
        }

        /// <summary>
        /// Crée les données de procédure en combinant les métadatas et les GameObjects drag & drop
        /// </summary>
        Dictionary<string, object> CreateDynamicProcedureData()
        {
            var procedureData = new Dictionary<string, object>();

            // Chercher la procédure dans les métadatas
            Dictionary<string, object> metadataProcedure = null;

            // Si une clé spécifique est définie, l'utiliser
            if (!string.IsNullOrEmpty(specificContentKey) && cachedObjectData.ContainsKey(specificContentKey))
            {
                var rawData = cachedObjectData[specificContentKey];
                if (rawData is Dictionary<string, object> dict)
                {
                    metadataProcedure = dict;
                }
                else if (rawData != null)
                {
                    // Convertir si c'est un JObject
                    string json = Newtonsoft.Json.JsonConvert.SerializeObject(rawData);
                    metadataProcedure = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
                }
            }
            // Sinon chercher la première procédure
            else
            {
                foreach (var key in cachedObjectData.Keys)
                {
                    if (key.ToLower().Contains("procedure"))
                    {
                        var rawData = cachedObjectData[key];
                        if (rawData is Dictionary<string, object> dict)
                        {
                            metadataProcedure = dict;
                            break;
                        }
                        else if (rawData != null)
                        {
                            string json = Newtonsoft.Json.JsonConvert.SerializeObject(rawData);
                            metadataProcedure = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
                            break;
                        }
                    }
                }
            }

            // Si on a trouvé des métadatas de procédure, les utiliser pour titre et description
            if (metadataProcedure != null)
            {
                // Ajouter l'option de highlight aux données
                procedureData["enableHighlight"] = enableYellowHighlight;
                procedureData["keepProgressOnOtherClick"] = keepProgressOnOtherClick;
                // Ajouter le script de reset si configuré
                if (useResetScript && resetScript != null)
                {
                    var resetInterface = resetScript as IProcedureReset;
                    if (resetInterface != null)
                    {
                        procedureData["resetScript"] = resetInterface;
                    }
                    else
                    {
                        Debug.LogWarning($"[InteractableObject] Reset script {resetScript.name} does not implement IProcedureReset interface");
                    }
                }

                // Copier le titre et la description depuis les métadatas
                if (metadataProcedure.ContainsKey("title"))
                    procedureData["title"] = metadataProcedure["title"];
                else
                    procedureData["title"] = new Dictionary<string, object> { ["en"] = "Procedure", ["fr"] = "Procédure" };

                if (metadataProcedure.ContainsKey("description"))
                    procedureData["description"] = metadataProcedure["description"];
                else
                    procedureData["description"] = new Dictionary<string, object> { ["en"] = "Follow the steps", ["fr"] = "Suivez les étapes" };

                // Créer les étapes en combinant métadatas et GameObjects
                for (int i = 0; i < procedureSequence.Count; i++)
                {
                    var stepObj = procedureSequence[i];
                    if (stepObj == null) continue;

                    string stepKey = $"step_{i + 1}";

                    // Récupérer l'ID metadata de l'objet
                    string objectId = "";
                    var mapper = stepObj.GetComponent<ObjectMetadataMapper>();
                    if (mapper != null)
                    {
                        objectId = mapper.MetadataId;
                    }
                    else
                    {
                        objectId = stepObj.name.ToLower().Replace(" ", "_");
                    }

                    // Vérifier si on a des données pour cette étape dans les métadatas
                    if (metadataProcedure.ContainsKey(stepKey))
                    {
                        // Utiliser les données des métadatas mais remplacer l'objectId par celui du GameObject
                        var stepData = metadataProcedure[stepKey];
                        if (stepData is Dictionary<string, object> stepDict)
                        {
                            var modifiedStep = new Dictionary<string, object>(stepDict);
                            modifiedStep["objectId"] = objectId;
                            procedureData[stepKey] = modifiedStep;
                        }
                        else if (stepData != null)
                        {
                            string json = Newtonsoft.Json.JsonConvert.SerializeObject(stepData);
                            var convertedStepDict = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
                            convertedStepDict["objectId"] = objectId;
                            procedureData[stepKey] = convertedStepDict;
                        }
                    }
                    else
                    {
                        // Si pas de métadatas pour cette étape, créer des données par défaut
                        procedureData[stepKey] = new Dictionary<string, object>
                        {
                            ["objectId"] = objectId,
                            ["instruction"] = new Dictionary<string, object>
                            {
                                ["en"] = $"Step {i + 1}: Interact with the highlighted object",
                                ["fr"] = $"Étape {i + 1}: Interagissez avec l'objet surligné"
                            },
                            ["validation"] = new Dictionary<string, object>
                            {
                                ["en"] = "Object interaction validated",
                                ["fr"] = "Interaction avec l'objet validée"
                            },
                            ["hint"] = new Dictionary<string, object>
                            {
                                ["en"] = "Click on the yellow highlighted object",
                                ["fr"] = "Cliquez sur l'objet surligné en jaune"
                            }
                        };
                    }

                    // S'assurer que l'objet a un ObjectMetadataMapper
                    if (mapper == null)
                    {
                        mapper = stepObj.AddComponent<ObjectMetadataMapper>();
                        mapper.MetadataId = objectId;
                        if (debugMode) Debug.Log($"[InteractableObject] Added ObjectMetadataMapper to {stepObj.name} with ID: {objectId}");
                    }
                }
            }
            else
            {
                // Si pas de métadatas, utiliser les valeurs par défaut
                if (debugMode) Debug.LogWarning("[InteractableObject] No procedure metadata found, using default values");

                procedureData["title"] = new Dictionary<string, object>
                {
                    ["en"] = "Procedure",
                    ["fr"] = "Procédure"
                };

                procedureData["description"] = new Dictionary<string, object>
                {
                    ["en"] = "Follow the steps in order",
                    ["fr"] = "Suivez les étapes dans l'ordre"
                };

                // Créer les étapes par défaut
                for (int i = 0; i < procedureSequence.Count; i++)
                {
                    var stepObj = procedureSequence[i];
                    if (stepObj == null) continue;

                    string stepKey = $"step_{i + 1}";
                    string objectId = "";
                    var mapper = stepObj.GetComponent<ObjectMetadataMapper>();
                    if (mapper != null)
                    {
                        objectId = mapper.MetadataId;
                    }
                    else
                    {
                        objectId = stepObj.name.ToLower().Replace(" ", "_");
                        mapper = stepObj.AddComponent<ObjectMetadataMapper>();
                        mapper.MetadataId = objectId;
                    }

                    procedureData[stepKey] = new Dictionary<string, object>
                    {
                        ["objectId"] = objectId,
                        ["instruction"] = new Dictionary<string, object>
                        {
                            ["en"] = $"Step {i + 1}: Click on {stepObj.name}",
                            ["fr"] = $"Étape {i + 1}: Cliquez sur {stepObj.name}"
                        },
                        ["validation"] = new Dictionary<string, object>
                        {
                            ["en"] = "Step completed",
                            ["fr"] = "Étape terminée"
                        },
                        ["hint"] = new Dictionary<string, object>
                        {
                            ["en"] = "Look for the highlighted object",
                            ["fr"] = "Cherchez l'objet surligné"
                        }
                    };
                }
            }

            return procedureData;
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
                ContentType.Text => new[] { "text", "info", "content" },
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

        #region Progression System

        /// <summary>
        /// Active ou désactive cet objet dans un système de progression guidée
        /// </summary>
        /// <param name="isActive">Si true, l'objet est l'étape actuelle</param>
        /// <param name="mode">Mode de visibilité pour les objets non actifs</param>
        public void SetProgressionState(bool isActive, ProgressionVisibilityMode mode)
        {
            isInProgressionMode = true;
            isActiveInProgression = isActive;
            currentVisibilityMode = mode;

            if (isActive)
            {
                // Objet actif : le rendre complètement interactable
                RestoreVisibility();
                SetInteractionEnabled(true);

                if (debugMode) Debug.Log($"[InteractableObject] {gameObject.name} is now ACTIVE in progression");
            }
            else
            {
                // Objet non actif : appliquer le mode de visibilité
                ApplyVisibilityMode(mode);
                SetInteractionEnabled(false);

                if (debugMode) Debug.Log($"[InteractableObject] {gameObject.name} is now INACTIVE with mode {mode}");
            }
        }

        /// <summary>
        /// Désactive le mode progression et restaure l'état normal
        /// </summary>
        public void ExitProgressionMode()
        {
            isInProgressionMode = false;
            isActiveInProgression = false;
            RestoreVisibility();
            SetInteractionEnabled(true);

            if (debugMode) Debug.Log($"[InteractableObject] {gameObject.name} exited progression mode");
        }

        /// <summary>
        /// Indique si cet objet est en mode progression
        /// </summary>
        public bool IsInProgressionMode => isInProgressionMode;

        /// <summary>
        /// Indique si cet objet est l'étape active dans la progression
        /// </summary>
        public bool IsActiveInProgression => isActiveInProgression;

        /// <summary>
        /// Applique le mode de visibilité aux objets non actifs
        /// </summary>
        private void ApplyVisibilityMode(ProgressionVisibilityMode mode)
        {
            if (debugMode) Debug.Log($"[InteractableObject] {gameObject.name} - Applying visibility mode: {mode}");

            switch (mode)
            {
                case ProgressionVisibilityMode.Visible:
                    // Reste visible et interactable (juste désactiver l'interaction)
                    RestoreVisibility();
                    break;

                case ProgressionVisibilityMode.Transparent:
                    // Rendre semi-transparent
                    if (debugMode) Debug.Log($"[InteractableObject] {gameObject.name} - Applying transparency 0.3f");
                    ApplyTransparency(0.3f);
                    break;

                case ProgressionVisibilityMode.Hidden:
                    // Rendre complètement invisible en désactivant le renderer et le collider
                    if (debugMode) Debug.Log($"[InteractableObject] {gameObject.name} - Disabling renderer and collider (hidden)");
                    if (objectRenderer != null)
                    {
                        objectRenderer.enabled = false;
                    }
                    // Désactiver aussi le collider pour ne pas intercepter les clics
                    Collider col = GetComponent<Collider>();
                    if (col != null)
                    {
                        col.enabled = false;
                        if (debugMode) Debug.Log($"[InteractableObject] {gameObject.name} - Collider disabled");
                    }
                    break;

                case ProgressionVisibilityMode.Disabled:
                    // Désactiver le GameObject complètement (pas recommandé pour la progression)
                    if (debugMode) Debug.Log($"[InteractableObject] {gameObject.name} - Disabling GameObject");
                    gameObject.SetActive(false);
                    break;
            }
        }

        /// <summary>
        /// Applique une transparence au matériau
        /// </summary>
        private void ApplyTransparency(float alpha)
        {
            if (objectRenderer == null || instanceMaterial == null)
            {
                if (debugMode) Debug.LogWarning($"[InteractableObject] {gameObject.name} - Cannot apply transparency: objectRenderer={objectRenderer}, instanceMaterial={instanceMaterial}");
                return;
            }

            if (debugMode) Debug.Log($"[InteractableObject] {gameObject.name} - ApplyTransparency({alpha}) - Material: {instanceMaterial.name}, Shader: {instanceMaterial.shader.name}");

            // Sauvegarder l'alpha original si ce n'est pas déjà fait
            if (originalAlpha == 1f && alpha < 1f)
            {
                if (instanceMaterial.HasProperty("_Color"))
                {
                    originalAlpha = instanceMaterial.GetColor("_Color").a;
                }
                else if (instanceMaterial.HasProperty("_BaseColor"))
                {
                    originalAlpha = instanceMaterial.GetColor("_BaseColor").a;
                }
            }

            // Activer le mode transparent si nécessaire
            if (alpha < 1f)
            {
                // Pour Standard shader (Built-in)
                if (instanceMaterial.HasProperty("_Mode"))
                {
                    instanceMaterial.SetFloat("_Mode", 3); // Transparent mode
                    instanceMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    instanceMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    instanceMaterial.SetInt("_ZWrite", 0);
                    instanceMaterial.DisableKeyword("_ALPHATEST_ON");
                    instanceMaterial.EnableKeyword("_ALPHABLEND_ON");
                    instanceMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    instanceMaterial.renderQueue = 3000;
                }
                // Pour URP shader (Universal Render Pipeline)
                else if (instanceMaterial.HasProperty("_Surface"))
                {
                    if (debugMode) Debug.Log($"[InteractableObject] {gameObject.name} - Configuring URP Transparent mode");

                    // Set Surface Type to Transparent
                    instanceMaterial.SetFloat("_Surface", 1); // 0 = Opaque, 1 = Transparent

                    // Set Blend Mode to Alpha
                    instanceMaterial.SetFloat("_Blend", 0); // 0 = Alpha, 1 = Premultiply, 2 = Additive, 3 = Multiply

                    // Configure blend operations
                    instanceMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    instanceMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    instanceMaterial.SetInt("_ZWrite", 0);

                    // Enable alpha clipping if available
                    if (instanceMaterial.HasProperty("_AlphaClip"))
                    {
                        instanceMaterial.SetFloat("_AlphaClip", 0);
                    }

                    // Set render queue for transparency
                    instanceMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

                    // Enable/disable keywords for URP
                    instanceMaterial.DisableKeyword("_ALPHATEST_ON");
                    instanceMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    instanceMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    instanceMaterial.EnableKeyword("_BLENDMODE_ALPHA");

                    if (debugMode) Debug.Log($"[InteractableObject] {gameObject.name} - URP Transparent mode configured");
                }
            }

            // Appliquer l'alpha
            if (instanceMaterial.HasProperty("_Color"))
            {
                Color color = instanceMaterial.GetColor("_Color");
                color.a = alpha;
                instanceMaterial.SetColor("_Color", color);
            }
            else if (instanceMaterial.HasProperty("_BaseColor"))
            {
                Color color = instanceMaterial.GetColor("_BaseColor");
                color.a = alpha;
                instanceMaterial.SetColor("_BaseColor", color);
            }
        }

        /// <summary>
        /// Restaure la visibilité complète de l'objet
        /// </summary>
        private void RestoreVisibility()
        {
            // S'assurer que le renderer est actif
            if (objectRenderer != null && !objectRenderer.enabled)
            {
                objectRenderer.enabled = true;
                if (debugMode) Debug.Log($"[InteractableObject] {gameObject.name} - Renderer re-enabled");
            }

            if (currentVisibilityMode == ProgressionVisibilityMode.Disabled)
            {
                if (!gameObject.activeSelf)
                {
                    gameObject.SetActive(true);
                }
            }
            else if (currentVisibilityMode == ProgressionVisibilityMode.Hidden)
            {
                // Réactiver aussi le collider
                Collider col = GetComponent<Collider>();
                if (col != null)
                {
                    col.enabled = true;
                    if (debugMode) Debug.Log($"[InteractableObject] {gameObject.name} - Collider re-enabled");
                }
                if (debugMode) Debug.Log($"[InteractableObject] {gameObject.name} - Restored from Hidden mode");
            }
            else if (currentVisibilityMode == ProgressionVisibilityMode.Transparent)
            {
                // Restaurer l'opacité complète
                ApplyTransparency(originalAlpha);

                // Restaurer le mode opaque si nécessaire
                if (instanceMaterial != null)
                {
                    // Pour Standard shader (Built-in)
                    if (instanceMaterial.HasProperty("_Mode"))
                    {
                        instanceMaterial.SetFloat("_Mode", 0); // Opaque mode
                        instanceMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                        instanceMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                        instanceMaterial.SetInt("_ZWrite", 1);
                        instanceMaterial.DisableKeyword("_ALPHATEST_ON");
                        instanceMaterial.DisableKeyword("_ALPHABLEND_ON");
                        instanceMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                        instanceMaterial.renderQueue = -1;
                    }
                    // Pour URP shader (Universal Render Pipeline)
                    else if (instanceMaterial.HasProperty("_Surface"))
                    {
                        if (debugMode) Debug.Log($"[InteractableObject] {gameObject.name} - Restoring URP Opaque mode");

                        // Set Surface Type to Opaque
                        instanceMaterial.SetFloat("_Surface", 0); // 0 = Opaque, 1 = Transparent

                        // Configure blend operations for opaque
                        instanceMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                        instanceMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                        instanceMaterial.SetInt("_ZWrite", 1);

                        // Set render queue back to default
                        instanceMaterial.renderQueue = -1;

                        // Disable transparency keywords
                        instanceMaterial.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
                        instanceMaterial.DisableKeyword("_BLENDMODE_ALPHA");
                        instanceMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                        instanceMaterial.DisableKeyword("_ALPHATEST_ON");

                        if (debugMode) Debug.Log($"[InteractableObject] {gameObject.name} - URP Opaque mode restored");
                    }
                }
            }
        }

        #endregion
    }
}