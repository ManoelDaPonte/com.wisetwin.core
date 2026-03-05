using UnityEngine;
using UnityEngine.AI;

namespace WiseTwin
{
    public enum ControlMode
    {
        KeyboardMouse = 0,
        MouseOnly = 1
    }

    /// <summary>
    /// Stores the user's control mode preference (keyboard+mouse vs mouse-only).
    /// Set during tutorial, persisted via PlayerPrefs.
    /// </summary>
    public static class ControlModeSettings
    {
        private const string PrefsKey = "WiseTwin_ControlMode";

        public static ControlMode CurrentMode { get; private set; } = ControlMode.KeyboardMouse;

        public static void SetMode(ControlMode mode)
        {
            CurrentMode = mode;
            PlayerPrefs.SetInt(PrefsKey, (int)mode);
        }

        public static void LoadFromPrefs()
        {
            CurrentMode = (ControlMode)PlayerPrefs.GetInt(PrefsKey, 0);
        }

        /// <summary>
        /// Reset to default state (called on restart before scene reload).
        /// </summary>
        public static void Reset()
        {
            CurrentMode = ControlMode.KeyboardMouse;
        }

        /// <summary>
        /// Apply the current control mode to the player in the scene.
        /// Enables the correct controller and disables the other.
        /// </summary>
        public static void ApplyToPlayer()
        {
            var fpc = Object.FindFirstObjectByType<FirstPersonCharacter>();
            if (fpc == null)
            {
                Debug.LogWarning("[ControlModeSettings] No FirstPersonCharacter found in scene");
                return;
            }

            var go = fpc.gameObject;

            if (CurrentMode == ControlMode.MouseOnly)
            {
                // FPC stays enabled but in camera-only mode (orbit, zoom, collision)
                fpc.enabled = true;
                fpc.cameraOnly = true;

                // Disable CharacterController (NavMeshAgent handles movement)
                var cc = go.GetComponent<CharacterController>();
                if (cc) cc.enabled = false;

                // CharacterController acts as a collider for trigger detection.
                // With it disabled, add a CapsuleCollider so OnTriggerEnter/Stay
                // still fires for zone validation in procedures.
                var capsule = go.GetComponent<CapsuleCollider>();
                if (capsule == null)
                {
                    capsule = go.AddComponent<CapsuleCollider>();
                    if (cc)
                    {
                        capsule.center = cc.center;
                        capsule.radius = cc.radius;
                        capsule.height = cc.height;
                    }
                    else
                    {
                        capsule.center = new Vector3(0f, 1f, 0f);
                        capsule.radius = 0.3f;
                        capsule.height = 2f;
                    }
                }
                capsule.enabled = true;

                // Add/enable kinematic Rigidbody (required for OnTriggerEnter/Stay
                // since CharacterController is disabled and can't act as one)
                var rb = go.GetComponent<Rigidbody>();
                if (rb == null)
                {
                    rb = go.AddComponent<Rigidbody>();
                }
                rb.isKinematic = true;
                rb.useGravity = false;

                // Add/enable NavMeshAgent
                var agent = go.GetComponent<NavMeshAgent>();
                if (agent == null)
                {
                    agent = go.AddComponent<NavMeshAgent>();
                    agent.speed = fpc.moveSpeed;
                    agent.angularSpeed = 0f;
                    agent.acceleration = 8f;
                    agent.stoppingDistance = 0.3f;
                    agent.autoBraking = true;
                }
                agent.enabled = true;

                // Warp agent to nearest NavMesh position
                if (NavMesh.SamplePosition(go.transform.position, out NavMeshHit navHit, 5f, NavMesh.AllAreas))
                {
                    agent.Warp(navHit.position);
                    Debug.Log($"[ControlModeSettings] NavMeshAgent warped to {navHit.position}");
                }
                else
                {
                    Debug.LogWarning("[ControlModeSettings] No NavMesh found near player! Ensure NavMesh is baked.");
                }

                // Add/enable ClickToMoveCharacter (handles click-to-move + rotation + animation only)
                var ctm = go.GetComponent<ClickToMoveCharacter>();
                if (ctm == null)
                {
                    ctm = go.AddComponent<ClickToMoveCharacter>();
                    ctm.debugMode = true;
                }
                ctm.enabled = true;

                Debug.Log("[ControlModeSettings] Mouse-only mode applied (FPC camera + NavMeshAgent movement)");
            }
            else
            {
                // Full keyboard+mouse mode
                fpc.enabled = true;
                fpc.cameraOnly = false;

                var cc = go.GetComponent<CharacterController>();
                if (cc) cc.enabled = true;

                // Disable click-to-move if present
                var ctm = go.GetComponent<ClickToMoveCharacter>();
                if (ctm) ctm.enabled = false;

                var agent = go.GetComponent<NavMeshAgent>();
                if (agent) agent.enabled = false;

                var capsule = go.GetComponent<CapsuleCollider>();
                if (capsule) capsule.enabled = false;

                var rb = go.GetComponent<Rigidbody>();
                if (rb) Object.Destroy(rb);

                Debug.Log("[ControlModeSettings] Keyboard+mouse mode applied");
            }
        }
    }
}
