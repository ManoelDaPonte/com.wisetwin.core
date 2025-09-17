using UnityEngine;
using WiseTwin.UI;

/// <summary>
/// Test script to quickly verify UI is working
/// </summary>
public class WiseTwinUITest : MonoBehaviour
{
    [Header("Test Settings")]
    [SerializeField] private bool autoStartTest = true;
    [SerializeField] private float delayBetweenTests = 2f;
    
    void Start()
    {
        if (autoStartTest)
        {
            Debug.Log("[WiseTwinUITest] Starting UI test in 1 second...");
            Invoke(nameof(RunTest), 1f);
        }
    }
    
    void RunTest()
    {
        Debug.Log("[WiseTwinUITest] Running UI test...");
        
        // Check if UI Manager exists
        if (WiseTwinUIManager.Instance == null)
        {
            Debug.LogError("[WiseTwinUITest] UI Manager not found! Please add WiseTwinUIManager to the scene.");
            return;
        }
        
        Debug.Log("[WiseTwinUITest] UI Manager found! Starting tests...");
        
        // Test 1: Set title
        WiseTwinUIManager.Instance.SetTrainingTitle("Test Training - Autoclave");
        
        // Test 2: Start training
        WiseTwinUIManager.Instance.StartTraining();
        
        // Test 3: Show notifications
        Invoke(nameof(TestNotifications), delayBetweenTests);
        
        // Test 4: Show question
        Invoke(nameof(TestQuestion), delayBetweenTests * 2);
        
        // Test 5: Update progress
        Invoke(nameof(TestProgress), delayBetweenTests * 3);
    }
    
    void TestNotifications()
    {
        Debug.Log("[WiseTwinUITest] Testing notifications...");
        
        WiseTwinUIManager.Instance.ShowNotification("Info: System ready", NotificationType.Info);
        WiseTwinUIManager.Instance.ShowNotification("Success: Connection établie!", NotificationType.Success);
        WiseTwinUIManager.Instance.ShowNotification("Warning: Check settings", NotificationType.Warning);
        WiseTwinUIManager.Instance.ShowNotification("Error: Test error", NotificationType.Error);
    }
    
    void TestQuestion()
    {
        Debug.Log("[WiseTwinUITest] Testing question modal...");
        
        string question = "Qui est autorisé à utiliser l'autoclave?";
        string[] options = {
            "Toute personne formée en production",
            "Toute personne habilitée à l'utilisation d'appareils sous pression",
            "Le chef d'équipe uniquement",
            "Un opérateur expérimenté, même sans habilitation"
        };
        
        WiseTwinUIManager.Instance.ShowQuestion(question, options, QuestionType.MultipleChoice);
        
        // Subscribe to events
        WiseTwinUIManager.Instance.OnAnswerSelected += OnAnswerSelected;
        WiseTwinUIManager.Instance.OnQuestionSubmitted += OnQuestionSubmitted;
    }
    
    void TestProgress()
    {
        Debug.Log("[WiseTwinUITest] Testing progress updates...");
        StartCoroutine(AnimateProgress());
    }
    
    System.Collections.IEnumerator AnimateProgress()
    {
        for (int i = 0; i <= 10; i++)
        {
            WiseTwinUIManager.Instance.UpdateProgress(i, 10);
            yield return new WaitForSeconds(0.5f);
        }
        
        // Complete training
        WiseTwinUIManager.Instance.CompleteTraining();
    }
    
    void OnAnswerSelected(int index)
    {
        Debug.Log($"[WiseTwinUITest] Answer selected: Option {index + 1}");
    }
    
    void OnQuestionSubmitted()
    {
        Debug.Log("[WiseTwinUITest] Question submitted!");
        
        // Show feedback
        WiseTwinUIManager.Instance.ShowNotification("Réponse correcte! ✅", NotificationType.Success, 3f);
        
        // Update progress
        WiseTwinUIManager.Instance.UpdateProgress(1, 5);
    }
    
    void OnDestroy()
    {
        if (WiseTwinUIManager.Instance != null)
        {
            WiseTwinUIManager.Instance.OnAnswerSelected -= OnAnswerSelected;
            WiseTwinUIManager.Instance.OnQuestionSubmitted -= OnQuestionSubmitted;
        }
    }
    
    // Manual test methods for context menu
    [ContextMenu("Manual Test - Show Question")]
    void ManualTestQuestion()
    {
        TestQuestion();
    }
    
    [ContextMenu("Manual Test - Show Notifications")]
    void ManualTestNotifications()
    {
        TestNotifications();
    }
    
    [ContextMenu("Manual Test - Complete Training")]
    void ManualTestComplete()
    {
        WiseTwinUIManager.Instance.CompleteTraining();
    }
}