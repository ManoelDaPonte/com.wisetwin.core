using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace WiseTwin
{
    /// <summary>
    /// Backward-compat reader for metadata values. The package is mono-language
    /// (flat strings), but older JSON files may still contain {"en": "...", "fr": "..."}
    /// objects. This helper accepts both formats and flattens legacy values.
    /// Prefers "en" over "fr" to match the editor import behavior.
    /// </summary>
    public static class LocalizedValueReader
    {
        /// <summary>
        /// Read a value from a Dictionary and return it as a flat string.
        /// Accepts: plain string, JObject {en, fr}, Dictionary {en, fr}, JValue.
        /// </summary>
        public static string ReadString(Dictionary<string, object> data, string key)
        {
            if (data == null || !data.ContainsKey(key)) return "";
            return Flatten(data[key]);
        }

        /// <summary>
        /// Flatten any object into a string, handling legacy multi-language formats.
        /// </summary>
        public static string Flatten(object value)
        {
            if (value == null) return "";
            if (value is string s) return s;

            if (value is JObject jObj)
            {
                var en = jObj["en"]?.ToString();
                if (!string.IsNullOrEmpty(en)) return en;
                var fr = jObj["fr"]?.ToString();
                if (!string.IsNullOrEmpty(fr)) return fr;
                return "";
            }

            if (value is JValue jv)
            {
                return jv.Value?.ToString() ?? "";
            }

            if (value is Dictionary<string, object> dict)
            {
                if (dict.TryGetValue("en", out var ven) && ven != null)
                {
                    var enStr = ven.ToString();
                    if (!string.IsNullOrEmpty(enStr)) return enStr;
                }
                if (dict.TryGetValue("fr", out var vfr) && vfr != null)
                {
                    return vfr.ToString() ?? "";
                }
                return "";
            }

            return value.ToString() ?? "";
        }

        /// <summary>
        /// Read a value that may be a flat array or a legacy {en: [...], fr: [...]} object.
        /// </summary>
        public static List<string> ReadStringList(Dictionary<string, object> data, string key)
        {
            var result = new List<string>();
            if (data == null || !data.ContainsKey(key)) return result;

            var value = data[key];
            if (value == null) return result;

            // Flat array
            if (value is JArray jArray)
            {
                foreach (var item in jArray) result.Add(item?.ToString() ?? "");
                return result;
            }
            if (value is List<object> list)
            {
                foreach (var item in list) result.Add(item?.ToString() ?? "");
                return result;
            }

            // Legacy multi-language object
            if (value is JObject jObj)
            {
                var en = jObj["en"] as JArray;
                if (en != null && en.Count > 0)
                {
                    foreach (var item in en) result.Add(item?.ToString() ?? "");
                    return result;
                }
                var fr = jObj["fr"] as JArray;
                if (fr != null)
                {
                    foreach (var item in fr) result.Add(item?.ToString() ?? "");
                    return result;
                }
            }

            if (value is Dictionary<string, object> dict)
            {
                if (dict.TryGetValue("en", out var ven) && ven is JArray jen && jen.Count > 0)
                {
                    foreach (var item in jen) result.Add(item?.ToString() ?? "");
                    return result;
                }
                if (dict.TryGetValue("fr", out var vfr) && vfr is JArray jfr)
                {
                    foreach (var item in jfr) result.Add(item?.ToString() ?? "");
                    return result;
                }
            }

            return result;
        }
    }
}
