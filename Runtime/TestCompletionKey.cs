using UnityEngine;

namespace WiseTwin
{
    public class TestCompletionKey : MonoBehaviour
{
    [Header("Notifier Reference")]
    public TrainingCompletionNotifier completionNotifier;
    
    [Header("Settings")]
    public bool enableKeyTest = true;
    public KeyCode testKey = KeyCode.Y;
    
    void Start()
    {
        // Trouver automatiquement le notifier s'il n'est pas assigné
        if (completionNotifier == null)
        {
            completionNotifier = FindFirstObjectByType<TrainingCompletionNotifier>();
            
            if (completionNotifier == null)
            {
                Debug.LogWarning("⚠️ TrainingCompletionNotifier non trouvé ! Assignez-le manuellement.");
            }
            else
            {
                Debug.Log("✅ TrainingCompletionNotifier trouvé automatiquement");
            }
        }
    }
    
    void Update()
    {
        if (!enableKeyTest) return;
        
        if (Input.GetKeyDown(testKey))
        {
            TestNotification();
        }
    }
    
    void TestNotification()
    {
        Debug.Log($"🎹 Touche {testKey} pressée - Test de notification de fin de formation");
        
        if (completionNotifier != null)
        {
            completionNotifier.FormationCompleted();
        }
        else
        {
            Debug.LogError("❌ TrainingCompletionNotifier non assigné !");
        }
    }
    
    void OnGUI()
    {
        if (!enableKeyTest) return;
        
        GUILayout.BeginArea(new Rect(10, 750, 300, 80));
        GUILayout.Label($"🎹 Appuyez sur '{testKey}' pour tester la notification");
        
        if (completionNotifier != null)
        {
            string projectName = Application.productName;
            if (string.IsNullOrEmpty(projectName))
                projectName = "Formation Unity";
            GUILayout.Label($"✅ Notifier: {projectName}");
        }
        else
        {
            GUILayout.Label("❌ Notifier: NON ASSIGNÉ");
        }
        
        GUILayout.EndArea();
    }
}}