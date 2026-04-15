using System.Collections.Generic;

namespace WiseTwin
{
    /// <summary>
    /// Runtime data structure for video triggers loaded from metadata.
    /// Mono-language: videoUrl is a single flat string.
    /// </summary>
    [System.Serializable]
    public class VideoTriggerData
    {
        public string targetObjectName;
        public string videoUrl;

        public static VideoTriggerData FromDictionary(Dictionary<string, object> dict)
        {
            var data = new VideoTriggerData();

            if (dict.TryGetValue("targetObjectName", out var name))
            {
                data.targetObjectName = name?.ToString() ?? "";
            }

            // Flat string, with legacy {en, fr} backward compat
            data.videoUrl = LocalizedValueReader.ReadString(dict, "videoUrl");

            return data;
        }
    }
}
