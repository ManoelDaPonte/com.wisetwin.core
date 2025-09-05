using UnityEngine;
using System.Runtime.InteropServices;

namespace WiseTwin
{
    public class TrainingCompletionNotifier : MonoBehaviour
{   
    [DllImport("__Internal")]
    private static extern void NotifyFormationCompleted();
    
    public void FormationCompleted(string trainingName = null)
    {
        #if UNITY_WEBGL && !UNITY_EDITOR
            // En mode WebGL (production), on appelle la fonction JavaScript
            NotifyFormationCompleted();
        #else
            // Dans l'éditeur Unity, on simule l'envoi avec un log
            Debug.Log("🧪 Mode Éditeur : Simulation de l'envoi à React.");
        #endif
    }
}}