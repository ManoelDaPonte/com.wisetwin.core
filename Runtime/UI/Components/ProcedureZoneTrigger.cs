using UnityEngine;
using UnityEngine.AI;
using WiseTwin.UI;

namespace WiseTwin
{
    /// <summary>
    /// Composant temporaire ajoute aux objets zone pendant une procedure.
    /// Quand le joueur entre dans le trigger, l'etape est validee.
    /// Supporte les deux modes: CharacterController (clavier) et NavMeshAgent (souris).
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
            TryValidate(other);
        }

        // NavMeshAgent moves the transform directly without a Rigidbody,
        // so OnTriggerEnter may not fire. OnTriggerStay catches the agent
        // if it was already inside the zone or entered between physics ticks.
        void OnTriggerStay(Collider other)
        {
            TryValidate(other);
        }

        void TryValidate(Collider other)
        {
            if (!isActive || procedureDisplayer == null) return;

            // Accept player with CharacterController (keyboard mode)
            // or NavMeshAgent (mouse-only mode)
            bool isPlayer = other.GetComponent<CharacterController>() != null
                         || other.GetComponent<NavMeshAgent>() != null;

            if (!isPlayer) return;

            Debug.Log($"[ProcedureZoneTrigger] Player entered zone {gameObject.name} - validating step {stepIndex + 1}");

            // Deactivate immediately to prevent double-trigger
            isActive = false;

            procedureDisplayer.ValidateZoneStep();
            ZoneCollectEffect.Play(gameObject);
        }

        void OnDisable()
        {
            isActive = false;
        }
    }
}
