using UnityEngine;
using System.Collections.Generic;

namespace WiseTwin
{
    /// <summary>
    /// Gestionnaire de localisation pour WiseTwin
    /// Gère la langue actuelle et fournit des helpers pour extraire les textes localisés
    /// </summary>
    public class LocalizationManager : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private string defaultLanguage = "en";
        [SerializeField] private string currentLanguage = "en";

        [Header("Supported Languages")]
        [SerializeField] private List<LanguageInfo> supportedLanguages = new List<LanguageInfo>()
        {
            new LanguageInfo { code = "en", displayName = "English", nativeName = "English" },
            new LanguageInfo { code = "fr", displayName = "French", nativeName = "Français" }
        };

        [Header("Debug")]
        [SerializeField] private bool debugMode = false;

        // Singleton
        public static LocalizationManager Instance { get; private set; }

        // Events
        public System.Action<string> OnLanguageChanged;

        // Properties
        public string CurrentLanguage => currentLanguage;
        public string DefaultLanguage => defaultLanguage;
        public List<LanguageInfo> SupportedLanguages => supportedLanguages;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);

                // Charger la langue sauvegardée
                LoadSavedLanguage();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        void LoadSavedLanguage()
        {
            // Charger depuis PlayerPrefs
            string savedLang = PlayerPrefs.GetString("WiseTwin_Language", "");

            if (!string.IsNullOrEmpty(savedLang) && IsLanguageSupported(savedLang))
            {
                currentLanguage = savedLang;
                if (debugMode) Debug.Log($"[LocalizationManager] Loaded saved language: {currentLanguage}");
            }
            else
            {
                // Essayer de détecter la langue du système
                currentLanguage = DetectSystemLanguage();
                if (debugMode) Debug.Log($"[LocalizationManager] Using system/default language: {currentLanguage}");
            }
        }

        string DetectSystemLanguage()
        {
            // Essayer de détecter la langue du système
            SystemLanguage systemLang = Application.systemLanguage;

            switch (systemLang)
            {
                case SystemLanguage.French:
                    if (IsLanguageSupported("fr")) return "fr";
                    break;
                case SystemLanguage.English:
                    if (IsLanguageSupported("en")) return "en";
                    break;
                // Ajouter d'autres langues selon les besoins
            }

            // Fallback to default
            return defaultLanguage;
        }

        /// <summary>
        /// Change la langue actuelle
        /// </summary>
        public void SetLanguage(string languageCode)
        {
            if (!IsLanguageSupported(languageCode))
            {
                Debug.LogWarning($"[LocalizationManager] Language '{languageCode}' is not supported. Using default.");
                languageCode = defaultLanguage;
            }

            if (currentLanguage != languageCode)
            {
                currentLanguage = languageCode;

                // Sauvegarder la préférence
                PlayerPrefs.SetString("WiseTwin_Language", currentLanguage);
                PlayerPrefs.Save();

                if (debugMode) Debug.Log($"[LocalizationManager] Language changed to: {currentLanguage}");

                // Déclencher l'événement
                OnLanguageChanged?.Invoke(currentLanguage);
            }
        }

        /// <summary>
        /// Vérifie si une langue est supportée
        /// </summary>
        public bool IsLanguageSupported(string languageCode)
        {
            return supportedLanguages.Exists(l => l.code == languageCode);
        }

        /// <summary>
        /// Obtient les informations d'une langue
        /// </summary>
        public LanguageInfo GetLanguageInfo(string languageCode)
        {
            return supportedLanguages.Find(l => l.code == languageCode);
        }

        /// <summary>
        /// Extrait un texte localisé depuis un objet de métadonnées
        /// </summary>
        public string GetLocalizedText(object data, string fallback = "")
        {
            if (data == null) return fallback;

            // Si c'est déjà une string, la retourner
            if (data is string simpleText)
            {
                return simpleText;
            }

            // Si c'est un dictionnaire de langues
            if (data is Dictionary<string, object> localizedText)
            {
                // Essayer la langue actuelle
                if (localizedText.ContainsKey(currentLanguage))
                {
                    return localizedText[currentLanguage]?.ToString() ?? fallback;
                }

                // Fallback to default language
                if (localizedText.ContainsKey(defaultLanguage))
                {
                    return localizedText[defaultLanguage]?.ToString() ?? fallback;
                }

                // Prendre la première langue disponible
                foreach (var kvp in localizedText)
                {
                    if (kvp.Value != null)
                    {
                        return kvp.Value.ToString();
                    }
                }
            }

            return fallback;
        }

        /// <summary>
        /// Extrait une liste de textes localisés (pour les options par exemple)
        /// </summary>
        public List<string> GetLocalizedList(object data)
        {
            var result = new List<string>();

            if (data == null) return result;

            // Si c'est déjà une liste
            if (data is List<object> simpleList)
            {
                foreach (var item in simpleList)
                {
                    result.Add(item?.ToString() ?? "");
                }
                return result;
            }

            // Si c'est un dictionnaire de langues contenant des listes
            if (data is Dictionary<string, object> localizedLists)
            {
                // Essayer la langue actuelle
                if (localizedLists.ContainsKey(currentLanguage))
                {
                    var list = localizedLists[currentLanguage] as List<object>;
                    if (list != null)
                    {
                        foreach (var item in list)
                        {
                            result.Add(item?.ToString() ?? "");
                        }
                        return result;
                    }
                }

                // Fallback to default language
                if (localizedLists.ContainsKey(defaultLanguage))
                {
                    var list = localizedLists[defaultLanguage] as List<object>;
                    if (list != null)
                    {
                        foreach (var item in list)
                        {
                            result.Add(item?.ToString() ?? "");
                        }
                        return result;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Extrait une valeur depuis des données localisées
        /// </summary>
        public T GetLocalizedValue<T>(Dictionary<string, object> data, string key, T defaultValue = default(T))
        {
            if (data == null || !data.ContainsKey(key)) return defaultValue;

            var value = data[key];

            // Si la valeur est déjà du bon type
            if (value is T directValue)
            {
                return directValue;
            }

            // Si c'est un dictionnaire de langues
            if (value is Dictionary<string, object> localizedValue)
            {
                // Essayer la langue actuelle
                if (localizedValue.ContainsKey(currentLanguage))
                {
                    var langValue = localizedValue[currentLanguage];
                    if (langValue is T typedValue)
                    {
                        return typedValue;
                    }
                }

                // Fallback to default
                if (localizedValue.ContainsKey(defaultLanguage))
                {
                    var langValue = localizedValue[defaultLanguage];
                    if (langValue is T typedValue)
                    {
                        return typedValue;
                    }
                }
            }

            return defaultValue;
        }

        /// <summary>
        /// Pour les tests dans l'éditeur
        /// </summary>
        [ContextMenu("Test Language Switch")]
        public void TestLanguageSwitch()
        {
            string newLang = currentLanguage == "en" ? "fr" : "en";
            SetLanguage(newLang);
            Debug.Log($"[LocalizationManager] Switched language to: {currentLanguage}");
        }
    }

    /// <summary>
    /// Information sur une langue supportée
    /// </summary>
    [System.Serializable]
    public class LanguageInfo
    {
        public string code;        // "en", "fr", etc.
        public string displayName; // "English", "French"
        public string nativeName;  // "English", "Français"
        public Sprite icon;        // Optionnel: icône/drapeau
    }
}