using System.Collections.Generic;
using Newtonsoft.Json;

// Classe pour les textes multilingues
[System.Serializable]
public class LocalizedString
{
    public string en;
    public string fr;

    // Constructeur par défaut
    public LocalizedString() { }

    // Constructeur avec valeurs
    public LocalizedString(string english, string french)
    {
        en = english;
        fr = french;
    }

    // Méthode helper pour obtenir le texte dans une langue
    public string Get(string language)
    {
        return language == "fr" ? fr : en;
    }

    // Conversion implicite depuis string (pour compatibilité)
    public static implicit operator LocalizedString(string value)
    {
        return new LocalizedString(value, value);
    }
}

// Classes partagées entre Editor et Runtime
[System.Serializable]
public class FormationMetadataComplete
{
    public string id;
    public LocalizedString title;
    public LocalizedString description;
    public string version;
    public string duration;
    public string difficulty;
    public List<string> tags;
    public string imageUrl;
    public List<object> modules;
    public string createdAt;
    public string updatedAt;
    public Dictionary<string, Dictionary<string, object>> unity; // SIMPLIFIÉ : directement les objets

    // New scenario-based system fields
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public List<object> scenarios; // Scenario configurations

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public Dictionary<string, object> settings; // Training settings (evaluationMode, etc.)

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public LocalizedString disclaimer; // Custom disclaimer text
}

// Classe pour la réponse API
[System.Serializable]
public class ApiResponse
{
    public bool success;
    public FormationMetadataComplete data;
    public string error;
}