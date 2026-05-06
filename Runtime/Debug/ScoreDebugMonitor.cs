using System.Collections.Generic;
using UnityEngine;

namespace WiseTwin.Debugging
{
    /// <summary>
    /// Live debugging tool for the WiseTwin score system. Displays the current cumulative score
    /// in a corner overlay and logs every score-affecting operation (interactions, procedure
    /// steps, custom events) to the Unity console.
    ///
    /// Usage: drop on any GameObject in the scene (typically as a child of WiseTwinSystem).
    /// Toggle the component checkbox in the inspector to enable/disable the overlay live.
    /// You can also use the menu shortcut: WiseTwin > Debug > Add Score Monitor to Scene.
    /// </summary>
    public class ScoreDebugMonitor : MonoBehaviour
    {
        public enum OverlayCorner { TopLeft, TopRight, BottomLeft, BottomRight }

        [Header("Display")]
        [Tooltip("Show the on-screen score overlay")]
        [SerializeField] bool showOverlay = true;
        [Tooltip("Where to anchor the overlay on screen")]
        [SerializeField] OverlayCorner overlayCorner = OverlayCorner.TopRight;
        [Tooltip("How many recent operations to show in the overlay")]
        [SerializeField, Range(3, 20)] int maxLogEntries = 8;

        [Header("Logging")]
        [Tooltip("Mirror each operation to the Unity console")]
        [SerializeField] bool logToConsole = true;
        [SerializeField] string logPrefix = "[ScoreDebug]";

        float currentScore = 100f;
        readonly List<string> recentEntries = new List<string>();

        void OnEnable()
        {
            currentScore = WiseTwinAPI.GetCurrentScore();
            recentEntries.Clear();

            WiseTwinAPI.OnScoreChanged += HandleScoreChanged;
            WiseTwinAPI.OnStepValidated += HandleStepValidated;
            WiseTwinAPI.OnScenarioStarted += HandleScenarioStarted;
            WiseTwinAPI.OnTrainingCompleted += HandleTrainingCompleted;
            WiseTwinAPI.OnCustomEventLogged += HandleCustomEventLogged;

            AddEntry($"Monitor enabled — initial score {currentScore:F1}%");
        }

        void OnDisable()
        {
            WiseTwinAPI.OnScoreChanged -= HandleScoreChanged;
            WiseTwinAPI.OnStepValidated -= HandleStepValidated;
            WiseTwinAPI.OnScenarioStarted -= HandleScenarioStarted;
            WiseTwinAPI.OnTrainingCompleted -= HandleTrainingCompleted;
            WiseTwinAPI.OnCustomEventLogged -= HandleCustomEventLogged;
        }

        void HandleScoreChanged(float newScore)
        {
            float delta = newScore - currentScore;
            string sign = delta >= 0 ? "+" : "";
            // ASCII-only — IMGUI fonts in WebGL builds drop arrow / delta glyphs
            AddEntry($"Score {currentScore:F1}% -> {newScore:F1}% (d {sign}{delta:F1})");
            currentScore = newScore;
        }

        void HandleStepValidated(int stepIndex, bool success)
        {
            AddEntry($"Step {stepIndex + 1}: {(success ? "OK" : "FAIL")}");
        }

        void HandleScenarioStarted(int index, ScenarioData scenario)
        {
            AddEntry($">> Scenario {index + 1} ({scenario.type}): {scenario.id}");
        }

        void HandleTrainingCompleted()
        {
            AddEntry($"## Training complete - final score {currentScore:F1}%");
        }

        void HandleCustomEventLogged(string eventId, bool success, float weight, string description)
        {
            string status = success ? "OK" : "FAIL";
            string desc = string.IsNullOrEmpty(description) ? "" : $" — {description}";
            AddEntry($"Custom '{eventId}': {status} (weight {weight:F1}){desc}");
        }

        void AddEntry(string text)
        {
            string entry = $"{System.DateTime.Now:HH:mm:ss} {text}";
            recentEntries.Add(entry);
            if (recentEntries.Count > maxLogEntries)
            {
                recentEntries.RemoveAt(0);
            }

            if (logToConsole)
            {
                Debug.Log($"{logPrefix} {text}");
            }
        }

        // ─────────────────────────────────────────────────────────────
        //  IMGUI overlay — small box in a corner with score + recent ops
        // ─────────────────────────────────────────────────────────────

        GUIStyle titleStyle;
        GUIStyle scoreStyle;
        GUIStyle entryStyle;
        Texture2D bgTexture;

        void EnsureStyles()
        {
            if (titleStyle != null) return;

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 13,
                normal = { textColor = new Color(0.7f, 0.85f, 1f) }
            };
            scoreStyle = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 22
            };
            entryStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                wordWrap = false,
                normal = { textColor = new Color(0.85f, 0.85f, 0.85f) }
            };

            bgTexture = new Texture2D(1, 1);
            bgTexture.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.75f));
            bgTexture.Apply();
        }

        void OnGUI()
        {
            if (!showOverlay) return;
            EnsureStyles();

            const float width = 360f;
            float height = 70f + recentEntries.Count * 16f;
            float x = (overlayCorner == OverlayCorner.TopLeft || overlayCorner == OverlayCorner.BottomLeft)
                ? 10f
                : Screen.width - width - 10f;
            float y = (overlayCorner == OverlayCorner.TopLeft || overlayCorner == OverlayCorner.TopRight)
                ? 10f
                : Screen.height - height - 10f;

            var rect = new Rect(x, y, width, height);
            GUI.DrawTexture(rect, bgTexture, ScaleMode.StretchToFill);

            GUILayout.BeginArea(new Rect(x + 10f, y + 8f, width - 20f, height - 16f));

            GUILayout.Label("WiseTwin Score Debug", titleStyle);

            scoreStyle.normal.textColor = ColorForScore(currentScore);
            GUILayout.Label($"{currentScore:F1}%", scoreStyle);

            GUILayout.Space(4f);

            for (int i = recentEntries.Count - 1; i >= 0; i--)
            {
                GUILayout.Label(recentEntries[i], entryStyle);
            }

            GUILayout.EndArea();
        }

        static Color ColorForScore(float score)
        {
            if (score >= 75f) return new Color(0.4f, 1f, 0.5f);
            if (score >= 50f) return new Color(1f, 0.85f, 0.3f);
            return new Color(1f, 0.45f, 0.45f);
        }

        void OnDestroy()
        {
            if (bgTexture != null) Destroy(bgTexture);
        }
    }
}
