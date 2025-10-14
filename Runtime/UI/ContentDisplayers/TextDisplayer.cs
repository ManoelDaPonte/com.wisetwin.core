using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System;
using WiseTwin.Analytics;

namespace WiseTwin.UI
{
    /// <summary>
    /// Afficheur spécialisé pour le contenu texte avec support de formatage markdown-like
    /// Design moderne avec header fixe et contenu scrollable minimaliste
    /// </summary>
    public class TextDisplayer : MonoBehaviour, IContentDisplayer
    {
        public event Action<string> OnClosed;
        public event Action<string, bool> OnCompleted;

        private string currentObjectId;
        private VisualElement rootElement;
        private VisualElement modalContainer;

        // Analytics tracking
        private TextInteractionData currentTextData;
        private float displayStartTime;
        private ScrollView contentScrollView;
        private float maxScrollPercentage = 0f;

        public void Display(string objectId, Dictionary<string, object> contentData, VisualElement root)
        {
            currentObjectId = objectId;
            rootElement = root;

            // Obtenir la langue actuelle
            string lang = LocalizationManager.Instance?.CurrentLanguage ?? "en";

            // Extraire les données
            string title = ExtractLocalizedText(contentData, "title", lang);
            string subtitle = ExtractLocalizedText(contentData, "subtitle", lang);
            string content = ExtractLocalizedText(contentData, "content", lang);
            bool showContinueButton = ExtractBool(contentData, "showContinueButton", true);

            // Créer l'interface
            CreateModernTextUI(title, subtitle, content, showContinueButton);

            // Initialiser le tracking analytics
            if (TrainingAnalytics.Instance != null)
            {
                displayStartTime = Time.time;

                // Trouver la clé du contenu texte (chercher "text_" dans les clés)
                string contentKey = contentData.Keys.FirstOrDefault(k => k.StartsWith("text_"));
                if (string.IsNullOrEmpty(contentKey))
                {
                    // Fallback : utiliser "text_content" si aucune clé trouvée
                    contentKey = "text_content";
                }

                currentTextData = new TextInteractionData
                {
                    contentKey = contentKey,
                    objectId = objectId,
                    timeDisplayed = 0f,
                    readComplete = false,
                    scrollPercentage = 0f
                };

                // Ne pas appeler TrackTextDisplay ici car il termine immédiatement l'interaction
                // On va juste démarrer l'interaction et la terminer quand on ferme
                var textId = $"{objectId}_{contentKey}";
                var interaction = TrainingAnalytics.Instance.StartInteraction(objectId, "text", "informative");
                if (interaction != null)
                {
                    interaction.attempts = 1; // Un texte affiché compte comme une tentative
                    var dataDict = currentTextData.ToDictionary();
                    foreach (var kvp in dataDict)
                    {
                        interaction.AddData(kvp.Key, kvp.Value);
                    }
                }
            }
        }

        void CreateModernTextUI(string title, string subtitle, string content, bool showContinueButton)
        {
            // Clear root
            rootElement.Clear();

            // Container modal avec fond sombre - cliquable pour fermer
            modalContainer = new VisualElement();
            modalContainer.style.position = Position.Absolute;
            modalContainer.style.width = Length.Percent(100);
            modalContainer.style.height = Length.Percent(100);
            modalContainer.style.backgroundColor = new Color(0, 0, 0, 0.85f);
            modalContainer.style.alignItems = Align.Center;
            modalContainer.style.justifyContent = Justify.Center;
            modalContainer.pickingMode = PickingMode.Position;

            // Fermer en cliquant sur le fond
            modalContainer.RegisterCallback<PointerDownEvent>((evt) => {
                // Vérifier si le clic est sur le fond et non sur la contentBox
                if (evt.target == modalContainer)
                {
                    Close();
                }
            });

            // Boîte de contenu principale avec design moderne
            var contentBox = new VisualElement();
            contentBox.style.position = Position.Relative;
            contentBox.style.width = 850;
            contentBox.style.maxWidth = Length.Percent(90);
            contentBox.style.height = 650;
            contentBox.style.maxHeight = Length.Percent(85);
            contentBox.style.backgroundColor = new Color(0.08f, 0.08f, 0.12f, 0.98f);
            contentBox.style.borderTopLeftRadius = 20;
            contentBox.style.borderTopRightRadius = 20;
            contentBox.style.borderBottomLeftRadius = 20;
            contentBox.style.borderBottomRightRadius = 20;
            contentBox.style.flexDirection = FlexDirection.Column;

            // Ombre portée subtile
            contentBox.style.borderLeftWidth = 1;
            contentBox.style.borderRightWidth = 1;
            contentBox.style.borderTopWidth = 1;
            contentBox.style.borderBottomWidth = 1;
            contentBox.style.borderLeftColor = new Color(0.2f, 0.2f, 0.25f, 0.3f);
            contentBox.style.borderRightColor = new Color(0.2f, 0.2f, 0.25f, 0.3f);
            contentBox.style.borderTopColor = new Color(0.2f, 0.2f, 0.25f, 0.3f);
            contentBox.style.borderBottomColor = new Color(0.2f, 0.2f, 0.25f, 0.3f);

            // ========== HEADER SECTION (Fixe) ==========
            var headerWrapper = new VisualElement();
            headerWrapper.style.position = Position.Relative;
            headerWrapper.style.flexShrink = 0;
            headerWrapper.style.backgroundColor = new Color(0.06f, 0.06f, 0.1f, 1f);
            headerWrapper.style.borderTopLeftRadius = 20;
            headerWrapper.style.borderTopRightRadius = 20;

            var headerSection = new VisualElement();
            headerSection.style.paddingTop = 40;
            headerSection.style.paddingBottom = 25;
            headerSection.style.paddingLeft = 50;
            headerSection.style.paddingRight = 50;


            // Titre avec style moderne
            if (!string.IsNullOrEmpty(title))
            {
                var titleLabel = new Label(title);
                titleLabel.style.fontSize = 32;
                titleLabel.style.color = new Color(0.95f, 0.95f, 0.97f, 1f);
                titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                titleLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                titleLabel.style.marginBottom = 8;
                headerSection.Add(titleLabel);
            }

            // Sous-titre élégant
            if (!string.IsNullOrEmpty(subtitle))
            {
                var subtitleLabel = new Label(subtitle);
                subtitleLabel.style.fontSize = 16;
                subtitleLabel.style.color = new Color(0.65f, 0.65f, 0.7f, 1f);
                subtitleLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                headerSection.Add(subtitleLabel);
            }

            // Ligne de séparation élégante
            var separator = new VisualElement();
            separator.style.height = 1;
            separator.style.backgroundColor = new Color(0.2f, 0.2f, 0.25f, 0.3f);
            separator.style.marginTop = 20;

            headerSection.Add(separator);
            headerWrapper.Add(headerSection);
            contentBox.Add(headerWrapper);

            // ========== CONTENT SECTION (Scrollable) ==========
            var contentWrapper = new VisualElement();
            contentWrapper.style.flexGrow = 1;
            contentWrapper.style.overflow = Overflow.Hidden;
            contentWrapper.style.position = Position.Relative;

            // Custom ScrollView sans les boutons de flèche
            var scrollView = new ScrollView();
            scrollView.mode = ScrollViewMode.Vertical;
            scrollView.style.flexGrow = 1;
            contentScrollView = scrollView;

            // Tracker le scroll pour analytics
            scrollView.RegisterCallback<WheelEvent>((evt) => TrackScrollProgress());
            scrollView.RegisterCallback<GeometryChangedEvent>((evt) => TrackScrollProgress());
            // Tracker aussi les changements de valeur du conteneur
            scrollView.contentContainer.RegisterCallback<GeometryChangedEvent>((evt) => TrackScrollProgress());

            // Padding pour le contenu
            var contentContainer = new VisualElement();
            contentContainer.style.paddingTop = 30;
            contentContainer.style.paddingBottom = 30;
            contentContainer.style.paddingLeft = 50;
            contentContainer.style.paddingRight = 45; // Un peu moins à droite pour la scrollbar

            // Ajouter le contenu parsé
            if (!string.IsNullOrEmpty(content))
            {
                ParseAndCreateContent(content, contentContainer);
            }

            // Bouton Continuer élégant
            if (showContinueButton)
            {
                var buttonContainer = new VisualElement();
                buttonContainer.style.marginTop = 40;
                buttonContainer.style.alignItems = Align.Center;

                var continueButton = new Button(() => {
                    // Marquer comme lu complètement si on clique sur continuer
                    if (currentTextData != null)
                    {
                        currentTextData.readComplete = true;
                        currentTextData.timeDisplayed = Time.time - displayStartTime;
                        currentTextData.scrollPercentage = maxScrollPercentage;
                    }
                    OnCompleted?.Invoke(currentObjectId, true);
                    Close();
                });

                continueButton.text = LocalizationManager.Instance?.CurrentLanguage == "fr" ? "Continuer" : "Continue";
                continueButton.style.width = 220;
                continueButton.style.height = 48;
                continueButton.style.fontSize = 16;
                continueButton.style.backgroundColor = new Color(0.1f, 0.7f, 0.5f, 1f);
                continueButton.style.color = Color.white;
                continueButton.style.unityFontStyleAndWeight = FontStyle.Bold;
                continueButton.style.borderTopLeftRadius = 24;
                continueButton.style.borderTopRightRadius = 24;
                continueButton.style.borderBottomLeftRadius = 24;
                continueButton.style.borderBottomRightRadius = 24;
                continueButton.style.borderLeftWidth = 0;
                continueButton.style.borderRightWidth = 0;
                continueButton.style.borderTopWidth = 0;
                continueButton.style.borderBottomWidth = 0;

                // Hover effect
                continueButton.RegisterCallback<MouseEnterEvent>((evt) => {
                    continueButton.style.backgroundColor = new Color(0.15f, 0.8f, 0.6f, 1f);
                    continueButton.style.scale = new Scale(new Vector3(1.05f, 1.05f, 1f));
                });

                continueButton.RegisterCallback<MouseLeaveEvent>((evt) => {
                    continueButton.style.backgroundColor = new Color(0.1f, 0.7f, 0.5f, 1f);
                    continueButton.style.scale = new Scale(Vector3.one);
                });

                buttonContainer.Add(continueButton);
                contentContainer.Add(buttonContainer);
            }

            scrollView.Add(contentContainer);
            contentWrapper.Add(scrollView);

            // Personnaliser la scrollbar immédiatement et après le chargement
            contentBox.RegisterCallback<AttachToPanelEvent>((evt) => {
                CustomizeScrollbar(scrollView);
            });

            // Double sécurité pour les changements de géométrie
            scrollView.RegisterCallback<GeometryChangedEvent>((evt) => {
                CustomizeScrollbar(scrollView);
            });

            contentBox.Add(contentWrapper);
            modalContainer.Add(contentBox);
            rootElement.Add(modalContainer);
        }

        void CustomizeScrollbar(ScrollView scrollView)
        {
            var scroller = scrollView.verticalScroller;
            if (scroller == null) return;

            // Positionner la scrollbar
            scroller.style.position = Position.Absolute;
            scroller.style.right = 10;
            scroller.style.top = 10;
            scroller.style.bottom = 10;
            scroller.style.width = 4;

            // Cacher TOUS les boutons de la scrollbar (plus agressif)
            var allButtons = scroller.Query<Button>().ToList();
            foreach (var button in allButtons)
            {
                button.style.display = DisplayStyle.None;
                button.style.visibility = Visibility.Hidden;
                button.style.width = 0;
                button.style.height = 0;
            }

            // Cibler spécifiquement les boutons haut/bas
            var lowButton = scroller.Q<Button>("unity-low-button");
            if (lowButton != null)
            {
                lowButton.style.display = DisplayStyle.None;
                lowButton.style.visibility = Visibility.Hidden;
                lowButton.style.position = Position.Absolute;
                lowButton.style.top = -1000;
            }

            var highButton = scroller.Q<Button>("unity-high-button");
            if (highButton != null)
            {
                highButton.style.display = DisplayStyle.None;
                highButton.style.visibility = Visibility.Hidden;
                highButton.style.position = Position.Absolute;
                highButton.style.top = -1000;
            }

            // Rendre la track invisible
            var tracker = scroller.Q<VisualElement>("unity-tracker");
            if (tracker != null)
            {
                tracker.style.backgroundColor = Color.clear;
                tracker.style.borderLeftWidth = 0;
                tracker.style.borderRightWidth = 0;
                tracker.style.borderTopWidth = 0;
                tracker.style.borderBottomWidth = 0;
            }

            // Style du dragger (curseur)
            var dragger = scroller.Q<VisualElement>("unity-dragger");
            if (dragger != null)
            {
                dragger.style.backgroundColor = new Color(0.4f, 0.4f, 0.45f, 0.4f);
                dragger.style.borderLeftWidth = 0;
                dragger.style.borderRightWidth = 0;
                dragger.style.borderTopWidth = 0;
                dragger.style.borderBottomWidth = 0;
                dragger.style.borderTopLeftRadius = 2;
                dragger.style.borderTopRightRadius = 2;
                dragger.style.borderBottomLeftRadius = 2;
                dragger.style.borderBottomRightRadius = 2;
                dragger.style.width = 4;
                dragger.style.marginLeft = 0;
                dragger.style.marginRight = 0;
                dragger.style.minHeight = 30;
            }

            // Masquer le slider de base
            var slider = scroller.Q<VisualElement>("unity-slider");
            if (slider != null)
            {
                slider.style.backgroundColor = Color.clear;
                slider.style.borderLeftWidth = 0;
                slider.style.borderRightWidth = 0;
                slider.style.borderTopWidth = 0;
                slider.style.borderBottomWidth = 0;
            }
        }

        void ParseAndCreateContent(string content, VisualElement container)
        {
            string[] lines = content.Split('\n');

            foreach (string line in lines)
            {
                string trimmedLine = line.Trim();

                if (string.IsNullOrEmpty(trimmedLine))
                {
                    // Espace entre paragraphes
                    var spacer = new VisualElement();
                    spacer.style.height = 15;
                    container.Add(spacer);
                    continue;
                }

                // Headers avec différents niveaux
                if (trimmedLine.StartsWith("###"))
                {
                    CreateHeader(trimmedLine.Substring(3).Trim(), container, 3);
                }
                else if (trimmedLine.StartsWith("##"))
                {
                    CreateHeader(trimmedLine.Substring(2).Trim(), container, 2);
                }
                else if (trimmedLine.StartsWith("#"))
                {
                    CreateHeader(trimmedLine.Substring(1).Trim(), container, 1);
                }
                // Listes
                else if (trimmedLine.StartsWith("-") || trimmedLine.StartsWith("•"))
                {
                    CreateBulletPoint(trimmedLine.Substring(1).Trim(), container);
                }
                // Citations
                else if (trimmedLine.StartsWith(">"))
                {
                    CreateQuote(trimmedLine.Substring(1).Trim(), container);
                }
                // Avertissements
                else if (trimmedLine.StartsWith("!"))
                {
                    CreateWarning(trimmedLine.Substring(1).Trim(), container);
                }
                // Paragraphe normal
                else
                {
                    CreateParagraph(trimmedLine, container);
                }
            }
        }

        void CreateHeader(string text, VisualElement container, int level)
        {
            var header = new Label(text);

            switch (level)
            {
                case 1:
                    header.style.fontSize = 26;
                    header.style.color = new Color(0.1f, 0.8f, 0.6f, 1f);
                    header.style.marginTop = 25;
                    header.style.marginBottom = 15;
                    break;
                case 2:
                    header.style.fontSize = 22;
                    header.style.color = new Color(0.9f, 0.9f, 0.95f, 1f);
                    header.style.marginTop = 20;
                    header.style.marginBottom = 12;
                    break;
                case 3:
                    header.style.fontSize = 18;
                    header.style.color = new Color(0.8f, 0.8f, 0.85f, 1f);
                    header.style.marginTop = 15;
                    header.style.marginBottom = 10;
                    break;
            }

            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.whiteSpace = WhiteSpace.Normal;
            container.Add(header);
        }

        void CreateParagraph(string text, VisualElement container)
        {
            var paragraph = new Label(text);
            paragraph.style.fontSize = 15;
            paragraph.style.color = new Color(0.85f, 0.85f, 0.88f, 1f);
            paragraph.style.marginBottom = 12;
            paragraph.style.whiteSpace = WhiteSpace.Normal;
            container.Add(paragraph);
        }

        void CreateBulletPoint(string text, VisualElement container)
        {
            var bulletContainer = new VisualElement();
            bulletContainer.style.flexDirection = FlexDirection.Row;
            bulletContainer.style.marginBottom = 8;
            bulletContainer.style.marginLeft = 20;

            var bullet = new Label("•");
            bullet.style.fontSize = 16;
            bullet.style.color = new Color(0.1f, 0.8f, 0.6f, 1f);
            bullet.style.marginRight = 10;
            bullet.style.width = 15;

            var content = new Label(text);
            content.style.fontSize = 15;
            content.style.color = new Color(0.85f, 0.85f, 0.88f, 1f);
            content.style.flexGrow = 1;
            content.style.whiteSpace = WhiteSpace.Normal;

            bulletContainer.Add(bullet);
            bulletContainer.Add(content);
            container.Add(bulletContainer);
        }

        void CreateQuote(string text, VisualElement container)
        {
            var quote = new VisualElement();
            quote.style.backgroundColor = new Color(0.1f, 0.1f, 0.15f, 0.5f);
            quote.style.borderLeftWidth = 3;
            quote.style.borderLeftColor = new Color(0.1f, 0.8f, 0.6f, 0.8f);
            quote.style.paddingTop = 12;
            quote.style.paddingBottom = 12;
            quote.style.paddingLeft = 20;
            quote.style.paddingRight = 20;
            quote.style.marginTop = 10;
            quote.style.marginBottom = 10;
            quote.style.borderTopLeftRadius = 4;
            quote.style.borderBottomLeftRadius = 4;

            var quoteText = new Label(text);
            quoteText.style.fontSize = 15;
            quoteText.style.color = new Color(0.75f, 0.75f, 0.8f, 1f);
            quoteText.style.unityFontStyleAndWeight = FontStyle.Italic;
            quoteText.style.whiteSpace = WhiteSpace.Normal;

            quote.Add(quoteText);
            container.Add(quote);
        }

        void CreateWarning(string text, VisualElement container)
        {
            var warning = new VisualElement();
            warning.style.backgroundColor = new Color(0.8f, 0.5f, 0.1f, 0.15f);
            warning.style.borderLeftWidth = 3;
            warning.style.borderLeftColor = new Color(0.9f, 0.6f, 0.2f, 1f);
            warning.style.paddingTop = 12;
            warning.style.paddingBottom = 12;
            warning.style.paddingLeft = 20;
            warning.style.paddingRight = 20;
            warning.style.marginTop = 10;
            warning.style.marginBottom = 10;
            warning.style.borderTopLeftRadius = 4;
            warning.style.borderBottomLeftRadius = 4;

            var warningText = new Label("⚠ " + text);
            warningText.style.fontSize = 15;
            warningText.style.color = new Color(0.95f, 0.8f, 0.4f, 1f);
            warningText.style.whiteSpace = WhiteSpace.Normal;

            warning.Add(warningText);
            container.Add(warning);
        }

        public void Close()
        {
            // Finaliser le tracking analytics
            if (TrainingAnalytics.Instance != null && currentTextData != null)
            {
                currentTextData.timeDisplayed = Time.time - displayStartTime;
                currentTextData.scrollPercentage = maxScrollPercentage;
                // Considérer comme lu si affiché plus de 5 secondes ou scrollé à plus de 70%
                // ou si le bouton "Continuer" a été cliqué (temps > 1s pour éviter les clics accidentels)
                currentTextData.readComplete = currentTextData.timeDisplayed > 5f || maxScrollPercentage > 70f || currentTextData.timeDisplayed > 1f;

                // Mettre à jour les données avant de terminer
                if (TrainingAnalytics.Instance.GetCurrentInteraction() != null)
                {
                    var dataDict = currentTextData.ToDictionary();
                    foreach (var kvp in dataDict)
                    {
                        TrainingAnalytics.Instance.AddDataToCurrentInteraction(kvp.Key, kvp.Value);
                    }

                    // Les textes donnent toujours 100 points (pas d'échec possible)
                    TrainingAnalytics.Instance.AddDataToCurrentInteraction("finalScore", 100f);
                }

                // Le texte est considéré comme "réussi" s'il a été affiché
                TrainingAnalytics.Instance.EndCurrentInteraction(true);
            }

            rootElement?.Clear();
            OnClosed?.Invoke(currentObjectId);
        }

        void TrackScrollProgress()
        {
            if (contentScrollView == null || contentScrollView.verticalScroller == null) return;

            var scroller = contentScrollView.verticalScroller;

            // Vérifier si on peut calculer le scroll
            if (scroller.highValue > scroller.lowValue)
            {
                float scrollPercentage = (scroller.value / (scroller.highValue - scroller.lowValue)) * 100f;

                // Garder le maximum atteint
                if (scrollPercentage > maxScrollPercentage)
                {
                    maxScrollPercentage = scrollPercentage;

                    if (currentTextData != null)
                    {
                        currentTextData.scrollPercentage = maxScrollPercentage;
                    }
                }
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

        bool ExtractBool(Dictionary<string, object> data, string key, bool defaultValue = false)
        {
            if (!data.ContainsKey(key)) return defaultValue;

            var value = data[key];
            if (value is bool boolValue) return boolValue;
            if (value is string stringValue) return bool.TryParse(stringValue, out bool result) && result;
            if (value is int intValue) return intValue != 0;

            return defaultValue;
        }
    }
}