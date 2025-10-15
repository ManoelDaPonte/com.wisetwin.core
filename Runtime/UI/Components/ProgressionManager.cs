using UnityEngine;
using System.Collections.Generic;
using System;
using WiseTwin.UI;

namespace WiseTwin
{
    /// <summary>
    /// Méthode de détection des InteractableObjects
    /// </summary>
    public enum DetectionMethod
    {
        /// <summary>
        /// Recherche dans toute la scène
        /// </summary>
        EntireScene,

        /// <summary>
        /// Recherche uniquement parmi les enfants d'un GameObject parent
        /// </summary>
        ChildrenOnly
    }

    /// <summary>
    /// Gestionnaire de progression guidée pour les objets interactables
    /// Affiche les objets un par un dans un ordre défini, et active le suivant après complétion
    /// </summary>
    public class ProgressionManager : MonoBehaviour
    {
        [Header("Auto-Detection")]
        [Tooltip("Détecter automatiquement tous les InteractableObjects dans la scène au démarrage")]
        [SerializeField] private bool autoDetectOnStart = true;

        [Tooltip("Méthode de détection des objets")]
        [SerializeField] private DetectionMethod detectionMethod = DetectionMethod.EntireScene;

        [Tooltip("Parent GameObject pour la recherche (si detectionMethod = ChildrenOnly)")]
        [SerializeField] private Transform searchParent;

        [Header("Configuration")]
        [Tooltip("Liste ordonnée des objets interactables à afficher progressivement (remplie automatiquement si autoDetect activé)")]
        [SerializeField] private List<InteractableObject> progressionSequence = new List<InteractableObject>();

        [Tooltip("Mode de visibilité pour les objets pas encore actifs")]
        [SerializeField] private ProgressionVisibilityMode visibilityMode = ProgressionVisibilityMode.Transparent;

        [Tooltip("Afficher les objets déjà complétés normalement ou les cacher")]
        [SerializeField] private bool showCompletedObjects = true;

        [Tooltip("Cacher aussi les objets complétés (un seul objet visible à la fois)")]
        [SerializeField] private bool hideCompletedObjects = false;

        [Tooltip("Permettre de revenir en arrière dans la progression")]
        [SerializeField] private bool allowBackTracking = false;

        [Tooltip("Réinitialiser la progression au démarrage")]
        [SerializeField] private bool resetOnStart = true;

        [Header("Options de complétion")]
        [Tooltip("Requis que tous les objets soient complétés avec succès")]
        [SerializeField] private bool requireSuccessForAll = true;

        [Tooltip("Nombre d'essais autorisés par objet (0 = illimité)")]
        [SerializeField] private int maxAttemptsPerObject = 0;

        [Header("Debug")]
        [SerializeField] private bool debugMode = false;

        // État de la progression
        private int currentStepIndex = 0;
        private HashSet<string> completedObjectIds = new HashSet<string>();
        private Dictionary<string, int> attemptCounts = new Dictionary<string, int>();
        private bool isProgressionActive = false;

        // Cache pour mapper objectId -> InteractableObject
        private Dictionary<string, InteractableObject> objectIdToInteractable = new Dictionary<string, InteractableObject>();

        // Événements
        public event Action<int, InteractableObject> OnStepActivated;
        public event Action<int, InteractableObject, bool> OnStepCompleted; // stepIndex, object, success
        public event Action OnProgressionCompleted;
        public event Action<InteractableObject, int> OnMaxAttemptsReached; // object, attemptCount

        // Propriétés publiques
        public int CurrentStepIndex => currentStepIndex;
        public int TotalSteps => progressionSequence.Count;
        public bool IsProgressionActive => isProgressionActive;
        public float ProgressPercentage => TotalSteps > 0 ? (float)currentStepIndex / TotalSteps * 100f : 0f;
        public List<InteractableObject> ProgressionSequence => new List<InteractableObject>(progressionSequence);

        void Start()
        {
            // Détection automatique des InteractableObjects si activé
            if (autoDetectOnStart)
            {
                AutoDetectInteractableObjects();
            }

            // S'assurer de l'abonnement aux événements (au cas où OnEnable était trop tôt)
            if (ContentDisplayManager.Instance != null)
            {
                // Se désabonner d'abord pour éviter les doublons
                ContentDisplayManager.Instance.OnContentCompleted -= HandleContentCompleted;
                // Puis se réabonner
                ContentDisplayManager.Instance.OnContentCompleted += HandleContentCompleted;
                if (debugMode) Debug.Log("[ProgressionManager] ✅ Re-subscribed to ContentDisplayManager.OnContentCompleted in Start()");
            }
            else
            {
                if (debugMode) Debug.LogError("[ProgressionManager] ❌ ContentDisplayManager.Instance is NULL in Start()!");
            }

            if (resetOnStart)
            {
                StartProgression();
            }
        }

        void OnEnable()
        {
            // S'abonner aux événements du ContentDisplayManager
            if (ContentDisplayManager.Instance != null)
            {
                ContentDisplayManager.Instance.OnContentCompleted += HandleContentCompleted;
                if (debugMode) Debug.Log("[ProgressionManager] Subscribed to ContentDisplayManager.OnContentCompleted");
            }
            else if (debugMode)
            {
                Debug.LogWarning("[ProgressionManager] ContentDisplayManager.Instance is null, cannot subscribe to events");
            }
        }

        void OnDisable()
        {
            // Se désabonner des événements
            if (ContentDisplayManager.Instance != null)
            {
                ContentDisplayManager.Instance.OnContentCompleted -= HandleContentCompleted;
            }
        }

        /// <summary>
        /// Démarre ou redémarre la progression guidée
        /// </summary>
        public void StartProgression()
        {
            if (progressionSequence.Count == 0)
            {
                Debug.LogWarning("[ProgressionManager] No objects in progression sequence!");
                return;
            }

            // Réinitialiser l'état
            currentStepIndex = 0;
            completedObjectIds.Clear();
            attemptCounts.Clear();
            objectIdToInteractable.Clear();

            // Construire le cache objectId -> InteractableObject
            foreach (var interactable in progressionSequence)
            {
                if (interactable != null)
                {
                    var mapper = interactable.GetComponent<ObjectMetadataMapper>();
                    if (mapper != null)
                    {
                        objectIdToInteractable[mapper.MetadataId] = interactable;
                    }
                    else
                    {
                        Debug.LogWarning($"[ProgressionManager] {interactable.name} has no ObjectMetadataMapper!");
                    }
                }
            }

            isProgressionActive = true;

            // Configurer la visibilité de tous les objets
            UpdateAllObjectsVisibility();

            if (debugMode) Debug.Log($"[ProgressionManager] Progression started with {progressionSequence.Count} steps");
        }

        /// <summary>
        /// Arrête la progression et restaure tous les objets à leur état normal
        /// </summary>
        public void StopProgression()
        {
            isProgressionActive = false;

            // Restaurer tous les objets
            foreach (var interactable in progressionSequence)
            {
                if (interactable != null)
                {
                    interactable.ExitProgressionMode();
                }
            }

            if (debugMode) Debug.Log("[ProgressionManager] Progression stopped");
        }

        /// <summary>
        /// Passe à l'étape suivante de la progression
        /// </summary>
        public void MoveToNextStep()
        {
            if (!isProgressionActive) return;

            if (currentStepIndex < progressionSequence.Count - 1)
            {
                currentStepIndex++;
                UpdateAllObjectsVisibility();

                var currentObject = progressionSequence[currentStepIndex];
                OnStepActivated?.Invoke(currentStepIndex, currentObject);

                if (debugMode) Debug.Log($"[ProgressionManager] Moved to step {currentStepIndex + 1}/{progressionSequence.Count}: {currentObject.name}");
            }
            else
            {
                // Progression complète
                CompleteProgression();
            }
        }

        /// <summary>
        /// Revient à l'étape précédente (si allowBackTracking est activé)
        /// </summary>
        public void MoveToPreviousStep()
        {
            if (!isProgressionActive || !allowBackTracking) return;

            if (currentStepIndex > 0)
            {
                currentStepIndex--;
                UpdateAllObjectsVisibility();

                var currentObject = progressionSequence[currentStepIndex];
                OnStepActivated?.Invoke(currentStepIndex, currentObject);

                if (debugMode) Debug.Log($"[ProgressionManager] Moved back to step {currentStepIndex + 1}/{progressionSequence.Count}: {currentObject.name}");
            }
        }

        /// <summary>
        /// Saute directement à une étape spécifique
        /// </summary>
        public void JumpToStep(int stepIndex)
        {
            if (!isProgressionActive) return;

            if (stepIndex >= 0 && stepIndex < progressionSequence.Count)
            {
                currentStepIndex = stepIndex;
                UpdateAllObjectsVisibility();

                var currentObject = progressionSequence[currentStepIndex];
                OnStepActivated?.Invoke(currentStepIndex, currentObject);

                if (debugMode) Debug.Log($"[ProgressionManager] Jumped to step {currentStepIndex + 1}/{progressionSequence.Count}: {currentObject.name}");
            }
        }

        /// <summary>
        /// Met à jour la visibilité de tous les objets selon leur position dans la progression
        /// </summary>
        private void UpdateAllObjectsVisibility()
        {
            for (int i = 0; i < progressionSequence.Count; i++)
            {
                var interactable = progressionSequence[i];
                if (interactable == null) continue;

                var mapper = interactable.GetComponent<ObjectMetadataMapper>();
                if (mapper == null) continue;

                string objectId = mapper.MetadataId;
                bool isCompleted = completedObjectIds.Contains(objectId);

                if (i < currentStepIndex)
                {
                    // Objet déjà passé
                    if (hideCompletedObjects)
                    {
                        // Cacher les objets complétés (un seul objet visible à la fois)
                        interactable.SetProgressionState(false, visibilityMode);
                    }
                    else if (showCompletedObjects && isCompleted)
                    {
                        // Garder les objets complétés visibles
                        interactable.ExitProgressionMode();
                    }
                    else
                    {
                        interactable.SetProgressionState(false, visibilityMode);
                    }
                }
                else if (i == currentStepIndex)
                {
                    // Objet actuel - le rendre interactable
                    interactable.SetProgressionState(true, visibilityMode);
                }
                else
                {
                    // Objet futur - le cacher selon le mode
                    interactable.SetProgressionState(false, visibilityMode);
                }
            }
        }

        /// <summary>
        /// Gère l'événement de complétion d'un contenu
        /// </summary>
        private void HandleContentCompleted(string objectId, bool success)
        {
            if (debugMode) Debug.Log($"[ProgressionManager] 🎯 HandleContentCompleted called: objectId={objectId}, success={success}");

            if (!isProgressionActive)
            {
                if (debugMode) Debug.Log("[ProgressionManager] Progression not active, ignoring completion event");
                return;
            }

            // Vérifier que c'est bien l'objet actuel
            if (currentStepIndex >= progressionSequence.Count) return;

            var currentObject = progressionSequence[currentStepIndex];
            var mapper = currentObject.GetComponent<ObjectMetadataMapper>();

            if (mapper == null || mapper.MetadataId != objectId)
            {
                // Ce n'est pas l'objet actuel, ignorer
                if (debugMode) Debug.Log($"[ProgressionManager] Content completed for {objectId}, but current object is {mapper?.MetadataId}");
                return;
            }

            // Incrémenter le compteur d'essais
            if (!attemptCounts.ContainsKey(objectId))
            {
                attemptCounts[objectId] = 0;
            }
            attemptCounts[objectId]++;

            if (debugMode) Debug.Log($"[ProgressionManager] Object {objectId} completed with success={success}, attempt {attemptCounts[objectId]}");

            // Vérifier le nombre d'essais max
            if (maxAttemptsPerObject > 0 && attemptCounts[objectId] >= maxAttemptsPerObject && !success)
            {
                OnMaxAttemptsReached?.Invoke(currentObject, attemptCounts[objectId]);

                if (debugMode) Debug.LogWarning($"[ProgressionManager] Max attempts reached for {objectId}");

                // Décider si on passe quand même à la suivante ou non
                if (!requireSuccessForAll)
                {
                    // Marquer comme "complété" même en échec
                    completedObjectIds.Add(objectId);
                    OnStepCompleted?.Invoke(currentStepIndex, currentObject, false);
                    MoveToNextStep();
                }
                return;
            }

            // Si succès, passer à l'étape suivante
            if (success)
            {
                completedObjectIds.Add(objectId);
                OnStepCompleted?.Invoke(currentStepIndex, currentObject, true);
                MoveToNextStep();
            }
            else
            {
                // Échec - permettre de réessayer
                OnStepCompleted?.Invoke(currentStepIndex, currentObject, false);

                if (!requireSuccessForAll)
                {
                    // Si on n'exige pas le succès, passer quand même à la suivante
                    completedObjectIds.Add(objectId);
                    MoveToNextStep();
                }
            }
        }

        /// <summary>
        /// Complète la progression
        /// </summary>
        private void CompleteProgression()
        {
            isProgressionActive = false;

            // Restaurer tous les objets
            foreach (var interactable in progressionSequence)
            {
                if (interactable != null)
                {
                    interactable.ExitProgressionMode();
                }
            }

            OnProgressionCompleted?.Invoke();

            if (debugMode) Debug.Log("[ProgressionManager] Progression completed!");
        }

        /// <summary>
        /// Vérifie si un objet spécifique est complété
        /// </summary>
        public bool IsObjectCompleted(string objectId)
        {
            return completedObjectIds.Contains(objectId);
        }

        /// <summary>
        /// Obtient le nombre d'essais pour un objet
        /// </summary>
        public int GetAttemptCount(string objectId)
        {
            return attemptCounts.ContainsKey(objectId) ? attemptCounts[objectId] : 0;
        }

        /// <summary>
        /// Obtient l'objet interactable actuel
        /// </summary>
        public InteractableObject GetCurrentObject()
        {
            if (currentStepIndex >= 0 && currentStepIndex < progressionSequence.Count)
            {
                return progressionSequence[currentStepIndex];
            }
            return null;
        }

        /// <summary>
        /// Détecte automatiquement tous les InteractableObjects dans la scène
        /// et les ajoute à la progression sequence selon leur ordre dans la hiérarchie
        /// </summary>
        public void AutoDetectInteractableObjects()
        {
            progressionSequence.Clear();

            InteractableObject[] foundObjects;

            // Déterminer la méthode de recherche
            if (detectionMethod == DetectionMethod.ChildrenOnly && searchParent != null)
            {
                // Rechercher uniquement parmi les enfants du parent spécifié
                foundObjects = searchParent.GetComponentsInChildren<InteractableObject>(true);

                if (debugMode)
                {
                    Debug.Log($"[ProgressionManager] Searching in children of '{searchParent.name}'");
                }
            }
            else
            {
                // Rechercher dans toute la scène
                foundObjects = FindObjectsByType<InteractableObject>(FindObjectsSortMode.None);

                if (debugMode)
                {
                    Debug.Log($"[ProgressionManager] Searching in entire scene");
                }
            }

            // Créer une liste avec les objets et leur index dans la hiérarchie
            var objectsWithIndex = new List<(InteractableObject obj, int siblingIndex, string path)>();

            foreach (var obj in foundObjects)
            {
                // Ignorer cet objet lui-même s'il a aussi un InteractableObject
                if (obj.transform == transform) continue;

                // Calculer le "path" hiérarchique pour le tri
                string hierarchyPath = GetHierarchyPath(obj.transform);
                int siblingIndex = obj.transform.GetSiblingIndex();

                objectsWithIndex.Add((obj, siblingIndex, hierarchyPath));
            }

            // Trier par path hiérarchique (cela garantit l'ordre de haut en bas dans la hiérarchie)
            objectsWithIndex.Sort((a, b) => string.Compare(a.path, b.path, StringComparison.Ordinal));

            // Ajouter à la progression sequence
            foreach (var item in objectsWithIndex)
            {
                progressionSequence.Add(item.obj);
            }

            if (debugMode)
            {
                Debug.Log($"[ProgressionManager] Auto-detected {progressionSequence.Count} InteractableObjects:");
                for (int i = 0; i < progressionSequence.Count; i++)
                {
                    Debug.Log($"  [{i}] {progressionSequence[i].name} - {objectsWithIndex[i].path}");
                }
            }

            if (progressionSequence.Count == 0)
            {
                Debug.LogWarning("[ProgressionManager] No InteractableObjects found!");
            }
        }

        /// <summary>
        /// Obtient le chemin hiérarchique complet d'un Transform pour le tri
        /// </summary>
        private string GetHierarchyPath(Transform t)
        {
            string path = t.GetSiblingIndex().ToString("D4");
            Transform parent = t.parent;

            while (parent != null)
            {
                path = parent.GetSiblingIndex().ToString("D4") + "/" + path;
                parent = parent.parent;
            }

            return path;
        }

        #region Editor Helpers

        [ContextMenu("Auto-Detect InteractableObjects")]
        private void ContextMenu_AutoDetect()
        {
            AutoDetectInteractableObjects();
            Debug.Log($"[ProgressionManager] Auto-detection completed! Found {progressionSequence.Count} objects.");
        }

        [ContextMenu("Start Progression")]
        private void ContextMenu_StartProgression()
        {
            StartProgression();
        }

        [ContextMenu("Stop Progression")]
        private void ContextMenu_StopProgression()
        {
            StopProgression();
        }

        [ContextMenu("Next Step")]
        private void ContextMenu_NextStep()
        {
            MoveToNextStep();
        }

        [ContextMenu("Previous Step")]
        private void ContextMenu_PreviousStep()
        {
            MoveToPreviousStep();
        }

        #endregion
    }
}
