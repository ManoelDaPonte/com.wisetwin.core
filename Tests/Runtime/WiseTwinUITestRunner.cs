using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;

namespace WiseTwin.Tests
{
    /// <summary>
    /// Test Runner pour vérifier que le système UI WiseTwin fonctionne correctement
    /// Utilisé pour les tests manuels dans l'éditeur Unity
    /// </summary>
    public class WiseTwinUITestRunner : MonoBehaviour
    {
        [Header("Test Configuration")]
        [SerializeField] private TestMode testMode = TestMode.LanguageSelection;
        [SerializeField] private bool autoStart = true;
        [SerializeField] private bool useMinimalSetup = true;

        [Header("Panel Settings")]
        [SerializeField] private PanelSettings panelSettings;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;

        private UIDocument uiDocument;
        private LanguageSelectionUI languageUI;
        private WiseTwinUIManager uiManager;

        public enum TestMode
        {
            LanguageSelection,      // Test sélection de langue uniquement
            FullSystem,            // Test système complet avec tous les managers
            UIToolkitBasic,        // Test basique UI Toolkit
            QuestionModal          // Test modal de question
        }

        void Awake()
        {
            if (enableDebugLogs) Debug.Log($"[WiseTwinUITestRunner] Starting test mode: {testMode}");

            // Setup UIDocument
            SetupUIDocument();
        }

        void Start()
        {
            if (autoStart)
            {
                StartCoroutine(RunTest());
            }
        }

        void SetupUIDocument()
        {
            // Récupérer ou créer UIDocument
            uiDocument = GetComponent<UIDocument>();
            if (uiDocument == null)
            {
                uiDocument = gameObject.AddComponent<UIDocument>();
                if (enableDebugLogs) Debug.Log("[WiseTwinUITestRunner] UIDocument component added");
            }

            // Assigner PanelSettings
            if (uiDocument.panelSettings == null)
            {
                if (panelSettings != null)
                {
                    uiDocument.panelSettings = panelSettings;
                    if (enableDebugLogs) Debug.Log("[WiseTwinUITestRunner] PanelSettings assigned from inspector");
                }
                else
                {
                    // Essayer de charger depuis Assets
                    #if UNITY_EDITOR
                    var settings = UnityEditor.AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/WiseTwinPanelSettings.asset");
                    if (settings != null)
                    {
                        uiDocument.panelSettings = settings;
                        panelSettings = settings;
                        if (enableDebugLogs) Debug.Log("[WiseTwinUITestRunner] PanelSettings loaded from Assets");
                    }
                    else
                    {
                        Debug.LogError("[WiseTwinUITestRunner] No PanelSettings found! Please assign one in the inspector.");
                    }
                    #endif
                }
            }
        }

        IEnumerator RunTest()
        {
            yield return new WaitForEndOfFrame();

            switch (testMode)
            {
                case TestMode.LanguageSelection:
                    yield return TestLanguageSelection();
                    break;

                case TestMode.FullSystem:
                    yield return TestFullSystem();
                    break;

                case TestMode.UIToolkitBasic:
                    TestUIToolkitBasic();
                    break;

                case TestMode.QuestionModal:
                    yield return TestQuestionModal();
                    break;
            }
        }

        IEnumerator TestLanguageSelection()
        {
            if (enableDebugLogs) Debug.Log("[WiseTwinUITestRunner] Testing Language Selection UI");

            if (useMinimalSetup)
            {
                // Setup minimal : juste LanguageSelectionUI
                languageUI = GetComponent<LanguageSelectionUI>();
                if (languageUI == null)
                {
                    languageUI = gameObject.AddComponent<LanguageSelectionUI>();
                    if (enableDebugLogs) Debug.Log("[WiseTwinUITestRunner] LanguageSelectionUI component added");
                }
            }
            else
            {
                // Setup complet avec tous les managers
                yield return SetupFullManagers();
            }

            // L'UI devrait s'afficher automatiquement si showOnStart = true
            if (enableDebugLogs) Debug.Log("[WiseTwinUITestRunner] Language selection should now be visible");
        }

        IEnumerator TestFullSystem()
        {
            if (enableDebugLogs) Debug.Log("[WiseTwinUITestRunner] Testing Full WiseTwin System");

            yield return SetupFullManagers();

            // L'UI devrait s'afficher automatiquement
            if (enableDebugLogs) Debug.Log("[WiseTwinUITestRunner] Full system test complete");
        }

        IEnumerator SetupFullManagers()
        {
            // WiseTwinManager
            var wiseTwinManager = FindFirstObjectByType<WiseTwinManager>();
            if (wiseTwinManager == null)
            {
                var managerGO = new GameObject("WiseTwinManager");
                wiseTwinManager = managerGO.AddComponent<WiseTwinManager>();
                if (enableDebugLogs) Debug.Log("[WiseTwinUITestRunner] WiseTwinManager created");
            }

            // LocalizationManager
            var localizationManager = FindFirstObjectByType<LocalizationManager>();
            if (localizationManager == null)
            {
                var locGO = new GameObject("LocalizationManager");
                localizationManager = locGO.AddComponent<LocalizationManager>();
                if (enableDebugLogs) Debug.Log("[WiseTwinUITestRunner] LocalizationManager created");
            }

            // WiseTwinUIManager
            uiManager = FindFirstObjectByType<WiseTwinUIManager>();
            if (uiManager == null)
            {
                uiManager = gameObject.AddComponent<WiseTwinUIManager>();
                if (enableDebugLogs) Debug.Log("[WiseTwinUITestRunner] WiseTwinUIManager added");
            }

            // LanguageSelectionUI
            languageUI = GetComponent<LanguageSelectionUI>();
            if (languageUI == null)
            {
                languageUI = gameObject.AddComponent<LanguageSelectionUI>();
                if (enableDebugLogs) Debug.Log("[WiseTwinUITestRunner] LanguageSelectionUI added");
            }

            yield return new WaitForSeconds(0.5f);
        }

        void TestUIToolkitBasic()
        {
            if (enableDebugLogs) Debug.Log("[WiseTwinUITestRunner] Testing Basic UI Toolkit");

            var root = uiDocument.rootVisualElement;
            root.Clear();

            // Fond coloré pour vérifier que ça fonctionne
            root.style.backgroundColor = new Color(0.1f, 0.1f, 0.2f, 1f);
            root.style.position = Position.Absolute;
            root.style.width = Length.Percent(100);
            root.style.height = Length.Percent(100);
            root.style.alignItems = Align.Center;
            root.style.justifyContent = Justify.Center;

            // Boîte centrale
            var box = new VisualElement();
            box.style.width = 400;
            box.style.height = 300;
            box.style.backgroundColor = new Color(0.2f, 0.3f, 0.5f, 1f);
            box.style.borderTopLeftRadius = 20;
            box.style.borderTopRightRadius = 20;
            box.style.borderBottomLeftRadius = 20;
            box.style.borderBottomRightRadius = 20;
            box.style.alignItems = Align.Center;
            box.style.justifyContent = Justify.Center;

            // Texte
            var label = new Label("UI Toolkit Works! ✅");
            label.style.fontSize = 32;
            label.style.color = Color.white;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;

            box.Add(label);
            root.Add(box);

            if (enableDebugLogs) Debug.Log("[WiseTwinUITestRunner] Basic UI created - you should see a blue box with text");
        }

        IEnumerator TestQuestionModal()
        {
            if (enableDebugLogs) Debug.Log("[WiseTwinUITestRunner] Testing Question Modal");

            // Setup UIManager
            uiManager = GetComponent<WiseTwinUIManager>();
            if (uiManager == null)
            {
                uiManager = gameObject.AddComponent<WiseTwinUIManager>();
            }

            yield return new WaitForSeconds(0.5f);

            // Afficher une question test
            uiManager.ShowQuestion(
                "Ceci est une question de test. Quelle est la bonne réponse?",
                new string[] { "Option A", "Option B", "Option C", "Option D" },
                QuestionType.MultipleChoice
            );

            if (enableDebugLogs) Debug.Log("[WiseTwinUITestRunner] Question modal should now be visible");
        }

        // Méthodes utilitaires pour les tests manuels
        [ContextMenu("Force Show Language Selection")]
        public void ForceShowLanguageSelection()
        {
            if (languageUI != null)
            {
                languageUI.ShowLanguageSelection();
            }
            else
            {
                Debug.LogWarning("LanguageSelectionUI not found!");
            }
        }

        [ContextMenu("Test Show Question")]
        public void TestShowQuestion()
        {
            if (uiManager != null)
            {
                uiManager.ShowQuestion(
                    "Test Question: What is 2 + 2?",
                    new string[] { "3", "4", "5", "6" },
                    QuestionType.MultipleChoice
                );
            }
            else
            {
                Debug.LogWarning("WiseTwinUIManager not found!");
            }
        }

        [ContextMenu("Clear UI")]
        public void ClearUI()
        {
            if (uiDocument != null && uiDocument.rootVisualElement != null)
            {
                uiDocument.rootVisualElement.Clear();
                Debug.Log("UI Cleared");
            }
        }
    }
}