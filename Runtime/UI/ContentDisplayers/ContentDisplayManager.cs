using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System;
using System.Linq;

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
                DontDestroyOnLoad(gameObject);
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

            // TODO: Ajouter d'autres types d'afficheurs
            // contentDisplayers[ContentType.Media] = new MediaDisplayer();
            // contentDisplayers[ContentType.Dialogue] = new DialogueDisplayer();
            // contentDisplayers[ContentType.Instruction] = new InstructionDisplayer();

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

            // Assigner le PanelSettings s'il n'est pas déjà assigné
            if (uiDocument.panelSettings == null)
            {
                Debug.LogWarning("[ContentDisplayManager] PanelSettings is null! UI won't display. Please assign WiseTwinPanelSettings to the UIDocument component.");

                #if UNITY_EDITOR
                var panelSettings = UnityEditor.AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/WiseTwinPanelSettings.asset");
                if (panelSettings != null)
                {
                    uiDocument.panelSettings = panelSettings;
                    if (debugMode) Debug.Log("[ContentDisplayManager] Auto-assigned PanelSettings from Assets");
                }
                else
                {
                    Debug.LogError("[ContentDisplayManager] Could not find WiseTwinPanelSettings.asset! Please create it and assign it manually.");
                }
                #endif
            }
            else
            {
                if (debugMode) Debug.Log($"[ContentDisplayManager] PanelSettings assigned: {uiDocument.panelSettings.name}");
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

            // Mettre à jour la progression dans le HUD (avec anti-triche)
            if (success && TrainingHUD.Instance != null)
            {
                TrainingHUD.Instance.IncrementProgressForObject(objectId);
            }
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