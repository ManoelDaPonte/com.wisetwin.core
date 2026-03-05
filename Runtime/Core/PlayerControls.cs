using UnityEngine;

namespace WiseTwin
{
    /// <summary>
    /// Centralized utility to enable/disable player controls.
    /// Works with both FirstPersonCharacter and ClickToMoveCharacter,
    /// regardless of which control mode is active.
    /// All UI components should use this instead of finding controllers directly.
    /// </summary>
    public static class PlayerControls
    {
        /// <summary>
        /// Enable or disable all player controls.
        /// Finds and updates whichever controller is currently active.
        /// </summary>
        public static void SetEnabled(bool enabled)
        {
            var fpc = Object.FindFirstObjectByType<FirstPersonCharacter>();
            if (fpc != null)
                fpc.SetControlsEnabled(enabled);

            var ctm = Object.FindFirstObjectByType<ClickToMoveCharacter>();
            if (ctm != null)
                ctm.SetControlsEnabled(enabled);
        }
    }
}
