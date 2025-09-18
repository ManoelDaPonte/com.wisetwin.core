using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System;
using System.Linq;

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
        [SerializeField] private float highlightIntensity = 2f;
        [SerializeField] private bool pulseHighlight = true;
        [SerializeField] private float pulseSpeed = 2f;

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

        // UI Elements
        private Label titleLabel;
        private Label descriptionLabel;
        private Label stepLabel;
        private Label progressLabel;
        private VisualElement progressBar;
        private VisualElement progressFill;
        private bool isMonitoringClicks = false;

        public class ProcedureStep
        {
            public string objectId;
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

            instructionPanel.Add(mainSection);

            // Section des boutons en bas
            var buttonSection = new VisualElement();
            buttonSection.style.paddingTop = 20;
            buttonSection.style.paddingBottom = 20;
            buttonSection.style.paddingLeft = 25;
            buttonSection.style.paddingRight = 25;
            buttonSection.style.borderTopWidth = 1;
            buttonSection.style.borderTopColor = new Color(0.3f, 0.3f, 0.35f, 0.5f);

            // Info text uniquement
            var infoLabel = new Label();
            infoLabel.text = LocalizationManager.Instance?.CurrentLanguage == "fr"
                ? "Cliquez sur l'objet surligné pour valider l'étape"
                : "Click on the highlighted object to validate the step";
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

            // Mettre à jour l'UI
            progressLabel.text = LocalizationManager.Instance?.CurrentLanguage == "fr"
                ? $"Étape {currentStepIndex + 1} / {steps.Count}"
                : $"Step {currentStepIndex + 1} / {steps.Count}";

            stepLabel.text = currentStep.instruction;

            // Ajouter le hint si présent
            if (!string.IsNullOrEmpty(currentStep.hint))
            {
                stepLabel.text += $"\n\n💡 {currentStep.hint}";
            }

            // Mettre à jour la barre de progression
            float progress = (float)currentStepIndex / steps.Count * 100f;
            progressFill.style.width = Length.Percent(progress);

            // Retirer la surbrillance de l'objet précédent
            if (currentHighlightedObject != null)
            {
                RemoveHighlight(currentHighlightedObject);

                // Retirer le composant de clic temporaire
                var oldClickHandler = currentHighlightedObject.GetComponent<ProcedureStepClickHandler>();
                if (oldClickHandler != null)
                {
                    Destroy(oldClickHandler);
                }
            }

            // Ajouter la surbrillance au nouvel objet
            if (currentStep.targetObject != null)
            {
                HighlightObject(currentStep.targetObject);
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

            // Créer un nouveau matériau avec émission
            Material highlightMaterial = new Material(renderer.material);

            // Activer l'émission
            highlightMaterial.EnableKeyword("_EMISSION");
            highlightMaterial.SetColor("_EmissionColor", highlightColor * highlightIntensity);

            // Changer la couleur principale pour un effet jaune
            if (highlightMaterial.HasProperty("_Color"))
            {
                highlightMaterial.SetColor("_Color", highlightColor);
            }
            else if (highlightMaterial.HasProperty("_BaseColor"))
            {
                highlightMaterial.SetColor("_BaseColor", highlightColor);
            }

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

        public void ValidateCurrentStep()
        {
            if (currentStepIndex >= steps.Count) return;

            var currentStep = steps[currentStepIndex];
            currentStep.completed = true;
            isMonitoringClicks = false;

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

            // Retirer les surbrillances actuelles
            if (currentHighlightedObject != null)
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
                        ResetProcedure();
                    }
                }
            }
        }

        void CompleteProcedure()
        {
            // Retirer toutes les surbrillances
            foreach (var obj in sequenceObjects)
            {
                RemoveHighlight(obj);
                EnableObjectInteraction(obj, true);
            }

            // Envoyer l'événement de complétion AVANT de fermer pour que ContentDisplayManager puisse le gérer
            OnCompleted?.Invoke(currentObjectId, true);

            // Fermer après avoir envoyé l'événement
            Close();
        }


        public void Close()
        {
            // Désactiver le monitoring des clics
            isMonitoringClicks = false;

            // Nettoyer le GameObject actuellement surligné
            if (currentHighlightedObject != null)
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
                        RemoveHighlight(obj);
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

            // Chercher les étapes (step_1, step_2, etc.)
            var stepKeys = data.Keys.Where(k => k.StartsWith("step_")).OrderBy(k => k).ToList();

            foreach (var stepKey in stepKeys)
            {
                if (data[stepKey] is Dictionary<string, object> stepData ||
                    (data[stepKey] != null && TryConvertToDict(data[stepKey], out stepData)))
                {
                    var step = new ProcedureStep
                    {
                        objectId = ExtractString(stepData, "objectId"),
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
            float currentIntensity = Mathf.Lerp(intensity * 0.5f, intensity, pulse);

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