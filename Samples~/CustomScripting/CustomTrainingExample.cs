using UnityEngine;
using UnityEngine.InputSystem;
using WiseTwin;

namespace WiseTwin.Samples
{
    /// <summary>
    /// Sample script demonstrating every method and event of the WiseTwinAPI public façade.
    ///
    /// To use this sample:
    ///  1. Drop this MonoBehaviour on any GameObject in your training scene.
    ///  2. Press Play.
    ///  3. Watch the Console — every API call and event will be logged.
    ///
    /// Then read the inline comments to learn how to wire each call into your own scripts.
    /// See API.md at the package root for full documentation and recipes.
    /// </summary>
    public class CustomTrainingExample : MonoBehaviour
    {
        [Header("Demo controls (use the inspector buttons or these keys)")]
        [SerializeField] Key validateStepKey = Key.V;
        [SerializeField] Key skipScenarioKey = Key.S;
        [SerializeField] Key logMistakeKey   = Key.M;
        [SerializeField] Key logSuccessKey   = Key.B;
        [SerializeField] Key completeKey     = Key.C;

        // ─────────────────────────────────────────────────────────────
        //  Subscribe to events on enable, unsubscribe on disable.
        //  This is the standard Unity pattern for static event subscription.
        // ─────────────────────────────────────────────────────────────

        void OnEnable()
        {
            WiseTwinAPI.OnStepValidated     += HandleStepValidated;
            WiseTwinAPI.OnScoreChanged      += HandleScoreChanged;
            WiseTwinAPI.OnScenarioStarted   += HandleScenarioStarted;
            WiseTwinAPI.OnTrainingCompleted += HandleTrainingCompleted;
        }

        void OnDisable()
        {
            WiseTwinAPI.OnStepValidated     -= HandleStepValidated;
            WiseTwinAPI.OnScoreChanged      -= HandleScoreChanged;
            WiseTwinAPI.OnScenarioStarted   -= HandleScenarioStarted;
            WiseTwinAPI.OnTrainingCompleted -= HandleTrainingCompleted;
        }

        // ─────────────────────────────────────────────────────────────
        //  Event handlers — react to what happens inside the package.
        // ─────────────────────────────────────────────────────────────

        void HandleStepValidated(int stepIndex, bool success)
        {
            Debug.Log($"[Sample] Step {stepIndex + 1} validated (success={success})");
        }

        void HandleScoreChanged(float newScore)
        {
            Debug.Log($"[Sample] Score is now {newScore:F1}%");
        }

        void HandleScenarioStarted(int index, ScenarioData scenario)
        {
            Debug.Log($"[Sample] Scenario started: {scenario.id} (type={scenario.type}, index={index})");
        }

        void HandleTrainingCompleted()
        {
            Debug.Log($"[Sample] Training completed! Final score: {WiseTwinAPI.GetCurrentScore():F1}%");
        }

        // ─────────────────────────────────────────────────────────────
        //  Demo controls — call API methods on key press.
        // ─────────────────────────────────────────────────────────────

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard[validateStepKey].wasPressedThisFrame)
            {
                bool ok = WiseTwinAPI.ValidateCurrentStep(success: true);
                Debug.Log($"[Sample] ValidateCurrentStep returned {ok}");
            }

            if (keyboard[skipScenarioKey].wasPressedThisFrame)
            {
                WiseTwinAPI.SkipCurrentScenario();
            }

            if (keyboard[logMistakeKey].wasPressedThisFrame)
            {
                WiseTwinAPI.LogCustomEvent(
                    eventId: "demo_mistake",
                    success: false,
                    weight: 2.0f,
                    description: "Sample script triggered a mistake event"
                );
            }

            if (keyboard[logSuccessKey].wasPressedThisFrame)
            {
                WiseTwinAPI.LogCustomEvent(
                    eventId: "demo_bonus",
                    success: true,
                    weight: 1.0f,
                    description: "Sample script triggered a bonus event"
                );
            }

            if (keyboard[completeKey].wasPressedThisFrame)
            {
                WiseTwinAPI.CompleteTraining("Demo Training");
            }
        }

        // ─────────────────────────────────────────────────────────────
        //  Inspector helpers — query state from a context menu.
        // ─────────────────────────────────────────────────────────────

        [ContextMenu("Print current state")]
        void PrintCurrentState()
        {
            Debug.Log($"[Sample] IsTrainingActive = {WiseTwinAPI.IsTrainingActive()}");
            Debug.Log($"[Sample] Current score = {WiseTwinAPI.GetCurrentScore():F1}%");

            var info = WiseTwinAPI.GetCurrentScenarioInfo();
            if (info.HasValue)
            {
                Debug.Log($"[Sample] Current scenario: {info.Value.Id} ({info.Value.Type}) — {info.Value.Index + 1}/{info.Value.Total}");
            }
            else
            {
                Debug.Log("[Sample] No scenario currently active");
            }
        }
    }
}
