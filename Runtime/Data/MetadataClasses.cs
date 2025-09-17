using System.Collections.Generic;

// Classes partagées entre Editor et Runtime
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
    public Dictionary<string, Dictionary<string, object>> unity; // SIMPLIFIÉ : directement les objets
}

// Classe pour la réponse API
[System.Serializable]
public class ApiResponse
{
    public bool success;
    public FormationMetadataComplete data;
    public string error;
}