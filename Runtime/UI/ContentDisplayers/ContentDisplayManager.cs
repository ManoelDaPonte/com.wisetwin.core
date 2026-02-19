using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace WiseTwin.UI
{
    /// <summary>
    /// Gestionnaire principal pour afficher différents types de contenu
    /// Détermine quel afficheur utiliser selon le type de contenu
    /// </summary>
    public class ContentDisplayManager : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private bool debugMode = false;

        // UI Document
        private UIDocument uiDocument;
        private VisualElement root;

        // Afficheurs de contenu
        private Dictionary<ContentType, IContentDisplayer> contentDisplayers;

        // État actuel
        private ContentType currentContentType;
        private IContentDisplayer currentDisplayer;
        private bool isDisplaying = false;

        // Singleton
        public static ContentDisplayManager Instance { get; private set; }

        // Public properties
        public bool DebugMode => debugMode;

        // Events
        public event Action<ContentType, string> OnContentDisplayed;
        public event Action<ContentType, string> OnContentClosed;
        public event Action<string, bool> OnContentCompleted; // objectId, success

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                // Ne pas appliquer DontDestroyOnLoad si on est dans WiseTwinSystem
                // C'est le parent WiseTwinSystem qui gère la persistance
                if (transform.parent == null)
                {
                    DontDestroyOnLoad(gameObject);
                }
                // Pas de warning si on est enfant de WiseTwinSystem
                InitializeDisplayers();
                SetupUIDocument();

                if (debugMode) Debug.Log("[ContentDisplayManager] Instance created and initialized");
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        void InitializeDisplayers()
        {
            contentDisplayers = new Dictionary<ContentType, IContentDisplayer>();

            // S'assurer que TrainingAnalytics existe pour capturer les métriques
            if (Analytics.TrainingAnalytics.Instance == null)
            {
                var analyticsGO = new GameObject("TrainingAnalytics");
                analyticsGO.AddComponent<Analytics.TrainingAnalytics>();

                // Si on a un parent WiseTwinSystem, mettre TrainingAnalytics dedans
                var wiseTwinSystem = GameObject.Find("WiseTwinSystem");
                if (wiseTwinSystem != null)
                {
                    analyticsGO.transform.SetParent(wiseTwinSystem.transform);
                }

                if (debugMode) Debug.Log("[ContentDisplayManager] Created TrainingAnalytics instance");
            }

            // Créer les afficheurs pour chaque type
            var questionDisplayer = new GameObject("QuestionDisplayer").AddComponent<QuestionDisplayer>();
            questionDisplayer.transform.SetParent(transform);
            contentDisplayers[ContentType.Question] = questionDisplayer;

            var procedureDisplayer = new GameObject("ProcedureDisplayer").AddComponent<ProcedureDisplayer>();
            procedureDisplayer.transform.SetParent(transform);
            contentDisplayers[ContentType.Procedure] = procedureDisplayer;

            var textDisplayer = new GameObject("TextDisplayer").AddComponent<TextDisplayer>();
            textDisplayer.transform.SetParent(transform);
            contentDisplayers[ContentType.Text] = textDisplayer;

            var dialogueDisplayer = new GameObject("DialogueDisplayer").AddComponent<DialogueDisplayer>();
            dialogueDisplayer.transform.SetParent(transform);
            contentDisplayers[ContentType.Dialogue] = dialogueDisplayer;

            if (debugMode) Debug.Log($"[ContentDisplayManager] Initialized {contentDisplayers.Count} displayers");
        }

        void SetupUIDocument()
        {
            // S'assurer d'avoir notre propre UIDocument
            uiDocument = GetComponent<UIDocument>();
            if (uiDocument == null)
            {
                uiDocument = gameObject.AddComponent<UIDocument>();
                if (debugMode) Debug.Log("[ContentDisplayManager] Created UIDocument component");
            }

            // Vérifier qu'on ne partage pas le UIDocument avec un autre composant
            var otherUIUsers = GetComponents<MonoBehaviour>()
                .Where(c => c != this && c.GetType().Name.Contains("HUD"))
                .ToArray();

            if (otherUIUsers.Length > 0)
            {
                Debug.LogError($"[ContentDisplayManager] WARNING: Sharing GameObject with {otherUIUsers[0].GetType().Name}! " +
                    "ContentDisplayManager should be on its own GameObject with its own UIDocument.");
            }

            if (uiDocument.panelSettings == null)
            {
                Debug.LogWarning("[ContentDisplayManager] PanelSettings is null! Please assign it in the inspector.");
            }
            else if (debugMode)
            {
                Debug.Log($"[ContentDisplayManager] PanelSettings assigned: {uiDocument.panelSettings.name}");
            }

            root = uiDocument.rootVisualElement;
            if (root == null)
            {
                Debug.LogError("[ContentDisplayManager] Root visual element is null!");
                return;
            }

            if (debugMode) Debug.Log($"[ContentDisplayManager] Root element setup - Width: {root.resolvedStyle.width}, Height: {root.resolvedStyle.height}");

            // Configuration de base du root
            root.style.position = Position.Absolute;
            root.style.width = Length.Percent(100);
            root.style.height = Length.Percent(100);
            root.pickingMode = PickingMode.Ignore; // Par défaut, ne pas bloquer les clics
        }

        /// <summary>
        /// Display a scenario from the new scenario-based system
        /// </summary>
        public void DisplayScenario(WiseTwin.ScenarioData scenario)
        {
            if (scenario == null)
            {
                Debug.LogError("[ContentDisplayManager] Scenario is null!");
                return;
            }

            // Determine ContentType from scenario type
            ContentType contentType;
            switch (scenario.type?.ToLower())
            {
                case "question":
                    contentType = ContentType.Question;
                    break;
                case "procedure":
                    contentType = ContentType.Procedure;
                    break;
                case "text":
                    contentType = ContentType.Text;
                    break;
                case "dialogue":
                    contentType = ContentType.Dialogue;
                    break;
                default:
                    Debug.LogError($"[ContentDisplayManager] Unknown scenario type: {scenario.type}");
                    return;
            }

            // Get content data
            var contentData = scenario.GetContentData();
            if (contentData == null)
            {
                Debug.LogError($"[ContentDisplayManager] No content data for scenario {scenario.id}");
                return;
            }

            // Convert JObject to Dictionary<string, object>
            Dictionary<string, object> contentDict = contentData.ToObject<Dictionary<string, object>>();

            // Display the content
            DisplayContent(scenario.id, contentType, contentDict);
        }

        /// <summary>
        /// Affiche un contenu en fonction de son type
        /// </summary>
        public void DisplayContent(string objectId, ContentType contentType, Dictionary<string, object> contentData)
        {
            if (isDisplaying)
            {
                if (debugMode) Debug.LogWarning("[ContentDisplayManager] Already displaying content, closing current...");
                CloseCurrentContent();
            }

            if (!contentDisplayers.ContainsKey(contentType))
            {
                Debug.LogError($"[ContentDisplayManager] No displayer for content type: {contentType}");
                ShowPlaceholderUI(contentType, contentData);
                return;
            }

            currentContentType = contentType;
            currentDisplayer = contentDisplayers[contentType];
            isDisplaying = true;

            // Bloquer les clics pendant l'affichage
            root.pickingMode = PickingMode.Position;

            if (debugMode)
            {
                Debug.Log($"[ContentDisplayManager] Before display - Root child count: {root.childCount}");
                Debug.Log($"[ContentDisplayManager] Root size: {root.resolvedStyle.width}x{root.resolvedStyle.height}");
            }

            // Afficher le contenu
            currentDisplayer.Display(objectId, contentData, root);

            if (debugMode) Debug.Log($"[ContentDisplayManager] After display - Root child count: {root.childCount}");

            // S'abonner aux événements de l'afficheur
            currentDisplayer.OnClosed += HandleContentClosed;
            currentDisplayer.OnCompleted += HandleContentCompleted;

            OnContentDisplayed?.Invoke(contentType, objectId);

            if (debugMode) Debug.Log($"[ContentDisplayManager] Displaying {contentType} content for {objectId}");
        }

        /// <summary>
        /// Affiche une UI placeholder pour les types non implémentés
        /// </summary>
        private void ShowPlaceholderUI(ContentType contentType, Dictionary<string, object> contentData)
        {
            // Clear root
            root.Clear();
            root.pickingMode = PickingMode.Position;

            // Container principal
            var container = new VisualElement();
            container.style.position = Position.Absolute;
            container.style.width = Length.Percent(100);
            container.style.height = Length.Percent(100);
            container.style.backgroundColor = new Color(0, 0, 0, 0.8f);
            container.style.alignItems = Align.Center;
            container.style.justifyContent = Justify.Center;

            // Boîte de contenu
            var contentBox = new VisualElement();
            contentBox.style.width = 600;
            contentBox.style.paddingTop = 40;
            contentBox.style.paddingBottom = 40;
            contentBox.style.paddingLeft = 40;
            contentBox.style.paddingRight = 40;
            contentBox.style.backgroundColor = new Color(0.1f, 0.1f, 0.15f, 0.98f);
            contentBox.style.borderTopLeftRadius = 20;
            contentBox.style.borderTopRightRadius = 20;
            contentBox.style.borderBottomLeftRadius = 20;
            contentBox.style.borderBottomRightRadius = 20;

            // Titre
            var title = new Label($"Content Type: {contentType}");
            title.style.fontSize = 28;
            title.style.color = Color.white;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.marginBottom = 20;
            title.style.unityTextAlign = TextAnchor.MiddleCenter;
            contentBox.Add(title);

            // Message
            var message = new Label("This content type is not yet implemented.\nClick anywhere to close.");
            message.style.fontSize = 18;
            message.style.color = new Color(0.8f, 0.8f, 0.8f);
            message.style.unityTextAlign = TextAnchor.MiddleCenter;
            message.style.marginBottom = 30;
            contentBox.Add(message);

            // Afficher les données de debug si activé
            if (debugMode && contentData != null)
            {
                var debugText = new Label($"Data keys: {string.Join(", ", contentData.Keys)}");
                debugText.style.fontSize = 14;
                debugText.style.color = new Color(0.6f, 0.6f, 0.6f);
                debugText.style.unityTextAlign = TextAnchor.MiddleCenter;
                contentBox.Add(debugText);
            }

            container.Add(contentBox);
            root.Add(container);

            // Fermer au clic
            container.RegisterCallback<MouseDownEvent>((evt) => {
                root.Clear();
                root.pickingMode = PickingMode.Ignore;
                isDisplaying = false;
            });
        }

        void HandleContentClosed(string objectId)
        {
            if (currentDisplayer != null)
            {
                currentDisplayer.OnClosed -= HandleContentClosed;
                currentDisplayer.OnCompleted -= HandleContentCompleted;
            }

            CloseCurrentContent();
            OnContentClosed?.Invoke(currentContentType, objectId);
        }

        void HandleContentCompleted(string objectId, bool success)
        {
            OnContentCompleted?.Invoke(objectId, success);
        }

        public void CloseCurrentContent()
        {
            if (currentDisplayer != null)
            {
                currentDisplayer.Close();
                currentDisplayer = null;
            }

            root.Clear();
            root.pickingMode = PickingMode.Ignore;
            isDisplaying = false;

            if (debugMode) Debug.Log("[ContentDisplayManager] Content closed");
        }

        public bool IsDisplaying => isDisplaying;
        public ContentType CurrentContentType => currentContentType;
    }

    /// <summary>
    /// Interface pour tous les afficheurs de contenu
    /// </summary>
    public interface IContentDisplayer
    {
        event Action<string> OnClosed;
        event Action<string, bool> OnCompleted; // objectId, success

        void Display(string objectId, Dictionary<string, object> contentData, VisualElement root);
        void Close();
    }
}