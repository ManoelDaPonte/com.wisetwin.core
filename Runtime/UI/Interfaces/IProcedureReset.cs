using UnityEngine;

namespace WiseTwin.UI
{
    /// <summary>
    /// Interface pour les scripts de reset personnalisés des procédures.
    /// Permet à l'utilisateur de définir comment réinitialiser les objets après une procédure.
    /// </summary>
    public interface IProcedureReset
    {
        /// <summary>
        /// Méthode appelée pour réinitialiser tous les objets touchés pendant la procédure.
        /// </summary>
        /// <param name="procedureObjects">Liste des GameObjects utilisés dans la procédure</param>
        void ResetProcedure(GameObject[] procedureObjects);
    }
}