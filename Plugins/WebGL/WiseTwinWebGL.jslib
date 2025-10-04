mergeInto(LibraryManager.library, {
    // === VERSION SIMPLIFIÉE - Une seule méthode de communication ===

    SendTrainingCompleted: function(jsonPtr) {
        var jsonData = UTF8ToString(jsonPtr);

        try {
            // Utiliser uniquement la méthode officielle react-unity-webgl
            if (typeof window.dispatchReactUnityEvent === 'function') {
                // Envoyer l'événement avec toutes les données
                window.dispatchReactUnityEvent("TrainingCompleted", jsonData);
                console.log('[WiseTwin] Training completion sent successfully');
            } else {
                console.error('[WiseTwin] dispatchReactUnityEvent not available - ensure react-unity-webgl is properly initialized');
                console.log('[WiseTwin] Training data that would have been sent:', JSON.parse(jsonData));
            }
        } catch (error) {
            console.error('[WiseTwin] Failed to send training completion:', error);
            console.log('[WiseTwin] Raw JSON:', jsonData);
        }
    }
});