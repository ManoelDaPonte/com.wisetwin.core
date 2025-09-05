using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;

#if UNITY_EDITOR
[System.Serializable]
public class EnhancedMetadataManager : EditorWindow
{
    [Header("Project Settings")]
    public string projectTitle = "Formation de Test";
    public string projectDescription = "Description de la formation";
    public string projectVersion = "1.0.0";
    public string category = "Intermédiaire";
    public int durationMinutes = 30;
    public int difficultyIndex = 1;
    public string imageUrl = "";
    
    // Constantes pour les options de difficulté
    private readonly string[] difficultyOptions = { "Facile", "Intermédiaire", "Difficile", "Très Difficile" };
    
    [Header("Advanced Settings")]
    public List<string> tags = new List<string> { "formation", "interactive" };
    public List<string> objectives = new List<string> { "Comprendre les interactions", "Répondre aux questions" };
    public List<string> prerequisites = new List<string>();
    
    [Header("Export Settings")]
    public bool includeTimestamp = true;
    
    // UI State
    private Vector2 scrollPosition;
    private Vector2 unityContentScrollPosition;
    private bool showAdvancedSettings = false;
    private string unityContentJSON = "";
    private bool isUnityContentValid = true;
    
    // Tabs
    private int selectedTab = 0;
    private string[] tabNames = { "Configuration", "Objets Unity", "Export & Preview" };
    
    // Variables pour le chargement
    private bool hasLoadedExistingJSON = false;
    private string currentLoadedFile = "";
    private string projectId; // Auto-généré depuis le nom du projet Unity
    
    [MenuItem("WiseTwin/Enhanced Metadata Manager")]
    public static void ShowWindow()
    {
        EnhancedMetadataManager window = GetWindow<EnhancedMetadataManager>("WiseTwin Metadata Manager");
        window.minSize = new Vector2(600, 700);
        window.Show();
    }
    
    void OnEnable()
    {
        InitializeProjectId();
        LoadExistingJSONContent();
        InitializeUnityContent();
    }
    
    void InitializeProjectId()
    {
        // L'ID du projet est automatiquement le nom du projet Unity
        projectId = Application.productName;
        if (string.IsNullOrEmpty(projectId))
        {
            projectId = "unity-project";
        }
        Debug.Log($"[WiseTwin] Project ID auto-généré: {projectId}");
    }
    
    void OnGUI()
    {
        DrawHeader();
        DrawLoadedFileInfo();
        DrawTabs();
        
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        switch (selectedTab)
        {
            case 0:
                DrawConfigurationTab();
                break;
            case 1:
                DrawUnityObjectsTab();
                break;
            case 2:
                DrawExportTab();
                break;
        }
        
        EditorGUILayout.EndScrollView();
        
        DrawBottomButtons();
    }
    
    void DrawLoadedFileInfo()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField($"🎯 Projet: {projectId} (basé sur le nom Unity)", EditorStyles.boldLabel);
        
        if (hasLoadedExistingJSON)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"📄 Fichier chargé: {currentLoadedFile}", EditorStyles.helpBox);
            if (GUILayout.Button("🔄 Recharger", GUILayout.Width(100)))
            {
                LoadExistingJSONContent();
            }
            if (GUILayout.Button("📁 Changer", GUILayout.Width(100)))
            {
                LoadDifferentJSONFile();
            }
            EditorGUILayout.EndHorizontal();
        }
        else
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("ℹ️ Aucun fichier JSON trouvé. Créer un nouveau contenu.", EditorStyles.helpBox);
            if (GUILayout.Button("📁 Charger fichier existant", GUILayout.Width(200)))
            {
                LoadDifferentJSONFile();
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();
    }
    
    void LoadExistingJSONContent()
    {
        Debug.Log("[WiseTwin MetadataManager] Recherche du fichier JSON existant...");
        
        string targetFileName = $"{projectId}-metadata.json";
        
        // Chemins possibles où chercher le fichier JSON
        string[] possiblePaths = {
            Path.Combine(Application.streamingAssetsPath, targetFileName),
            Path.Combine(Application.streamingAssetsPath, "metadata.json"), // Fallback
        };
        
        string foundPath = null;
        foreach (string path in possiblePaths)
        {
            if (File.Exists(path))
            {
                foundPath = path;
                Debug.Log($"✅ Fichier JSON trouvé: {path}");
                break;
            }
        }
        
        if (foundPath != null)
        {
            try
            {
                string jsonContent = File.ReadAllText(foundPath);
                ParseExistingJSON(jsonContent);
                currentLoadedFile = Path.GetFileName(foundPath);
                hasLoadedExistingJSON = true;
                
                Debug.Log($"✅ Contenu JSON chargé depuis: {foundPath}");
                ShowNotification(new GUIContent($"Fichier chargé: {currentLoadedFile}"));
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Erreur lors du chargement du JSON: {e.Message}");
                hasLoadedExistingJSON = false;
            }
        }
        else
        {
            Debug.Log($"ℹ️ Aucun fichier JSON existant trouvé pour le projet '{projectId}'.");
            hasLoadedExistingJSON = false;
        }
    }
    
    void ParseExistingJSON(string jsonContent)
    {
        try
        {
            var metadata = JsonConvert.DeserializeObject<FormationMetadataComplete>(jsonContent);
            
            // Charger les données de base
            if (!string.IsNullOrEmpty(metadata.title)) projectTitle = metadata.title;
            if (!string.IsNullOrEmpty(metadata.description)) projectDescription = metadata.description;
            if (!string.IsNullOrEmpty(metadata.version)) projectVersion = metadata.version;
            if (!string.IsNullOrEmpty(metadata.category)) category = metadata.category;
            if (!string.IsNullOrEmpty(metadata.imageUrl)) imageUrl = metadata.imageUrl;
            
            // Parser la durée (extraire les chiffres)
            if (!string.IsNullOrEmpty(metadata.duration))
            {
                ParseDurationFromString(metadata.duration);
            }
            
            // Parser la difficulté (trouver l'index)
            if (!string.IsNullOrEmpty(metadata.difficulty))
            {
                ParseDifficultyFromString(metadata.difficulty);
            }
            
            // Charger les listes
            if (metadata.tags != null && metadata.tags.Count > 0)
                tags = new List<string>(metadata.tags);
            if (metadata.objectives != null && metadata.objectives.Count > 0)
                objectives = new List<string>(metadata.objectives);
            if (metadata.prerequisites != null && metadata.prerequisites.Count > 0)
                prerequisites = new List<string>(metadata.prerequisites);
            
            // 🎯 IMPORTANT : Extraire la section Unity (contenu des objets)
            if (metadata.unity != null)
            {
                // Convertir le contenu Unity directement en JSON formaté
                unityContentJSON = JsonConvert.SerializeObject(metadata.unity, Formatting.Indented);
                ValidateUnityContent();
                
                Debug.Log($"📦 Section Unity chargée avec {metadata.unity.Count} objets");
            }
            else
            {
                Debug.Log("⚠️ Aucune section Unity trouvée dans le JSON");
                InitializeUnityContent();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Erreur parsing JSON existant: {e.Message}");
            InitializeUnityContent();
        }
    }
    
    void ParseDurationFromString(string durationStr)
    {
        try
        {
            // Extraire les chiffres de la chaîne (ex: "30 minutes" -> 30)
            string numbersOnly = System.Text.RegularExpressions.Regex.Match(durationStr, @"\d+").Value;
            if (!string.IsNullOrEmpty(numbersOnly))
            {
                durationMinutes = int.Parse(numbersOnly);
            }
        }
        catch
        {
            durationMinutes = 30; // valeur par défaut
        }
    }
    
    void ParseDifficultyFromString(string difficultyStr)
    {
        for (int i = 0; i < difficultyOptions.Length; i++)
        {
            if (string.Equals(difficultyOptions[i], difficultyStr, System.StringComparison.OrdinalIgnoreCase))
            {
                difficultyIndex = i;
                return;
            }
        }
        difficultyIndex = 1; // Par défaut "Intermédiaire"
    }
    
    void LoadDifferentJSONFile()
    {
        string path = EditorUtility.OpenFilePanel(
            "Charger un fichier JSON de métadonnées", 
            Application.streamingAssetsPath, 
            "json"
        );
        
        if (!string.IsNullOrEmpty(path))
        {
            try
            {
                string jsonContent = File.ReadAllText(path);
                ParseExistingJSON(jsonContent);
                currentLoadedFile = Path.GetFileName(path);
                hasLoadedExistingJSON = true;
                
                ShowNotification(new GUIContent($"Fichier chargé: {currentLoadedFile}"));
                Debug.Log($"✅ Nouveau fichier JSON chargé: {path}");
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("Erreur", $"Impossible de charger le fichier:\n{e.Message}", "OK");
                Debug.LogError($"❌ Erreur chargement fichier: {e.Message}");
            }
        }
    }
    
    void DrawHeader()
    {
        EditorGUILayout.Space();
        EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 2), new Color(0.3f, 0.6f, 1f));
        EditorGUILayout.Space();
        
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("🎯", GUILayout.Width(30));
        EditorGUILayout.LabelField("WiseTwin Metadata Manager", EditorStyles.largeLabel);
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.LabelField("Génération et gestion des métadonnées de formation Unity", EditorStyles.helpBox);
        EditorGUILayout.Space();
    }
    
    void DrawTabs()
    {
        selectedTab = GUILayout.Toolbar(selectedTab, tabNames, GUILayout.Height(25));
        EditorGUILayout.Space();
    }
    
    void DrawConfigurationTab()
    {
        // Basic Settings
        EditorGUILayout.LabelField("📋 Configuration de Base", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
        
        // Project ID en lecture seule
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.TextField("ID du Projet (auto)", projectId);
        EditorGUI.EndDisabledGroup();
        
        projectTitle = EditorGUILayout.TextField("Titre", projectTitle);
        
        EditorGUILayout.LabelField("Description");
        projectDescription = EditorGUILayout.TextArea(projectDescription, GUILayout.Height(60));
        
        projectVersion = EditorGUILayout.TextField("Version", projectVersion);
        category = EditorGUILayout.TextField("Catégorie", category);
        
        // Durée en minutes (champ numérique)
        EditorGUILayout.BeginHorizontal();
        durationMinutes = EditorGUILayout.IntField("Durée (minutes)", durationMinutes);
        EditorGUILayout.LabelField($"→ {durationMinutes} minutes", EditorStyles.miniLabel, GUILayout.Width(100));
        EditorGUILayout.EndHorizontal();
        
        // Difficulté (dropdown)
        difficultyIndex = EditorGUILayout.Popup("Difficulté", difficultyIndex, difficultyOptions);
        
        imageUrl = EditorGUILayout.TextField("URL Image", imageUrl);
        
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();
        
        // Advanced Settings
        showAdvancedSettings = EditorGUILayout.Foldout(showAdvancedSettings, "⚙️ Paramètres Avancés", true);
        if (showAdvancedSettings)
        {
            EditorGUILayout.BeginVertical("box");
            
            DrawStringList("🏷️ Tags", tags);
            DrawStringList("🎯 Objectifs", objectives);
            DrawStringList("📚 Prérequis", prerequisites);
            
            EditorGUILayout.EndVertical();
        }
    }
    
    void DrawUnityObjectsTab()
    {
        EditorGUILayout.LabelField("🎮 Configuration Unity", EditorStyles.boldLabel);
        
        // Unity Content Editor
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("✏️ Éditeur de Contenu Unity (JSON)", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Définissez ici les objets et leurs contenus (questions, interactions, etc.)", EditorStyles.helpBox);
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("📋 Copier", GUILayout.Width(80)))
        {
            EditorGUIUtility.systemCopyBuffer = unityContentJSON;
            ShowNotification(new GUIContent("JSON copié !"));
        }
        if (GUILayout.Button("📥 Coller", GUILayout.Width(80)))
        {
            unityContentJSON = EditorGUIUtility.systemCopyBuffer;
            ValidateUnityContent();
        }
        if (GUILayout.Button("🧹 Vider", GUILayout.Width(80)))
        {
            if (EditorUtility.DisplayDialog("Confirmation", "Êtes-vous sûr de vouloir vider le contenu Unity ?", "Oui", "Annuler"))
            {
                unityContentJSON = "{}";
                ValidateUnityContent();
            }
        }
        if (GUILayout.Button("🎯 Exemple", GUILayout.Width(80)))
        {
            LoadExampleUnityContent();
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(5);
        
        unityContentScrollPosition = EditorGUILayout.BeginScrollView(unityContentScrollPosition, GUILayout.Height(300));
        
        GUI.color = isUnityContentValid ? Color.white : new Color(1f, 0.8f, 0.8f);
        string newContent = EditorGUILayout.TextArea(unityContentJSON, GUILayout.ExpandHeight(true));
        GUI.color = Color.white;
        
        if (newContent != unityContentJSON)
        {
            unityContentJSON = newContent;
            ValidateUnityContent();
        }
        
        EditorGUILayout.EndScrollView();
        
        if (!isUnityContentValid)
        {
            EditorGUILayout.HelpBox("❌ JSON invalide ! Vérifiez la syntaxe.", MessageType.Error);
        }
        else
        {
            EditorGUILayout.HelpBox("✅ JSON valide", MessageType.Info);
        }
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("💡 Conseils", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("• Utilisez des IDs d'objets comme clés principales", EditorStyles.helpBox);
        EditorGUILayout.LabelField("• Vous pouvez définir n'importe quelle structure de données", EditorStyles.helpBox);
        EditorGUILayout.LabelField("• Exemples: questions, dialogues, instructions, médias, etc.", EditorStyles.helpBox);
        
        EditorGUILayout.EndVertical();
    }
    
    void LoadExampleUnityContent()
    {
        var exampleContent = new Dictionary<string, Dictionary<string, object>>
        {
            ["cube_rouge"] = new Dictionary<string, object>
            {
                ["question_1"] = new Dictionary<string, object>
                {
                    ["text"] = "Quelle est la couleur de ce cube ?",
                    ["type"] = "multiple-choice",
                    ["options"] = new string[] { "Rouge", "Bleu", "Vert" },
                    ["correctAnswer"] = 0,
                    ["feedback"] = "Correct ! C'est bien un cube rouge.",
                    ["incorrectFeedback"] = "Non, regardez bien la couleur !"
                }
            },
            ["sphere_bleue"] = new Dictionary<string, object>
            {
                ["question_1"] = new Dictionary<string, object>
                {
                    ["text"] = "Cette forme est-elle une sphère ?",
                    ["type"] = "true-false",
                    ["correctAnswer"] = 1,
                    ["feedback"] = "Exact ! C'est bien une sphère.",
                    ["incorrectFeedback"] = "Non, observez bien la forme !"
                }
            }
        };
        
        unityContentJSON = JsonConvert.SerializeObject(exampleContent, Formatting.Indented);
        ValidateUnityContent();
        ShowNotification(new GUIContent("Exemple chargé !"));
    }
    
    void DrawExportTab()
    {
        EditorGUILayout.LabelField("💾 Export et Prévisualisation", EditorStyles.boldLabel);
        
        // Export Settings
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("⚙️ Paramètres d'Export", EditorStyles.boldLabel);
        
        includeTimestamp = EditorGUILayout.Toggle("Inclure timestamp", includeTimestamp);
        
        EditorGUILayout.LabelField($"📁 Fichier de sortie: {projectId}-metadata.json", EditorStyles.helpBox);
        EditorGUILayout.LabelField($"📍 Destination: StreamingAssets/", EditorStyles.helpBox);
        
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();
        
        // Preview Section
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("👁️ Prévisualisation JSON", EditorStyles.boldLabel);
        
        if (GUILayout.Button("🔍 Générer Prévisualisation"))
        {
            ShowJSONPreview();
        }
        
        EditorGUILayout.EndVertical();
    }
    
    void DrawStringList(string label, List<string> list)
    {
        EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
        
        for (int i = 0; i < list.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            list[i] = EditorGUILayout.TextField($"  [{i}]", list[i]);
            if (GUILayout.Button("❌", GUILayout.Width(25)))
            {
                list.RemoveAt(i);
                break;
            }
            EditorGUILayout.EndHorizontal();
        }
        
        if (GUILayout.Button($"➕ Ajouter {label}"))
        {
            list.Add("");
        }
        
        EditorGUILayout.Space();
    }
    
    void DrawBottomButtons()
    {
        EditorGUILayout.Space();
        EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1), Color.gray);
        EditorGUILayout.Space();
        
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("🔍 Prévisualiser JSON", GUILayout.Height(30)))
        {
            ShowJSONPreview();
        }
        
        if (GUILayout.Button("💾 Générer Metadata", GUILayout.Height(30)))
        {
            GenerateMetadata();
        }
        
        EditorGUILayout.EndHorizontal();
    }
    
    void ValidateUnityContent()
    {
        try
        {
            if (!string.IsNullOrEmpty(unityContentJSON))
            {
                JsonConvert.DeserializeObject(unityContentJSON);
                isUnityContentValid = true;
            }
        }
        catch
        {
            isUnityContentValid = false;
        }
    }
    
    void InitializeUnityContent()
    {
        if (string.IsNullOrEmpty(unityContentJSON))
        {
            unityContentJSON = JsonConvert.SerializeObject(new Dictionary<string, object>(), Formatting.Indented);
        }
        ValidateUnityContent();
    }
    
    void ShowJSONPreview()
    {
        var metadata = GenerateCompleteMetadata();
        string json = JsonConvert.SerializeObject(metadata, Formatting.Indented);
        
        MetadataPreviewWindow.ShowWindow(json);
    }
    
    FormationMetadataComplete GenerateCompleteMetadata()
    {
        var metadata = new FormationMetadataComplete
        {
            id = projectId,
            title = projectTitle,
            description = projectDescription,
            version = projectVersion,
            category = category,
            duration = $"{durationMinutes} minutes", // Formatage automatique
            difficulty = difficultyOptions[difficultyIndex], // Récupération depuis le dropdown
            tags = new List<string>(tags),
            imageUrl = imageUrl,
            modules = new List<object>(),
            objectives = new List<string>(objectives),
            prerequisites = new List<string>(prerequisites),
            createdAt = includeTimestamp ? System.DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ") : "",
            updatedAt = includeTimestamp ? System.DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ") : ""
        };
        
        // 🎯 STRUCTURE SIMPLIFIÉE : Unity contient directement les objets
        try
        {
            if (!string.IsNullOrEmpty(unityContentJSON) && isUnityContentValid)
            {
                // Parser le JSON directement vers la section unity
                metadata.unity = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, object>>>(unityContentJSON);
            }
            else
            {
                // Si pas de contenu Unity, créer une section vide
                metadata.unity = new Dictionary<string, Dictionary<string, object>>();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Erreur lors du parsing du contenu Unity: {e.Message}");
            metadata.unity = new Dictionary<string, Dictionary<string, object>>();
        }
        
        return metadata;
    }
    
    void GenerateMetadata()
    {
        // Créer le dossier StreamingAssets s'il n'existe pas
        string streamingAssetsPath = Application.streamingAssetsPath;
        if (!Directory.Exists(streamingAssetsPath))
        {
            Directory.CreateDirectory(streamingAssetsPath);
        }
        
        var metadata = GenerateCompleteMetadata();
        string json = JsonConvert.SerializeObject(metadata, Formatting.Indented);
        
        string fileName = $"{projectId}-metadata.json";
        string fullPath = Path.Combine(streamingAssetsPath, fileName);
        
        File.WriteAllText(fullPath, json);
        AssetDatabase.Refresh();
        
        Debug.Log($"[WiseTwin MetadataManager] Metadata généré vers : {fullPath}");
        EditorUtility.DisplayDialog("Génération Réussie", 
            $"Metadata généré avec succès !\n\n" +
            $"📁 Fichier : {fileName}\n" +
            $"📍 Emplacement : StreamingAssets/\n" +
            $"🎯 Le fichier sera automatiquement inclus dans le build Unity.", 
            "Parfait !");
            
        // Marquer comme chargé pour la prochaine ouverture
        currentLoadedFile = fileName;
        hasLoadedExistingJSON = true;
    }
}

// Fenêtre de prévisualisation
public class MetadataPreviewWindow : EditorWindow
{
    private Vector2 scrollPosition;
    private string jsonContent;
    
    public static void ShowWindow(string json)
    {
        MetadataPreviewWindow window = GetWindow<MetadataPreviewWindow>("Prévisualisation JSON");
        window.jsonContent = json;
        window.minSize = new Vector2(500, 400);
        window.Show();
    }
    
    void OnGUI()
    {
        EditorGUILayout.LabelField("📋 Prévisualisation du JSON Metadata", EditorStyles.largeLabel);
        EditorGUILayout.Space();
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("📋 Copier JSON"))
        {
            EditorGUIUtility.systemCopyBuffer = jsonContent;
            ShowNotification(new GUIContent("JSON copié dans le presse-papier !"));
        }
        if (GUILayout.Button("💾 Sauvegarder sous..."))
        {
            string path = EditorUtility.SaveFilePanel("Sauvegarder JSON", "", "metadata", "json");
            if (!string.IsNullOrEmpty(path))
            {
                File.WriteAllText(path, jsonContent);
                ShowNotification(new GUIContent($"Sauvegardé : {path}"));
            }
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space();
        
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        EditorGUILayout.TextArea(jsonContent, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();
    }
}

#endif