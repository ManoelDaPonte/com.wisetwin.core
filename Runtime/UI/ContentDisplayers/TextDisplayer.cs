using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System;
using System.Text.RegularExpressions;

namespace WiseTwin.UI
{
    /// <summary>
    /// Afficheur spécialisé pour du contenu textuel riche (avec support markdown-like)
    /// </summary>
    public class TextDisplayer : MonoBehaviour, IContentDisplayer
    {
        public event Action<string> OnClosed;
        public event Action<string, bool> OnCompleted;

        private string currentObjectId;
        private VisualElement rootElement;
        private VisualElement modalContainer;

        public void Display(string objectId, Dictionary<string, object> contentData, VisualElement root)
        {
            currentObjectId = objectId;
            rootElement = root;

            Debug.Log($"[TextDisplayer] Starting display for {objectId}");

            // Obtenir la langue actuelle
            string lang = LocalizationManager.Instance?.CurrentLanguage ?? "en";

            // Extraire les données
            string title = ExtractLocalizedText(contentData, "title", lang);
            string content = ExtractLocalizedText(contentData, "content", lang);
            string subtitle = ExtractLocalizedText(contentData, "subtitle", lang);
            bool showContinueButton = ExtractBool(contentData, "showContinueButton", true);

            // Créer l'UI
            CreateTextUI(title, subtitle, content, showContinueButton);
        }

        void CreateTextUI(string title, string subtitle, string content, bool showContinueButton)
        {
            // Clear root
            rootElement.Clear();

            // Container modal
            modalContainer = new VisualElement();
            modalContainer.style.position = Position.Absolute;
            modalContainer.style.width = Length.Percent(100);
            modalContainer.style.height = Length.Percent(100);
            modalContainer.style.backgroundColor = new Color(0, 0, 0, 0.85f);
            modalContainer.style.alignItems = Align.Center;
            modalContainer.style.justifyContent = Justify.Center;
            modalContainer.pickingMode = PickingMode.Position;

            // Boîte de contenu
            var contentBox = new ScrollView();
            contentBox.style.width = 800;
            contentBox.style.maxWidth = Length.Percent(90);
            contentBox.style.height = 600;
            contentBox.style.maxHeight = Length.Percent(80);
            contentBox.style.backgroundColor = new Color(0.1f, 0.1f, 0.15f, 0.98f);
            contentBox.style.borderTopLeftRadius = 25;
            contentBox.style.borderTopRightRadius = 25;
            contentBox.style.borderBottomLeftRadius = 25;
            contentBox.style.borderBottomRightRadius = 25;
            contentBox.style.paddingTop = 40;
            contentBox.style.paddingBottom = 40;
            contentBox.style.paddingLeft = 40;
            contentBox.style.paddingRight = 40;

            // Container interne pour le contenu
            var innerContainer = new VisualElement();
            contentBox.Add(innerContainer);

            // Bouton fermer (X)
            var closeButton = new Button(() => Close());
            closeButton.text = "✕";
            closeButton.style.position = Position.Absolute;
            closeButton.style.top = 15;
            closeButton.style.right = 15;
            closeButton.style.width = 35;
            closeButton.style.height = 35;
            closeButton.style.fontSize = 24;
            closeButton.style.backgroundColor = new Color(0.8f, 0.2f, 0.2f, 0.8f);
            closeButton.style.color = Color.white;
            closeButton.style.borderTopLeftRadius = 17;
            closeButton.style.borderTopRightRadius = 17;
            closeButton.style.borderBottomLeftRadius = 17;
            closeButton.style.borderBottomRightRadius = 17;
            modalContainer.Add(closeButton);

            // Titre
            if (!string.IsNullOrEmpty(title))
            {
                var titleLabel = new Label(title);
                titleLabel.style.fontSize = 28;
                titleLabel.style.color = new Color(0.1f, 0.8f, 0.6f, 1f);
                titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                titleLabel.style.marginBottom = 15;
                titleLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                innerContainer.Add(titleLabel);
            }

            // Sous-titre
            if (!string.IsNullOrEmpty(subtitle))
            {
                var subtitleLabel = new Label(subtitle);
                subtitleLabel.style.fontSize = 18;
                subtitleLabel.style.color = new Color(0.7f, 0.7f, 0.7f, 1f);
                subtitleLabel.style.marginBottom = 25;
                subtitleLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                innerContainer.Add(subtitleLabel);
            }

            // Contenu principal avec support markdown-like
            if (!string.IsNullOrEmpty(content))
            {
                ParseAndCreateContent(content, innerContainer);
            }

            // Bouton Continuer (optionnel)
            if (showContinueButton)
            {
                var continueButton = new Button(() => {
                    OnCompleted?.Invoke(currentObjectId, true);
                    Close();
                });
                continueButton.text = LocalizationManager.Instance?.CurrentLanguage == "fr" ? "Continuer" : "Continue";
                continueButton.style.width = 200;
                continueButton.style.height = 50;
                continueButton.style.fontSize = 18;
                continueButton.style.backgroundColor = new Color(0.1f, 0.8f, 0.6f, 1f);
                continueButton.style.color = Color.white;
                continueButton.style.unityFontStyleAndWeight = FontStyle.Bold;
                continueButton.style.borderTopLeftRadius = 10;
                continueButton.style.borderTopRightRadius = 10;
                continueButton.style.borderBottomLeftRadius = 10;
                continueButton.style.borderBottomRightRadius = 10;
                continueButton.style.marginTop = 30;
                continueButton.style.alignSelf = Align.Center;
                innerContainer.Add(continueButton);
            }

            modalContainer.Add(contentBox);
            rootElement.Add(modalContainer);
        }

        void ParseAndCreateContent(string content, VisualElement container)
        {
            // Diviser le contenu par lignes
            string[] lines = content.Split('\n');

            foreach (string line in lines)
            {
                string trimmedLine = line.Trim();

                if (string.IsNullOrEmpty(trimmedLine))
                {
                    // Ligne vide = espace
                    var spacer = new VisualElement();
                    spacer.style.height = 10;
                    container.Add(spacer);
                    continue;
                }

                // Headers (# ## ###)
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
                // Liste à puces (- ou *)
                else if (trimmedLine.StartsWith("-") || trimmedLine.StartsWith("*"))
                {
                    CreateBulletPoint(trimmedLine.Substring(1).Trim(), container);
                }
                // Liste numérotée (1. 2. etc)
                else if (Regex.IsMatch(trimmedLine, @"^\d+\."))
                {
                    int dotIndex = trimmedLine.IndexOf('.');
                    CreateNumberedPoint(trimmedLine.Substring(dotIndex + 1).Trim(), container);
                }
                // Important/Warning (!)
                else if (trimmedLine.StartsWith("!"))
                {
                    CreateWarningBox(trimmedLine.Substring(1).Trim(), container);
                }
                // Citation (>)
                else if (trimmedLine.StartsWith(">"))
                {
                    CreateQuote(trimmedLine.Substring(1).Trim(), container);
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
            header.style.color = Color.white;
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.marginTop = 15;
            header.style.marginBottom = 10;

            switch (level)
            {
                case 1:
                    header.style.fontSize = 24;
                    header.style.color = new Color(0.1f, 0.8f, 0.6f, 1f);
                    break;
                case 2:
                    header.style.fontSize = 20;
                    header.style.color = new Color(0.2f, 0.7f, 0.9f, 1f);
                    break;
                case 3:
                    header.style.fontSize = 18;
                    break;
            }

            container.Add(header);
        }

        void CreateParagraph(string text, VisualElement container)
        {
            var paragraph = new Label(ProcessInlineFormatting(text));
            paragraph.style.fontSize = 16;
            paragraph.style.color = new Color(0.9f, 0.9f, 0.9f, 1f);
            paragraph.style.whiteSpace = WhiteSpace.Normal;
            paragraph.style.marginBottom = 10;
            container.Add(paragraph);
        }

        void CreateBulletPoint(string text, VisualElement container)
        {
            var bulletContainer = new VisualElement();
            bulletContainer.style.flexDirection = FlexDirection.Row;
            bulletContainer.style.marginBottom = 5;
            bulletContainer.style.marginLeft = 20;

            var bullet = new Label("•");
            bullet.style.fontSize = 16;
            bullet.style.color = new Color(0.1f, 0.8f, 0.6f, 1f);
            bullet.style.marginRight = 10;
            bulletContainer.Add(bullet);

            var content = new Label(ProcessInlineFormatting(text));
            content.style.fontSize = 16;
            content.style.color = new Color(0.9f, 0.9f, 0.9f, 1f);
            content.style.flexGrow = 1;
            content.style.whiteSpace = WhiteSpace.Normal;
            bulletContainer.Add(content);

            container.Add(bulletContainer);
        }

        void CreateNumberedPoint(string text, VisualElement container)
        {
            var bulletContainer = new VisualElement();
            bulletContainer.style.flexDirection = FlexDirection.Row;
            bulletContainer.style.marginBottom = 5;
            bulletContainer.style.marginLeft = 20;

            var content = new Label(ProcessInlineFormatting(text));
            content.style.fontSize = 16;
            content.style.color = new Color(0.9f, 0.9f, 0.9f, 1f);
            content.style.flexGrow = 1;
            content.style.whiteSpace = WhiteSpace.Normal;
            bulletContainer.Add(content);

            container.Add(bulletContainer);
        }

        void CreateWarningBox(string text, VisualElement container)
        {
            var warningBox = new VisualElement();
            warningBox.style.backgroundColor = new Color(0.8f, 0.6f, 0.1f, 0.2f);
            warningBox.style.borderLeftWidth = 3;
            warningBox.style.borderLeftColor = new Color(0.8f, 0.6f, 0.1f, 1f);
            warningBox.style.paddingTop = 10;
            warningBox.style.paddingBottom = 10;
            warningBox.style.paddingLeft = 15;
            warningBox.style.paddingRight = 15;
            warningBox.style.marginTop = 10;
            warningBox.style.marginBottom = 10;
            warningBox.style.borderTopLeftRadius = 5;
            warningBox.style.borderTopRightRadius = 5;
            warningBox.style.borderBottomLeftRadius = 5;
            warningBox.style.borderBottomRightRadius = 5;

            var warningLabel = new Label("⚠️ " + text);
            warningLabel.style.fontSize = 16;
            warningLabel.style.color = new Color(1f, 0.9f, 0.6f, 1f);
            warningLabel.style.whiteSpace = WhiteSpace.Normal;
            warningBox.Add(warningLabel);

            container.Add(warningBox);
        }

        void CreateQuote(string text, VisualElement container)
        {
            var quoteBox = new VisualElement();
            quoteBox.style.borderLeftWidth = 3;
            quoteBox.style.borderLeftColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            quoteBox.style.paddingLeft = 15;
            quoteBox.style.marginTop = 10;
            quoteBox.style.marginBottom = 10;
            quoteBox.style.marginLeft = 10;

            var quoteLabel = new Label(text);
            quoteLabel.style.fontSize = 16;
            quoteLabel.style.color = new Color(0.7f, 0.7f, 0.7f, 1f);
            quoteLabel.style.unityFontStyleAndWeight = FontStyle.Italic;
            quoteLabel.style.whiteSpace = WhiteSpace.Normal;
            quoteBox.Add(quoteLabel);

            container.Add(quoteBox);
        }

        string ProcessInlineFormatting(string text)
        {
            // Pour l'instant, on retourne le texte tel quel
            // On pourrait ajouter du support pour **bold**, *italic*, etc.
            return text;
        }

        public void Close()
        {
            rootElement?.Clear();
            OnClosed?.Invoke(currentObjectId);
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
            if (data.ContainsKey(key))
            {
                var value = data[key];
                if (value is bool boolValue) return boolValue;
                if (bool.TryParse(value?.ToString(), out bool parsed)) return parsed;
            }
            return defaultValue;
        }
    }
}