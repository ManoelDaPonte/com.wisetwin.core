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
        [SerializeField] private bool pulseHighlight = true;
        [SerializeField] private float pulseSpeed = 3f; // Augmenté pour une pulsation plus visible

        private string currentObjectId;
        private VisualElement rootElement;
        private VisualElement modalContainer;

        // Données de la procédure
        private string procedureTitle;
        private string procedureDescription;
        private List<ProcedureStep> steps;
        private int currentStepIndex = 0;

        // GameObjects de la séquence
        private List<GameObject> sequenceObjects;
        private Dictionary<GameObject, Material> originalMaterials;
        private GameObject currentHighlightedObject;
        private bool shouldHighlight = true; // Contrôle si on doit surligner ou non
        private bool keepProgressOnOtherClick = false; // Ne pas réinitialiser à 0 si on clique ailleurs
        private IProcedureReset resetScript; // Script de reset personnalisé

        // UI Elements
        private Label titleLabel;
        private Label descriptionLabel;
        private Label stepLabel;
        private Label progressLabel;
        private Label errorFeedbackLabel;
        private VisualElement progressBar;
        private VisualElement progressFill;
        private bool isMonitoringClicks = false;

        // Analytics tracking
        private ProcedureInteractionData currentStepData;
        private float stepStartTime;
        private int wrongClicksCount = 0;
        private int hintsUsedCount = 0;
        private int totalWrongClicksInProcedure = 0; // Compteur global d'erreurs pour toute la procédure

        public class ProcedureStep
        {
            public string objectId;
            public string title;
            public string instruction;
            public string validation;
            public string hint;
            public GameObject targetObject;
            public bool completed = false;
        }

        public void Display(string objectId, Dictionary<string, object> contentData, VisualElement root)
        {
            currentObjectId = objectId;
            rootElement = root;
            currentStepIndex = 0;
            totalWrongClicksInProcedure = 0; // Réinitialiser le compteur d'erreurs

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

            // Récupérer le script de reset si fourni
            if (contentData.ContainsKey("resetScript"))
            {
                resetScript = contentData["resetScript"] as IProcedureReset;
                if (resetScript != null)
                {
                    Debug.Log($"[ProcedureDisplayer] Reset script configured");
                }
            }

            // Obtenir la langue actuelle
            string lang = LocalizationManager.Instance?.CurrentLanguage ?? "en";

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
            sequenceObjects = new List<GameObject>();

            // Trouver les GameObjects pour chaque étape
            foreach (var step in steps)
            {
                if (!string.IsNullOrEmpty(step.objectId))
                {
                    // Chercher l'objet par son metadata ID
                    var allMappers = FindObjectsByType<ObjectMetadataMapper>(FindObjectsSortMode.None);
                    foreach (var mapper in allMappers)
                    {
                        if (mapper.MetadataId == step.objectId)
                        {
                            step.targetObject = mapper.gameObject;
                            sequenceObjects.Add(mapper.gameObject);

                            // Stocker le matériau original
                            var renderer = mapper.GetComponent<Renderer>();
                            if (renderer != null)
                            {
                                originalMaterials[mapper.gameObject] = renderer.material;
                            }

                            Debug.Log($"[ProcedureDisplayer] Found object for step: {step.objectId} -> {mapper.gameObject.name}");
                            break;
                        }
                    }

                    if (step.targetObject == null)
                    {
                        Debug.LogWarning($"[ProcedureDisplayer] Could not find GameObject with metadata ID: {step.objectId}");
                    }
                }
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

            // Bouton fermer (X)
            var closeButton = new Button(() => Close());
            closeButton.text = "✕";
            closeButton.style.position = Position.Absolute;
            closeButton.style.top = 15;
            closeButton.style.right = 15;
            closeButton.style.width = 30;
            closeButton.style.height = 30;
            closeButton.style.fontSize = 20;
            closeButton.style.backgroundColor = new Color(0.8f, 0.2f, 0.2f, 0.8f);
            closeButton.style.color = Color.white;
            closeButton.style.borderTopLeftRadius = 15;
            closeButton.style.borderTopRightRadius = 15;
            closeButton.style.borderBottomLeftRadius = 15;
            closeButton.style.borderBottomRightRadius = 15;
            headerSection.Add(closeButton);

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

            // Info text adapté selon si le highlight est activé ou non
            var infoLabel = new Label();
            if (shouldHighlight)
            {
                infoLabel.text = LocalizationManager.Instance?.CurrentLanguage == "fr"
                    ? "Cliquez sur l'objet surligné pour valider l'étape"
                    : "Click on the highlighted object to validate the step";
            }
            else
            {
                infoLabel.text = LocalizationManager.Instance?.CurrentLanguage == "fr"
                    ? "Cliquez sur l'objet indiqué pour valider l'étape"
                    : "Click on the indicated object to validate the step";
            }
            infoLabel.style.fontSize = 14;
            infoLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
            infoLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            infoLabel.style.whiteSpace = WhiteSpace.Normal;
            buttonSection.Add(infoLabel);

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
            isMonitoringClicks = true;
            stepStartTime = Time.time;
            wrongClicksCount = 0;
            hintsUsedCount = 0;

            // Cacher le feedback d'erreur de l'étape précédente
            if (errorFeedbackLabel != null)
            {
                errorFeedbackLabel.style.display = DisplayStyle.None;
            }

            // Initialiser le tracking analytics pour cette étape
            if (TrainingAnalytics.Instance != null)
            {
                currentStepData = new ProcedureInteractionData
                {
                    stepNumber = currentStepIndex + 1,
                    totalSteps = steps.Count,
                    title = currentStep.title,
                    instruction = currentStep.instruction,
                    hintsUsed = 0,
                    wrongClicks = 0
                };

                // TrackProcedureStep appelle déjà StartInteraction, pas besoin de le faire ici
                TrainingAnalytics.Instance.TrackProcedureStep(currentStep.objectId ?? currentObjectId, currentStepData);
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
                hintsUsedCount = 1;
                if (currentStepData != null)
                {
                    currentStepData.hintsUsed = 1;
                }
            }

            // Mettre à jour la barre de progression
            float progress = (float)currentStepIndex / steps.Count * 100f;
            progressFill.style.width = Length.Percent(progress);

            // Retirer la surbrillance de l'objet précédent si elle était active
            if (currentHighlightedObject != null && shouldHighlight)
            {
                RemoveHighlight(currentHighlightedObject);

                // Retirer le composant de clic temporaire
                var oldClickHandler = currentHighlightedObject.GetComponent<ProcedureStepClickHandler>();
                if (oldClickHandler != null)
                {
                    Destroy(oldClickHandler);
                }
            }

            // Ajouter la surbrillance au nouvel objet si l'option est activée
            if (currentStep.targetObject != null)
            {
                if (shouldHighlight)
                {
                    HighlightObject(currentStep.targetObject);
                }
                currentHighlightedObject = currentStep.targetObject;

                // Ajouter un composant temporaire pour gérer le clic
                var clickHandler = currentStep.targetObject.GetComponent<ProcedureStepClickHandler>();
                if (clickHandler == null)
                {
                    clickHandler = currentStep.targetObject.AddComponent<ProcedureStepClickHandler>();
                }
                clickHandler.Initialize(this, currentStepIndex);

                // Désactiver les interactions normales sur les autres objets
                foreach (var obj in sequenceObjects)
                {
                    if (obj != currentStep.targetObject)
                    {
                        EnableObjectInteraction(obj, false);
                    }
                }
            }
        }

        void HighlightObject(GameObject obj)
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

            // Ajouter un composant pour l'animation de pulsation si activé
            if (pulseHighlight && obj.GetComponent<PulseEffect>() == null)
            {
                var pulse = obj.AddComponent<PulseEffect>();
                pulse.Initialize(highlightColor, highlightIntensity, pulseSpeed);
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

        void EnableObjectInteraction(GameObject obj, bool enabled)
        {
            var interactable = obj.GetComponent<InteractableObject>();
            if (interactable != null)
            {
                interactable.SetInteractionEnabled(enabled);
            }
        }

        void ShowErrorFeedback()
        {
            if (errorFeedbackLabel == null || currentStepIndex >= steps.Count) return;

            var currentStep = steps[currentStepIndex];
            string expectedObjectName = currentStep.targetObject != null ? currentStep.targetObject.name : "l'objet suivant";

            string message = LocalizationManager.Instance?.CurrentLanguage == "fr"
                ? $"❌ Mauvais objet ! Cliquez sur : {expectedObjectName}"
                : $"❌ Wrong object! Click on: {expectedObjectName}";

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

        public void ValidateCurrentStep()
        {
            if (currentStepIndex >= steps.Count) return;

            var currentStep = steps[currentStepIndex];
            currentStep.completed = true;
            isMonitoringClicks = false;

            // Terminer le tracking de cette étape
            if (TrainingAnalytics.Instance != null && currentStepData != null)
            {
                currentStepData.wrongClicks = wrongClicksCount;
                currentStepData.hintsUsed = hintsUsedCount;

                // Success = true seulement si aucune erreur sur cette étape
                bool stepSuccess = wrongClicksCount == 0;
                float stepScore = stepSuccess ? 100f : 0f;

                TrainingAnalytics.Instance.AddDataToCurrentInteraction("finalScore", stepScore);
                TrainingAnalytics.Instance.EndCurrentInteraction(stepSuccess);
            }

            // Attendre un peu avant de passer à l'étape suivante
            StartCoroutine(NextStepAfterDelay(0.5f));
        }

        System.Collections.IEnumerator NextStepAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);

            currentStepIndex++;
            StartCurrentStep();
        }

        public void ResetProcedure()
        {
            Debug.Log("[ProcedureDisplayer] Resetting procedure - clicked outside sequence");

            // Incrémenter le compteur de clics incorrects
            wrongClicksCount++;
            totalWrongClicksInProcedure++;
            if (currentStepData != null)
            {
                currentStepData.wrongClicks = wrongClicksCount;
            }

            // Afficher le feedback d'erreur
            ShowErrorFeedback();

            // Appeler le script de reset si configuré
            if (resetScript != null && sequenceObjects.Count > 0)
            {
                try
                {
                    resetScript.ResetProcedure(sequenceObjects.ToArray());
                    Debug.Log("[ProcedureDisplayer] Custom reset script executed");
                }
                catch (Exception e)
                {
                    Debug.LogError($"[ProcedureDisplayer] Error executing reset script: {e.Message}");
                }
            }

            // Retirer les surbrillances actuelles si elles sont actives
            if (currentHighlightedObject != null && shouldHighlight)
            {
                RemoveHighlight(currentHighlightedObject);

                // Retirer le composant de clic temporaire
                var clickHandler = currentHighlightedObject.GetComponent<ProcedureStepClickHandler>();
                if (clickHandler != null)
                {
                    Destroy(clickHandler);
                }
            }

            // Réinitialiser l'index
            currentStepIndex = 0;
            isMonitoringClicks = false;

            // Réactiver toutes les interactions
            foreach (var obj in sequenceObjects)
            {
                EnableObjectInteraction(obj, true);
            }

            // Redémarrer la première étape
            StartCurrentStep();
        }

        void Update()
        {
            // Détecter les clics en dehors de la séquence
            if (isMonitoringClicks && UnityEngine.InputSystem.Mouse.current != null)
            {
                if (UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
                {
                    CheckOutsideClick();
                }
            }
        }

        void CheckOutsideClick()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null) return;

            Vector2 mousePos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
            Ray ray = mainCamera.ScreenPointToRay(new Vector3(mousePos.x, mousePos.y, 0));

            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, Mathf.Infinity))
            {
                GameObject clickedObject = hit.transform.gameObject;

                // Vérifier si l'objet cliqué est dans la séquence
                bool isInSequence = false;
                foreach (var obj in sequenceObjects)
                {
                    if (clickedObject == obj || clickedObject.transform.IsChildOf(obj.transform))
                    {
                        isInSequence = true;
                        break;
                    }
                }

                // Si on a cliqué en dehors de la séquence et que ce n'est pas l'UI
                if (!isInSequence && hit.collider != null && currentStepIndex < steps.Count)
                {
                    // Vérifier que ce n'est pas l'objet actuellement surligné
                    if (clickedObject != currentHighlightedObject)
                    {
                        if (keepProgressOnOtherClick)
                        {
                            // Juste incrémenter le compteur d'erreurs sans reset
                            wrongClicksCount++;
                            totalWrongClicksInProcedure++;
                            if (currentStepData != null)
                            {
                                currentStepData.wrongClicks = wrongClicksCount;
                            }
                            Debug.Log($"[ProcedureDisplayer] Wrong click detected (progress kept) - Step wrong clicks: {wrongClicksCount}, Total: {totalWrongClicksInProcedure}");

                            // Afficher le feedback d'erreur
                            ShowErrorFeedback();
                        }
                        else
                        {
                            // Comportement normal : reset complet
                            ResetProcedure();
                        }
                    }
                }
            }
        }

        void CompleteProcedure()
        {
            // Retirer toutes les surbrillances si elles étaient actives
            if (shouldHighlight)
            {
                foreach (var obj in sequenceObjects)
                {
                    RemoveHighlight(obj);
                    EnableObjectInteraction(obj, true);
                }
            }
            else
            {
                // Juste réactiver les interactions si pas de highlight
                foreach (var obj in sequenceObjects)
                {
                    EnableObjectInteraction(obj, true);
                }
            }

            // Les étapes individuelles gèrent déjà leur propre tracking
            // Pas besoin de terminer une interaction ici car la dernière étape l'a déjà fait
            Debug.Log($"[ProcedureDisplayer] Procedure completed - Total wrong clicks: {totalWrongClicksInProcedure}");

            // Envoyer l'événement de complétion AVANT de fermer pour que ContentDisplayManager puisse le gérer
            OnCompleted?.Invoke(currentObjectId, true);

            // Fermer après avoir envoyé l'événement
            Close();
        }


        public void Close()
        {
            // Désactiver le monitoring des clics
            isMonitoringClicks = false;

            // Nettoyer le GameObject actuellement surligné si le highlight était actif
            if (currentHighlightedObject != null && shouldHighlight)
            {
                RemoveHighlight(currentHighlightedObject);

                // Retirer le composant de clic temporaire
                var clickHandler = currentHighlightedObject.GetComponent<ProcedureStepClickHandler>();
                if (clickHandler != null)
                {
                    Destroy(clickHandler);
                }

                currentHighlightedObject = null;
            }

            // Nettoyer toutes les surbrillances et réactiver les interactions
            if (sequenceObjects != null)
            {
                foreach (var obj in sequenceObjects)
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

            // Réinitialiser l'état
            currentStepIndex = 0;
            steps = null;
            sequenceObjects = null;
            originalMaterials?.Clear();

            rootElement?.Clear();
            OnClosed?.Invoke(currentObjectId);
        }

        List<ProcedureStep> ExtractProcedureSteps(Dictionary<string, object> data, string language)
        {
            var procedureSteps = new List<ProcedureStep>();

            // Chercher les étapes (step_1, step_2, etc.) et les trier numériquement
            var stepKeys = data.Keys
                .Where(k => k.StartsWith("step_"))
                .OrderBy(k =>
                {
                    // Extraire le numéro de l'étape pour un tri numérique
                    if (int.TryParse(k.Replace("step_", ""), out int stepNumber))
                        return stepNumber;
                    return 999; // Mettre à la fin si pas de numéro valide
                })
                .ToList();

            foreach (var stepKey in stepKeys)
            {
                if (data[stepKey] is Dictionary<string, object> stepData ||
                    (data[stepKey] != null && TryConvertToDict(data[stepKey], out stepData)))
                {
                    var step = new ProcedureStep
                    {
                        objectId = ExtractString(stepData, "objectId"),
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