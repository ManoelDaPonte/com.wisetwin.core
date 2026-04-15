using System.Collections.Generic;
using Newtonsoft.Json;

// Classes partagées entre Editor et Runtime.
// Mono-language: all text fields are flat strings.
[System.Serializable]
public class FormationMetadataComplete
{
    public string id;
    public string title;
    public string description;
    public string version;
    public string duration;
    public string difficulty;
    public List<string> tags;
    public string imageUrl;
    public List<object> modules;
    public string createdAt;
    public string updatedAt;
    public Dictionary<string, Dictionary<string, object>> unity;

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public List<object> scenarios;

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public Dictionary<string, object> settings;

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string disclaimer;

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public List<object> videoTriggers;
}

[System.Serializable]
public class ApiResponse
{
    public bool success;
    public FormationMetadataComplete data;
    public string error;
}
