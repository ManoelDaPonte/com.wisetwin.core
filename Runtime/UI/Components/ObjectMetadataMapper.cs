using UnityEngine;

/// <summary>
/// Helper script pour mapper les objets 3D aux IDs des metadata
/// Attachez ce script aux objets interactifs pour qu'ils correspondent aux métadonnées
/// </summary>
public class ObjectMetadataMapper : MonoBehaviour
{
    [Header("Metadata Mapping")]
    [SerializeField] private string metadataId = "";
    [SerializeField] private bool autoDetectId = true;
    
    [Header("Visual Feedback")]
    [SerializeField] private bool showNameLabel = true;
    [SerializeField] private Color labelColor = Color.white;
    
    public string MetadataId
    {
        get => string.IsNullOrEmpty(metadataId) ? GenerateIdFromName() : metadataId;
        set => metadataId = value;
    }
    
    void Start()
    {
        if (autoDetectId && string.IsNullOrEmpty(metadataId))
        {
            metadataId = GenerateIdFromName();
            Debug.Log($"[ObjectMetadataMapper] Auto-generated ID for {gameObject.name}: {metadataId}");
        }
        
        // S'assurer qu'il y a un collider
        if (GetComponent<Collider>() == null)
        {
            // Ajouter un collider basé sur le type d'objet
            if (gameObject.name.ToLower().Contains("cube"))
            {
                gameObject.AddComponent<BoxCollider>();
            }
            else if (gameObject.name.ToLower().Contains("sphere"))
            {
                gameObject.AddComponent<SphereCollider>();
            }
            else
            {
                // Par défaut, box collider
                gameObject.AddComponent<BoxCollider>();
            }
            
            Debug.Log($"[ObjectMetadataMapper] Added collider to {gameObject.name}");
        }
    }
    
    string GenerateIdFromName()
    {
        // Convertir le nom du GameObject en ID metadata
        // "Red Cube" -> "red_cube"
        // "Blue Sphere" -> "blue_sphere"
        
        string id = gameObject.name.ToLower();
        id = id.Replace(" ", "_");
        id = id.Replace("-", "_");
        
        return id;
    }
    
    void OnMouseEnter()
    {
        // Feedback visuel au survol
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = renderer.material.color * 1.2f;
        }
    }
    
    void OnMouseExit()
    {
        // Retirer le feedback visuel
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = renderer.material.color / 1.2f;
        }
    }
    
    void OnDrawGizmos()
    {
        if (showNameLabel)
        {
            // Afficher le nom et l'ID au-dessus de l'objet dans l'éditeur
            Vector3 position = transform.position + Vector3.up * 2;
            
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(position, 
                $"{gameObject.name}\n[{MetadataId}]", 
                new GUIStyle()
                {
                    normal = new GUIStyleState() { textColor = labelColor },
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold
                });
            #endif
        }
    }
    
    [ContextMenu("Show Metadata ID")]
    public void ShowMetadataId()
    {
        Debug.Log($"[ObjectMetadataMapper] {gameObject.name} -> Metadata ID: {MetadataId}");
    }
}