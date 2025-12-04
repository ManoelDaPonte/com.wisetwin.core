using System.Collections.Generic;

namespace WiseTwin
{
    /// <summary>
    /// Runtime data structure for video triggers loaded from metadata
    /// </summary>
    [System.Serializable]
    public class VideoTriggerData
    {
        public string targetObjectName;
        public string videoUrlEN;
        public string videoUrlFR;

        /// <summary>
        /// Get the video URL based on current language with fallback
        /// </summary>
        public string GetVideoUrl()
        {
            string currentLang = LocalizationManager.Instance?.CurrentLanguage ?? "en";

            if (currentLang == "fr")
            {
                // Try French first, fallback to English
                return !string.IsNullOrEmpty(videoUrlFR) ? videoUrlFR : videoUrlEN;
            }
            else
            {
                // Try English first, fallback to French
                return !string.IsNullOrEmpty(videoUrlEN) ? videoUrlEN : videoUrlFR;
            }
        }

        /// <summary>
        /// Parse video trigger from JSON dictionary
        /// </summary>
        public static VideoTriggerData FromDictionary(Dictionary<string, object> dict)
        {
            var data = new VideoTriggerData();

            if (dict.TryGetValue("targetObjectName", out var name))
            {
                data.targetObjectName = name?.ToString() ?? "";
            }

            if (dict.TryGetValue("videoUrl", out var urlObj))
            {
                if (urlObj is Newtonsoft.Json.Linq.JObject jObj)
                {
                    var urlDict = jObj.ToObject<Dictionary<string, string>>();
                    if (urlDict != null)
                    {
                        urlDict.TryGetValue("en", out data.videoUrlEN);
                        urlDict.TryGetValue("fr", out data.videoUrlFR);
                    }
                }
                else if (urlObj is Dictionary<string, object> urlDictObj)
                {
                    data.videoUrlEN = urlDictObj.TryGetValue("en", out var en) ? en?.ToString() : "";
                    data.videoUrlFR = urlDictObj.TryGetValue("fr", out var fr) ? fr?.ToString() : "";
                }
            }

            return data;
        }
    }
}
