using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System;
using System.Linq;
using WiseTwin.Analytics;

namespace WiseTwin.UI
{
    /// <summary>
    /// Afficheur spécialisé pour les procédures séquentielles
    /// Guide l'utilisateur à travers une séquence d'objets 3D à interagir dans le bon ordre
    /// </summary>
    public class ProcedureDisplayer : MonoBehaviour, IContentDisplayer
    {
        public event Action<string> OnClosed;
        public event Action<string, bool> OnCompleted;

        [Header("Visual Settings")]
        [SerializeField] private Color highlightColor = new Color(1f, 0.9f, 0.3f, 1f); // Jaune
        [SerializeField] private float highlightIntensity = 3.5f; // Augmenté pour plus de visibilité
        [SerializeField] private bool pulseHighlight = true; // Pulse jaune quand pas de survol
        [SerializeField] private float pulseSpeed = 3f; // Augmenté pour une pulsation plus visible

        private string currentObjectId;
        private VisualElement rootElement;
        private VisualElement modalContainer;

        // Données de la procédure
        private string procedureKey;        // Clé de la procédure pour tracking
        private string procedureTitle;
        private string procedureDescription;
        private List<ProcedureStep> steps;
        private List<FakeObjectData> fakeObjects; // NEW: Fake objects that show error messages
        private int currentStepIndex = 0;
        private float procedureStartTime;   // Temps de début de la procédure

        // GameObjects de la séquence
        private List<GameObject> allSequenceObjects; // Tous les objets (target + fake) de toutes les étapes
        private Dictionary<GameObject, Material> originalMaterials;
        private List<GameObject> currentHighlightedObjects = new List<GameObject>(); // Objets surlignés à l'étape actuelle
        private GameObject currentCorrectObject; // L'objet correct de l'étape actuelle
        private bool shouldHighlight = true; // Contrôle si on doit surligner ou non
        private bool keepProgressOnOtherClick = false; // Ne pas réinitialiser à 0 si on clique ailleurs

        // UI Elements
        private Label titleLabel;
        private Label descriptionLabel;
        private Label stepLabel;
        private Label progressLabel;
        private Label errorFeedbackLabel;
        private VisualElement progressBar;
        private VisualElement progressFill;
        private Button validateButton; // NEW: Manual validation button
        private VisualElement imageContainer; // NEW: Container for step image
        private VisualElement imageElement; // NEW: The actual image element
        private bool imageZoomed = false; // NEW: Track image zoom state

        // Analytics tracking
        private float stepStartTime;
        private int wrongClicksCount = 0; // Erreurs sur l'étape en cours
        private int totalWrongClicksInProcedure = 0; // Compteur global d'erreurs pour toute la procédure
        private List<ProcedureStepData> completedSteps; // Liste des étapes complétées pour tracking

        public class ProcedureStep
        {
            public string targetObjectName; // Object name instead of ID
            public string title;
            public string instruction;
            public string validation;
            public string hint;
            public GameObject targetObject; // renamed from correctObject
            public bool completed = false;
            public Color highlightColor = Color.yellow;
            public bool useBlinking = true;
            public string validationType = "click"; // "click", "manual", "zone"
            public string zoneObjectName; // Name of the zone trigger object (when validationType == "zone")
            public GameObject zoneObject; // Resolved zone trigger GameObject
            public bool requireManualValidation => validationType == "manual"; // Backward compat read-only
            public string imagePath; // Path to the image for this step
            public List<FakeObjectData> fakeObjects = new List<FakeObjectData>(); // Fake objects specific to this step
        }

        public class FakeObjectData
        {
            public string objectName;
            public string errorMessage;
            public GameObject gameObject;
        }

        public void Display(string objectId, Dictionary<string, object> contentData, VisualElement root)
        {
            currentObjectId = objectId;
            rootElement = root;
            currentStepIndex = 0;
            totalWrongClicksInProcedure = 0; // Réinitialiser le compteur d'erreurs
            procedureStartTime = Time.time; // Enregistrer le temps de début
            completedSteps = new List<ProcedureStepData>(); // Initialiser la liste des étapes

            // Vérifier si on doit activer le highlight
            if (contentData.ContainsKey("enableHighlight"))
            {
                shouldHighlight = contentData["enableHighlight"] is bool highlight ? highlight : true;
                Debug.Log($"[ProcedureDisplayer] Highlight enabled: {shouldHighlight}");
            }
            else
            {
                shouldHighlight = true; // Par défaut, on active le highlight
            }

            // Vérifier si on doit garder la progression lors d'un clic ailleurs
            if (contentData.ContainsKey("keepProgressOnOtherClick"))
            {
                keepProgressOnOtherClick = contentData["keepProgressOnOtherClick"] is bool keepProgress ? keepProgress : false;
                Debug.Log($"[ProcedureDisplayer] Keep progress on other click: {keepProgressOnOtherClick}");
            }
            else
            {
                keepProgressOnOtherClick = false; // Par défaut, on reset
            }

            // Obtenir la langue actuelle
            string lang = LocalizationManager.Instance?.CurrentLanguage ?? "en";

            // Trouver la clé de la procédure (la première clé qui commence par "procedure_")
            procedureKey = contentData.Keys.FirstOrDefault(k => k.StartsWith("procedure_"));
            if (string.IsNullOrEmpty(procedureKey))
            {
                // Fallback : chercher n'importe quelle clé de procédure
                procedureKey = "procedure";
                Debug.LogWarning($"[ProcedureDisplayer] No procedure_ key found in contentData, using fallback 'procedure'");
            }

            // Extraire les données de la procédure
            procedureTitle = ExtractLocalizedText(contentData, "title", lang);
            procedureDescription = ExtractLocalizedText(contentData, "description", lang);

            // Extraire les étapes
            steps = ExtractProcedureSteps(contentData, lang);

            if (steps == null || steps.Count == 0)
            {
                Debug.LogError($"[ProcedureDisplayer] No steps found for procedure {objectId}");
                return;
            }

            // Initialiser les matériaux originaux
            originalMaterials = new Dictionary<GameObject, Material>();
            allSequenceObjects = new List<GameObject>();

            // Trouver les GameObjects pour chaque étape par nom
            foreach (var step in steps)
            {
                // Chercher l'objet target par nom
                if (!string.IsNullOrEmpty(step.targetObjectName))
                {
                    step.targetObject = GameObject.Find(step.targetObjectName);

                    if (step.targetObject != null)
                    {
                        allSequenceObjects.Add(step.targetObject);

                        // Stocker le matériau original
                        var renderer = step.targetObject.GetComponent<Renderer>();
                        if (renderer != null)
                        {
                            originalMaterials[step.targetObject] = renderer.material;
                        }

                        Debug.Log($"[ProcedureDisplayer] Found target object for step: {step.targetObjectName}");
                    }
                    else
                    {
                        Debug.LogWarning($"[ProcedureDisplayer] Could not find GameObject with name: {step.targetObjectName}");
                    }
                }

                // Find fake objects for this step
                if (step.fakeObjects != null && step.fakeObjects.Count > 0)
                {
                    foreach (var fake in step.fakeObjects)
                    {
                        if (string.IsNullOrEmpty(fake.objectName)) continue;

                        fake.gameObject = GameObject.Find(fake.objectName);

                        if (fake.gameObject != null)
                        {
                            allSequenceObjects.Add(fake.gameObject);

                            // Stocker le matériau original
                            var renderer = fake.gameObject.GetComponent<Renderer>();
                            if (renderer != null && !originalMaterials.ContainsKey(fake.gameObject))
                            {
                                originalMaterials[fake.gameObject] = renderer.material;
                            }

                            Debug.Log($"[ProcedureDisplayer] Found step-specific fake object: {fake.objectName}");
                        }
                        else
                        {
                            Debug.LogWarning($"[ProcedureDisplayer] Could not find step-specific fake GameObject with name: {fake.objectName}");
                        }
                    }
                }

                // Find zone trigger object for this step
                if (step.validationType == "zone" && !string.IsNullOrEmpty(step.zoneObjectName))
                {
                    step.zoneObject = GameObject.Find(step.zoneObjectName);
                    if (step.zoneObject != null)
                    {
                        Debug.Log($"[ProcedureDisplayer] Found zone object for step: {step.zoneObjectName}");
                    }
                    else
                    {
                        Debug.LogWarning($"[ProcedureDisplayer] Could not find zone GameObject with name: {step.zoneObjectName}");
                    }
                }
            }

            // Trouver les fake objects par nom
            if (fakeObjects != null)
            {
                foreach (var fake in fakeObjects)
                {
                    if (string.IsNullOrEmpty(fake.objectName)) continue;

                    fake.gameObject = GameObject.Find(fake.objectName);

                    if (fake.gameObject != null)
                    {
                        allSequenceObjects.Add(fake.gameObject);

                        // Stocker le matériau original
                        var renderer = fake.gameObject.GetComponent<Renderer>();
                        if (renderer != null && !originalMaterials.ContainsKey(fake.gameObject))
                        {
                            originalMaterials[fake.gameObject] = renderer.material;
                        }

                        Debug.Log($"[ProcedureDisplayer] Found fake object: {fake.objectName}");
                    }
                    else
                    {
                        Debug.LogWarning($"[ProcedureDisplayer] Could not find fake GameObject with name: {fake.objectName}");
                    }
                }
            }

            // Démarrer le tracking de la procédure globale
            if (TrainingAnalytics.Instance != null)
            {
                TrainingAnalytics.Instance.StartProcedureInteraction(currentObjectId, procedureKey, steps.Count);
                Debug.Log($"[ProcedureDisplayer] Started procedure tracking: {procedureKey} with {steps.Count} steps");
            }

            // Créer l'UI
            CreateProcedureUI();

            // Commencer la première étape
            StartCurrentStep();
        }

        void CreateProcedureUI()
        {
            // Clear root
            rootElement.Clear();

            // Container modal semi-transparent
            modalContainer = new VisualElement();
            modalContainer.style.position = Position.Absolute;
            modalContainer.style.width = Length.Percent(100);
            modalContainer.style.height = Length.Percent(100);
            modalContainer.style.backgroundColor = new Color(0, 0, 0, 0.3f); // Plus transparent pour mieux voir la scène
            modalContainer.style.alignItems = Align.FlexEnd; // Aligner à droite
            modalContainer.style.justifyContent = Justify.Center;
            modalContainer.pickingMode = PickingMode.Position;

            // Panneau d'instructions vertical à droite
            var instructionPanel = new VisualElement();
            instructionPanel.style.width = 400;
            instructionPanel.style.height = Length.Percent(90);
            instructionPanel.style.maxHeight = 800;
            instructionPanel.style.backgroundColor = new Color(0.1f, 0.1f, 0.15f, 0.98f);
            instructionPanel.style.marginRight = 20;
            instructionPanel.style.borderTopLeftRadius = 20;
            instructionPanel.style.borderTopRightRadius = 20;
            instructionPanel.style.borderBottomLeftRadius = 20;
            instructionPanel.style.borderBottomRightRadius = 20;
            instructionPanel.style.borderLeftWidth = 3;
            instructionPanel.style.borderLeftColor = new Color(0.1f, 0.8f, 0.6f, 1f);
            instructionPanel.style.flexDirection = FlexDirection.Column;

            // Header avec titre et bouton fermer
            var headerSection = new VisualElement();
            headerSection.style.paddingTop = 20;
            headerSection.style.paddingBottom = 15;
            headerSection.style.paddingLeft = 25;
            headerSection.style.paddingRight = 25;
            headerSection.style.borderBottomWidth = 1;
            headerSection.style.borderBottomColor = new Color(0.3f, 0.3f, 0.35f, 0.5f);

            // Titre de la procédure
            titleLabel = new Label(procedureTitle);
            titleLabel.style.fontSize = 24;
            titleLabel.style.color = new Color(0.1f, 0.8f, 0.6f, 1f);
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.marginBottom = 5;
            titleLabel.style.whiteSpace = WhiteSpace.Normal;
            headerSection.Add(titleLabel);

            // Description
            if (!string.IsNullOrEmpty(procedureDescription))
            {
                descriptionLabel = new Label(procedureDescription);
                descriptionLabel.style.fontSize = 14;
                descriptionLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
                descriptionLabel.style.whiteSpace = WhiteSpace.Normal;
                headerSection.Add(descriptionLabel);
            }

            instructionPanel.Add(headerSection);

            // Section de progression
            var progressSection = new VisualElement();
            progressSection.style.paddingTop = 15;
            progressSection.style.paddingBottom = 15;
            progressSection.style.paddingLeft = 25;
            progressSection.style.paddingRight = 25;
            progressSection.style.borderBottomWidth = 1;
            progressSection.style.borderBottomColor = new Color(0.3f, 0.3f, 0.35f, 0.5f);

            progressLabel = new Label($"Étape 1 / {steps.Count}");
            progressLabel.style.fontSize = 16;
            progressLabel.style.color = Color.white;
            progressLabel.style.marginBottom = 10;
            progressLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            progressSection.Add(progressLabel);

            progressBar = new VisualElement();
            progressBar.style.height = 8;
            progressBar.style.backgroundColor = new Color(0.3f, 0.3f, 0.35f);
            progressBar.style.borderTopLeftRadius = 4;
            progressBar.style.borderTopRightRadius = 4;
            progressBar.style.borderBottomLeftRadius = 4;
            progressBar.style.borderBottomRightRadius = 4;

            progressFill = new VisualElement();
            progressFill.style.position = Position.Absolute;
            progressFill.style.width = Length.Percent(0);
            progressFill.style.height = 8;
            progressFill.style.backgroundColor = new Color(0.1f, 0.8f, 0.6f, 1f);
            progressFill.style.borderTopLeftRadius = 4;
            progressFill.style.borderTopRightRadius = 4;
            progressFill.style.borderBottomLeftRadius = 4;
            progressFill.style.borderBottomRightRadius = 4;
            progressBar.Add(progressFill);

            progressSection.Add(progressBar);
            instructionPanel.Add(progressSection);

            // Section principale avec ScrollView pour l'instruction
            var mainSection = new ScrollView();
            mainSection.style.flexGrow = 1;
            mainSection.style.paddingTop = 20;
            mainSection.style.paddingBottom = 20;
            mainSection.style.paddingLeft = 25;
            mainSection.style.paddingRight = 25;

            // Instruction de l'étape actuelle
            stepLabel = new Label();
            stepLabel.style.fontSize = 18;
            stepLabel.style.color = Color.white;
            stepLabel.style.whiteSpace = WhiteSpace.Normal;
            mainSection.Add(stepLabel);

            // NEW: Image container for step images
            imageContainer = new VisualElement();
            imageContainer.style.marginTop = 15;
            imageContainer.style.marginBottom = 15;
            imageContainer.style.display = DisplayStyle.None; // Hidden by default
            imageContainer.style.alignItems = Align.Center;
            imageContainer.style.flexDirection = FlexDirection.Column;

            imageElement = new VisualElement();
            imageElement.style.width = Length.Percent(100);
            imageElement.style.height = 200; // Default height
            imageElement.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
            // Note: backgroundPosition not available in older Unity versions, using backgroundPositionX/Y
            imageElement.style.backgroundPositionX = new BackgroundPosition(BackgroundPositionKeyword.Center);
            imageElement.style.backgroundPositionY = new BackgroundPosition(BackgroundPositionKeyword.Center);
            imageElement.style.borderTopLeftRadius = 8;
            imageElement.style.borderTopRightRadius = 8;
            imageElement.style.borderBottomLeftRadius = 8;
            imageElement.style.borderBottomRightRadius = 8;
            // Make the image clickable
            imageElement.pickingMode = PickingMode.Position;
            imageElement.RegisterCallback<ClickEvent>(OnImageClicked);

            imageContainer.Add(imageElement);

            // Add hint text for clickable image
            var imageHintLabel = new Label();
            imageHintLabel.name = "image-hint";
            imageHintLabel.style.fontSize = 12;
            imageHintLabel.style.color = new Color(0.7f, 0.7f, 0.7f, 0.8f);
            imageHintLabel.style.marginTop = 5;
            imageHintLabel.text = LocalizationManager.Instance?.CurrentLanguage == "fr"
                ? "🔍 Cliquez sur l'image pour agrandir"
                : "🔍 Click on image to zoom";
            imageContainer.Add(imageHintLabel);

            mainSection.Add(imageContainer);

            // Label de feedback d'erreur (caché par défaut)
            errorFeedbackLabel = new Label();
            errorFeedbackLabel.style.fontSize = 16;
            errorFeedbackLabel.style.color = new Color(1f, 0.3f, 0.3f, 1f);
            errorFeedbackLabel.style.backgroundColor = new Color(0.8f, 0.2f, 0.2f, 0.2f);
            errorFeedbackLabel.style.paddingTop = 10;
            errorFeedbackLabel.style.paddingBottom = 10;
            errorFeedbackLabel.style.paddingLeft = 15;
            errorFeedbackLabel.style.paddingRight = 15;
            errorFeedbackLabel.style.marginTop = 15;
            errorFeedbackLabel.style.borderTopLeftRadius = 8;
            errorFeedbackLabel.style.borderTopRightRadius = 8;
            errorFeedbackLabel.style.borderBottomLeftRadius = 8;
            errorFeedbackLabel.style.borderBottomRightRadius = 8;
            errorFeedbackLabel.style.whiteSpace = WhiteSpace.Normal;
            errorFeedbackLabel.style.display = DisplayStyle.None; // Caché par défaut
            mainSection.Add(errorFeedbackLabel);

            instructionPanel.Add(mainSection);

            // Section des boutons en bas
            var buttonSection = new VisualElement();
            buttonSection.style.paddingTop = 20;
            buttonSection.style.paddingBottom = 20;
            buttonSection.style.paddingLeft = 25;
            buttonSection.style.paddingRight = 25;
            buttonSection.style.borderTopWidth = 1;
            buttonSection.style.borderTopColor = new Color(0.3f, 0.3f, 0.35f, 0.5f);

            // Info text adapté selon le contexte (avec/sans fakes, avec/sans highlight)
            // On vérifiera dynamiquement pour chaque étape s'il y a des fakes
            var infoLabel = new Label();
            infoLabel.name = "instruction-label";
            infoLabel.style.fontSize = 14;
            infoLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
            infoLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            infoLabel.style.whiteSpace = WhiteSpace.Normal;
            infoLabel.style.paddingTop = 10;
            infoLabel.style.paddingBottom = 10;
            infoLabel.style.paddingLeft = 15;
            infoLabel.style.paddingRight = 15;
            infoLabel.style.backgroundColor = new Color(0.15f, 0.15f, 0.2f, 0.5f);
            infoLabel.style.borderTopLeftRadius = 8;
            infoLabel.style.borderTopRightRadius = 8;
            infoLabel.style.borderBottomLeftRadius = 8;
            infoLabel.style.borderBottomRightRadius = 8;
            buttonSection.Add(infoLabel);

            // NEW: Manual validation button (hidden by default, shown when step requires it)
            validateButton = new Button();
            validateButton.text = LocalizationManager.Instance?.CurrentLanguage == "fr" ? "✓ Valider l'étape" : "✓ Validate Step";
            validateButton.style.marginTop = 15;
            validateButton.style.height = 45;
            validateButton.style.fontSize = 16;
            validateButton.style.backgroundColor = new Color(0.1f, 0.8f, 0.6f, 1f);
            validateButton.style.color = Color.white;
            validateButton.style.unityFontStyleAndWeight = FontStyle.Bold;
            validateButton.style.borderTopLeftRadius = 10;
            validateButton.style.borderTopRightRadius = 10;
            validateButton.style.borderBottomLeftRadius = 10;
            validateButton.style.borderBottomRightRadius = 10;
            validateButton.style.display = DisplayStyle.None; // Hidden by default
            validateButton.clicked += OnValidateButtonClicked;
            buttonSection.Add(validateButton);

            instructionPanel.Add(buttonSection);
            modalContainer.Add(instructionPanel);

            rootElement.Add(modalContainer);
        }

        void StartCurrentStep()
        {
            if (currentStepIndex >= steps.Count)
            {
                CompleteProcedure();
                return;
            }

            var currentStep = steps[currentStepIndex];
            stepStartTime = Time.time;
            wrongClicksCount = 0;

            // Cacher le feedback d'erreur de l'étape précédente
            if (errorFeedbackLabel != null)
            {
                errorFeedbackLabel.style.display = DisplayStyle.None;
            }

            // Mettre à jour l'UI
            progressLabel.text = LocalizationManager.Instance?.CurrentLanguage == "fr"
                ? $"Étape {currentStepIndex + 1} / {steps.Count}"
                : $"Step {currentStepIndex + 1} / {steps.Count}";

            // Afficher le titre de l'étape si présent, suivi de l'instruction
            if (!string.IsNullOrEmpty(currentStep.title))
            {
                stepLabel.text = $"<b><size=22>{currentStep.title}</size></b>\n\n{currentStep.instruction}";
            }
            else
            {
                stepLabel.text = currentStep.instruction;
            }

            // Ajouter le hint si présent
            if (!string.IsNullOrEmpty(currentStep.hint))
            {
                stepLabel.text += $"\n\n💡 {currentStep.hint}";
            }

            // NEW: Show/hide image if available
            if (!string.IsNullOrEmpty(currentStep.imagePath))
            {
                ShowStepImage(currentStep.imagePath);
            }
            else
            {
                HideStepImage();
            }

            // Show/hide manual validation button (only for manual validation type)
            if (currentStep.validationType == "manual")
            {
                if (validateButton != null)
                {
                    validateButton.style.display = DisplayStyle.Flex;
                    validateButton.text = LocalizationManager.Instance?.CurrentLanguage == "fr"
                        ? "✓ Valider l'étape"
                        : "✓ Validate Step";
                }
            }
            else
            {
                if (validateButton != null)
                {
                    validateButton.style.display = DisplayStyle.None;
                }
            }

            // Mettre à jour la barre de progression
            float progress = (float)currentStepIndex / steps.Count * 100f;
            progressFill.style.width = Length.Percent(progress);

            // Mettre à jour le texte d'instruction selon le contexte
            UpdateInstructionLabel(currentStep);

            // IMPORTANT : Nettoyer TOUS les objets de la séquence avant de démarrer la nouvelle étape
            // Cela évite qu'un objet garde un handler ou une surbrillance de l'étape précédente
            foreach (var obj in allSequenceObjects)
            {
                if (obj != null)
                {
                    // Retirer le handler de clic
                    var oldHandler = obj.GetComponent<ProcedureStepClickHandler>();
                    if (oldHandler != null)
                    {
                        Destroy(oldHandler);
                    }

                    // Retirer la surbrillance
                    if (shouldHighlight)
                    {
                        RemoveHighlight(obj);
                    }
                }
            }

            // Nettoyer les zone triggers des étapes précédentes
            CleanupZoneTriggers();

            currentHighlightedObjects.Clear();
            currentCorrectObject = null;

            // Configurer l'étape selon le type de validation
            switch (currentStep.validationType)
            {
                case "click":
                    // Surligner TOUS les objets de l'étape actuelle (target + fakes)
                    if (currentStep.targetObject != null)
                    {
                        currentCorrectObject = currentStep.targetObject;

                        // Créer une liste de tous les objets à surligner
                        var objectsToHighlight = new List<GameObject> { currentStep.targetObject };

                        // Ajouter les fake objects spécifiques à cette étape
                        if (currentStep.fakeObjects != null && currentStep.fakeObjects.Count > 0)
                        {
                            foreach (var fake in currentStep.fakeObjects)
                            {
                                if (fake.gameObject != null)
                                {
                                    objectsToHighlight.Add(fake.gameObject);
                                }
                            }
                        }

                        foreach (var obj in objectsToHighlight)
                        {
                            if (obj == null) continue;

                            // Surligner l'objet UNIQUEMENT si shouldHighlight ET useBlinking sont activés
                            if (shouldHighlight && currentStep.useBlinking)
                            {
                                HighlightObject(obj, currentStep.useBlinking);
                            }

                            currentHighlightedObjects.Add(obj);

                            // Ajouter un composant pour gérer le clic
                            var clickHandler = obj.AddComponent<ProcedureStepClickHandler>();
                            clickHandler.Initialize(this, currentStepIndex, obj);
                        }
                    }
                    break;

                case "zone":
                    // Zone trigger mode - add ProcedureZoneTrigger to the zone object
                    if (currentStep.zoneObject != null)
                    {
                        var zoneTrigger = currentStep.zoneObject.AddComponent<ProcedureZoneTrigger>();
                        zoneTrigger.Initialize(this, currentStepIndex);

                        // Optionally highlight the target object if present (visual cue only, no click handler)
                        if (currentStep.targetObject != null && shouldHighlight && currentStep.useBlinking)
                        {
                            HighlightObject(currentStep.targetObject, currentStep.useBlinking);
                            currentHighlightedObjects.Add(currentStep.targetObject);
                        }

                        Debug.Log($"[ProcedureDisplayer] Zone trigger mode - waiting for player to enter zone: {currentStep.zoneObjectName}");
                    }
                    else
                    {
                        Debug.LogWarning($"[ProcedureDisplayer] Zone object not found: {currentStep.zoneObjectName}");
                    }
                    break;

                case "manual":
                    // Manual validation mode - no object highlighting or click handlers needed
                    Debug.Log("[ProcedureDisplayer] Manual validation mode - no object interaction required");
                    break;
            }
        }

        void UpdateInstructionLabel(ProcedureStep step)
        {
            // Trouver le label d'instruction
            var instructionLabel = modalContainer?.Q<Label>("instruction-label");
            if (instructionLabel == null) return;

            string lang = LocalizationManager.Instance?.CurrentLanguage ?? "en";

            switch (step.validationType)
            {
                case "manual":
                    instructionLabel.text = lang == "fr"
                        ? "Lisez les instructions et cliquez sur 'Valider l'étape' pour passer à la suite."
                        : "Read the instructions and click 'Validate Step' to continue.";
                    break;

                case "zone":
                    instructionLabel.text = lang == "fr"
                        ? "Dirigez-vous vers la zone indiquée pour valider l'étape."
                        : "Walk to the indicated zone to validate the step.";
                    break;

                default: // "click"
                    bool hasFakeObjects = (step.fakeObjects != null && step.fakeObjects.Count > 0);

                    if (hasFakeObjects)
                    {
                        if (shouldHighlight && step.useBlinking)
                        {
                            instructionLabel.text = lang == "fr"
                                ? "⚠️ Cliquez sur le bon objet. Attention, plusieurs objets clignotent, choisissez le bon !"
                                : "⚠️ Click on the correct object. Beware, multiple objects are blinking, choose the right one!";
                        }
                        else
                        {
                            instructionLabel.text = lang == "fr"
                                ? "⚠️ Trouvez et cliquez sur le bon objet."
                                : "⚠️ Find and click on the correct object.";
                        }
                    }
                    else
                    {
                        if (shouldHighlight && step.useBlinking)
                        {
                            instructionLabel.text = lang == "fr"
                                ? "💡 Cliquez sur l'objet qui clignote pour valider l'étape."
                                : "💡 Click on the blinking object to validate the step.";
                        }
                        else
                        {
                            instructionLabel.text = lang == "fr"
                                ? "💡 Trouvez et cliquez sur l'objet demandé pour valider l'étape."
                                : "💡 Find and click on the requested object to validate the step.";
                        }
                    }
                    break;
            }
        }

        void HighlightObject(GameObject obj, bool useBlinking)
        {
            if (obj == null) return;

            var renderer = obj.GetComponent<Renderer>();
            if (renderer == null) return;

            // Créer un nouveau matériau avec émission (garde la couleur d'origine)
            Material highlightMaterial = new Material(renderer.material);

            // Activer l'émission
            highlightMaterial.EnableKeyword("_EMISSION");
            highlightMaterial.SetColor("_EmissionColor", highlightColor * highlightIntensity);

            // NE PAS changer la couleur de base - garder la couleur originale de l'objet

            renderer.material = highlightMaterial;

            // Ajouter un composant pour l'animation de pulsation UNIQUEMENT si useBlinking est activé
            if (useBlinking && pulseHighlight)
            {
                // Détruire l'ancien PulseEffect s'il existe (peut rester d'une étape précédente)
                var oldPulse = obj.GetComponent<PulseEffect>();
                if (oldPulse != null)
                {
                    DestroyImmediate(oldPulse);
                }

                // Ajouter un nouveau PulseEffect
                var pulse = obj.AddComponent<PulseEffect>();
                pulse.Initialize(highlightColor, highlightIntensity, pulseSpeed);
                Debug.Log($"[ProcedureDisplayer] Blinking enabled for object: {obj.name}");
            }
            else
            {
                Debug.Log($"[ProcedureDisplayer] Blinking disabled for object: {obj.name}");
            }
        }

        void RemoveHighlight(GameObject obj)
        {
            if (obj == null) return;

            var renderer = obj.GetComponent<Renderer>();
            if (renderer == null) return;

            // Restaurer le matériau original
            if (originalMaterials.ContainsKey(obj))
            {
                renderer.material = originalMaterials[obj];
            }

            // Retirer l'effet de pulsation
            var pulse = obj.GetComponent<PulseEffect>();
            if (pulse != null)
            {
                Destroy(pulse);
            }
        }

        /// <summary>
        /// Enable/disable object interaction (no longer uses InteractableObject component)
        /// Objects are now interacted with directly via raycasts in the procedure system
        /// </summary>
        void EnableObjectInteraction(GameObject obj, bool enabled)
        {
            // In the new system, we don't need to enable/disable anything
            // Objects are clicked directly, and the procedure system handles validation
            // This method is kept for compatibility but does nothing
        }

        void ShowErrorFeedback(string customMessage = null)
        {
            if (errorFeedbackLabel == null || currentStepIndex >= steps.Count) return;

            string message;

            // Use custom message if provided, otherwise use generic one
            if (!string.IsNullOrEmpty(customMessage))
            {
                message = $"{customMessage} (Erreurs: {wrongClicksCount})";
            }
            else
            {
                // Message d'erreur sans révéler la bonne réponse
                message = LocalizationManager.Instance?.CurrentLanguage == "fr"
                    ? $"Mauvaise réponse ! Réessayez. (Erreurs: {wrongClicksCount})"
                    : $"Wrong answer! Try again. (Errors: {wrongClicksCount})";
            }

            errorFeedbackLabel.text = message;
            errorFeedbackLabel.style.display = DisplayStyle.Flex;

            // Cacher le message après 3 secondes
            StartCoroutine(HideErrorFeedbackAfterDelay(3f));
        }

        System.Collections.IEnumerator HideErrorFeedbackAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (errorFeedbackLabel != null)
            {
                errorFeedbackLabel.style.display = DisplayStyle.None;
            }
        }

        public void ValidateCurrentStep(GameObject clickedObject)
        {
            if (currentStepIndex >= steps.Count) return;

            var currentStep = steps[currentStepIndex];

            // Guard: zone steps are validated via ProcedureZoneTrigger, not clicks
            if (currentStep.validationType == "zone") return;

            // Vérifier si l'objet cliqué est le bon
            if (clickedObject != currentCorrectObject)
            {
                // Mauvaise réponse ! Incrémenter les erreurs
                wrongClicksCount++;
                totalWrongClicksInProcedure++;

                Debug.Log($"[ProcedureDisplayer] Wrong object clicked! Expected: {currentCorrectObject?.name}, Got: {clickedObject?.name}. Wrong clicks: {wrongClicksCount}");

                // NEW: Check if the clicked object is a fake and show its specific error message
                string customErrorMessage = null;

                // First check step-specific fake objects
                if (currentStep.fakeObjects != null)
                {
                    foreach (var fake in currentStep.fakeObjects)
                    {
                        if (fake.gameObject == clickedObject)
                        {
                            customErrorMessage = fake.errorMessage;
                            break;
                        }
                    }
                }

                // Fallback to global fake objects if no step-specific fake found
                if (customErrorMessage == null && fakeObjects != null)
                {
                    foreach (var fake in fakeObjects)
                    {
                        if (fake.gameObject == clickedObject)
                        {
                            customErrorMessage = fake.errorMessage;
                            break;
                        }
                    }
                }

                // Afficher le feedback d'erreur (custom ou générique)
                ShowErrorFeedback(customErrorMessage);

                // Ne PAS passer à l'étape suivante, laisser l'utilisateur réessayer
                return;
            }

            // Bonne réponse !
            currentStep.completed = true;

            // NEW: If manual validation is NOT required, we track and proceed automatically
            if (!currentStep.requireManualValidation)
            {
                // Calculer la durée de cette étape
                float stepDuration = Time.time - stepStartTime;

                // Créer les données de cette étape pour le tracking
                var stepData = new ProcedureStepData
                {
                    stepNumber = currentStepIndex + 1,
                    stepKey = $"step_{currentStepIndex + 1}",
                    targetObjectId = currentStep.targetObjectName,
                    completed = true,
                    duration = stepDuration,
                    wrongClicksOnThisStep = wrongClicksCount
                };

                // Ajouter l'étape à la liste
                completedSteps.Add(stepData);

                // Ajouter l'étape au tracking global de la procédure
                if (TrainingAnalytics.Instance != null)
                {
                    TrainingAnalytics.Instance.AddProcedureStepData(stepData);
                }

                Debug.Log($"[ProcedureDisplayer] Step {stepData.stepNumber} completed CORRECTLY - Duration: {stepDuration}s, Wrong clicks on this step: {wrongClicksCount}");

                // Attendre un peu avant de passer à l'étape suivante
                StartCoroutine(NextStepAfterDelay(0.5f));
            }
            else
            {
                // Manual validation required - we DON'T track yet, that will be done when validate button is clicked
                Debug.Log("[ProcedureDisplayer] Step with manual validation - object click not required, tracking will be done on validation");
            }
        }

        System.Collections.IEnumerator NextStepAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);

            currentStepIndex++;
            StartCurrentStep();
        }

        /// <summary>
        /// Réinitialise la procédure
        /// Note: Cette méthode n'est plus appelée automatiquement pour les clics hors séquence
        /// </summary>
        public void ResetProcedure()
        {
            Debug.Log("[ProcedureDisplayer] Resetting procedure manually");

            // Retirer les surbrillances actuelles si elles sont actives
            if (shouldHighlight)
            {
                foreach (var obj in currentHighlightedObjects)
                {
                    if (obj != null)
                    {
                        RemoveHighlight(obj);

                        // Retirer le composant de clic temporaire
                        var clickHandler = obj.GetComponent<ProcedureStepClickHandler>();
                        if (clickHandler != null)
                        {
                            Destroy(clickHandler);
                        }
                    }
                }
            }
            currentHighlightedObjects.Clear();

            // Réinitialiser l'index
            currentStepIndex = 0;

            // Réactiver toutes les interactions
            foreach (var obj in allSequenceObjects)
            {
                EnableObjectInteraction(obj, true);
            }

            // Redémarrer la première étape
            StartCurrentStep();
        }

        // NEW: Handle validate button click
        void OnValidateButtonClicked()
        {
            if (currentStepIndex >= steps.Count) return;

            var currentStep = steps[currentStepIndex];

            // For manual validation, we don't need to check if an object was clicked
            // The user just needs to press the validation button
            Debug.Log("[ProcedureDisplayer] Manual validation button clicked, proceeding to next step");

            // Mark the step as completed
            currentStep.completed = true;

            // Calculate step duration
            float stepDuration = Time.time - stepStartTime;

            // Create step data for tracking
            var stepData = new ProcedureStepData
            {
                stepNumber = currentStepIndex + 1,
                stepKey = $"step_{currentStepIndex + 1}",
                targetObjectId = currentStep.targetObjectName,
                completed = true,
                duration = stepDuration,
                wrongClicksOnThisStep = wrongClicksCount
            };

            // Add to completed steps
            completedSteps.Add(stepData);

            // Track in analytics
            if (TrainingAnalytics.Instance != null)
            {
                TrainingAnalytics.Instance.AddProcedureStepData(stepData);
            }

            // Hide any feedback
            if (errorFeedbackLabel != null)
            {
                errorFeedbackLabel.style.display = DisplayStyle.None;
            }

            // Proceed to the next step
            currentStepIndex++;
            StartCurrentStep();
        }

        /// <summary>
        /// Called by ProcedureZoneTrigger when the player enters the zone
        /// </summary>
        public void ValidateZoneStep()
        {
            if (currentStepIndex >= steps.Count) return;

            var currentStep = steps[currentStepIndex];

            // Guard: only process if this is a zone step
            if (currentStep.validationType != "zone") return;

            Debug.Log($"[ProcedureDisplayer] Zone step {currentStepIndex + 1} validated");

            // Mark the step as completed
            currentStep.completed = true;

            // Calculate step duration
            float stepDuration = Time.time - stepStartTime;

            // Create step data for tracking (zone = 0 wrong clicks)
            var stepData = new ProcedureStepData
            {
                stepNumber = currentStepIndex + 1,
                stepKey = $"step_{currentStepIndex + 1}",
                targetObjectId = currentStep.zoneObjectName,
                completed = true,
                duration = stepDuration,
                wrongClicksOnThisStep = 0
            };

            // Add to completed steps
            completedSteps.Add(stepData);

            // Track in analytics
            if (TrainingAnalytics.Instance != null)
            {
                TrainingAnalytics.Instance.AddProcedureStepData(stepData);
            }

            Debug.Log($"[ProcedureDisplayer] Zone step {stepData.stepNumber} completed - Duration: {stepDuration}s");

            // Hide any feedback
            if (errorFeedbackLabel != null)
            {
                errorFeedbackLabel.style.display = DisplayStyle.None;
            }

            // Proceed to the next step
            currentStepIndex++;
            StartCurrentStep();
        }

        /// <summary>
        /// Clean up ProcedureZoneTrigger components from all steps
        /// </summary>
        void CleanupZoneTriggers()
        {
            foreach (var step in steps)
            {
                if (step.zoneObject != null)
                {
                    var zoneTrigger = step.zoneObject.GetComponent<ProcedureZoneTrigger>();
                    if (zoneTrigger != null)
                    {
                        Destroy(zoneTrigger);
                    }
                }
            }
        }

        // Show step image
        void ShowStepImage(string imagePath)
        {
            if (imageContainer == null || imageElement == null) return;

            Debug.Log($"[ProcedureDisplayer] ShowStepImage called with path: {imagePath}");

            // Try to load the image from StreamingAssets or Resources
            // For now, we'll use the path as-is (it should be a path to a Sprite asset)
            var texture = LoadImageTexture(imagePath);

            if (texture != null)
            {
                Debug.Log($"[ProcedureDisplayer] Image loaded successfully, setting as background");
                imageElement.style.backgroundImage = new StyleBackground(texture);
                imageContainer.style.display = DisplayStyle.Flex;

                // Add hover effect to show it's clickable
                imageElement.RegisterCallback<MouseEnterEvent>(evt =>
                {
                    if (!imageZoomed)
                    {
                        imageElement.style.opacity = 0.85f;
                        imageElement.style.borderLeftWidth = 2;
                        imageElement.style.borderRightWidth = 2;
                        imageElement.style.borderTopWidth = 2;
                        imageElement.style.borderBottomWidth = 2;
                        imageElement.style.borderLeftColor = new Color(0.1f, 0.8f, 0.6f, 0.5f);
                        imageElement.style.borderRightColor = new Color(0.1f, 0.8f, 0.6f, 0.5f);
                        imageElement.style.borderTopColor = new Color(0.1f, 0.8f, 0.6f, 0.5f);
                        imageElement.style.borderBottomColor = new Color(0.1f, 0.8f, 0.6f, 0.5f);
                    }
                });

                imageElement.RegisterCallback<MouseLeaveEvent>(evt =>
                {
                    if (!imageZoomed)
                    {
                        imageElement.style.opacity = 1f;
                        imageElement.style.borderLeftWidth = 0;
                        imageElement.style.borderRightWidth = 0;
                        imageElement.style.borderTopWidth = 0;
                        imageElement.style.borderBottomWidth = 0;
                    }
                });

                imageZoomed = false;
            }
            else
            {
                Debug.LogWarning($"[ProcedureDisplayer] Failed to load image from path: {imagePath}");
                HideStepImage();
            }
        }

        // NEW: Hide step image
        void HideStepImage()
        {
            if (imageContainer != null)
            {
                imageContainer.style.display = DisplayStyle.None;
            }
            imageZoomed = false;
        }

        // NEW: Handle image click for zoom
        void OnImageClicked(ClickEvent evt)
        {
            if (imageElement == null) return;

            Debug.Log($"[ProcedureDisplayer] Image clicked, zoomed state: {imageZoomed}");

            if (!imageZoomed)
            {
                // Zoom in - create a full-screen overlay at the root of the UI
                Debug.Log("[ProcedureDisplayer] Zooming in image");

                // Get the root visual element (full screen)
                var uiDocument = rootElement.panel.visualTree;

                // Create full-screen overlay
                var overlay = new VisualElement();
                overlay.name = "image-zoom-overlay";
                overlay.style.position = Position.Absolute;
                overlay.style.left = 0;
                overlay.style.top = 0;
                overlay.style.width = Length.Percent(100);
                overlay.style.height = Length.Percent(100);
                overlay.style.backgroundColor = new Color(0, 0, 0, 0.95f);
                overlay.style.justifyContent = Justify.Center;
                overlay.style.alignItems = Align.Center;
                overlay.pickingMode = PickingMode.Position;

                // Create a copy of the image for the zoom view
                var zoomedImage = new VisualElement();
                zoomedImage.style.width = Length.Percent(90);
                zoomedImage.style.height = Length.Percent(90);
                zoomedImage.style.backgroundImage = imageElement.style.backgroundImage;
                zoomedImage.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
                zoomedImage.style.backgroundPositionX = new BackgroundPosition(BackgroundPositionKeyword.Center);
                zoomedImage.style.backgroundPositionY = new BackgroundPosition(BackgroundPositionKeyword.Center);

                // Add close instruction
                var closeLabel = new Label();
                closeLabel.text = LocalizationManager.Instance?.CurrentLanguage == "fr"
                    ? "✕ Cliquez n'importe où pour fermer"
                    : "✕ Click anywhere to close";
                closeLabel.style.position = Position.Absolute;
                closeLabel.style.top = 20;
                closeLabel.style.right = 20;
                closeLabel.style.color = Color.white;
                closeLabel.style.fontSize = 18;
                closeLabel.style.unityFontStyleAndWeight = FontStyle.Bold;

                overlay.Add(zoomedImage);
                overlay.Add(closeLabel);

                // Add to root of UI (full screen)
                uiDocument.Add(overlay);
                overlay.BringToFront(); // Ensure it's on top of everything

                // Click overlay to close
                overlay.RegisterCallback<ClickEvent>(closeEvt =>
                {
                    Debug.Log("[ProcedureDisplayer] Closing zoomed image");
                    uiDocument.Remove(overlay);
                    imageZoomed = false;
                    evt.StopPropagation(); // Prevent the click from triggering again
                });

                imageZoomed = true;
                evt.StopPropagation(); // Prevent the click from bubbling up
            }
        }

        // NEW: Load image texture from path
        Texture2D LoadImageTexture(string imagePath)
        {
            if (string.IsNullOrEmpty(imagePath)) return null;

            Debug.Log($"[ProcedureDisplayer] Attempting to load image from: {imagePath}");

            // Clean up the path for Resources.Load
            string resourcePath = imagePath;

            // Remove "Assets/Resources/" prefix if present
            if (resourcePath.StartsWith("Assets/Resources/"))
            {
                resourcePath = resourcePath.Substring("Assets/Resources/".Length);
            }
            else if (resourcePath.StartsWith("Resources/"))
            {
                resourcePath = resourcePath.Substring("Resources/".Length);
            }

            // Remove file extension if present
            if (resourcePath.Contains("."))
            {
                resourcePath = resourcePath.Substring(0, resourcePath.LastIndexOf('.'));
            }

            Debug.Log($"[ProcedureDisplayer] Cleaned resource path: {resourcePath}");

            // Try to load as Texture2D first (for PNG/JPG)
            var texture = Resources.Load<Texture2D>(resourcePath);
            if (texture != null)
            {
                Debug.Log($"[ProcedureDisplayer] Successfully loaded texture: {resourcePath}");
                return texture;
            }

            // Try to load as Sprite
            var sprite = Resources.Load<Sprite>(resourcePath);
            if (sprite != null)
            {
                Debug.Log($"[ProcedureDisplayer] Successfully loaded sprite: {resourcePath}");
                return sprite.texture;
            }

            // If not in Resources, try loading from StreamingAssets
            string streamingPath = System.IO.Path.Combine(Application.streamingAssetsPath, imagePath);
            if (System.IO.File.Exists(streamingPath))
            {
                Debug.Log($"[ProcedureDisplayer] Loading from StreamingAssets: {streamingPath}");
                byte[] imageData = System.IO.File.ReadAllBytes(streamingPath);
                Texture2D tex = new Texture2D(2, 2);
                if (tex.LoadImage(imageData))
                {
                    return tex;
                }
            }

            Debug.LogWarning($"[ProcedureDisplayer] Could not load image from path: {imagePath} (cleaned: {resourcePath})");
            return null;
        }

        void Update()
        {
            // Plus besoin de détecter les clics en dehors de la séquence
            // On ne compte les erreurs que quand l'utilisateur clique sur un objet surligné mais mauvais
            // Ce qui est géré directement dans ValidateCurrentStep()
        }

        void CompleteProcedure()
        {
            // Retirer toutes les surbrillances si elles étaient actives
            if (shouldHighlight)
            {
                foreach (var obj in allSequenceObjects)
                {
                    if (obj != null)
                    {
                        RemoveHighlight(obj);
                        EnableObjectInteraction(obj, true);
                    }
                }
            }
            else
            {
                // Juste réactiver les interactions si pas de highlight
                foreach (var obj in allSequenceObjects)
                {
                    if (obj != null)
                    {
                        EnableObjectInteraction(obj, true);
                    }
                }
            }

            // Calculer la durée totale de la procédure
            float totalDuration = Time.time - procedureStartTime;
            bool perfectCompletion = totalWrongClicksInProcedure == 0;

            // Terminer le tracking de la procédure globale
            if (TrainingAnalytics.Instance != null)
            {
                TrainingAnalytics.Instance.CompleteProcedureInteraction(perfectCompletion, totalWrongClicksInProcedure, totalDuration);
            }

            Debug.Log($"[ProcedureDisplayer] Procedure completed - Duration: {totalDuration}s, Total wrong clicks: {totalWrongClicksInProcedure}, Perfect: {perfectCompletion}");

            // Envoyer l'événement de complétion AVANT de fermer pour que ContentDisplayManager puisse le gérer
            OnCompleted?.Invoke(currentObjectId, true);

            // Fermer après avoir envoyé l'événement
            Close();
        }


        public void Close()
        {

            // Nettoyer tous les GameObjects actuellement surlignés si le highlight était actif
            if (shouldHighlight)
            {
                foreach (var obj in currentHighlightedObjects)
                {
                    if (obj != null)
                    {
                        RemoveHighlight(obj);

                        // Retirer le composant de clic temporaire
                        var clickHandler = obj.GetComponent<ProcedureStepClickHandler>();
                        if (clickHandler != null)
                        {
                            Destroy(clickHandler);
                        }
                    }
                }
            }
            currentHighlightedObjects.Clear();
            currentCorrectObject = null;

            // Nettoyer toutes les surbrillances et réactiver les interactions
            if (allSequenceObjects != null)
            {
                foreach (var obj in allSequenceObjects)
                {
                    if (obj != null)
                    {
                        if (shouldHighlight)
                        {
                            RemoveHighlight(obj);
                        }
                        EnableObjectInteraction(obj, true);

                        // S'assurer qu'aucun handler ne reste
                        var handler = obj.GetComponent<ProcedureStepClickHandler>();
                        if (handler != null)
                        {
                            Destroy(handler);
                        }
                    }
                }
            }

            // Nettoyer les zone triggers
            if (steps != null)
            {
                CleanupZoneTriggers();
            }

            // Réinitialiser l'état
            currentStepIndex = 0;
            steps = null;
            allSequenceObjects = null;
            originalMaterials?.Clear();

            rootElement?.Clear();
            OnClosed?.Invoke(currentObjectId);
        }

        List<ProcedureStep> ExtractProcedureSteps(Dictionary<string, object> data, string language)
        {
            var procedureSteps = new List<ProcedureStep>();

            // NEW FORMAT: Check for "steps" array (scenario-based metadata)
            if (data.ContainsKey("steps"))
            {
                var stepsData = data["steps"];
                if (TryConvertToList(stepsData, out List<Dictionary<string, object>> stepsList))
                {
                    foreach (var stepData in stepsList)
                    {
                        // Determine validationType: use "validationType" field if present, fallback to "requireManualValidation" for backward compat
                        string valType = ExtractString(stepData, "validationType");
                        if (string.IsNullOrEmpty(valType))
                        {
                            valType = ExtractBool(stepData, "requireManualValidation", false) ? "manual" : "click";
                        }

                        var step = new ProcedureStep
                        {
                            targetObjectName = ExtractString(stepData, "targetObjectName"),
                            instruction = ExtractLocalizedText(stepData, "text", language),
                            hint = ExtractLocalizedText(stepData, "hint", language),
                            highlightColor = ParseColor(ExtractString(stepData, "highlightColor"), Color.yellow),
                            useBlinking = ExtractBool(stepData, "useBlinking", true),
                            validationType = valType,
                            zoneObjectName = ExtractString(stepData, "zoneObjectName"),
                            imagePath = ExtractLocalizedText(stepData, "imagePath", language)
                        };

                        // NEW: Extract fake objects for this step
                        if (stepData.ContainsKey("fakeObjects") && TryConvertToList(stepData["fakeObjects"], out List<Dictionary<string, object>> stepFakeList))
                        {
                            foreach (var fakeData in stepFakeList)
                            {
                                step.fakeObjects.Add(new FakeObjectData
                                {
                                    objectName = ExtractString(fakeData, "objectName"),
                                    errorMessage = ExtractLocalizedText(fakeData, "errorMessage", language)
                                });
                            }
                        }

                        procedureSteps.Add(step);
                    }

                    // Extract global fake objects (for backward compatibility)
                    if (data.ContainsKey("fakeObjects") && TryConvertToList(data["fakeObjects"], out List<Dictionary<string, object>> fakeList))
                    {
                        fakeObjects = new List<FakeObjectData>();
                        foreach (var fakeData in fakeList)
                        {
                            fakeObjects.Add(new FakeObjectData
                            {
                                objectName = ExtractString(fakeData, "objectName"),
                                errorMessage = ExtractLocalizedText(fakeData, "errorMessage", language)
                            });
                        }
                    }

                    return procedureSteps;
                }
            }

            // LEGACY FORMAT: Check for step_1, step_2, etc. (old system - kept for backward compatibility)
            var stepKeys = data.Keys
                .Where(k => k.StartsWith("step_"))
                .OrderBy(k =>
                {
                    if (int.TryParse(k.Replace("step_", ""), out int stepNumber))
                        return stepNumber;
                    return 999;
                })
                .ToList();

            foreach (var stepKey in stepKeys)
            {
                if (data[stepKey] is Dictionary<string, object> stepData ||
                    (data[stepKey] != null && TryConvertToDict(data[stepKey], out stepData)))
                {
                    var step = new ProcedureStep
                    {
                        targetObjectName = ExtractString(stepData, "correctObjectId"), // Legacy: use correctObjectId as targetObjectName
                        title = ExtractLocalizedText(stepData, "title", language),
                        instruction = ExtractLocalizedText(stepData, "instruction", language),
                        validation = ExtractLocalizedText(stepData, "validation", language),
                        hint = ExtractLocalizedText(stepData, "hint", language)
                    };

                    procedureSteps.Add(step);
                }
            }

            return procedureSteps;
        }

        bool TryConvertToList(object obj, out List<Dictionary<string, object>> list)
        {
            list = null;
            if (obj == null) return false;

            try
            {
                string json = Newtonsoft.Json.JsonConvert.SerializeObject(obj);
                list = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(json);
                return list != null;
            }
            catch
            {
                return false;
            }
        }

        Color ParseColor(string colorHex, Color defaultColor)
        {
            if (string.IsNullOrEmpty(colorHex)) return defaultColor;

            if (ColorUtility.TryParseHtmlString(colorHex, out Color color))
            {
                return color;
            }

            return defaultColor;
        }

        bool ExtractBool(Dictionary<string, object> data, string key, bool defaultValue)
        {
            if (!data.ContainsKey(key)) return defaultValue;

            var value = data[key];
            if (value is bool boolValue) return boolValue;
            if (value is string strValue && bool.TryParse(strValue, out bool parsed)) return parsed;

            return defaultValue;
        }

        bool TryConvertToDict(object obj, out Dictionary<string, object> dict)
        {
            dict = null;
            if (obj == null) return false;

            try
            {
                string json = Newtonsoft.Json.JsonConvert.SerializeObject(obj);
                dict = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
                return dict != null;
            }
            catch
            {
                return false;
            }
        }

        // Méthodes utilitaires
        string ExtractLocalizedText(Dictionary<string, object> data, string key, string language)
        {
            if (!data.ContainsKey(key)) return "";

            var textData = data[key];

            if (textData is string simpleText) return simpleText;

            if (textData is Dictionary<string, object> localizedText)
            {
                if (localizedText.ContainsKey(language))
                    return localizedText[language]?.ToString() ?? "";
                if (localizedText.ContainsKey("en"))
                    return localizedText["en"]?.ToString() ?? "";
            }
            else if (textData != null && textData.GetType().FullName.Contains("JObject"))
            {
                string json = Newtonsoft.Json.JsonConvert.SerializeObject(textData);
                var localizedJObject = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                if (localizedJObject != null)
                {
                    if (localizedJObject.ContainsKey(language))
                        return localizedJObject[language];
                    if (localizedJObject.ContainsKey("en"))
                        return localizedJObject["en"];
                }
            }

            return "";
        }

        string ExtractString(Dictionary<string, object> data, string key)
        {
            return data.ContainsKey(key) ? data[key]?.ToString() ?? "" : "";
        }
    }

    /// <summary>
    /// Effet de pulsation pour les objets mis en surbrillance
    /// </summary>
    public class PulseEffect : MonoBehaviour
    {
        private Renderer objectRenderer;
        private Color baseColor;
        private float intensity;
        private float speed;
        private float time;

        public void Initialize(Color color, float emissionIntensity, float pulseSpeed)
        {
            objectRenderer = GetComponent<Renderer>();
            baseColor = color;
            intensity = emissionIntensity;
            speed = pulseSpeed;
        }

        void Update()
        {
            if (objectRenderer == null) return;

            time += Time.deltaTime * speed;
            float pulse = (Mathf.Sin(time) + 1f) / 2f; // Valeur entre 0 et 1
            // Pulse entre 0 (couleur originale) et intensité max (jaune brillant)
            float currentIntensity = Mathf.Lerp(0f, intensity, pulse);

            if (objectRenderer.material.HasProperty("_EmissionColor"))
            {
                objectRenderer.material.SetColor("_EmissionColor", baseColor * currentIntensity);
            }
        }

        void OnDestroy()
        {
            // Nettoyer si nécessaire
        }
    }
}