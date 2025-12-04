using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections;

namespace WiseTwin
{
    /// <summary>
    /// Displays video in a fullscreen-like UI (90% width, 90% height)
    /// Uses Unity VideoPlayer with RenderTexture displayed on a RawImage
    /// Click anywhere to close the video
    /// </summary>
    public class VideoDisplayer : MonoBehaviour
    {
        public static VideoDisplayer Instance { get; private set; }

        /// <summary>
        /// Returns true if a video is currently playing - use this to block other inputs
        /// </summary>
        public static bool IsPlaying => Instance != null && Instance.isShowing;

        // UI Components
        private Canvas canvas;
        private CanvasScaler canvasScaler;
        private Image backgroundImage;
        private RawImage videoImage;
        private VideoPlayer videoPlayer;
        private RenderTexture renderTexture;

        // State
        private bool isShowing = false;
        private FirstPersonCharacter playerController;

        // Debug
        private bool EnableDebugLogs => WiseTwinManager.Instance != null && WiseTwinManager.Instance.EnableDebugLogs;

        void DebugLog(string message)
        {
            if (EnableDebugLogs)
                Debug.Log($"[VideoDisplayer] {message}");
        }

        void DebugLogError(string message)
        {
            Debug.LogError($"[VideoDisplayer] {message}");
        }

        void Awake()
        {
            // Check if instance is null OR if it points to a destroyed object (can happen in editor between play sessions)
            if (Instance == null || Instance.gameObject == null)
            {
                Instance = this;
                SetupUI();
                DebugLog("Instance created and ready");
            }
            else if (Instance != this)
            {
                DebugLog("Duplicate instance destroyed");
                Destroy(gameObject);
            }
        }

        void SetupUI()
        {
            // Create Canvas
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000; // High sorting order to be on top

            canvasScaler = gameObject.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920, 1080);
            canvasScaler.matchWidthOrHeight = 0.5f;

            gameObject.AddComponent<GraphicRaycaster>();

            // Create background (dark overlay)
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(transform, false);
            backgroundImage = bgObj.AddComponent<Image>();
            backgroundImage.color = new Color(0, 0, 0, 0.9f);

            RectTransform bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            // Add button for click to close
            Button bgButton = bgObj.AddComponent<Button>();
            bgButton.transition = Selectable.Transition.None;
            bgButton.onClick.AddListener(HideVideo);

            // Create video container (90% x 90%)
            GameObject videoContainer = new GameObject("VideoContainer");
            videoContainer.transform.SetParent(transform, false);

            RectTransform containerRect = videoContainer.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.05f, 0.05f);
            containerRect.anchorMax = new Vector2(0.95f, 0.95f);
            containerRect.offsetMin = Vector2.zero;
            containerRect.offsetMax = Vector2.zero;

            // Create RawImage for video
            videoImage = videoContainer.AddComponent<RawImage>();
            videoImage.color = Color.white; // Must be white to display texture correctly

            // Add button for click to close on video too
            Button videoButton = videoContainer.AddComponent<Button>();
            videoButton.transition = Selectable.Transition.None;
            videoButton.onClick.AddListener(HideVideo);

            // Create VideoPlayer
            videoPlayer = gameObject.AddComponent<VideoPlayer>();
            videoPlayer.playOnAwake = false;
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.isLooping = false;
            videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;

            // Subscribe to video events
            videoPlayer.prepareCompleted += OnVideoPrepared;
            videoPlayer.loopPointReached += OnVideoEnded;
            videoPlayer.errorReceived += OnVideoError;

            // Hide by default
            canvas.enabled = false;
        }

        /// <summary>
        /// Show video from URL
        /// </summary>
        public void ShowVideo(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                DebugLog("Cannot play empty URL");
                return;
            }

            DebugLog($"Loading video: {url}");

            // Show canvas immediately but delay input blocking
            // This allows procedure steps to validate on the same frame before we block inputs
            canvas.enabled = true;

            // Create RenderTexture if needed or recreate with correct size
            CreateRenderTexture();

            // Set video source
            videoPlayer.source = VideoSource.Url;
            videoPlayer.url = url;

            // Prepare and play
            videoPlayer.Prepare();

            // Delay input blocking by one frame to allow procedure validation to complete
            StartCoroutine(DelayedInputBlocking());
        }

        /// <summary>
        /// Delays setting isShowing and blocking player controls by one frame.
        /// This allows other click handlers (like ProcedureStepClickHandler) to process
        /// their click events before the video blocks all inputs.
        /// </summary>
        IEnumerator DelayedInputBlocking()
        {
            // Wait one frame - this allows procedure validation to happen first
            yield return null;

            isShowing = true;

            // Block player controls during video (like a cinematic)
            if (playerController == null)
                playerController = FindFirstObjectByType<FirstPersonCharacter>();

            if (playerController != null)
            {
                playerController.SetControlsEnabled(false);
                DebugLog("Player controls disabled (after 1 frame delay)");
            }
        }

        void CreateRenderTexture()
        {
            // Create render texture matching screen aspect ratio
            int width = 1920;
            int height = 1080;

            if (renderTexture != null)
            {
                renderTexture.Release();
                Destroy(renderTexture);
            }

            renderTexture = new RenderTexture(width, height, 0);
            renderTexture.Create();

            videoPlayer.targetTexture = renderTexture;
            videoImage.texture = renderTexture;
        }

        void OnVideoPrepared(VideoPlayer source)
        {
            DebugLog($"Video prepared, playing... (Duration: {source.length:F1}s, Size: {source.width}x{source.height})");

            // Update render texture to match video dimensions
            if (source.width > 0 && source.height > 0)
            {
                if (renderTexture != null)
                {
                    renderTexture.Release();
                    Destroy(renderTexture);
                }

                renderTexture = new RenderTexture((int)source.width, (int)source.height, 0);
                renderTexture.Create();

                videoPlayer.targetTexture = renderTexture;
                videoImage.texture = renderTexture;
            }

            source.Play();
        }

        void OnVideoEnded(VideoPlayer source)
        {
            DebugLog("Video ended, closing automatically");
            HideVideo();
        }

        void OnVideoError(VideoPlayer source, string message)
        {
            DebugLogError($"Video error: {message}");
            DebugLogError($"Failed URL: {source.url}");
        }

        /// <summary>
        /// Hide video and stop playback
        /// </summary>
        public void HideVideo()
        {
            if (!isShowing) return;

            DebugLog("Hiding video");

            isShowing = false;
            canvas.enabled = false;

            videoPlayer.Stop();

            // Re-enable player controls
            if (playerController != null)
            {
                playerController.SetControlsEnabled(true);
                DebugLog("Player controls re-enabled");
            }

            if (renderTexture != null)
            {
                renderTexture.Release();
                Destroy(renderTexture);
                renderTexture = null;
            }

            videoImage.texture = null;
        }

        void Update()
        {
            // Allow Escape key to close (using new Input System)
            if (isShowing && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                HideVideo();
            }
        }

        void OnDestroy()
        {
            if (renderTexture != null)
            {
                renderTexture.Release();
                Destroy(renderTexture);
            }

            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
