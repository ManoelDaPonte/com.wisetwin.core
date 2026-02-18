using UnityEngine;
using WiseTwin.UI;

namespace WiseTwin
{
    /// <summary>
    /// Composant temporaire ajouté aux objets zone pendant une procédure.
    /// Quand le joueur (CharacterController) entre dans le trigger, l'étape est validée.
    /// </summary>
    public class ProcedureZoneTrigger : MonoBehaviour
    {
        private ProcedureDisplayer procedureDisplayer;
        private int stepIndex;
        private bool isActive = false;

        public void Initialize(ProcedureDisplayer displayer, int index)
        {
            procedureDisplayer = displayer;
            stepIndex = index;
            isActive = true;

            Debug.Log($"[ProcedureZoneTrigger] Initialized on {gameObject.name} for step {index + 1}");
        }

        void OnTriggerEnter(Collider other)
        {
            if (!isActive || procedureDisplayer == null) return;

            // Check if the entering object is the player (CharacterController)
            if (other.GetComponent<CharacterController>() != null)
            {
                Debug.Log($"[ProcedureZoneTrigger] Player entered zone {gameObject.name} - validating step {stepIndex + 1}");

                // Deactivate immediately to prevent double-trigger
                isActive = false;

                // Validate the zone step
                procedureDisplayer.ValidateZoneStep();
            }
        }

        void OnDisable()
        {
            isActive = false;
        }
    }
}
