namespace WiseTwin
{
    /// <summary>
    /// Définit comment les objets non actifs sont affichés dans une progression guidée
    /// </summary>
    public enum ProgressionVisibilityMode
    {
        /// <summary>
        /// L'objet reste complètement visible mais non interactable
        /// </summary>
        Visible,

        /// <summary>
        /// L'objet devient semi-transparent (grayed out)
        /// </summary>
        Transparent,

        /// <summary>
        /// L'objet devient complètement invisible
        /// </summary>
        Hidden,

        /// <summary>
        /// Le GameObject est désactivé dans la hiérarchie Unity
        /// </summary>
        Disabled
    }
}
